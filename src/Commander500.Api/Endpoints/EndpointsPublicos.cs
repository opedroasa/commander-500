using System.Security.Claims;
using Commander500.Aplicacao.Servicos;
using Commander500.Infraestrutura.Seguranca;

namespace Commander500.Api.Endpoints;

/// <summary>Endpoints do site público (sem login).</summary>
public static class EndpointsPublicos
{
    public static void MapearEndpointsPublicos(this IEndpointRouteBuilder app)
    {
        app.MapGet("/saude", () => Results.Ok(new { situacao = "ok" })).WithTags("Saúde").AllowAnonymous()
            .WithSummary("Usado pelo ping que mantém a Api acordada no Render.");

        var temporadas = app.MapGroup("/api/temporadas").WithTags("Público · Temporadas").AllowAnonymous();
        temporadas.MapGet("/", (ServicoTemporada s) => s.Listar());
        temporadas.MapGet("/{id:int}", (int id, ServicoTemporada s) => s.Obter(id));
        temporadas.MapGet("/{id:int}/ranking", (int id, ServicoTemporada s) => s.Ranking(id));
        temporadas.MapGet("/{id:int}/comandantes", (int id, ServicoEstatisticas s) => s.Comandantes(id));
        temporadas.MapGet("/{id:int}/rodadas", (int id, ServicoTemporada s) => s.Rodadas(id));
        temporadas.MapGet("/{id:int}/etapas", (int id, ServicoEstatisticas s) => s.Etapas(id))
            .WithSummary("Etapas da temporada (mais recente primeiro) com o(s) líder(es) de cada uma.");

        app.MapGet("/api/etapas/{id:int}", (int id, ServicoEstatisticas s) => s.Etapa(id))
            .WithTags("Público · Etapas").AllowAnonymous()
            .WithSummary("Etapa: ranking do dia (vitórias, bye, confronto direto) e rodadas.");

        app.MapGet("/api/comandantes/estatisticas", (int? idTemporada, ServicoEstatisticas s) => s.Comandantes(idTemporada))
            .WithTags("Público · Comandantes").AllowAnonymous()
            .WithSummary("Ranking de comandantes (todas as temporadas ou uma) com arte do Scryfall e nº de decks.");

        app.MapGet("/api/jogadores/{id:int}/perfil", (int id, int? idTemporada, ServicoEstatisticas s, ClaimsPrincipal usuario) =>
                s.PerfilJogador(id, idTemporada, usuario.IsInRole(GeradorTokenJwt.PapelAdmin)))
            .WithTags("Público · Jogadores").AllowAnonymous()
            .WithSummary("Mini perfil: estatísticas gerais e da temporada, temporadas/etapas vencidas e decks.");

        var rodadas = app.MapGroup("/api/rodadas").WithTags("Público · Rodadas").AllowAnonymous();
        rodadas.MapGet("/atuais", (ServicoRodada s) => s.ListarAtuais())
            .WithSummary("Rodadas sorteadas/em andamento de todas as temporadas ativas.");
        rodadas.MapGet("/{id:int}", (int id, ServicoRodada s) => s.Obter(id));
    }
}
