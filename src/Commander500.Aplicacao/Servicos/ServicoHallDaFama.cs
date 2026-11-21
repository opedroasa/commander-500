using Commander500.Aplicacao.Comum;
using Commander500.Aplicacao.Interfaces;
using Commander500.Aplicacao.Modelos;
using Commander500.Dominio.Regras;
using Microsoft.EntityFrameworkCore;

namespace Commander500.Aplicacao.Servicos;

/// <summary>Hall da Fama: ranking final de todas as temporadas encerradas, com o deck de cada jogador e a foto do top.</summary>
public class ServicoHallDaFama(IContextoBanco banco, IArmazenamentoFotos fotos)
{
    public async Task<IReadOnlyList<TemporadaHallDaFamaResposta>> Listar()
    {
        var temporadas = await banco.Temporadas.AsNoTracking().Where(t => !t.Ativa)
            .OrderByDescending(t => t.DataFim ?? t.DataInicio).ThenByDescending(t => t.Id).ToListAsync();
        if (temporadas.Count == 0) return [];

        var ids = temporadas.Select(t => t.Id).ToList();
        var etapas = await banco.Etapas.AsNoTracking().Where(e => ids.Contains(e.IdTemporada)).ToListAsync();
        var rodadas = await banco.Rodadas.AsNoTracking().Where(r => ids.Contains(r.IdTemporada)).ComResultados().ToListAsync();
        var nomes = await banco.Jogadores.AsNoTracking()
            .ToDictionaryAsync(j => j.Id, j => j.Apelido == null ? j.Nome : $"{j.Nome} ({j.Apelido})");

        // Decks usados nessas temporadas, com os comandantes (para a arte).
        var decks = rodadas.SelectMany(r => r.Participacoes).Where(p => p.Deck is not null)
            .Select(p => p.Deck!).DistinctBy(d => d.Id).ToDictionary(d => d.Id);

        return temporadas.Select(t =>
        {
            var rodadasDaTemporada = rodadas.Where(r => r.IdTemporada == t.Id).ToList();
            var ranking = CalculadoraRankingTemporada.Calcular(t, etapas.Where(e => e.IdTemporada == t.Id), rodadasDaTemporada, nomes);
            var deckDoJogador = RegrasHallDaFama.DeckMaisUsado(rodadasDaTemporada);
            var linhas = ranking.Select(l =>
            {
                var deck = deckDoJogador.TryGetValue(l.IdJogador, out var idDeck) && decks.TryGetValue(idDeck, out var d)
                    ? new DeckHallDaFamaResposta(d.Id, d.Nome,
                        d.Comandantes.Select(dc => dc.Comandante!).OrderBy(c => c.Nome).Select(c => c.ParaResposta()).ToList())
                    : null;
                return new LinhaHallDaFamaResposta(l.Posicao, l.IdJogador, l.NomeJogador, l.Pontos, l.Empatado, deck);
            }).ToList();
            return new TemporadaHallDaFamaResposta(t.Id, t.Nome, t.DataInicio, t.DataFim, t.ModoPontuacao,
                fotos.Url(t.CaminhoFoto), t.LegendaFoto, linhas);
        }).ToList();
    }
}
