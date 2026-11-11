using Commander500.Api.Domain;
using Commander500.Api.Domain.Services;

namespace Commander500.Tests;

public class StageRankingCalculatorTests
{
    private static readonly Dictionary<int, string> Names = Enumerable.Range(1, 12)
        .ToDictionary(i => i, i => $"Jogador {(char)('A' + i - 1)}");

    private static StageRankingRow Row(IReadOnlyList<StageRankingRow> r, int playerId) => r.Single(x => x.PlayerId == playerId);

    [Fact]
    public void Bye_counts_as_a_win_in_the_stage()
    {
        // Spec example: Ana (1) wins 2 tables; Bruno (5) wins 1 table and has 1 bye → both have 2 wins.
        var rounds = new[]
        {
            new RoundBuilder(1).Table(winner: 1, 1, 2, 3, 4).Table(winner: 5, 5, 6, 7, 8).Build(),
            new RoundBuilder(2).Table(winner: 1, 1, 2, 3, 4).Table(winner: 6, 6, 7, 8, 9).Bye(5).Build(),
        };

        var ranking = StageRankingCalculator.Calculate(rounds, Names);

        Assert.Equal(2, Row(ranking, 1).Wins);
        Assert.Equal(2, Row(ranking, 5).Wins);
        Assert.Equal(1, Row(ranking, 5).TableWins);
        Assert.Equal(1, Row(ranking, 5).Byes);
    }

    [Fact]
    public void Among_equal_wins_whoever_had_a_bye_ranks_below()
    {
        var rounds = new[]
        {
            new RoundBuilder(1).Table(winner: 1, 1, 2, 3, 4).Table(winner: 5, 5, 6, 7, 8).Build(),
            new RoundBuilder(2).Table(winner: 1, 1, 2, 3, 4).Table(winner: 6, 6, 7, 8, 9).Bye(5).Build(),
        };

        var ranking = StageRankingCalculator.Calculate(rounds, Names);

        Assert.Equal(1, Row(ranking, 1).Position);
        Assert.Equal(2, Row(ranking, 5).Position);
        Assert.False(Row(ranking, 1).Tied);
        Assert.Equal([1, 5], ranking.Take(2).Select(r => r.PlayerId));
    }

    [Fact]
    public void Head_to_head_breaks_a_tie_between_two_players()
    {
        // Players 1 and 5 both win 2 tables, no byes. In round 3 they faced each other and 5 won.
        var rounds = new[]
        {
            new RoundBuilder(1).Table(winner: 1, 1, 2, 3, 4).Table(winner: 5, 5, 6, 7, 8).Build(),
            new RoundBuilder(2).Table(winner: 5, 1, 5, 2, 6).Table(winner: 3, 3, 4, 7, 8).Build(),
            new RoundBuilder(3).Table(winner: 1, 1, 2, 3, 4).Table(winner: 7, 5, 6, 7, 8).Build(),
        };

        var ranking = StageRankingCalculator.Calculate(rounds, Names);

        Assert.Equal(2, Row(ranking, 1).Wins);
        Assert.Equal(2, Row(ranking, 5).Wins);
        Assert.Equal(1, Row(ranking, 5).Position);
        Assert.Equal(2, Row(ranking, 1).Position);
        Assert.Equal(1, Row(ranking, 5).HeadToHeadWins);
        Assert.Equal(0, Row(ranking, 1).HeadToHeadWins);
    }

    [Fact]
    public void Without_head_to_head_the_players_share_the_position()
    {
        // 1 and 5 each win 1 table and never sat together.
        var round = new RoundBuilder(1).Table(winner: 1, 1, 2, 3, 4).Table(winner: 5, 5, 6, 7, 8).Build();

        var ranking = StageRankingCalculator.Calculate([round], Names);

        Assert.Equal(1, Row(ranking, 1).Position);
        Assert.Equal(1, Row(ranking, 5).Position);
        Assert.True(Row(ranking, 1).Tied);
        Assert.True(Row(ranking, 5).Tied);
        // The next group starts at position 3 (competition ranking: 1, 1, 3).
        Assert.All(ranking.Where(r => r.Wins == 0), r => Assert.Equal(3, r.Position));
    }

