using Commander500.Api.Domain;
using Commander500.Api.Domain.Services;

namespace Commander500.Tests;

public class RankingCalculatorTests
{
    private static readonly Dictionary<int, string> Names = Enumerable.Range(1, 12)
        .ToDictionary(i => i, i => $"Jogador {(char)('A' + i - 1)}");

    private static Season DefaultSeason() => new() { Id = 1, Name = "T1" };

    private static RankingRow Row(IReadOnlyList<RankingRow> ranking, int playerId) => ranking.Single(r => r.PlayerId == playerId);

    [Fact]
    public void Only_the_winner_scores_on_a_win()
    {
        var round = new RoundBuilder(1).Table(winner: 2, 1, 2, 3, 4).Build();

        var ranking = RankingCalculator.Calculate(DefaultSeason(), [round], Names);

        Assert.Equal(3, Row(ranking, 2).Points);
        Assert.Equal(1, Row(ranking, 2).Wins);
        Assert.All(new[] { 1, 3, 4 }, p =>
        {
            Assert.Equal(0, Row(ranking, p).Points);
            Assert.Equal(1, Row(ranking, p).Losses);
            Assert.Equal(1, Row(ranking, p).Matches);
        });
        Assert.Equal(2, ranking[0].PlayerId);
    }

    [Fact]
    public void Draw_gives_draw_points_to_all_four()
    {
        var round = new RoundBuilder(1).Table(winner: null, 1, 2, 3, 4).Build();

        var ranking = RankingCalculator.Calculate(DefaultSeason(), [round], Names);

        Assert.All(ranking, r =>
        {
            Assert.Equal(1, r.Points);
            Assert.Equal(1, r.Draws);
            Assert.Equal(0, r.Wins);
        });
    }

    [Theory]
    [InlineData(1, 5)]
    [InlineData(2, 7)]
    [InlineData(3, 9)]
    public void Bye_points_depend_on_how_many_were_left_out(int byeCount, int expectedPoints)
    {
        var season = new Season { PointsBye1 = 5, PointsBye2 = 7, PointsBye3 = 9 };
        var round = new RoundBuilder(1).Table(winner: 1, 1, 2, 3, 4).Bye(Enumerable.Range(5, byeCount).ToArray()).Build();

        var ranking = RankingCalculator.Calculate(season, [round], Names);

        Assert.All(ranking.Where(r => r.PlayerId >= 5), r =>
        {
            Assert.Equal(expectedPoints, r.Points);
            Assert.Equal(1, r.Byes);
            Assert.Equal(0, r.Matches); // bye is not a match
            Assert.Null(r.WinRate);
        });
    }

    [Fact]
    public void Win_rate_ignores_byes()
    {
        var rounds = new[]
        {
            new RoundBuilder(1).Table(winner: 1, 1, 2, 3, 4).Build(),
            new RoundBuilder(2).Table(winner: 2, 1, 2, 3, 4).Build(),
            new RoundBuilder(3).Table(winner: 2, 2, 3, 4, 5).Bye(1).Build(),
        };

        var p1 = Row(RankingCalculator.Calculate(DefaultSeason(), rounds, Names), 1);

        Assert.Equal(2, p1.Matches);
        Assert.Equal(1, p1.Byes);
        Assert.Equal(0.5, p1.WinRate);
        Assert.Equal(3 + 3, p1.Points);
    }

    [Fact]
    public void Rounds_that_are_not_closed_do_not_count()
    {
        var rounds = new[]
        {
            new RoundBuilder(1).Table(winner: 1, 1, 2, 3, 4).Build(),
            new RoundBuilder(2, RoundStatus.InProgress).Table(winner: 2, 1, 2, 3, 4).Build(),
            new RoundBuilder(3, RoundStatus.Drawn).PendingTable(1, 2, 3, 4).Bye(5).Build(),
        };

        var ranking = RankingCalculator.Calculate(DefaultSeason(), rounds, Names);

        Assert.Equal(4, ranking.Count);
        Assert.Equal(3, Row(ranking, 1).Points);
        Assert.Equal(0, Row(ranking, 2).Points);
    }

