using Commander500.Api.Contracts;
using Commander500.Api.Domain;
using Commander500.Api.Domain.Services;
using Commander500.Api.Infrastructure.Scryfall;

namespace Commander500.Api.Application;

public static class Mapping
{
    public static SeasonDto ToDto(this Season s, int roundsCount, int stagesCount) =>
        new(s.Id, s.Name, s.StartDate, s.EndDate, s.IsActive, s.ConfigLocked, SeasonConfig.From(s), roundsCount, stagesCount);

    public static CommanderDto ToDto(this Commander c) => new(c.Id, c.Name, c.ArtUrl, c.ImageUrl, c.ColorIdentity);

    public static ScryfallCardDto ToDto(this ScryfallCard c) => new(c.Id, c.Name, c.ArtUrl, c.ImageUrl, c.ColorIdentity, c.TypeLine);

    public static DeckDto ToDto(this Deck d, bool includeReviewNote = true) => new(
        d.Id, d.PlayerId, d.Player?.Name ?? "", d.Name, d.ListUrl, d.Status, includeReviewNote ? d.ReviewNote : null, d.CreatedAt,
        d.Commanders.Select(dc => dc.Commander!).OrderBy(c => c.Name).Select(c => c.ToDto()).ToList());

    public static string CommanderLabel(Deck? d) =>
        d is null ? "" : string.Join(" + ", d.Commanders.Select(dc => dc.Commander?.Name).Where(n => n is not null).Order());

    public static EntryDto ToDto(this RoundEntry e) => new(
        e.Id, e.PlayerId, e.Player?.Name ?? "", e.Player?.Nickname,
        e.DeckId, e.Deck?.Name ?? "", e.Deck?.Status ?? DeckStatus.Pending, CommanderLabel(e.Deck),
        e.Deck?.Commanders.Select(dc => dc.Commander).OrderBy(c => c?.Name).FirstOrDefault(c => c?.ArtUrl is not null)?.ArtUrl,
        e.IsBye);

    public static RoundDto ToDto(this Round r, DateTime nowUtc)
    {
        var entries = r.Entries.OrderBy(e => e.Player?.Name).ToList();
        var byId = entries.ToDictionary(e => e.Id);
        var timer = RoundWorkflow.GetTimer(r, nowUtc);

        var tables = r.Tables.OrderBy(t => t.Number).Select(t => new TableDto(
            t.Id, t.Number, t.Result, t.WinnerEntryId, t.UpdatedAt, t.UpdatedBy,
            t.Seats.OrderBy(s => s.Seat)
                .Select(s => new SeatDto(s.Seat, (s.RoundEntry ?? byId[s.RoundEntryId]).ToDto()))
                .ToList())).ToList();

        // Position inside the stage: 1 + rounds of the same stage with a lower season number.
        var numberInStage = r.Stage is null ? 1 : 1 + r.Stage.Rounds.Count(x => x.Number < r.Number);

        return new RoundDto(
            r.Id, r.SeasonId, r.Season?.Name ?? "", r.StageId, r.Stage?.Number ?? 0, numberInStage, r.Number, r.Date,
            r.Status, RoundWorkflow.StatusLabel(r.Status), r.DurationMinutes, r.Season?.ExtraTurnsAfterTime ?? 0,
            nowUtc,
            timer is null ? null : new TimerDto(timer.StartedAt, timer.DurationMinutes, timer.EndsAt, timer.RemainingSeconds, timer.Expired),
            r.ClosedAt,
            entries.Select(e => e.ToDto()).ToList(),
            entries.Where(e => e.IsBye).Select(e => e.ToDto()).ToList(),
            tables);
    }

    public static CommanderStatsDto ToDto(this CommanderStatsRow row, IReadOnlyDictionary<int, Commander> commanders) => new(
        row.Name,
        row.CommanderIds.Select(id => commanders.TryGetValue(id, out var c) ? c.ToDto() : new CommanderDto(id, "?", null, null, null))
            .OrderBy(c => c.Name).ToList(),
        row.Matches, row.Wins, row.Draws, row.WinRate, row.Decks, row.Players);
}
