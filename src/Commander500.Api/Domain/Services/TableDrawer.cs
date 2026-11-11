namespace Commander500.Api.Domain.Services;

/// <param name="Key">Caller-defined identifier (e.g. index of the round entry).</param>
/// <param name="PreviousByes">Byes this player already received in the season.</param>
/// <param name="LastSeat">Seat (1–4) in the previous round of the same stage, to avoid repeating it.</param>
public record DrawCandidate(int Key, int PreviousByes, int? LastSeat = null);

/// <param name="ByeKeys">Candidates left out of the tables.</param>
/// <param name="Tables">Each table lists candidate keys in seat order (index 0 = seat 1, plays first).</param>
/// <param name="RepeatedPairs">Pairs of players who already shared a table earlier in the stage (0 = all new matchups).</param>
/// <param name="RepeatedSeats">Players in the same seat as in their previous round.</param>
public record DrawResult(IReadOnlyList<int> ByeKeys, IReadOnlyList<IReadOnlyList<int>> Tables, int RepeatedPairs = 0, int RepeatedSeats = 0);

/// <summary>Table draw (spec 4.2 / 4.3).</summary>
public static class TableDrawer
{
    public const int TableSize = 4;

    /// <summary>Random arrangements tried when there is history to avoid; the best one wins.</summary>
    public const int Attempts = 300;

    private const int PairPenalty = 100; // repeated opponent: much worse than…
    private const int SeatPenalty = 1;   // …a repeated seat

    /// <param name="previousPairs">
    /// How many times each pair of candidate keys already shared a table earlier in the same stage
    /// (key order inside the tuple does not matter). Empty/null = plain random draw.
    /// </param>
    public static DrawResult Draw(
        IReadOnlyList<DrawCandidate> candidates, IRandomSource rng,
        IReadOnlyDictionary<(int, int), int>? previousPairs = null)
    {
        if (candidates.Count < TableSize)
            throw new DomainException($"São necessários pelo menos {TableSize} jogadores presentes para sortear as mesas.");
        if (candidates.Select(c => c.Key).Distinct().Count() != candidates.Count)
            throw new DomainException("Há jogadores duplicados no sorteio.");

        var byeCount = candidates.Count % TableSize;

        // Shuffle first, then a stable sort by previous byes: the ones with fewest byes come first,
        // and ties are in random order — i.e. a random pick among those with the fewest byes.
        var shuffled = candidates.ToList();
        Shuffle(shuffled, rng);
        var byes = shuffled.OrderBy(c => c.PreviousByes).Take(byeCount).Select(c => c.Key).ToList();
        var playing = shuffled.Where(c => !byes.Contains(c.Key)).ToList();

        var hasHistory = (previousPairs?.Count ?? 0) > 0 || playing.Any(c => c.LastSeat is not null);
        var attempts = hasHistory ? Attempts : 1;

        List<List<DrawCandidate>>? best = null;
        (int Pairs, int Seats) bestScore = (int.MaxValue, int.MaxValue);
        for (var i = 0; i < attempts; i++)
        {
            Shuffle(playing, rng);
            var tables = playing.Chunk(TableSize).Select(t => t.ToList()).ToList();
            var score = Score(tables, previousPairs);
            if (Weight(score) < Weight(bestScore))
            {
                best = tables;
                bestScore = score;
                if (Weight(score) == 0) break;
            }
        }

        return new DrawResult(
            byes,
            best!.Select(t => (IReadOnlyList<int>)t.Select(c => c.Key).ToList()).ToList(),
            bestScore.Pairs, bestScore.Seats);
    }

    private static int Weight((int Pairs, int Seats) s) =>
        s.Pairs == int.MaxValue ? int.MaxValue : s.Pairs * PairPenalty + s.Seats * SeatPenalty;

    private static (int Pairs, int Seats) Score(List<List<DrawCandidate>> tables, IReadOnlyDictionary<(int, int), int>? previousPairs)
    {
        int pairs = 0, seats = 0;
        foreach (var table in tables)
        {
            for (var s = 0; s < table.Count; s++)
            {
                if (table[s].LastSeat == s + 1) seats++;
                if (previousPairs is null) continue;
                for (var o = s + 1; o < table.Count; o++)
                    pairs += previousPairs.GetValueOrDefault(PairKey(table[s].Key, table[o].Key));
            }
        }
        return (pairs, seats);
    }

    /// <summary>Order-independent key for a pair of candidates.</summary>
    public static (int, int) PairKey(int a, int b) => a < b ? (a, b) : (b, a);

    /// <summary>In-place Fisher–Yates shuffle.</summary>
    public static void Shuffle<T>(IList<T> list, IRandomSource rng)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