    [Fact]
    public void Default_tiebreak_is_points_then_wins_then_fewer_matches_then_name()
    {
        // P1: 1 win + 0 draws in 1 match        = 3 pts, 1 win, 1 match
        // P5: 3 draws in 3 matches               = 3 pts, 0 wins, 3 matches
        // P9: 1 win in 2 matches                 = 3 pts, 1 win, 2 matches
        // P10: 3 pts via a single bye            = 3 pts, 0 wins, 0 matches
        var rounds = new[]
        {
            new RoundBuilder(1).Table(winner: 1, 1, 2, 3, 4).Table(winner: null, 5, 6, 7, 8).Bye(10).Build(),
            new RoundBuilder(2).Table(winner: null, 5, 6, 7, 8).Table(winner: 9, 9, 2, 3, 4).Build(),
            new RoundBuilder(3).Table(winner: null, 5, 6, 7, 8).Table(winner: 2, 9, 2, 3, 4).Build(),
        };

        var ranking = RankingCalculator.Calculate(DefaultSeason(), rounds, Names);
        var order = ranking.Where(r => r.Points == 3).Select(r => r.PlayerId).ToList();

        // P2 also ends with 3 pts: 1 win in 3 matches.
        // wins ↓: {1, 9, 2} before {5..8, 10}; among those, fewer matches first: 1 (1), 9 (2), 2 (3)
        // among 0-win players: 10 (0 matches) before 5..8 (3 matches each), then 5..8 by name
        Assert.Equal([1, 9, 2, 10, 5, 6, 7, 8], order);
        Assert.Equal(Enumerable.Range(1, ranking.Count), ranking.Select(r => r.Position));
    }

    [Fact]
    public void Tiebreak_criteria_are_configurable()
    {
        var rounds = new[]
        {
            new RoundBuilder(1).Table(winner: 1, 1, 2, 3, 4).Table(winner: null, 5, 6, 7, 8).Build(),
            new RoundBuilder(2).Table(winner: null, 5, 6, 7, 8).Build(),
            new RoundBuilder(3).Table(winner: null, 5, 6, 7, 8).Build(),
        };
        // P1 and P5..8 all have 3 points. With "Draws" first, the drawers come before P1.
        var season = new Season { Tiebreakers = "Draws,Wins" };

        var ranking = RankingCalculator.Calculate(season, rounds, Names);

        Assert.Equal([5, 6, 7, 8, 1], ranking.Where(r => r.Points == 3).Select(r => r.PlayerId));
    }

    [Fact]
    public void Name_breaks_remaining_ties()
    {
        var names = new Dictionary<int, string> { [1] = "Zeca", [2] = "Ana", [3] = "Ícaro", [4] = "bruno" };
        var round = new RoundBuilder(1).Table(winner: null, 1, 2, 3, 4).Build();

        var ranking = RankingCalculator.Calculate(DefaultSeason(), [round], names);

        Assert.Equal(["Ana", "bruno", "Ícaro", "Zeca"], ranking.Select(r => r.PlayerName));
    }

    [Fact]
    public void Correcting_a_result_recalculates_the_ranking()
    {
        var round = new RoundBuilder(1).Table(winner: 1, 1, 2, 3, 4).Build();
        var season = DefaultSeason();
        Assert.Equal(1, RankingCalculator.Calculate(season, [round], Names)[0].PlayerId);

        var table = round.Tables[0];
        var p3Entry = round.Entries.Single(e => e.PlayerId == 3);
        RoundWorkflow.SetResult(round, table, TableResult.Win, p3Entry.Id, DateTime.UtcNow, "admin");

        var ranking = RankingCalculator.Calculate(season, [round], Names);
        Assert.Equal(3, ranking[0].PlayerId);
        Assert.Equal(0, Row(ranking, 1).Points);
    }

    [Fact]
    public void Scoring_uses_the_season_configuration()
    {
        var season = new Season { PointsWin = 5, PointsDraw = 2 };
        var rounds = new[]
        {
            new RoundBuilder(1).Table(winner: 1, 1, 2, 3, 4).Build(),
            new RoundBuilder(2).Table(winner: null, 1, 2, 3, 4).Build(),
        };

        var ranking = RankingCalculator.Calculate(season, rounds, Names);

        Assert.Equal(7, Row(ranking, 1).Points);
        Assert.Equal(2, Row(ranking, 2).Points);
    }
}
