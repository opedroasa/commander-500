namespace Commander500.Api.Domain.Services;

/// <param name="Name">"A" or "A + B" for partner/background pairs.</param>
/// <param name="WinRate">Null while <see cref="Matches"/> is below the minimum.</param>
/// <param name="Decks">Registered decks with exactly this commander set (only when decks are passed in).</param>
/// <param name="Players">Distinct players who played it in the given rounds.</param>
public record CommanderStatsRow(
    string Name, IReadOnlyList<int> CommanderIds,
    int Matches, int Wins, int Draws, double? WinRate, int Decks, int Players);

/// <summary>
/// Commander statistics. A deck's commander set is one row: a "Tymna + Thrasios" deck is counted
/// apart from Tymna with another partner. Byes are neither matches nor wins (spec 4.2).
/// Requires Deck.Commanders.Commander to be loaded on round entries and on <paramref name="decks"/>.
/// </summary>
public static class CommanderStatsCalculator
{
    /// <param name="decks">Registered decks; counts <see cref="CommanderStatsRow.Decks"/>.</param>
    /// <param name="includeUnplayed">Also list commander sets that have decks but no matches yet.</param>
    public static IReadOnlyList<CommanderStatsRow> Calculate(
        IEnumerable<Round> rounds, int minMatchesForWinRate,
        IEnumerable<Deck>? decks = null, bool includeUnplayed = false)
    {
        var stats = new Dictionary<string, Acc>();

        Acc? For(Deck? deck, bool create)
        {
            var commanders = deck?.Commanders.Select(dc => dc.Commander!).Where(c => c is not null)
                .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ToList();
            if (commanders is null || commanders.Count == 0) return null;
            var key = string.Join(",", commanders.Select(c => c.Id).Order());
            if (!stats.TryGetValue(key, out var acc) && create)
                stats[key] = acc = new Acc(string.Join(" + ", commanders.Select(c => c.Name)), commanders.Select(c => c.Id).ToList());
            return acc;
        }

        foreach (var round in rounds.Where(r => r.Status == RoundStatus.Closed))
        {
            var entriesById = round.Entries.ToDictionary(e => e.Id);
            foreach (var table in round.Tables.Where(t => t.Result != TableResult.Pending))
            {
                foreach (var seat in table.Seats)
                {
                    var entry = seat.RoundEntry ?? entriesById[seat.RoundEntryId];
                    var acc = For(entry.Deck, create: true);
                    if (acc is null) continue;
                    acc.Matches++;
                    acc.PlayerIds.Add(entry.PlayerId);
                    if (table.Result == TableResult.Draw) acc.Draws++;
                    else if (table.WinnerEntryId == entry.Id) acc.Wins++;
                }
            }
        }

        foreach (var deck in decks ?? [])
            if (For(deck, create: includeUnplayed) is { } acc) acc.Decks++;

        return stats.Values
            .OrderByDescending(a => a.Matches).ThenByDescending(a => a.Wins).ThenByDescending(a => a.Decks)
            .ThenBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
            .Select(a => new CommanderStatsRow(a.Name, a.Ids, a.Matches, a.Wins, a.Draws,
                a.Matches >= Math.Max(1, minMatchesForWinRate) ? (double)a.Wins / a.Matches : null,
                a.Decks, a.PlayerIds.Count))
            .ToList();
    }

    private sealed class Acc(string name, List<int> ids)
    {
        public string Name { get; } = name;
        public List<int> Ids { get; } = ids;
        public HashSet<int> PlayerIds { get; } = [];
        public int Matches, Wins, Draws, Decks;
    }
}
