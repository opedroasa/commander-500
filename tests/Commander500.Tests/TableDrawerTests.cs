using Commander500.Api.Domain;
using Commander500.Api.Domain.Services;

namespace Commander500.Tests;

public class TableDrawerTests
{
    private static List<DrawCandidate> Candidates(int n, Func<int, int>? previousByes = null) =>
        Enumerable.Range(1, n).Select(k => new DrawCandidate(k, previousByes?.Invoke(k) ?? 0)).ToList();

    [Theory]
    [InlineData(4, 1, 0)]
    [InlineData(5, 1, 1)]
    [InlineData(6, 1, 2)]
    [InlineData(7, 1, 3)]
    [InlineData(8, 2, 0)]
    [InlineData(13, 3, 1)]
    [InlineData(23, 5, 3)]
    public void Distributes_players_into_tables_of_four_and_byes(int n, int expectedTables, int expectedByes)
    {
        var result = TableDrawer.Draw(Candidates(n), new SeededRandomSource(n));

        Assert.Equal(expectedTables, result.Tables.Count);
        Assert.All(result.Tables, t => Assert.Equal(4, t.Count));
        Assert.Equal(expectedByes, result.ByeKeys.Count);

        var everyone = result.Tables.SelectMany(t => t).Concat(result.ByeKeys).Order().ToList();
        Assert.Equal(Enumerable.Range(1, n), everyone); // each player exactly once
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public void Fewer_than_four_players_cannot_be_drawn(int n)
    {
        var ex = Assert.Throws<DomainException>(() => TableDrawer.Draw(Candidates(n), new SeededRandomSource(1)));
        Assert.Contains("4", ex.Message);
    }

    [Fact]
    public void Duplicate_candidates_are_rejected() =>
        Assert.Throws<DomainException>(() =>
            TableDrawer.Draw([new(1, 0), new(1, 0), new(2, 0), new(3, 0), new(4, 0)], new SeededRandomSource(1)));

    [Fact]
    public void Bye_goes_to_the_only_player_without_previous_bye()
    {
        // 5 present → 1 bye. Player 3 never had a bye; everyone else had one.
        for (var seed = 0; seed < 200; seed++)
        {
            var result = TableDrawer.Draw(Candidates(5, k => k == 3 ? 0 : 1), new SeededRandomSource(seed));
            Assert.Equal([3], result.ByeKeys);
        }
    }

    [Fact]
    public void Byes_prefer_players_with_fewest_byes_then_next_level()
    {
        // 7 present → 3 byes. Players 1 and 2 have 0 byes, 3 and 4 have 1, the rest have 2.
        // Both 0-bye players must be out, plus one of the 1-bye players.
        for (var seed = 0; seed < 200; seed++)
        {
            var result = TableDrawer.Draw(Candidates(7, k => k switch { 1 or 2 => 0, 3 or 4 => 1, _ => 2 }), new SeededRandomSource(seed));
            Assert.Contains(1, result.ByeKeys);
            Assert.Contains(2, result.ByeKeys);
            Assert.Single(result.ByeKeys, k => k is 3 or 4);
        }
    }

    [Fact]
    public void Ties_for_bye_are_broken_randomly()
    {
        var byeCounts = new Dictionary<int, int>();
        for (var seed = 0; seed < 500; seed++)
            foreach (var k in TableDrawer.Draw(Candidates(5), new SeededRandomSource(seed)).ByeKeys)
                byeCounts[k] = byeCounts.GetValueOrDefault(k) + 1;

        Assert.Equal(5, byeCounts.Count); // every tied player got the bye at some point
        Assert.All(byeCounts.Values, c => Assert.InRange(c, 50, 150)); // roughly uniform (expected 100)
    }

    [Fact]
    public void Seat_order_varies_between_draws()
    {
        var firstSeats = Enumerable.Range(0, 100)
            .Select(seed => TableDrawer.Draw(Candidates(8), new SeededRandomSource(seed)).Tables[0][0])
            .Distinct().Count();
        Assert.True(firstSeats > 4, "seat 1 should not always go to the same players");
    }

    [Fact]
    public void Crypto_rng_produces_valid_draw()
    {
        var result = TableDrawer.Draw(Candidates(10), new CryptoRandomSource());
        Assert.Equal(2, result.Tables.Count);
        Assert.Equal(2, result.ByeKeys.Count);
    }
}
