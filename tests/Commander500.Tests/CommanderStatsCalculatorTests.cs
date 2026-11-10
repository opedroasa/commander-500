using Commander500.Api.Domain;
using Commander500.Api.Domain.Services;

namespace Commander500.Tests;

public class CommanderStatsCalculatorTests
{
    [Fact]
    public void Partner_pair_is_one_row_separate_from_single_commander()
    {
        var tymnaThrasios = Decks.Of("Tymna the Weaver", "Thrasios, Triton Hero");
        var tymnaKraum = Decks.Of("Tymna the Weaver", "Kraum, Ludevic's Opus");
        var atraxa = Decks.Of("Atraxa, Praetors' Voice");
        var urDragon = Decks.Of("The Ur-Dragon");

        var round = new RoundBuilder(1)
            .WithDeck(1, tymnaThrasios).WithDeck(2, tymnaKraum).WithDeck(3, atraxa).WithDeck(4, urDragon)
            .Table(winner: 1, 1, 2, 3, 4).Build();

        var stats = CommanderStatsCalculator.Calculate([round], minMatchesForWinRate: 1);

        Assert.Equal(4, stats.Count);
        var pair = stats.Single(s => s.CommanderIds.Count == 2 && s.Name.Contains("Thrasios"));
        Assert.Equal("Thrasios, Triton Hero + Tymna the Weaver", pair.Name);
        Assert.Equal(1, pair.Wins);
        Assert.Equal(0, stats.Single(s => s.Name.Contains("Kraum")).Wins);
    }

    [Fact]
    public void Byes_are_not_matches_or_wins()
    {
        var atraxa = Decks.Of("Atraxa, Praetors' Voice");
        var other = Decks.Of("Teysa Karlov");
        var round = new RoundBuilder(1)
            .WithDeck(1, other).WithDeck(2, other).WithDeck(3, other).WithDeck(4, other).WithDeck(5, atraxa)
            .Table(winner: 1, 1, 2, 3, 4).Bye(5).Build();

        var stats = CommanderStatsCalculator.Calculate([round], 1);

        Assert.DoesNotContain(stats, s => s.Name.StartsWith("Atraxa"));
        var teysa = Assert.Single(stats);
        Assert.Equal(4, teysa.Matches);
        Assert.Equal(1, teysa.Wins);
    }

    [Fact]
    public void Win_rate_only_shown_after_minimum_matches()
    {
        var a = Decks.Of("Commander A");
        var b = Decks.Of("Commander B");
        var rounds = Enumerable.Range(1, 3).Select(i =>
        {
            var builder = new RoundBuilder(i).WithDeck(1, a).WithDeck(2, b).WithDeck(3, b).WithDeck(4, b);
            return builder.Table(winner: i == 1 ? 1 : null, 1, 2, 3, 4).Build();
        }).ToList();

        var withTwo = CommanderStatsCalculator.Calculate(rounds.Take(2), minMatchesForWinRate: 3);
        Assert.Null(withTwo.Single(s => s.Name == "Commander A").WinRate);

        var withThree = CommanderStatsCalculator.Calculate(rounds, minMatchesForWinRate: 3);
        var rowA = withThree.Single(s => s.Name == "Commander A");
        Assert.Equal(3, rowA.Matches);
        Assert.Equal(1, rowA.Wins);
        Assert.Equal(2, rowA.Draws);
        Assert.Equal(1.0 / 3, rowA.WinRate!.Value, 5);
        Assert.Equal("Commander B", withThree[0].Name); // most played first (9 matches)
    }
}
