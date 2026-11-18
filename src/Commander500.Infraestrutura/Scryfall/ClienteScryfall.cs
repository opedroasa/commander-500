using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Commander500.Aplicacao.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Commander500.Infraestrutura.Scryfall;

/// <summary>
/// Cliente da API pública do Scryfall (https://scryfall.com/docs/api): gratuita e sem chave.
/// Regras deles: mandar User-Agent + Accept e no máximo ~10 requisições por segundo. Aqui as chamadas têm 100 ms
/// de intervalo e os resultados ficam em cache. As imagens são carregadas direto de cards.scryfall.io, como o Scryfall permite.
/// </summary>
public class ClienteScryfall(HttpClient http, IMemoryCache cache, ILogger<ClienteScryfall> log) : IClienteScryfall
{
    public const string EnderecoBase = "https://api.scryfall.com/";
    public const string IdentificacaoAplicativo = "Commander500FantasyStore/1.0";

    private static readonly SemaphoreSlim Fila = new(1, 1);
    private static DateTime _ultimaChamada = DateTime.MinValue;
    private static readonly JsonSerializerOptions OpcoesJson = new() { PropertyNameCaseInsensitive = true };

    public Task<IReadOnlyList<CartaScryfall>> PesquisarComandantes(string busca, CancellationToken cancelamento = default)
    {
        var texto = busca.Replace("\"", "").Trim();
        if (texto.Length < 2) return Task.FromResult<IReadOnlyList<CartaScryfall>>([]);
        return cache.GetOrCreateAsync($"scryfall:busca:{texto.ToLowerInvariant()}", async item =>
        {
            item.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6);
            var consulta = $"name:\"{texto}\" (is:commander OR t:background) game:paper";
            var lista = await Obter<ListaCartas>($"cards/search?q={Uri.EscapeDataString(consulta)}&order=edhrec&unique=cards", cancelamento);
            return (IReadOnlyList<CartaScryfall>)(lista?.Data.Take(12).Select(Converter).ToList() ?? []);
        })!;
    }

    public Task<CartaScryfall?> ObterPorId(Guid id, CancellationToken cancelamento = default) =>
        cache.GetOrCreateAsync($"scryfall:id:{id}", async item =>
        {
            item.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1);
            var carta = await Obter<CartaJson>($"cards/{id}", cancelamento);
            return carta is null ? null : Converter(carta);
        });

    public async Task<CartaScryfall?> ObterPorNomeExato(string nome, CancellationToken cancelamento = default)
    {
        var carta = await Obter<CartaJson>($"cards/named?exact={Uri.EscapeDataString(nome.Trim())}", cancelamento);
        return carta is null ? null : Converter(carta);
    }

    /// <summary>GET respeitando o intervalo entre chamadas. Devolve null no 404; lança HttpRequestException nos outros erros.</summary>
    private async Task<T?> Obter<T>(string caminho, CancellationToken cancelamento) where T : class
    {
        await Fila.WaitAsync(cancelamento);
        try
        {
            var espera = _ultimaChamada.AddMilliseconds(100) - DateTime.UtcNow;
            if (espera > TimeSpan.Zero) await Task.Delay(espera, cancelamento);
            _ultimaChamada = DateTime.UtcNow;

            using var resposta = await http.GetAsync(caminho, cancelamento);
            if (resposta.StatusCode == HttpStatusCode.NotFound) return null;
            if (!resposta.IsSuccessStatusCode)
            {
                log.LogWarning("Scryfall {Caminho} respondeu {Status}", caminho, (int)resposta.StatusCode);
                throw new HttpRequestException($"Scryfall respondeu {(int)resposta.StatusCode}", null, resposta.StatusCode);
            }
            await using var conteudo = await resposta.Content.ReadAsStreamAsync(cancelamento);
            return await JsonSerializer.DeserializeAsync<T>(conteudo, OpcoesJson, cancelamento);
        }
        finally
        {
            Fila.Release();
        }
    }

    private static CartaScryfall Converter(CartaJson carta)
    {
        // Cartas dupla-face (ex.: "Kefka, Court Mage // Kefka, Ruler of Ruin") têm imagem por face.
        var imagens = carta.ImageUris ?? carta.CardFaces?.FirstOrDefault(f => f.ImageUris is not null)?.ImageUris;
        var identidadeCor = string.Concat("WUBRG".Where(cor => carta.ColorIdentity?.Contains(cor.ToString()) == true));
        return new CartaScryfall(carta.Id, carta.Name, imagens?.ArtCrop, imagens?.Normal, identidadeCor, carta.TypeLine ?? "");
    }

    // Formato do JSON do Scryfall (nomes em inglês porque vêm da API deles).
    private sealed class ListaCartas
    {
        public List<CartaJson> Data { get; set; } = [];
    }

    private sealed class CartaJson
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        [JsonPropertyName("type_line")] public string? TypeLine { get; set; }
        [JsonPropertyName("color_identity")] public List<string>? ColorIdentity { get; set; }
        [JsonPropertyName("image_uris")] public ImagensJson? ImageUris { get; set; }
        [JsonPropertyName("card_faces")] public List<FaceJson>? CardFaces { get; set; }
    }

    private sealed class FaceJson
    {
        [JsonPropertyName("image_uris")] public ImagensJson? ImageUris { get; set; }
    }

    private sealed class ImagensJson
    {
        [JsonPropertyName("art_crop")] public string? ArtCrop { get; set; }
        public string? Normal { get; set; }
    }
}
