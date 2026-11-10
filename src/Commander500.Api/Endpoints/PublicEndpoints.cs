using Commander500.Api.Application;

namespace Commander500.Api.Endpoints;

public static class PublicEndpoints
{
    public static void MapPublicEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "ok" })).WithTags("Health").AllowAnonymous();

        var seasons = app.MapGroup("/api/seasons").WithTags("Public · Seasons").AllowAnonymous();
        seasons.MapGet("/", (SeasonService s) => s.List());
        seasons.MapGet("/{id:int}", (int id, SeasonService s) => s.Get(id));
        seasons.MapGet("/{id:int}/ranking", (int id, SeasonService s) => s.Ranking(id));
        seasons.MapGet("/{id:int}/commanders", (int id, SeasonService s) => s.Commanders(id));
        seasons.MapGet("/{id:int}/rounds", (int id, SeasonService s) => s.Rounds(id));

        var rounds = app.MapGroup("/api/rounds").WithTags("Public · Rounds").AllowAnonymous();
        rounds.MapGet("/current", (RoundService s) => s.Current())
            .WithSummary("Rodadas sorteadas/em andamento de todas as temporadas ativas.");
        rounds.MapGet("/{id:int}", (int id, RoundService s) => s.Get(id));
    }
}
