using Commander500.Api.Contracts;
using Commander500.Api.Data;
using Commander500.Api.Domain;
using Commander500.Api.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace Commander500.Api.Application;

/// <summary>Read-only statistics: commanders, player profile and stages (etapas). Everything is derived from closed rounds.</summary>
public class StatsService(AppDbContext db, TimeProvider clock)
{
    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    /// <summary>Closed rounds with entries, decks/commanders and tables, optionally limited to one season.</summary>
    private Task<List<Round>> ClosedRounds(int? seasonId = null) =>
        db.Rounds.AsNoTracking()
            .Where(r => r.Status == RoundStatus.Closed && (seasonId == null || r.SeasonId == seasonId))
            .Include(r => r.Entries).ThenInclude(e => e.Deck).ThenInclude(d => d!.Commanders).ThenInclude(dc => dc.Commander)
            .Include(r => r.Tables).ThenInclude(t => t.Seats)
            .AsSplitQuery()
            .ToListAsync();

    private Task<Dictionary<int, string>> PlayerNames() =>
        db.Players.AsNoTracking().ToDictionaryAsync(p => p.Id, p => p.Nickname == null ? p.Name : $"{p.Name} ({p.Nickname})");

    // ------------------------------------------------------------------ Commanders

    /// <summary>
    /// Commander ranking. Without <paramref name="seasonId"/>: every season, plus commanders that only have
    /// registered decks. With it: only that season's matches (deck counts are still the global registrations).
    /// </summary>
    public async Task<IReadOnlyList<CommanderStatsDto>> Commanders(int? seasonId)
    {
        var minMatches = 3;
        if (seasonId is { } id)
        {
            var season = await db.Seasons.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id)
                         ?? throw new NotFoundException($"Temporada {id} não encontrada.");
            minMatches = season.MinMatchesForCommanderWinRate;
        }

        var rounds = await ClosedRounds(seasonId);
        var decks = await db.Decks.AsNoTracking().Include(d => d.Commanders).ThenInclude(dc => dc.Commander).ToListAsync();
        var rows = CommanderStatsCalculator.Calculate(rounds, minMatches, decks, includeUnplayed: seasonId is null);

