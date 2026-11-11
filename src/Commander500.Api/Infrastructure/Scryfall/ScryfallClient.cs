using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;

namespace Commander500.Api.Infrastructure.Scryfall;

/// <summary>Card data we keep from Scryfall.</summary>
/// <param name="ColorIdentity">WUBRG-ordered letters, "" for colorless.</param>
public record ScryfallCard(Guid Id, string Name, string? ArtUrl, string? ImageUrl, string ColorIdentity, string TypeLine);

public interface IScryfallClient
{
    /// <summary>Commanders (and backgrounds) whose name contains <paramref name="query"/>, most played first (EDHREC order).</summary>
    Task<IReadOnlyList<ScryfallCard>> SearchCommanders(string query, CancellationToken ct = default);
    Task<ScryfallCard?> GetById(Guid id, CancellationToken ct = default);
    Task<ScryfallCard?> GetByExactName(string name, CancellationToken ct = default);
}

/// <summary>
/// Thin client for the public Scryfall API (https://scryfall.com/docs/api): free, no key.
/// Their rules: send a User-Agent + Accept header and keep ≤ 10 requests/second — we space calls 100 ms apart
/// and cache results. Images are hot-linked from cards.scryfall.io, as Scryfall allows.
/// </summary>
public class ScryfallClient(HttpClient http, IMemoryCache cache, ILogger<ScryfallClient> logger) : IScryfallClient
{
    public const string BaseAddress = "https://api.scryfall.com/";
    public const string UserAgent = "Commander500FantasyStore/1.0";

    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static DateTime _lastCall = DateTime.MinValue;
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    public Task<IReadOnlyList<ScryfallCard>> SearchCommanders(string query, CancellationToken ct = default)
    {
        var q = query.Replace("\"", "").Trim();
        if (q.Length < 2) return Task.FromResult<IReadOnlyList<ScryfallCard>>([]);
        return cache.GetOrCreateAsync($"scryfall:search:{q.ToLowerInvariant()}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6);
            var search = $"name:\"{q}\" (is:commander OR t:background) game:paper";
            var list = await Get<CardList>($"cards/search?q={Uri.EscapeDataString(search)}&order=edhrec&unique=cards", ct);
            return (IReadOnlyList<ScryfallCard>)(list?.Data.Take(12).Select(Map).ToList() ?? []);
        })!;
    }

    public Task<ScryfallCard?> GetById(Guid id, CancellationToken ct = default) =>
        cache.GetOrCreateAsync($"scryfall:id:{id}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1);
            var card = await Get<Card>($"cards/{id}", ct);
            return card is null ? null : Map(card);
        });

    public async Task<ScryfallCard?> GetByExactName(string name, CancellationToken ct = default)
    {
        var card = await Get<Card>($"cards/named?exact={Uri.EscapeDataString(name.Trim())}", ct);
        return card is null ? null : Map(card);
    }

    /// <summary>GET with throttling. Returns null on 404; throws <see cref="HttpRequestException"/> on other failures.</summary>
    private async Task<T?> Get<T>(string path, CancellationToken ct) where T : class
    {
        await Gate.WaitAsync(ct);
        try
        {
            var wait = _lastCall.AddMilliseconds(100) - DateTime.UtcNow;
            if (wait > TimeSpan.Zero) await Task.Delay(wait, ct);
            _lastCall = DateTime.UtcNow;

            using var res = await http.GetAsync(path, ct);
            if (res.StatusCode == HttpStatusCode.NotFound) return null;
            if (!res.IsSuccessStatusCode)
            {
                logger.LogWarning("Scryfall {Path} respondeu {Status}", path, (int)res.StatusCode);
                throw new HttpRequestException($"Scryfall respondeu {(int)res.StatusCode}", null, res.StatusCode);
            }
            await using var stream = await res.Content.ReadAsStreamAsync(ct);
            return await JsonSerializer.DeserializeAsync<T>(stream, Json, ct);
        }
        finally
        {
            Gate.Release();
        }
    }

    private static ScryfallCard Map(Card c)
    {
        // Double-faced cards (e.g. "Kefka, Court Mage // Kefka, Ruler of Ruin") have images per face.
        var images = c.ImageUris ?? c.CardFaces?.FirstOrDefault(f => f.ImageUris is not null)?.ImageUris;
        var identity = string.Concat("WUBRG".Where(color => c.ColorIdentity?.Contains(color.ToString()) == true));
        return new ScryfallCard(c.Id, c.Name, images?.ArtCrop, images?.Normal, identity, c.TypeLine ?? "");
    }

    private sealed class CardList
    {
        public List<Card> Data { get; set; } = [];
    }

    private sealed class Card
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        [JsonPropertyName("type_line")] public string? TypeLine { get; set; }
        [JsonPropertyName("color_identity")] public List<string>? ColorIdentity { get; set; }
        [JsonPropertyName("image_uris")] public ImageUris? ImageUris { get; set; }
        [JsonPropertyName("card_faces")] public List<Face>? CardFaces { get; set; }
    }

    private sealed class Face
    {
        [JsonPropertyName("image_uris")] public ImageUris? ImageUris { get; set; }
    }

    private sealed class ImageUris
    {
        [JsonPropertyName("art_crop")] public string? ArtCrop { get; set; }
        public string? Normal { get; set; }
    }
}
