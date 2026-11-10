using Commander500.Api.Domain;
using Commander500.Api.Domain.Services;
using Commander500.Api.Infrastructure;

namespace Commander500.Tests;

public class SeasonRulesTests
{
    private static SeasonConfig Config(int win = 3) => new(win, 1, 3, 3, 3, 60, 0, 3, [TiebreakCriterion.Wins, TiebreakCriterion.FewerMatches]);

    [Fact]
    public void Defaults_match_the_spec()
    {
        var s = new Season();
        Assert.Equal((3, 1, 3, 3, 3, 60), (s.PointsWin, s.PointsDraw, s.PointsBye1, s.PointsBye2, s.PointsBye3, s.DefaultRoundMinutes));
        Assert.Equal([TiebreakCriterion.Wins, TiebreakCriterion.FewerMatches], s.GetTiebreakers());
        Assert.Equal(3, s.MinMatchesForCommanderWinRate);
    }

    [Fact]
    public void Config_can_be_edited_until_locked()
    {
        var season = new Season();
        SeasonRules.ApplyConfig(season, Config(win: 4));
        Assert.Equal(4, season.PointsWin);

        season.ConfigLocked = true;
        Assert.Throws<DomainException>(() => SeasonRules.ApplyConfig(season, Config(win: 5)));
        Assert.Equal(4, season.PointsWin);
    }

    [Fact]
    public void Invalid_config_is_rejected()
    {
        var season = new Season();
        Assert.Throws<ValidationException>(() => SeasonRules.ApplyConfig(season, Config(win: -1)));
        Assert.Throws<ValidationException>(() => SeasonRules.ApplyConfig(season,
            Config() with { Tiebreakers = [TiebreakCriterion.FewerMatches, TiebreakCriterion.MoreMatches] }));
        Assert.Throws<ValidationException>(() => SeasonRules.ApplyConfig(season, Config() with { DefaultRoundMinutes = 0 }));
    }

    [Fact]
    public void Unknown_tiebreaker_string_is_rejected() =>
        Assert.Throws<DomainException>(() => Season.ParseTiebreakers("Wins,Luck"));
}

public class CommanderNameTests
{
    [Theory]
    [InlineData("Atraxa, Praetors' Voice", "  atraxa,   PRAETORS’ voice ")]
    [InlineData("Éowyn, Fearless Knight", "eowyn, fearless knight")]
    public void Equivalent_spellings_share_the_same_key(string a, string b) =>
        Assert.Equal(CommanderName.Normalize(a), CommanderName.Normalize(b));

    [Fact]
    public void Clean_keeps_display_case_but_trims_spaces() =>
        Assert.Equal("The Ur-Dragon", CommanderName.Clean("  The   Ur-Dragon "));
}

public class ConnectionStringTests
{
    [Fact]
    public void Postgres_uri_is_converted()
    {
        var cs = ConnectionStrings.Normalize("postgresql://user:p%40ss@ep-x.neon.tech/db?sslmode=require");
        Assert.Contains("Host=ep-x.neon.tech", cs);
        Assert.Contains("Database=db", cs);
        Assert.Contains("Username=user", cs);
        Assert.Contains("Password=p@ss", cs);
        Assert.Contains("SSL Mode=Require", cs);
    }

    [Fact]
    public void Key_value_string_is_kept() =>
        Assert.Equal("Host=localhost;Database=x", ConnectionStrings.Normalize("Host=localhost;Database=x"));
}
