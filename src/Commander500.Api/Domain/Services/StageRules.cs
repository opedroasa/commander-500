namespace Commander500.Api.Domain.Services;

/// <summary>Etapas are created automatically: one per date inside a season, numbered by date.</summary>
public static class StageRules
{
    /// <summary>Returns the season's stage for <paramref name="date"/>, adding a new one to <paramref name="stages"/> if needed.</summary>
    public static Stage Resolve(Season season, List<Stage> stages, DateOnly date)
    {
        if (date < season.StartDate || (season.EndDate is { } end && date > end))
            throw new ValidationException($"A data deve estar dentro da temporada ({season.StartDate:dd/MM/yyyy}" +
                                          (season.EndDate is { } e ? $" a {e:dd/MM/yyyy})." : " em diante)."));

        var stage = stages.FirstOrDefault(s => s.Date == date);
        if (stage is null)
        {
            stage = new Stage { Season = season, SeasonId = season.Id, Date = date };
            stages.Add(stage);
            Renumber(stages);
        }
        return stage;
    }

    /// <summary>Numbers stages 1..N by date.</summary>
    public static void Renumber(IEnumerable<Stage> stages)
    {
        var n = 1;
        foreach (var s in stages.OrderBy(s => s.Date)) s.Number = n++;
    }
}
