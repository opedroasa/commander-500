using Commander500.Api.Domain;
using Commander500.Api.Domain.Services;

namespace Commander500.Tests;

/// <summary>Second+ round of a stage: the draw avoids repeating table-mates and seats.</summary>
public class StageDrawTests
{
    private static readonly Dictionary<int, int> NoByes = new();

    private static Round DraftRound(int id, IEnumerable<int> players)
    {
        var round = new Round { Id = id, Number = id, Status = RoundStatus.Draft, DurationMinutes = 60, Season = new Season() };
        RoundWorkflow.SetEntries(round, players.Select(p => new EntryInput(p, 1000 + p)).ToList());
        foreach (var e in round.Entries) e.Id = id * 100 + e.PlayerId;
        return round;
    }

    private static HashSet<(int, int)> Pairs(Round round) =>
        round.Tables.SelectMany(t =>
        {
            var ps = t.Seats.Select(s => s.RoundEntry!.PlayerId).ToList();
            return from a in ps from b in ps where a < b select (a, b);
        }).ToHashSet();

    private static Dictionary<int, int> Seats(Round round) =>
        round.Tables.SelectMany(t => t.Seats).ToDictionary(s => s.RoundEntry!.PlayerId, s => s.Seat);

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(42)]
    public void Sixteen_players_get_completely_new_opponents(int seed)
    {
        // Round 1 tables were {1-4}, {5-8}, {9-12}, {13-16}. Round 2 can mix them so nobody repeats an opponent.
        var round1 = new RoundBuilder(1).Table(1, 1, 2, 3, 4).Table(5, 5, 6, 7, 8).Table(9, 9, 10, 11, 12).Table(13, 13, 14, 15, 16).Build();
        var round2 = DraftRound(2, Enumerable.Range(1, 16));

        RoundWorkflow.Draw(round2, NoByes, new SeededRandomSource(seed), [round1]);

        Assert.Empty(Pairs(round2).Intersect(Pairs(round1)));
    }

    [Theory]
    [InlineData(3)]
    [InlineData(11)]
    public void Nobody_keeps_the_same_seat_when_avoidable(int seed)
    {
        // Only 4 players: the opponents must repeat, but the seats can all change.
        var round1 = new RoundBuilder(1).Table(1, 1, 2, 3, 4).Build();
        var round2 = DraftRound(2, [1, 2, 3, 4]);

        RoundWorkflow.Draw(round2, NoByes, new SeededRandomSource(seed), [round1]);

        var before = Seats(round1);
        Assert.All(Seats(round2), kv => Assert.NotEqual(before[kv.Key], kv.Value));
    }

    [Fact]
    public void Eight_players_repeat_only_the_unavoidable_minimum()
    {
        // From {1-4},{5-8} any two new tables of 4 must keep at least 2 old pairs each (2+2 split) → 4 repeats total.
        var round1 = new RoundBuilder(1).Table(1, 1, 2, 3, 4).Table(5, 5, 6, 7, 8).Build();
        var round2 = DraftRound(2, Enumerable.Range(1, 8));

        RoundWorkflow.Draw(round2, NoByes, new SeededRandomSource(5), [round1]);

        Assert.Equal(4, Pairs(round2).Intersect(Pairs(round1)).Count());
    }

    [Fact]
    public void Player_who_left_is_simply_not_drawn_and_new_round_still_avoids_repeats()
    {
        var round1 = new RoundBuilder(1).Table(1, 1, 2, 3, 4).Table(5, 5, 6, 7, 8).Table(9, 9, 10, 11, 12).Table(13, 13, 14, 15, 16).Build();
        // Player 16 went home → 15 present → 3 tables + 3 byes.
        var round2 = DraftRound(2, Enumerable.Range(1, 15));

        RoundWorkflow.Draw(round2, NoByes, new SeededRandomSource(9), [round1]);

        Assert.Equal(3, round2.Tables.Count);
        Assert.Equal(3, round2.Entries.Count(e => e.IsBye));
        Assert.DoesNotContain(round2.Entries, e => e.PlayerId == 16);
        // Depending on who sits out, an old table may keep 4 players for only 3 new tables, so one repeated
        // pair can be unavoidable — but never 3 players from the same old table together again.
        var oldTable = (int p) => (p - 1) / 4;
        Assert.All(round2.Tables, t =>
            Assert.True(t.Seats.GroupBy(s => oldTable(s.RoundEntry!.PlayerId)).Max(g => g.Count()) <= 2));
    }

    [Fact]
    public void History_counts_pairs_across_all_earlier_rounds_and_keeps_latest_seat()
    {
        var r1 = new RoundBuilder(1).Table(1, 1, 2, 3, 4).Build();
        var r2 = new RoundBuilder(2).Table(1, 2, 1, 4, 3).Build();

        var history = StageHistory.From([r2, r1]);

        Assert.Equal(2, history.Pairs[(1, 2)]);
        Assert.Equal(2, history.LastSeat[1]); // seat in round 2 (player 1 was second)
        Assert.Equal(1, history.LastSeat[2]);
    }

    [Fact]
    public void Without_history_the_draw_is_a_plain_random_draw()
    {
        var round = DraftRound(1, Enumerable.Range(1, 8));
        RoundWorkflow.Draw(round, NoByes, new SeededRandomSource(1));
        Assert.Equal(2, round.Tables.Count);
        Assert.All(round.Tables, t => Assert.Equal(4, t.Seats.Count));
    }
}
