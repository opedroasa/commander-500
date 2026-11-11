using System.Security.Claims;
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
        seasons.MapGet("/{id:int}/commanders", (int id, StatsService s) => s.Commanders(id));
        seasons.MapGet("/{id:int}/rounds", (int id, SeasonService s) => s.Rounds(id));
        seasons.MapGet("/{id:int}/stages", (int id, StatsService s) => s.Stages(id))
            .WithSummary("Etapas da temporada (mais recente primeiro) com o(s) líder(es) de cada uma.");

        app.MapGet("/api/stages/{id:int}", (int id, StatsService s) => s.Stage(id))
            .WithTags("Public · Stages").AllowAnonymous()
            .WithSummary("Etapa: ranking do dia (vitórias, bye, confronto direto) e rodadas.");

        app.MapGet("/api/commanders/stats", (int? seasonId, StatsService s) => s.Commanders(seasonId))
            .WithTags("Public · Commanders").AllowAnonymous()
            .WithSummary("Ranking de comandantes (todas as temporadas ou uma) com arte do Scryfall e nº de decks.");

        app.MapGet("/api/players/{id:int}/profile", (int id, int? seasonId, StatsService s, ClaimsPrincipal user) =>
                s.PlayerProfile(id, seasonId, user.IsInRole("Admin")))
            .WithTags("Public · Players").AllowAnonymous()
            .WithSummary("Mini perfil: estatísticas gerais e da temporada, temporadas/etapas vencidas e decks.");

        var rounds = app.MapGroup("/api/rounds").WithTags("Public · Rounds").AllowAnonymous();
        rounds.MapGet("/current", (RoundService s) => s.Current())
            .WithSummary("Rodadas sorteadas/em andamento de todas as temporadas ativas.");
        rounds.MapGet("/{id:int}", (int id, RoundService s) => s.Get(id));
    }
}
