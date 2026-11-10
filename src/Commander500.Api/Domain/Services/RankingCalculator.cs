using System.Globalization;

namespace Commander500.Api.Domain.Services;

public record RankingRow(
    int Position, int PlayerId, string PlayerName,
    int Points, int Wins, int Draws, int Losses, int Byes,
    /// <summary>Tables actually played (byes excluded).</summary>
    int Matches,
    /// <summary>Wins / Matches; null when no matches.</summary>
    double? WinRate);

/// <summary>
/// Season ranking (spec 4.1). Points are never stored: they are always derived from
/// closed rounds' results + the season config, so correcting a result recalculates everything.
/// </summary>
public static class RankingCalculator
{
    private static readonly StringComparer NameComparer = StringComparer.Create(new CultureInfo("pt-BR"), ignoreCase: true);

    public static IReadOnlyList<RankingRow> Calculate(Season season, IEnumerable<Round> rounds, IReadOnlyDictionary<int, string> playerNames)
    {
        var stats = new Dictionary<int, Acc>();
        Acc For(int playerId) => stats.TryGetValue(playerId, out var a) ? a : stats[playerId] = new Acc(playerId);

        foreach (var round in rounds.Where(r => r.Status == RoundStatus.Closed))
        {
            var byes = round.Entries.Where(e => e.IsBye).ToList();
            var byePoints = season.ByePoints(byes.Count);
            foreach (var bye in byes)
            {
                var a = For(bye.PlayerId);
                a.Byes++;
                a.Points += byePoints;
            }

            var entriesById = round.Entries.ToDictionary(e => e.Id);
            foreach (var table in round.Tables.Where(t => t.Result != TableResult.Pending))
            {
                foreach (var seat in table.Seats)
                {
                    var entry = seat.RoundEntry ?? entriesById[seat.RoundEntryId];
                    var a = For(entry.PlayerId);
                    a.Matches++;
                    if (table.Result == TableResult.Draw)
                    {
                        a.Draws++;
                        a.Points += season.PointsDraw;
                    }
                    else if (table.WinnerEntryId == entry.Id)
                    {
                        a.Wins++;
                        a.Points += season.PointsWin;
                    }
                    else
                    {
                        a.Losses++;
                    }
                }
            }
        }

        foreach (var a in stats.Values)
            a.Name = playerNames.TryGetValue(a.PlayerId, out var n) ? n : $"#{a.PlayerId}";

        IOrderedEnumerable<Acc> ordered = stats.Values.OrderByDescending(a => a.Points);
        foreach (var criterion in season.GetTiebreakers())
        {
            ordered = criterion switch
            {
                TiebreakCriterion.Wins => ordered.ThenByDescending(a => a.Wins),
                TiebreakCriterion.FewerMatches => ordered.ThenBy(a => a.Matches),
                TiebreakCriterion.MoreMatches => ordered.ThenByDescending(a => a.Matches),
                TiebreakCriterion.Draws => ordered.ThenByDescending(a => a.Draws),
                TiebreakCriterion.WinRate => ordered.ThenByDescending(a => a.WinRate ?? -1),
                _ => ordered,
            };
        }
        ordered = ordered.ThenBy(a => a.Name, NameComparer).ThenBy(a => a.PlayerId);

        return ordered
            .Select((a, i) => new RankingRow(i + 1, a.PlayerId, a.Name, a.Points, a.Wins, a.Draws, a.Losses, a.Byes, a.Matches, a.WinRate))
            .ToList();
    }

    private sealed class Acc(int playerId)
    {
        public int PlayerId { get; } = playerId;
        public string Name { get; set; } = "";
        public int Points, Wins, Draws, Losses, Byes, Matches;
        public double? WinRate => Matches == 0 ? null : (double)Wins / Matches;
    }
}
