using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Commander500.Api.Application;
using Commander500.Api.Auth;
using Commander500.Api.Contracts;
using Commander500.Api.Domain;
using Commander500.Api.Domain.Services;

namespace Commander500.Api.Endpoints;

public static class AdminEndpoints
{
    public const string AdminPolicy = "Admin";

    public static void MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/login", async (LoginRequest req, AuthService auth) =>
                await auth.Login(req) is { } res ? Results.Ok(res) : Results.Problem("E-mail ou senha inválidos.", statusCode: 401))
            .WithTags("Auth").AllowAnonymous();

        var api = app.MapGroup("/api").RequireAuthorization(AdminPolicy);

        // ---- Players
        var players = api.MapGroup("/players").WithTags("Admin · Players");
        players.MapGet("/", (CatalogService s) => s.Players());
        players.MapGet("/{id:int}", (int id, CatalogService s) => s.Player(id));
        players.MapPost("/", async (PlayerUpsertRequest req, CatalogService s) => Created("players", await s.CreatePlayer(req), p => p.Id));
        players.MapPut("/{id:int}", (int id, PlayerUpsertRequest req, CatalogService s) => s.UpdatePlayer(id, req));
        players.MapDelete("/{id:int}", async (int id, CatalogService s) => { await s.DeletePlayer(id); return Results.NoContent(); });

        // ---- Commanders
        var commanders = api.MapGroup("/commanders").WithTags("Admin · Commanders");
        commanders.MapGet("/", (string? search, CatalogService s) => s.Commanders(search));
        commanders.MapPost("/", async (CommanderUpsertRequest req, CatalogService s) => Created("commanders", await s.CreateCommander(req), c => c.Id));
        commanders.MapPut("/{id:int}", (int id, CommanderUpsertRequest req, CatalogService s) => s.UpdateCommander(id, req));
        commanders.MapDelete("/{id:int}", async (int id, CatalogService s) => { await s.DeleteCommander(id); return Results.NoContent(); });

        // ---- Decks
        var decks = api.MapGroup("/decks").WithTags("Admin · Decks");
        decks.MapGet("/", (int? playerId, DeckStatus? status, CatalogService s) => s.Decks(playerId, status));
        decks.MapGet("/{id:int}", (int id, CatalogService s) => s.Deck(id));
        decks.MapPost("/", async (DeckUpsertRequest req, CatalogService s) => Created("decks", await s.CreateDeck(req), d => d.Id));
        decks.MapPut("/{id:int}", (int id, DeckUpsertRequest req, CatalogService s) => s.UpdateDeck(id, req));
        decks.MapPatch("/{id:int}/review", (int id, DeckReviewRequest req, CatalogService s) => s.ReviewDeck(id, req));
        decks.MapDelete("/{id:int}", async (int id, CatalogService s) => { await s.DeleteDeck(id); return Results.NoContent(); });

        // ---- Seasons
        var seasons = api.MapGroup("/seasons").WithTags("Admin · Seasons");
        seasons.MapPost("/", async (SeasonUpsertRequest req, SeasonService s) => Created("seasons", await s.Create(req), x => x.Id));
        seasons.MapPut("/{id:int}", (int id, SeasonUpsertRequest req, SeasonService s) => s.Update(id, req));
        seasons.MapPut("/{id:int}/config", (int id, SeasonConfig cfg, SeasonService s) => s.UpdateConfig(id, cfg))
            .WithSummary("Pontuação, bye, timer, desempate. Bloqueado após a 1ª rodada encerrada.");
        seasons.MapPost("/{id:int}/end", (int id, SeasonService s) => s.End(id));
        seasons.MapDelete("/{id:int}", async (int id, SeasonService s) => { await s.Delete(id); return Results.NoContent(); });

        // ---- Round flow
        seasons.MapPost("/{id:int}/rounds", async (int id, CreateRoundRequest? req, RoundService s) =>
            Created("rounds", await s.Create(id, req ?? new CreateRoundRequest(null, null)), r => r.Id)).WithTags("Admin · Rounds");

        var rounds = api.MapGroup("/rounds").WithTags("Admin · Rounds");
        rounds.MapPut("/{id:int}/entries", (int id, List<EntryInput> entries, RoundService s) => s.SetEntries(id, entries))
            .WithSummary("Presentes e deck de cada um (Rascunho/Sorteada; volta para Rascunho).");
        rounds.MapPost("/{id:int}/draw", (int id, RoundService s) => s.Draw(id)).WithSummary("Sorteio / re-sorteio.");
        rounds.MapPost("/{id:int}/start", (int id, StartRoundRequest? req, RoundService s) => s.Start(id, req?.DurationMinutes));
        rounds.MapPost("/{id:int}/close", (int id, RoundService s) => s.Close(id));
        rounds.MapDelete("/{id:int}", async (int id, RoundService s) => { await s.Delete(id); return Results.NoContent(); });

        api.MapPut("/tables/{id:int}/result", (int id, TableResultRequest req, RoundService s, ClaimsPrincipal user) =>
                s.SetTableResult(id, req, user.FindFirstValue(JwtRegisteredClaimNames.Email) ?? user.FindFirstValue(ClaimTypes.Email)))
            .WithTags("Admin · Rounds");
    }

    private static IResult Created<T>(string resource, T body, Func<T, int> id) =>
        Results.Created($"/api/{resource}/{id(body)}", body);
}
