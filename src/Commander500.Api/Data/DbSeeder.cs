using Commander500.Api.Domain;
using Commander500.Api.Domain.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Commander500.Api.Data;

/// <summary>
/// Startup seed. The admin comes from config (Admin__Email / Admin__Password env vars) and is created
/// only if it does not exist yet. Demo data (1 season, 10 players with decks, 3 closed rounds) only when Seed:DemoData=true
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
            var admin = await db.AdminUsers.FirstOrDefaultAsync(u => u.Email == email);
            if (admin is null)
            {
                admin = new AdminUser { Email = email };
                admin.PasswordHash = hasher.HashPassword(admin, password);
                db.AdminUsers.Add(admin);
                await db.SaveChangesAsync();
                logger.LogInformation("Admin {Email} criado.", email);
            }
            else if (hasher.VerifyHashedPassword(admin, admin.PasswordHash, password) == PasswordVerificationResult.Failed)
            {
                // The env var is the source of truth: changing Admin__Password and restarting rotates the password.
                admin.PasswordHash = hasher.HashPassword(admin, password);
                await db.SaveChangesAsync();
                logger.LogInformation("Senha do admin {Email} atualizada a partir da configuração.", email);
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
        var season = new Season
        {
            Name = "Temporada 1",
            StartDate = DateOnly.FromDateTime(now.AddDays(-21)),
            IsActive = true,
        };
        db.Seasons.Add(season);

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
        await SeedDemoRoundsAsync(db, season, now);
    }

    /// <summary>
    /// Three closed weekly rounds played through the real domain workflow (draw with bye priority,
    /// results, close), so the ranking, commander stats and history have data. Deterministic RNG.
    /// </summary>
    private static async Task SeedDemoRoundsAsync(AppDbContext db, Season season, DateTime now)
    {
        var rng = new SeededRandomSource(500);
        var players = await db.Players.Include(p => p.Decks).OrderBy(p => p.Id).ToListAsync();
        var rounds = new List<Round>();

        for (var number = 1; number <= 3; number++)
        {
            var date = season.StartDate.AddDays(7 * (number - 1));
            var startedAt = date.ToDateTime(new TimeOnly(22, 0), DateTimeKind.Utc); // 19h em Brasília

            // Round 2 has one absentee, so it gets 1 bye instead of 2.
            var present = number == 2 ? players.Where(p => p != players[^1]).ToList() : players;
            var round = new Round { Season = season, SeasonId = season.Id, Number = number, Date = date, DurationMinutes = season.DefaultRoundMinutes };
            RoundWorkflow.SetEntries(round, present.Select(p => new EntryInput(p.Id, p.Decks.OrderBy(d => d.Id).First().Id)).ToList());
            RoundWorkflow.Draw(round, RoundWorkflow.CountPreviousByes(rounds, excludingRoundId: -1), rng);
            RoundWorkflow.Start(round, startedAt);
            db.Rounds.Add(round);
            await db.SaveChangesAsync(); // assigns entry ids used by the results

            foreach (var table in round.Tables)
            {
                var pick = rng.Next(5); // 0..3 = winner's seat, 4 = draw
                var winner = pick < 4 ? table.Seats.OrderBy(s => s.Seat).ElementAt(pick).RoundEntry!.Id : (int?)null;
                RoundWorkflow.SetResult(round, table, winner is null ? TableResult.Draw : TableResult.Win, winner, startedAt.AddMinutes(55), "seed");
            }
            RoundWorkflow.Close(round, startedAt.AddMinutes(60));
            await db.SaveChangesAsync();
            rounds.Add(round);
        }
    }
}
