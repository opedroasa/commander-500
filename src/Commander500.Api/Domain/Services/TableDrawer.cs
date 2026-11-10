namespace Commander500.Api.Domain.Services;

/// <param name="Key">Caller-defined identifier (e.g. index of the round entry).</param>
/// <param name="PreviousByes">Byes this player already received in the season.</param>
public record DrawCandidate(int Key, int PreviousByes);

/// <param name="ByeKeys">Candidates left out of the tables.</param>
/// <param name="Tables">Each table lists candidate keys in seat order (index 0 = seat 1, plays first).</param>
public record DrawResult(IReadOnlyList<int> ByeKeys, IReadOnlyList<IReadOnlyList<int>> Tables);

/// <summary>Table draw (spec 4.2 / 4.3).</summary>
public static class TableDrawer
{
    public const int TableSize = 4;

    public static DrawResult Draw(IReadOnlyList<DrawCandidate> candidates, IRandomSource rng)
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

        var playing = shuffled.Select(c => c.Key).Where(k => !byes.Contains(k)).ToList();
        Shuffle(playing, rng);

        var tables = playing.Chunk(TableSize).Select(t => (IReadOnlyList<int>)t.ToList()).ToList();
        return new DrawResult(byes, tables);
    }

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
