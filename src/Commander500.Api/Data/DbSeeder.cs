using Commander500.Api.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Commander500.Api.Data;

/// <summary>
/// Startup seed. The admin comes from config (Admin__Email / Admin__Password env vars) and is created
/// only if it does not exist yet. Demo data (1 season, 10 players with decks) only when Seed:DemoData=true
/// and the database has no seasons.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services, ILogger logger)
    {
        var db = services.GetRequiredService<AppDbContext>();
        var config = services.GetRequiredService<IConfiguration>();
        var hasher = services.GetRequiredService<IPasswordHasher<AdminUser>>();
        var now = services.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime;

        var email = config["Admin:Email"]?.Trim().ToLowerInvariant();
        var password = config["Admin:Password"];
        if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(password))
        {
            if (!await db.AdminUsers.AnyAsync(u => u.Email == email))
            {
                var admin = new AdminUser { Email = email };
                admin.PasswordHash = hasher.HashPassword(admin, password);
                db.AdminUsers.Add(admin);
                await db.SaveChangesAsync();
                logger.LogInformation("Admin {Email} criado.", email);
            }
        }
        else if (!await db.AdminUsers.AnyAsync())
        {
            logger.LogWarning("Nenhum admin cadastrado. Defina Admin__Email e Admin__Password.");
        }

        if (config.GetValue<bool>("Seed:DemoData") && !await db.Seasons.AnyAsync())
        {
            await SeedDemoAsync(db, now);
            logger.LogInformation("Dados de demonstração criados.");
        }
    }

    private static async Task SeedDemoAsync(AppDbContext db, DateTime now)
    {
        db.Seasons.Add(new Season
        {
            Name = "Temporada 1",
            StartDate = DateOnly.FromDateTime(now),
            IsActive = true,
        });

        var commanders = new Dictionary<string, Commander>();
        Commander Cmd(string name)
        {
            var key = CommanderName.Normalize(name);
            if (!commanders.TryGetValue(key, out var c))
                commanders[key] = c = new Commander { Name = name, NormalizedName = key };
            return c;
        }

        (string Player, string? Nick, (string Deck, string[] Cmds, DeckStatus Status)[] Decks)[] data =
        [
            ("Ana Souza", "Aninha", [("Elfos", ["Lathril, Blade of the Elves"], DeckStatus.Approved)]),
            ("Bruno Lima", null, [("Dragões", ["The Ur-Dragon"], DeckStatus.Approved), ("Zumbis", ["Wilhelt, the Rotcleaver"], DeckStatus.Pending)]),
            ("Carla Mendes", null, [("Tokens", ["Rhys the Redeemed"], DeckStatus.Approved)]),
            ("Diego Rocha", "Didi", [("Parceiros", ["Tymna the Weaver", "Thrasios, Triton Hero"], DeckStatus.Approved)]),
            ("Eduardo Alves", null, [("Artefatos", ["Urza, Lord High Artificer"], DeckStatus.Rejected)]),
            ("Fernanda Costa", null, [("Counters", ["Atraxa, Praetors' Voice"], DeckStatus.Approved)]),
            ("Gabriel Pereira", null, [("Voltron", ["Light-Paws, Emperor's Voice"], DeckStatus.Pending)]),
            ("Helena Martins", "Lena", [("Aristocratas", ["Teysa Karlov"], DeckStatus.Approved)]),
            ("Igor Santos", null, [("Background", ["Wilson, Refined Grizzly", "Raised by Giants"], DeckStatus.Approved)]),
            ("Julia Ferreira", null, [("Spellslinger", ["Kalamax, the Stormsire"], DeckStatus.Approved), ("Landfall", ["Omnath, Locus of Creation"], DeckStatus.Pending)]),
        ];

        foreach (var (playerName, nick, decks) in data)
        {
            var player = new Player { Name = playerName, Nickname = nick, CreatedAt = now };
            foreach (var (deckName, cmds, status) in decks)
            {
                var deck = new Deck
                {
                    Name = deckName,
                    Status = status,
                    ReviewNote = status == DeckStatus.Rejected ? "Acima do teto de preço." : null,
                    CreatedAt = now,
                };
                foreach (var c in cmds) deck.Commanders.Add(new DeckCommander { Deck = deck, Commander = Cmd(c) });
                player.Decks.Add(deck);
            }
            db.Players.Add(player);
        }

        await db.SaveChangesAsync();
    }
}
