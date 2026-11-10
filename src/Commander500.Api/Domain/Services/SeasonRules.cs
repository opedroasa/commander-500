namespace Commander500.Api.Domain.Services;

public record SeasonConfig(
    int PointsWin, int PointsDraw,
    int PointsBye1, int PointsBye2, int PointsBye3,
    int DefaultRoundMinutes, int ExtraTurnsAfterTime,
    int MinMatchesForCommanderWinRate,
    IReadOnlyList<TiebreakCriterion> Tiebreakers)
{
    public static SeasonConfig From(Season s) => new(
        s.PointsWin, s.PointsDraw, s.PointsBye1, s.PointsBye2, s.PointsBye3,
        s.DefaultRoundMinutes, s.ExtraTurnsAfterTime, s.MinMatchesForCommanderWinRate, s.GetTiebreakers());
}

public static class SeasonRules
{
    /// <summary>Applies scoring config. Rejected once the season's first round is closed (spec 4.1).</summary>
    public static void ApplyConfig(Season season, SeasonConfig config)
    {
        if (season.ConfigLocked)
            throw new DomainException("Configuração travada: a primeira rodada da temporada já foi encerrada.");

        int[] points = [config.PointsWin, config.PointsDraw, config.PointsBye1, config.PointsBye2, config.PointsBye3];
        if (points.Any(p => p is < 0 or > 100))
            throw new ValidationException("Pontos devem estar entre 0 e 100.");
        if (config.DefaultRoundMinutes is < 1 or > RoundWorkflow.MaxDurationMinutes)
            throw new ValidationException($"Duração padrão deve estar entre 1 e {RoundWorkflow.MaxDurationMinutes} minutos.");
        if (config.ExtraTurnsAfterTime is < 0 or > 20)
            throw new ValidationException("Turnos extras devem estar entre 0 e 20.");
        if (config.MinMatchesForCommanderWinRate is < 1 or > 100)
            throw new ValidationException("Mínimo de partidas para % de vitória deve estar entre 1 e 100.");
        var tiebreakers = config.Tiebreakers ?? [];
        if (tiebreakers.Distinct().Count() != tiebreakers.Count)
            throw new ValidationException("Critérios de desempate repetidos.");
        if (tiebreakers.Contains(TiebreakCriterion.FewerMatches) && tiebreakers.Contains(TiebreakCriterion.MoreMatches))
            throw new ValidationException("Use apenas um entre 'menos partidas' e 'mais partidas'.");

        season.PointsWin = config.PointsWin;
        season.PointsDraw = config.PointsDraw;
        season.PointsBye1 = config.PointsBye1;
        season.PointsBye2 = config.PointsBye2;
        season.PointsBye3 = config.PointsBye3;
        season.DefaultRoundMinutes = config.DefaultRoundMinutes;
        season.ExtraTurnsAfterTime = config.ExtraTurnsAfterTime;
        season.MinMatchesForCommanderWinRate = config.MinMatchesForCommanderWinRate;
        season.Tiebreakers = string.Join(",", tiebreakers);
    }
}