        var commanders = await db.Commanders.AsNoTracking().ToDictionaryAsync(c => c.Id);
        return rows.Select(r => r.ToDto(commanders)).ToList();
    }

    // ------------------------------------------------------------------ Player profile

    public async Task<PlayerProfileDto> PlayerProfile(int playerId, int? seasonId, bool isAdmin)
    {
        var player = await db.Players.AsNoTracking()
            .Include(p => p.Decks).ThenInclude(d => d.Commanders).ThenInclude(dc => dc.Commander)
            .FirstOrDefaultAsync(p => p.Id == playerId)
            ?? throw new NotFoundException($"Jogador {playerId} não encontrado.");

        var seasons = await db.Seasons.AsNoTracking().ToListAsync();
        var stages = await db.Stages.AsNoTracking().Include(s => s.Rounds).ToListAsync();
        var rounds = await ClosedRounds();
        var names = await PlayerNames();

        // Overall: same counters as the ranking, over every closed round (points are per season, so not shown here).
        var overallRow = RankingCalculator.Calculate(new Season(), rounds, names).FirstOrDefault(r => r.PlayerId == playerId);

        PlayerSeasonStatsDto? seasonStats = null;
        if (seasonId is { } sid && seasons.FirstOrDefault(s => s.Id == sid) is { } season)
        {
            var row = RankingCalculator.Calculate(season, rounds.Where(r => r.SeasonId == sid), names).FirstOrDefault(r => r.PlayerId == playerId);
            seasonStats = new PlayerSeasonStatsDto(season.Id, season.Name, row?.Position, row?.Points ?? 0, Stats(row));
        }

        // Season champion = 1st place of a season that has ended (inactive).
        var seasonsWon = seasons
            .Where(s => !s.IsActive)
            .Where(s => RankingCalculator.Calculate(s, rounds.Where(r => r.SeasonId == s.Id), names).FirstOrDefault()?.PlayerId == playerId)
            .OrderByDescending(s => s.StartDate)
            .Select(s => new SeasonRefDto(s.Id, s.Name))
            .ToList();

        // Stage champion = position 1 (ties share it) of a stage whose rounds are all closed.
        var roundsById = rounds.ToDictionary(r => r.Id);
        var stagesWon = stages
            .Where(IsComplete)
            .Where(st => StageRankingCalculator.Calculate(st.Rounds.Select(r => roundsById[r.Id]), names)
                .Any(r => r.Position == 1 && r.PlayerId == playerId))
            .OrderByDescending(st => st.Date)
            .Select(st => new StageRefDto(st.Id, st.SeasonId, seasons.First(s => s.Id == st.SeasonId).Name, st.Number, st.Date))
            .ToList();

        var seasonsPlayed = rounds.Where(r => r.Entries.Any(e => e.PlayerId == playerId)).Select(r => r.SeasonId).Distinct().Count();

        return new PlayerProfileDto(
            player.Id, player.Name, player.Nickname, player.IsActive,
            Stats(overallRow), seasonsPlayed, seasonStats, seasonsWon, stagesWon,
            player.Decks.OrderBy(d => d.Name).Select(d => d.ToDto(includeReviewNote: isAdmin)).ToList());
    }

    private static PlayerStatsDto Stats(RankingRow? r) => r is null
        ? new PlayerStatsDto(0, 0, 0, 0, 0, 0, null)
        : new PlayerStatsDto(r.Matches + r.Byes, r.Matches, r.Wins, r.Draws, r.Losses, r.Byes, r.WinRate);

    // ------------------------------------------------------------------ Stages (etapas)

    private static bool IsComplete(Stage s) => s.Rounds.Count > 0 && s.Rounds.All(r => r.Status == RoundStatus.Closed);

    public async Task<IReadOnlyList<StageDto>> Stages(int seasonId)
    {
        var season = await db.Seasons.AsNoTracking().FirstOrDefaultAsync(s => s.Id == seasonId)
                     ?? throw new NotFoundException($"Temporada {seasonId} não encontrada.");
        var stages = await db.Stages.AsNoTracking().Where(s => s.SeasonId == seasonId).Include(s => s.Rounds)
            .OrderByDescending(s => s.Date).ToListAsync();
        var rounds = (await ClosedRounds(seasonId)).ToDictionary(r => r.Id);
        var names = await PlayerNames();

        return stages.Select(st => ToDto(st, season, st.Rounds.Where(r => rounds.ContainsKey(r.Id)).Select(r => rounds[r.Id]), names)).ToList();
    }

    public async Task<StageDetailDto> Stage(int stageId)
    {
        var stage = await db.Stages.AsNoTracking().Include(s => s.Season).Include(s => s.Rounds)
                        .FirstOrDefaultAsync(s => s.Id == stageId)
                    ?? throw new NotFoundException($"Etapa {stageId} não encontrada.");
        var rounds = await RoundService.WithGraph(db.Rounds.AsNoTrackingWithIdentityResolution())
            .Where(r => r.StageId == stageId).OrderBy(r => r.Number).ToListAsync();
        var names = await PlayerNames();

        var ranking = StageRankingCalculator.Calculate(rounds, names);
        return new StageDetailDto(ToDto(stage, stage.Season!, rounds, names), ranking, rounds.Select(r => r.ToDto(Now)).ToList());
    }

    private static StageDto ToDto(Stage st, Season season, IEnumerable<Round> rounds, IReadOnlyDictionary<int, string> names)
    {
        var leaders = StageRankingCalculator.Calculate(rounds, names)
            .Where(r => r.Position == 1)
            .Select(r => new PlayerRefDto(r.PlayerId, r.PlayerName))
            .ToList();
        return new StageDto(st.Id, st.SeasonId, season.Name, st.Number, st.Date, st.Rounds.Count,
            st.Rounds.Count(r => r.Status == RoundStatus.Closed), IsComplete(st), leaders);
    }
}