    [Fact]
    public void Circular_head_to_head_stays_tied()
    {
        // 1 beats 2, 2 beats 3, 3 beats 1 — each with 1 win, 1 head-to-head win.
        var rounds = new[]
        {
            new RoundBuilder(1).Table(winner: 1, 1, 2, 10, 11).Build(),
            new RoundBuilder(2).Table(winner: 2, 2, 3, 10, 11).Build(),
            new RoundBuilder(3).Table(winner: 3, 3, 1, 10, 11).Build(),
        };

        var ranking = StageRankingCalculator.Calculate(rounds, Names);

        Assert.All(new[] { 1, 2, 3 }, p =>
        {
            Assert.Equal(1, Row(ranking, p).Position);
            Assert.True(Row(ranking, p).Tied);
            Assert.Equal(1, Row(ranking, p).HeadToHeadWins);
        });
    }

    [Fact]
    public void Head_to_head_counts_wins_against_any_of_the_tied_group()
    {
        // 1, 2, 3 all end with 1 win. 1 beat 2 and 3 at the same table; 2 and 3 won elsewhere (vs non-tied players).
        var rounds = new[]
        {
            new RoundBuilder(1).Table(winner: 1, 1, 2, 3, 10).Build(),
            new RoundBuilder(2).Table(winner: 2, 2, 9, 10, 11).Build(),
            new RoundBuilder(3).Table(winner: 3, 3, 9, 10, 11).Build(),
        };

        var ranking = StageRankingCalculator.Calculate(rounds, Names);

        Assert.Equal(1, Row(ranking, 1).Position);
        Assert.False(Row(ranking, 1).Tied);
        Assert.Equal(2, Row(ranking, 2).Position);
        Assert.Equal(2, Row(ranking, 3).Position);
        Assert.True(Row(ranking, 2).Tied);
    }

    [Fact]
    public void Open_rounds_do_not_count()
    {
        var rounds = new[]
        {
            new RoundBuilder(1).Table(winner: 1, 1, 2, 3, 4).Build(),
            new RoundBuilder(2, RoundStatus.InProgress).Table(winner: 2, 1, 2, 3, 4).Build(),
        };

        var ranking = StageRankingCalculator.Calculate(rounds, Names);

        Assert.Equal(1, Row(ranking, 1).Wins);
        Assert.Equal(0, Row(ranking, 2).Wins);
        Assert.Equal(1, ranking[0].PlayerId);
    }

    [Fact]
    public void Draws_are_not_wins()
    {
        var round = new RoundBuilder(1).Table(winner: null, 1, 2, 3, 4).Build();

        var ranking = StageRankingCalculator.Calculate([round], Names);

        Assert.All(ranking, r =>
        {
            Assert.Equal(0, r.Wins);
            Assert.Equal(1, r.Draws);
            Assert.Equal(1, r.Position);
        });
    }
}

public class StageRulesTests
{
    private static Season Season() => new() { Id = 1, StartDate = new DateOnly(2026, 11, 1), EndDate = new DateOnly(2026, 12, 15) };

    [Fact]
    public void Same_date_reuses_the_stage()
    {
        var season = Season();
        var stages = new List<Stage>();

        var a = StageRules.Resolve(season, stages, new DateOnly(2026, 12, 8));
        var b = StageRules.Resolve(season, stages, new DateOnly(2026, 12, 8));

        Assert.Same(a, b);
        Assert.Single(stages);
        Assert.Equal(1, a.Number);
    }

    [Fact]
    public void Stages_are_numbered_by_date_even_when_created_out_of_order()
    {
        var season = Season();
        var stages = new List<Stage>();

        var dec8 = StageRules.Resolve(season, stages, new DateOnly(2026, 12, 8));
        var nov15 = StageRules.Resolve(season, stages, new DateOnly(2026, 11, 15));
        var dec1 = StageRules.Resolve(season, stages, new DateOnly(2026, 12, 1));

        Assert.Equal((1, 2, 3), (nov15.Number, dec1.Number, dec8.Number));
    }

    [Theory]
    [InlineData(2026, 10, 31)]
    [InlineData(2026, 12, 16)]
    public void Date_must_be_inside_the_season(int y, int m, int d) =>
        Assert.Throws<ValidationException>(() => StageRules.Resolve(Season(), [], new DateOnly(y, m, d)));

    [Fact]
    public void Open_ended_season_accepts_any_later_date()
    {
        var season = Season();
        season.EndDate = null;
        Assert.Equal(1, StageRules.Resolve(season, [], new DateOnly(2027, 3, 1)).Number);
    }
}
