using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Commander500.Aplicacao.Modelos;
using Commander500.Aplicacao.Servicos;
using Commander500.Dominio.Enumeradores;
using Commander500.Dominio.Regras;

namespace Commander500.Api.Endpoints;

/// <summary>Endpoints da área do lojista. Todos exigem o token JWT (política "Admin"), menos o login.</summary>
public static class EndpointsAdmin
{
    public const string PoliticaAdmin = "Admin";

    public static void MapearEndpointsAdmin(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/autenticacao/login", async (LoginEnvio envio, ServicoAutenticacao s) =>
                await s.Entrar(envio) is { } resposta ? Results.Ok(resposta) : Results.Problem("E-mail ou senha inválidos.", statusCode: 401))
            .WithTags("Autenticação").AllowAnonymous();

        var api = app.MapGroup("/api").RequireAuthorization(PoliticaAdmin);

        // ----- Jogadores
        var jogadores = api.MapGroup("/jogadores").WithTags("Admin · Jogadores");
        jogadores.MapGet("/", (ServicoJogador s) => s.Listar());
        jogadores.MapGet("/{id:int}", (int id, ServicoJogador s) => s.Obter(id));
        jogadores.MapPost("/", async (JogadorEnvio envio, ServicoJogador s) => Criado("jogadores", await s.Criar(envio), j => j.Id));
        jogadores.MapPut("/{id:int}", (int id, JogadorEnvio envio, ServicoJogador s) => s.Alterar(id, envio));
        jogadores.MapDelete("/{id:int}", async (int id, ServicoJogador s) => { await s.Excluir(id); return Results.NoContent(); });

        // ----- Campo de comandante (busca no Scryfall)
        api.MapGet("/scryfall/comandantes", (string? busca, ServicoComandante s) => s.Pesquisar(busca))
            .WithTags("Admin · Decks")
            .WithSummary("Busca comandantes/backgrounds no Scryfall pelo nome (mín. 2 letras).");

        // ----- Decks
        var decks = api.MapGroup("/decks").WithTags("Admin · Decks");
        decks.MapGet("/", (int? idJogador, SituacaoDeck? situacao, ServicoDeck s) => s.Listar(idJogador, situacao));
        decks.MapGet("/{id:int}", (int id, ServicoDeck s) => s.Obter(id));
        decks.MapPost("/", async (DeckEnvio envio, ServicoDeck s) => Criado("decks", await s.Criar(envio), d => d.Id));
        decks.MapPut("/{id:int}", (int id, DeckEnvio envio, ServicoDeck s) => s.Alterar(id, envio));
        decks.MapPatch("/{id:int}/revisao", (int id, RevisaoDeckEnvio envio, ServicoDeck s) => s.Revisar(id, envio));
        decks.MapDelete("/{id:int}", async (int id, ServicoDeck s) => { await s.Excluir(id); return Results.NoContent(); });

        // ----- Temporadas
        var temporadas = api.MapGroup("/temporadas").WithTags("Admin · Temporadas");
        temporadas.MapPost("/", async (TemporadaEnvio envio, ServicoTemporada s) => Criado("temporadas", await s.Criar(envio), t => t.Id));
        temporadas.MapPut("/{id:int}", (int id, TemporadaEnvio envio, ServicoTemporada s) => s.Alterar(id, envio));
        temporadas.MapPut("/{id:int}/configuracao", (int id, ConfiguracaoTemporada configuracao, ServicoTemporada s) => s.AlterarConfiguracao(id, configuracao))
            .WithSummary("Pontuação, bye, timer, desempate. Bloqueado após a 1ª rodada encerrada.");
        temporadas.MapPost("/{id:int}/encerrar", (int id, ServicoTemporada s) => s.Encerrar(id));
        temporadas.MapDelete("/{id:int}", async (int id, ServicoTemporada s) => { await s.Excluir(id); return Results.NoContent(); });

        // ----- Condução da rodada
        temporadas.MapPost("/{id:int}/rodadas", async (int id, NovaRodadaEnvio? envio, ServicoRodada s) =>
            Criado("rodadas", await s.Criar(id, envio ?? new NovaRodadaEnvio(null, null)), r => r.Id))
            .WithTags("Admin · Rodadas")
            .WithSummary("Cria a rodada na etapa da data (cria a etapa se for uma data nova).");

        var rodadas = api.MapGroup("/rodadas").WithTags("Admin · Rodadas");
        rodadas.MapPut("/{id:int}/participantes", (int id, List<ParticipanteInformado> participantes, ServicoRodada s) => s.DefinirParticipantes(id, participantes))
            .WithSummary("Presentes e deck de cada um (Rascunho/Sorteada; volta para Rascunho).");
        rodadas.MapPost("/{id:int}/sortear", (int id, ServicoRodada s) => s.Sortear(id)).WithSummary("Sorteio / re-sorteio.");
        rodadas.MapPost("/{id:int}/iniciar", (int id, IniciarRodadaEnvio? envio, ServicoRodada s) => s.Iniciar(id, envio?.DuracaoMinutos));
        rodadas.MapPost("/{id:int}/encerrar", (int id, ServicoRodada s) => s.Encerrar(id));
        rodadas.MapDelete("/{id:int}", async (int id, ServicoRodada s) => { await s.Excluir(id); return Results.NoContent(); });

        api.MapPut("/mesas/{id:int}/resultado", (int id, ResultadoMesaEnvio envio, ServicoRodada s, ClaimsPrincipal usuario) =>
                s.RegistrarResultadoMesa(id, envio, usuario.FindFirstValue(JwtRegisteredClaimNames.Email) ?? usuario.FindFirstValue(ClaimTypes.Email)))
            .WithTags("Admin · Rodadas");
    }

    /// <summary>201 Created com o endereço do registro novo.</summary>
    private static IResult Criado<T>(string recurso, T corpo, Func<T, int> obterId) =>
        Results.Created($"/api/{recurso}/{obterId(corpo)}", corpo);
}
