using Commander500.Api.Data;
using Commander500.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Commander500.Api.Infrastructure.Scryfall;

/// <summary>
/// Background job: fills Scryfall data (art, card image, colors) for commanders that were saved by name only
/// (seed data, typed while offline). Runs shortly after startup and then every few hours.
/// Names Scryfall doesn't know are retried at most once a day. Disable with Scryfall__Enabled=false.
/// </summary>
public class ScryfallEnrichmentService(IServiceScopeFactory scopes, TimeProvider clock, ILogger<ScryfallEnrichmentService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            while (!stoppingToken.IsCancellationRequested)
            {
                await RunOnce(stoppingToken);
                await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
            }
        }
        catch (OperationCanceledException) { /* shutting down */ }
    }

    public async Task RunOnce(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var scryfall = scope.ServiceProvider.GetRequiredService<IScryfallClient>();
        var now = clock.GetUtcNow().UtcDateTime;
        var retryBefore = now.AddDays(-1);

        List<Commander> pending;
        try
        {
            pending = await db.Commanders
                .Where(c => c.ScryfallId == null && (c.ScryfallCheckedAt == null || c.ScryfallCheckedAt < retryBefore))
                .OrderBy(c => c.Id).Take(100).ToListAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Enriquecimento Scryfall: banco indisponível.");
            return;
        }

        var filled = 0;
        foreach (var commander in pending)
        {
            try
            {
                var card = await scryfall.GetByExactName(commander.Name, ct);
                commander.ScryfallCheckedAt = now;
                var key = card is null ? "" : CommanderName.Normalize(card.Name);
                if (card is not null && !await db.Commanders.AnyAsync(c => c.Id != commander.Id && (c.ScryfallId == card.Id || c.NormalizedName == key), ct))
                {
                    CommanderCards.Apply(commander, card);
                    filled++;
                }
                await db.SaveChangesAsync(ct);
            }
            catch (HttpRequestException ex)
            {
                logger.LogInformation("Scryfall indisponível ({Message}); tento de novo mais tarde.", ex.Message);
                return;
            }
        }
        if (pending.Count > 0) logger.LogInformation("Scryfall: {Filled}/{Total} comandantes atualizados.", filled, pending.Count);
    }
}

public static class CommanderCards
{
    /// <summary>Copies Scryfall data onto a commander. The display name becomes Scryfall's official name.</summary>
    public static void Apply(Commander commander, ScryfallCard card)
    {
        commander.ScryfallId = card.Id;
        commander.Name = card.Name;
        commander.NormalizedName = CommanderName.Normalize(card.Name);
        commander.ArtUrl = card.ArtUrl;
        commander.ImageUrl = card.ImageUrl;
        commander.ColorIdentity = card.ColorIdentity;
        commander.ScryfallCheckedAt = DateTime.UtcNow;
    }
}
