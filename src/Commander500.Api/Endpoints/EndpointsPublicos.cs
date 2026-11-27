using System.Security.Claims;
using Commander500.Aplicacao.Servicos;
using Commander500.Aplicacao.Interfaces;
using Commander500.Infraestrutura.Fotos;
using Commander500.Infraestrutura.Seguranca;

namespace Commander500.Api.Endpoints;

/// <summary>Endpoints do site público (sem login).</summary>
public static class EndpointsPublicos
{
    public static void MapearEndpointsPublicos(this IEndpointRouteBuilder app)
    {
        app.MapGet("/saude", () => Results.Ok(new { situacao = "ok" })).WithTags("Saúde").AllowAnonymous()
            .WithSummary("Diz se a Api está no ar (sem consultar o banco). Útil para testar a publicação e para pings de monitoramento.");

        var temporadas = app.MapGroup("/api/temporadas").WithTags("Público · Temporadas").AllowAnonymous();
        temporadas.MapGet("/", (ServicoTemporada s) => s.Listar());
        temporadas.MapGet("/{id:int}", (int id, ServicoTemporada s) => s.Obter(id));
        temporadas.MapGet("/{id:int}/ranking", (int id, ServicoTemporada s) => s.Ranking(id));
        temporadas.MapGet("/{id:int}/comandantes", (int id, ServicoEstatisticas s) => s.Comandantes(id));
        temporadas.MapGet("/{id:int}/rodadas", (int id, ServicoTemporada s) => s.Rodadas(id));
        temporadas.MapGet("/{id:int}/etapas", (int id, ServicoEstatisticas s) => s.Etapas(id))
            .WithSummary("Etapas da temporada (mais recente primeiro) com o(s) líder(es) de cada uma.");

        app.MapGet("/api/regulamento", (ServicoRegulamento s) => s.Obter())
            .WithTags("Público · Regulamento").AllowAnonymous()
            .WithSummary("Regulamento da liga (HTML do editor).");

        app.MapGet("/api/configuracoes-loja", (ServicoConfiguracaoLoja s) => s.Obter())
            .WithTags("Público · Loja").AllowAnonymous()
            .WithSummary("Link do grupo de WhatsApp do Commander 500.");

        // ----- Lista pública de jogadores e páginas completas do jogador e do comandante
        var paginas = app.MapGroup("/api/estatisticas").WithTags("Público · Jogadores e comandantes").AllowAnonymous();
        paginas.MapGet("/jogadores", (ServicoPaginas s) => s.ListarJogadores())
            .WithSummary("Jogadores ativos ou que já jogaram, com resumo (títulos, vitórias, deck principal).");
        paginas.MapGet("/jogadores/{id:int}", (int id, ServicoPaginas s) => s.PaginaJogador(id))
            .WithSummary("Página do jogador: temporadas, títulos, decks, evolução mês a mês, nêmesis e confrontos.");
        paginas.MapGet("/comandantes/{chave}", (string chave, ServicoPaginas s) => s.PaginaComandante(chave))
            .WithSummary("Página do comandante pela chave (ids em ordem, ex.: 12-34 para dupla): números, títulos, evolução e listas.");

        app.MapGet("/api/hall-da-fama", (ServicoHallDaFama s) => s.Listar())
            .WithTags("Público · Hall da Fama").AllowAnonymous()
            .WithSummary("Ranking final das temporadas encerradas, com o deck mais usado de cada jogador e a foto do top.");

        // Fotos guardadas na pasta local (desenvolvimento). Com o Supabase configurado, as fotos vêm direto de lá.
        app.MapGet("/api/fotos/{**caminho}", (string caminho, IArmazenamentoFotos armazenamento) =>
            {
                if (armazenamento is not ArmazenamentoFotosLocal local) return Results.NotFound();
                var arquivo = local.CaminhoNoDisco(caminho);
                if (!File.Exists(arquivo)) return Results.NotFound();
                var tipo = Path.GetExtension(arquivo).ToLowerInvariant() switch
                {
                    ".png" => "image/png",
                    ".webp" => "image/webp",
                    _ => "image/jpeg",
                };
                return Results.File(arquivo, tipo);
            })
            .ExcludeFromDescription().AllowAnonymous();

        app.MapGet("/api/etapas/{id:int}", (int id, ServicoEstatisticas s) => s.Etapa(id))
            .WithTags("Público · Etapas").AllowAnonymous()
            .WithSummary("Etapa: ranking do dia (pontos, bye, confronto direto), pontos na temporada e rodadas.");

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
