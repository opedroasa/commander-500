namespace Commander500.Api.Domain.Services;

/// <param name="Name">"A" or "A + B" for partner/background pairs.</param>
/// <param name="WinRate">Null while <see cref="Matches"/> is below the season minimum.</param>
public record CommanderStatsRow(string Name, IReadOnlyList<int> CommanderIds, int Matches, int Wins, int Draws, double? WinRate);

/// <summary>
/// Commander statistics (spec 6). A deck's commander set is one row: a "Tymna + Thrasios" deck
/// is counted apart from Tymna with another partner. Byes are neither matches nor wins (spec 4.2).
/// Requires entries' Deck.Commanders.Commander to be loaded.
/// </summary>
public static class CommanderStatsCalculator
{
    public static IReadOnlyList<CommanderStatsRow> Calculate(IEnumerable<Round> rounds, int minMatchesForWinRate)
    {
        var stats = new Dictionary<string, Acc>();

        foreach (var round in rounds.Where(r => r.Status == RoundStatus.Closed))
        {
            var entriesById = round.Entries.ToDictionary(e => e.Id);
            foreach (var table in round.Tables.Where(t => t.Result != TableResult.Pending))
            {
                foreach (var seat in table.Seats)
                {
                    var entry = seat.RoundEntry ?? entriesById[seat.RoundEntryId];
                    var commanders = entry.Deck?.Commanders
                        .Select(dc => dc.Commander!)
                        .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    if (commanders is null || commanders.Count == 0) continue;

                    var key = string.Join(",", commanders.Select(c => c.Id).Order());
                    if (!stats.TryGetValue(key, out var acc))
                        stats[key] = acc = new Acc(string.Join(" + ", commanders.Select(c => c.Name)), commanders.Select(c => c.Id).ToList());

                    acc.Matches++;
                    if (table.Result == TableResult.Draw) acc.Draws++;
                    else if (table.WinnerEntryId == entry.Id) acc.Wins++;
                }
            }
        }

        return stats.Values
            .OrderByDescending(a => a.Matches).ThenByDescending(a => a.Wins).ThenBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
            .Select(a => new CommanderStatsRow(a.Name, a.Ids, a.Matches, a.Wins, a.Draws,
                a.Matches >= Math.Max(1, minMatchesForWinRate) ? (double)a.Wins / a.Matches : null))
            .ToList();
    }

    private sealed class Acc(string name, List<int> ids)
    {
        public string Name { get; } = name;
        public List<int> Ids { get; } = ids;
        public int Matches, Wins, Draws;
    }
}
