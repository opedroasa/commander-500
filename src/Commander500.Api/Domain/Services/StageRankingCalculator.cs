using System.Globalization;

namespace Commander500.Api.Domain.Services;

/// <param name="Position">Shared by tied players (competition ranking: 1, 1, 3…).</param>
/// <param name="Wins">Table wins + byes (in a stage a bye counts as a win).</param>
/// <param name="HeadToHeadWins">Wins at tables shared with other players tied on wins/byes (0 when not tied).</param>
/// <param name="Tied">True when another player has the same position.</param>
public record StageRankingRow(
    int Position, int PlayerId, string PlayerName,
    int Wins, int TableWins, int Byes, int Draws, int Losses, int Matches,
    int HeadToHeadWins, bool Tied);

/// <summary>
/// Etapa ranking (prize of the day). Order:
/// 1. wins ↓ (a bye counts as a win);
/// 2. byes ↑ — among equal wins, whoever had a bye ranks below;
/// 3. head-to-head ↓ — among the players still tied, wins at tables where they faced each other;
/// 4. still equal → same position (the store decides the prize).
/// Only closed rounds count.
/// </summary>
public static class StageRankingCalculator
{
    private static readonly StringComparer NameComparer = StringComparer.Create(new CultureInfo("pt-BR"), ignoreCase: true);

    public static IReadOnlyList<StageRankingRow> Calculate(IEnumerable<Round> stageRounds, IReadOnlyDictionary<int, string> playerNames)
    {
        var closed = stageRounds.Where(r => r.Status == RoundStatus.Closed).ToList();
        var acc = new Dictionary<int, Acc>();
        Acc For(int id) => acc.TryGetValue(id, out var a) ? a : acc[id] = new Acc(id, playerNames.GetValueOrDefault(id) ?? $"#{id}");

        // Decided tables as player-id lists, to compute head-to-head later.
        var decided = new List<(int[] Players, int? Winner)>();

        foreach (var round in closed)
        {
            foreach (var bye in round.Entries.Where(e => e.IsBye)) For(bye.PlayerId).Byes++;

            var byId = round.Entries.ToDictionary(e => e.Id);
            foreach (var table in round.Tables.Where(t => t.Result != TableResult.Pending))
            {
                var seated = table.Seats.Select(s => s.RoundEntry ?? byId[s.RoundEntryId]).ToList();
                int? winner = null;
                foreach (var entry in seated)
                {
                    var a = For(entry.PlayerId);
                    a.Matches++;
                    if (table.Result == TableResult.Draw) a.Draws++;
                    else if (table.WinnerEntryId == entry.Id) { a.TableWins++; winner = entry.PlayerId; }
                    else a.Losses++;
                }
                decided.Add((seated.Select(e => e.PlayerId).ToArray(), winner));
            }
        }

        var rows = new List<StageRankingRow>();
        var groups = acc.Values
            .GroupBy(a => (a.Wins, a.Byes))
            .OrderByDescending(g => g.Key.Wins).ThenBy(g => g.Key.Byes);

        foreach (var group in groups)
        {
            var members = group.ToList();
            var ids = members.Select(m => m.PlayerId).ToHashSet();
            if (members.Count > 1)
                foreach (var m in members)
                    m.HeadToHead = decided.Count(t => t.Winner == m.PlayerId && t.Players.Any(p => p != m.PlayerId && ids.Contains(p)));

            var ordered = members.OrderByDescending(m => m.HeadToHead).ThenBy(m => m.Name, NameComparer).ThenBy(m => m.PlayerId).ToList();
            var groupStart = rows.Count;
            foreach (var m in ordered)
            {
                // Position = 1 + players strictly ahead (earlier groups + better head-to-head in this group).
                var position = groupStart + 1 + ordered.Count(o => o.HeadToHead > m.HeadToHead);
                var tied = ordered.Count(o => o.HeadToHead == m.HeadToHead) > 1;
                rows.Add(new StageRankingRow(position, m.PlayerId, m.Name, m.Wins, m.TableWins, m.Byes, m.Draws, m.Losses, m.Matches,
                    members.Count > 1 ? m.HeadToHead : 0, tied));
            }
        }
        return rows;
    }

    private sealed class Acc(int playerId, string name)
    {
        public int PlayerId { get; } = playerId;
        public string Name { get; } = name;
        public int TableWins, Byes, Draws, Losses, Matches, HeadToHead;
        public int Wins => TableWins + Byes;
    }
}
