using Commander500.Api.Contracts;
using Commander500.Api.Data;
using Commander500.Api.Domain;
using Commander500.Api.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace Commander500.Api.Application;

public class SeasonService(AppDbContext db, TimeProvider clock)
{
    public async Task<IReadOnlyList<SeasonDto>> List()
    {
        var rows = await db.Seasons.AsNoTracking()
            .OrderByDescending(s => s.IsActive).ThenByDescending(s => s.StartDate).ThenByDescending(s => s.Id)
            .Select(s => new { Season = s, Count = s.Rounds.Count, Stages = s.Stages.Count })
            .ToListAsync();
        return rows.Select(r => r.Season.ToDto(r.Count, r.Stages)).ToList();
    }

    public async Task<SeasonDto> Get(int id)
    {
        var row = await db.Seasons.AsNoTracking().Where(s => s.Id == id)
            .Select(s => new { Season = s, Count = s.Rounds.Count, Stages = s.Stages.Count }).FirstOrDefaultAsync()
            ?? throw new NotFoundException($"Temporada {id} não encontrada.");
        return row.Season.ToDto(row.Count, row.Stages);
    }

    public async Task<SeasonDto> Create(SeasonUpsertRequest req)
    {
        var season = new Season();
        Apply(season, req);
        if (req.Config is not null) SeasonRules.ApplyConfig(season, req.Config);
        db.Seasons.Add(season);
        await db.SaveChangesAsync();
        return await Get(season.Id);
    }

    /// <summary>Name, dates and active flag. Ending a season = setting EndDate and IsActive=false.</summary>
    public async Task<SeasonDto> Update(int id, SeasonUpsertRequest req)
    {
        var season = await Find(id);
        Apply(season, req);
        if (req.Config is not null) SeasonRules.ApplyConfig(season, req.Config);
        await db.SaveChangesAsync();
        return await Get(id);
    }

    public async Task<SeasonDto> UpdateConfig(int id, SeasonConfig config)
    {
        var season = await Find(id);
        SeasonRules.ApplyConfig(season, config);
        await db.SaveChangesAsync();
        return await Get(id);
    }

    /// <summary>Ends the season: inactive, EndDate = today if not set. Open rounds must be closed or deleted first.</summary>
    public async Task<SeasonDto> End(int id)
    {
        var season = await Find(id);
        if (await db.Rounds.AnyAsync(r => r.SeasonId == id && r.Status != RoundStatus.Closed))
            throw new DomainException("Há rodadas em aberto nesta temporada. Encerre ou exclua-as antes.");
        season.IsActive = false;
        season.EndDate ??= DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        await db.SaveChangesAsync();
        return await Get(id);
    }

    public async Task Delete(int id)
    {
        var season = await Find(id);
        if (await db.Rounds.AnyAsync(r => r.SeasonId == id))
            throw new DomainException("A temporada já tem rodadas e não pode ser excluída. Encerre-a em vez disso.");
        db.Seasons.Remove(season);
        await db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<RankingRow>> Ranking(int id)
    {
        var season = await FindNoTracking(id);
        var rounds = await db.Rounds.AsNoTracking()
            .Where(r => r.SeasonId == id && r.Status == RoundStatus.Closed)
            .Include(r => r.Entries).Include(r => r.Tables).ThenInclude(t => t.Seats)
            .AsSplitQuery().ToListAsync();
        var playerIds = rounds.SelectMany(r => r.Entries).Select(e => e.PlayerId).Distinct().ToList();
        var names = await db.Players.Where(p => playerIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Nickname == null ? p.Name : $"{p.Name} ({p.Nickname})");
        return RankingCalculator.Calculate(season, rounds, names);
    }

    public async Task<IReadOnlyList<RoundDto>> Rounds(int id)
    {
        await FindNoTracking(id);
        var now = clock.GetUtcNow().UtcDateTime;
        var rounds = await RoundService.WithGraph(db.Rounds.AsNoTrackingWithIdentityResolution())
            .Where(r => r.SeasonId == id).OrderByDescending(r => r.Number).ToListAsync();
        return rounds.Select(r => r.ToDto(now)).ToList();
    }

    private static void Apply(Season season, SeasonUpsertRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) throw new ValidationException("Nome da temporada é obrigatório.");
        if (req.EndDate is { } end && end < req.StartDate) throw new ValidationException("Data de fim anterior ao início.");
        season.Name = req.Name.Trim();
        season.StartDate = req.StartDate;
        season.EndDate = req.EndDate;
        season.IsActive = req.IsActive;
    }

    private async Task<Season> Find(int id) =>
        await db.Seasons.FindAsync(id) ?? throw new NotFoundException($"Temporada {id} não encontrada.");

    private async Task<Season> FindNoTracking(int id) =>
        await db.Seasons.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id) ?? throw new NotFoundException($"Temporada {id} não encontrada.");
}
