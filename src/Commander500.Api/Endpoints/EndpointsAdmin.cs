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
        temporadas.MapPut("/{id:int}/proxima-etapa", (int id, ProximaEtapaEnvio envio, ServicoTemporada s) => s.DefinirProximaEtapa(id, envio))
            .WithSummary("Data/hora da próxima etapa e prazo das listas (aviso da página inicial).");
        temporadas.MapPost("/{id:int}/encerrar", (int id, ServicoTemporada s) => s.Encerrar(id));
        temporadas.MapDelete("/{id:int}", async (int id, ServicoTemporada s) => { await s.Excluir(id); return Results.NoContent(); });

        // ----- Condução da rodada
        temporadas.MapPost("/{id:int}/rodadas", async (int id, NovaRodadaEnvio? envio, ServicoRodada s) =>
            Criado("rodadas", await s.Criar(id, envio ?? new NovaRodadaEnvio(null, null)), r => r.Id))
            .WithTags("Admin · Rodadas")
            .WithSummary("Cria a rodada na etapa da data (cria a etapa se for uma data nova; data passada = retroativa) ou na etapa informada.");
        temporadas.MapGet("/{id:int}/finalistas", (int id, int? quantidade, ServicoEstatisticas s) => s.SugerirFinalistas(id, quantidade ?? 4))
            .WithTags("Admin · Rodadas")
            .WithSummary("Top N do ranking com o último deck de cada um (sugestão para a mesa final).");
        temporadas.MapPost("/{id:int}/mesa-final", async (int id, MesaFinalEnvio envio, ServicoRodada s) =>
                Criado("rodadas", await s.CriarMesaFinal(id, envio), r => r.Id))
            .WithTags("Admin · Rodadas")
            .WithSummary("Forma a mesa final (modo por colocação): cria a etapa final e a 1ª rodada com os finalistas.");

        var rodadas = api.MapGroup("/rodadas").WithTags("Admin · Rodadas");
        rodadas.MapPut("/{id:int}/participantes", (int id, List<ParticipanteInformado> participantes, ServicoRodada s) => s.DefinirParticipantes(id, participantes))
            .WithSummary("Presentes e deck de cada um (Rascunho/Sorteada; volta para Rascunho).");
        rodadas.MapPost("/{id:int}/sortear", (int id, ServicoRodada s) => s.Sortear(id)).WithSummary("Sorteio / re-sorteio.");
        rodadas.MapPut("/{id:int}/mesas", (int id, MontagemMesasEnvio envio, ServicoRodada s) => s.MontarMesas(id, envio))
            .WithSummary("Monta as mesas à mão (quem ficar de fora é bye).");
        rodadas.MapPost("/{id:int}/iniciar", (int id, IniciarRodadaEnvio? envio, ServicoRodada s) => s.Iniciar(id, envio?.DuracaoMinutos));
        rodadas.MapPost("/{id:int}/encerrar", (int id, ServicoRodada s) => s.Encerrar(id));
        rodadas.MapDelete("/{id:int}", async (int id, ServicoRodada s) => { await s.Excluir(id); return Results.NoContent(); });

        api.MapPut("/mesas/{id:int}/resultado", (int id, ResultadoMesaEnvio envio, ServicoRodada s, ClaimsPrincipal usuario) =>
                s.RegistrarResultadoMesa(id, envio, usuario.FindFirstValue(JwtRegisteredClaimNames.Email) ?? usuario.FindFirstValue(ClaimTypes.Email)))
            .WithTags("Admin · Rodadas");
        api.MapPut("/mesas/{id:int}/quem-comeca", (int id, QuemComecaEnvio envio, ServicoRodada s) => s.DefinirQuemComeca(id, envio))
            .WithTags("Admin · Rodadas")
            .WithSummary("Troca quem começa jogando (vai para a cadeira 1; os outros giram junto).");

        api.MapPut("/regulamento", (RegulamentoEnvio envio, ServicoRegulamento s, ClaimsPrincipal usuario) =>
                s.Salvar(envio, usuario.FindFirstValue(JwtRegisteredClaimNames.Email) ?? usuario.FindFirstValue(ClaimTypes.Email)))
            .WithTags("Admin · Regulamento");
    }

    /// <summary>201 Created com o endereço do registro novo.</summary>
    private static IResult Criado<T>(string recurso, T corpo, Func<T, int> obterId) =>
        Results.Created($"/api/{recurso}/{obterId(corpo)}", corpo);
}
