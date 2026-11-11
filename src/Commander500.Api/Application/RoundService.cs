using Commander500.Api.Contracts;
using Commander500.Api.Data;
using Commander500.Api.Domain;
using Commander500.Api.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace Commander500.Api.Application;

public class RoundService(AppDbContext db, TimeProvider clock, IRandomSource rng)
{
    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    /// <summary>Round with everything the round screens need. Has a cycle (Round → Stage → Rounds): read-only callers must use AsNoTrackingWithIdentityResolution.</summary>
    public static IQueryable<Round> WithGraph(IQueryable<Round> q) => q
        .Include(r => r.Season)
        .Include(r => r.Stage).ThenInclude(s => s!.Rounds)
        .Include(r => r.Entries).ThenInclude(e => e.Player)
        .Include(r => r.Entries).ThenInclude(e => e.Deck).ThenInclude(d => d!.Commanders).ThenInclude(dc => dc.Commander)
        .Include(r => r.Tables).ThenInclude(t => t.Seats)
        .AsSplitQuery();

    private async Task<Round> Load(int id) =>
        await WithGraph(db.Rounds).FirstOrDefaultAsync(r => r.Id == id)
        ?? throw new NotFoundException($"Rodada {id} não encontrada.");

    public async Task<RoundDto> Get(int id) => (await Load(id)).ToDto(Now);

    /// <summary>Drawn / in-progress rounds of every active season (several seasons may be active).</summary>
    public async Task<IReadOnlyList<RoundDto>> Current()
    {
        var rounds = await WithGraph(db.Rounds.AsNoTrackingWithIdentityResolution())
            .Where(r => r.Season!.IsActive && (r.Status == RoundStatus.Drawn || r.Status == RoundStatus.InProgress))
            .OrderBy(r => r.Season!.Name).ThenBy(r => r.Number)
            .ToListAsync();
        return rounds.Select(r => r.ToDto(Now)).ToList();
    }

    public async Task<RoundDto> Create(int seasonId, CreateRoundRequest req)
    {
        var season = await db.Seasons.FindAsync(seasonId) ?? throw new NotFoundException($"Temporada {seasonId} não encontrada.");
        if (!season.IsActive) throw new DomainException("A temporada não está ativa.");
        var duration = req.DurationMinutes ?? season.DefaultRoundMinutes;
        if (duration is < 1 or > RoundWorkflow.MaxDurationMinutes)
            throw new ValidationException($"Duração deve estar entre 1 e {RoundWorkflow.MaxDurationMinutes} minutos.");

        var date = req.Date ?? DateOnly.FromDateTime(Now);
        var stages = await db.Stages.Where(s => s.SeasonId == seasonId).ToListAsync();
        var stage = StageRules.Resolve(season, stages, date);
        if (stage.Id == 0) db.Stages.Add(stage);

        var lastNumber = await db.Rounds.Where(r => r.SeasonId == seasonId).MaxAsync(r => (int?)r.Number) ?? 0;
        var round = new Round
        {
            SeasonId = seasonId,
            Stage = stage,
            Number = lastNumber + 1,
            Date = date,
            DurationMinutes = duration,
            Status = RoundStatus.Draft,
        };

        // Another round of the same stage (etapa): start with the previous round's players and decks.
        // The store only unchecks who left; they keep their results in the stage.
        if (stage.Id != 0)
        {
            var previous = await db.Rounds.Include(r => r.Entries)
                .Where(r => r.StageId == stage.Id).OrderByDescending(r => r.Number).FirstOrDefaultAsync();
            if (previous is not null)
                RoundWorkflow.SetEntries(round, previous.Entries.Select(e => new EntryInput(e.PlayerId, e.DeckId)).ToList());
        }
        db.Rounds.Add(round);
        await db.SaveChangesAsync();
        return await Reload(round.Id);
    }

    public async Task<RoundDto> SetEntries(int id, IReadOnlyList<EntryInput> entries)
    {
        var round = await Load(id);

        var deckIds = entries.Select(e => e.DeckId).Distinct().ToList();
        var decks = await db.Decks.Where(d => deckIds.Contains(d.Id)).ToDictionaryAsync(d => d.Id);
        var playerIds = entries.Select(e => e.PlayerId).Distinct().ToList();
        var players = await db.Players.Where(p => playerIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id);
        foreach (var e in entries)
        {
            if (!players.ContainsKey(e.PlayerId)) throw new ValidationException($"Jogador {e.PlayerId} não existe.");
            if (!decks.TryGetValue(e.DeckId, out var deck)) throw new ValidationException($"Deck {e.DeckId} não existe.");
            if (deck.PlayerId != e.PlayerId)
                throw new ValidationException($"O deck '{deck.Name}' não pertence a {players[e.PlayerId].Name}.");
        }

        RoundWorkflow.SetEntries(round, entries);
        await db.SaveChangesAsync();
        return await Reload(id);
    }

    public async Task<RoundDto> Draw(int id)
    {
        var round = await Load(id);
        var seasonRounds = await db.Rounds.Where(r => r.SeasonId == round.SeasonId).Include(r => r.Entries).ToListAsync();
        var previousByes = RoundWorkflow.CountPreviousByes(seasonRounds, id);
        // Earlier rounds of the same stage: the draw avoids repeating table-mates and seats.
        var stageRounds = await db.Rounds.AsNoTracking()
            .Where(r => r.StageId == round.StageId && r.Id != id && r.Status != RoundStatus.Draft)
            .Include(r => r.Entries).Include(r => r.Tables).ThenInclude(t => t.Seats)
            .AsSplitQuery().ToListAsync();
        RoundWorkflow.Draw(round, previousByes, rng, stageRounds);
        await db.SaveChangesAsync();
        return await Reload(id);
    }

    public async Task<RoundDto> Start(int id, int? durationMinutes)
    {
        var round = await Load(id);
        RoundWorkflow.Start(round, Now, durationMinutes);
        await db.SaveChangesAsync();
        return await Reload(id);
    }

    public async Task<RoundDto> SetTableResult(int tableId, TableResultRequest req, string? user)
    {
        var roundId = await db.Tables.Where(t => t.Id == tableId).Select(t => (int?)t.RoundId).FirstOrDefaultAsync()
            ?? throw new NotFoundException($"Mesa {tableId} não encontrada.");
        var round = await Load(roundId);
        var table = round.Tables.Single(t => t.Id == tableId);
        RoundWorkflow.SetResult(round, table, req.Result, req.WinnerEntryId, Now, user);
        await db.SaveChangesAsync();
        return await Reload(roundId);
    }

    public async Task<RoundDto> Close(int id)
    {
        var round = await Load(id);
        RoundWorkflow.Close(round, Now);
        await db.SaveChangesAsync();
        return await Reload(id);
    }

    public async Task Delete(int id)
    {
        var round = await Load(id);
        RoundWorkflow.EnsureDeletable(round);
        db.Rounds.Remove(round);

        // Last round of the stage → drop the empty stage and renumber the others.
        if (round.Stage is { } stage && stage.Rounds.All(r => r.Id == round.Id))
        {
            db.Stages.Remove(stage);
            var others = await db.Stages.Where(s => s.SeasonId == round.SeasonId && s.Id != stage.Id).ToListAsync();
            StageRules.Renumber(others);
        }
        await db.SaveChangesAsync();
    }

    /// <summary>Drops tracked state so navigations of newly added rows are loaded fresh.</summary>
    private async Task<RoundDto> Reload(int id)
    {
        db.ChangeTracker.Clear();
        return await Get(id);
    }
}
