using Commander500.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Commander500.Aplicacao.Comum;

public static class ConsultasRodada
{
    /// <summary>
    /// Rodada com tudo o que as telas de rodada precisam (temporada, etapa, jogadores, decks, comandantes, mesas).
    /// Tem um ciclo (Rodada → Etapa → Rodadas): consultas só de leitura devem usar AsNoTrackingWithIdentityResolution.
    /// </summary>
    public static IQueryable<Rodada> ComTudo(this IQueryable<Rodada> consulta) => consulta
        .Include(r => r.Temporada)
        .Include(r => r.Etapa).ThenInclude(e => e!.Rodadas)
        .Include(r => r.Participacoes).ThenInclude(p => p.Jogador)
        .Include(r => r.Participacoes).ThenInclude(p => p.Deck).ThenInclude(d => d!.Comandantes).ThenInclude(dc => dc.Comandante)
        .Include(r => r.Mesas).ThenInclude(m => m.Cadeiras)
        .AsSplitQuery();

    /// <summary>Rodadas encerradas com participações, decks/comandantes e mesas (para rankings e estatísticas).</summary>
    public static IQueryable<Rodada> ComResultados(this IQueryable<Rodada> consulta) => consulta
        .Include(r => r.Participacoes).ThenInclude(p => p.Deck).ThenInclude(d => d!.Comandantes).ThenInclude(dc => dc.Comandante)
        .Include(r => r.Mesas).ThenInclude(m => m.Cadeiras)
        .AsSplitQuery();
}
