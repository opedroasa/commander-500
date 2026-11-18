using Commander500.Aplicacao.Comum;
using Commander500.Aplicacao.Interfaces;
using Commander500.Aplicacao.Modelos;
using Commander500.Dominio.Comum;
using Commander500.Dominio.Entidades;
using Commander500.Dominio.Enumeradores;
using Commander500.Dominio.Regras;
using Microsoft.EntityFrameworkCore;

namespace Commander500.Aplicacao.Servicos;

/// <summary>Consultas de estatística: comandantes, perfil do jogador e etapas. Tudo calculado a partir das rodadas encerradas.</summary>
public class ServicoEstatisticas(IContextoBanco banco, TimeProvider relogio)
{
    private DateTime Agora => relogio.GetUtcNow().UtcDateTime;

    private Task<List<Rodada>> RodadasEncerradas(int? idTemporada = null) =>
        banco.Rodadas.AsNoTracking()
            .Where(r => r.Situacao == SituacaoRodada.Encerrada && (idTemporada == null || r.IdTemporada == idTemporada))
            .ComResultados()
            .ToListAsync();

    /// <summary>Rodadas de todas as situações (as abertas dizem se a etapa terminou; os cálculos só pontuam as encerradas).</summary>
    private Task<List<Rodada>> TodasRodadas() =>
        banco.Rodadas.AsNoTracking().ComResultados().ToListAsync();

    private Task<Dictionary<int, string>> NomesJogadores() =>
        banco.Jogadores.AsNoTracking().ToDictionaryAsync(j => j.Id, j => j.Apelido == null ? j.Nome : $"{j.Nome} ({j.Apelido})");

    // ------------------------------------------------------------------ Comandantes

    /// <summary>
    /// Ranking de comandantes. Sem temporada: todas as temporadas, incluindo comandantes que só têm deck cadastrado.
    /// Com temporada: só as partidas dela (a contagem de decks continua sendo a de decks cadastrados).
    /// </summary>
    public async Task<IReadOnlyList<EstatisticaComandanteResposta>> Comandantes(int? idTemporada)
    {
        var minimoPartidas = 3;
        if (idTemporada is { } id)
        {
            var temporada = await banco.Temporadas.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id)
                            ?? throw new NaoEncontradoException($"Temporada {id} não encontrada.");
            minimoPartidas = temporada.MinimoPartidasPercentualComandante;
        }

        var rodadas = await RodadasEncerradas(idTemporada);
        var decks = await banco.Decks.AsNoTracking().Include(d => d.Comandantes).ThenInclude(dc => dc.Comandante).ToListAsync();
        var linhas = CalculadoraEstatisticasComandante.Calcular(rodadas, minimoPartidas, decks, incluirSemPartidas: idTemporada is null);

        var comandantes = await banco.Comandantes.AsNoTracking().ToDictionaryAsync(c => c.Id);
        return linhas.Select(l => l.ParaResposta(comandantes)).ToList();
    }

    // ------------------------------------------------------------------ Perfil do jogador

    public async Task<PerfilJogadorResposta> PerfilJogador(int idJogador, int? idTemporada, bool ehAdmin)
    {
        var jogador = await banco.Jogadores.AsNoTracking()
            .Include(j => j.Decks).ThenInclude(d => d.Comandantes).ThenInclude(dc => dc.Comandante)
            .FirstOrDefaultAsync(j => j.Id == idJogador)
            ?? throw new NaoEncontradoException($"Jogador {idJogador} não encontrado.");

        var temporadas = await banco.Temporadas.AsNoTracking().ToListAsync();
        var etapas = await banco.Etapas.AsNoTracking().ToListAsync();
        var todas = await TodasRodadas();
        var rodadas = todas.Where(r => r.Situacao == SituacaoRodada.Encerrada).ToList();
        var nomes = await NomesJogadores();

        IReadOnlyList<LinhaRanking> RankingDa(Temporada t) => CalculadoraRankingTemporada.Calcular(
            t, etapas.Where(e => e.IdTemporada == t.Id), todas.Where(r => r.IdTemporada == t.Id), nomes);

        // Geral: os mesmos contadores do ranking, em todas as rodadas encerradas (pontos são por temporada, então não aparecem aqui).
        var linhaGeral = CalculadoraRanking.Calcular(new Temporada(), rodadas, nomes).FirstOrDefault(l => l.IdJogador == idJogador);

        TemporadaJogadorResposta? naTemporada = null;
        if (idTemporada is { } id && temporadas.FirstOrDefault(t => t.Id == id) is { } temporada)
        {
            var linha = RankingDa(temporada).FirstOrDefault(l => l.IdJogador == idJogador);
            naTemporada = new TemporadaJogadorResposta(temporada.Id, temporada.Nome, linha?.Posicao, linha?.Pontos ?? 0, Estatisticas(linha));
        }

        // Campeão da temporada = 1º lugar (com a final somada) de uma temporada encerrada (inativa).
        var temporadasVencidas = temporadas
            .Where(t => !t.Ativa)
            .Where(t => RankingDa(t).Any(l => l.Posicao == 1 && l.IdJogador == idJogador))
            .OrderByDescending(t => t.DataInicio)
            .Select(t => new ReferenciaTemporadaResposta(t.Id, t.Nome))
            .ToList();

        // Campeão da etapa = posição 1 (empatados dividem) de uma etapa com todas as rodadas encerradas (a final não conta aqui).
        var rodadasPorEtapa = todas.GroupBy(r => r.IdEtapa).ToDictionary(g => g.Key, g => g.ToList());
        var etapasVencidas = etapas
            .Where(e => !e.Final && rodadasPorEtapa.TryGetValue(e.Id, out var rs) && RegrasEtapa.EstaConcluida(rs))
            .Where(e => CalculadoraRankingEtapa.Calcular(rodadasPorEtapa[e.Id], nomes, temporadas.First(t => t.Id == e.IdTemporada))
                .Any(l => l.Posicao == 1 && l.IdJogador == idJogador))
            .OrderByDescending(e => e.Data)
            .Select(e => new ReferenciaEtapaResposta(e.Id, e.IdTemporada, temporadas.First(t => t.Id == e.IdTemporada).Nome, e.Numero, e.Data))
            .ToList();

        var temporadasJogadas = rodadas.Where(r => r.Participacoes.Any(p => p.IdJogador == idJogador)).Select(r => r.IdTemporada).Distinct().Count();

        return new PerfilJogadorResposta(
            jogador.Id, jogador.Nome, jogador.Apelido, jogador.Ativo,
            Estatisticas(linhaGeral), temporadasJogadas, naTemporada, temporadasVencidas, etapasVencidas,
            jogador.Decks.OrderBy(d => d.Nome).Select(d => d.ParaResposta(incluirObservacaoRevisao: ehAdmin)).ToList());
    }

    private static EstatisticasJogadorResposta Estatisticas(LinhaRanking? l) => l is null
        ? new EstatisticasJogadorResposta(0, 0, 0, 0, 0, 0, null)
        : new EstatisticasJogadorResposta(l.Partidas + l.Byes, l.Partidas, l.Vitorias, l.Empates, l.Derrotas, l.Byes, l.PercentualVitoria);

    // ------------------------------------------------------------------ Etapas

    private static bool EstaConcluida(Etapa e) => e.Rodadas.Count > 0 && e.Rodadas.All(r => r.Situacao == SituacaoRodada.Encerrada);

    /// <summary>Etapas da temporada (mais recente primeiro) com os líderes de cada uma.</summary>
    public async Task<IReadOnlyList<EtapaResposta>> Etapas(int idTemporada)
    {
        var temporada = await banco.Temporadas.AsNoTracking().FirstOrDefaultAsync(t => t.Id == idTemporada)
                        ?? throw new NaoEncontradoException($"Temporada {idTemporada} não encontrada.");
        var etapas = await banco.Etapas.AsNoTracking().Where(e => e.IdTemporada == idTemporada).Include(e => e.Rodadas)
            .OrderByDescending(e => e.Final).ThenByDescending(e => e.Data).ToListAsync();
        var rodadas = (await RodadasEncerradas(idTemporada)).ToDictionary(r => r.Id);
        var nomes = await NomesJogadores();

        return etapas.Select(e => ParaResposta(e, temporada, e.Rodadas.Where(r => rodadas.ContainsKey(r.Id)).Select(r => rodadas[r.Id]), nomes)).ToList();
    }

    /// <summary>
    /// Mesa final: sugere os finalistas (top N do ranking) com o deck mais recente de cada um,
    /// para o lojista conferir antes de formar a mesa.
    /// </summary>
    public async Task<IReadOnlyList<FinalistaSugeridoResposta>> SugerirFinalistas(int idTemporada, int quantidade)
    {
        var temporada = await banco.Temporadas.AsNoTracking().FirstOrDefaultAsync(t => t.Id == idTemporada)
                        ?? throw new NaoEncontradoException($"Temporada {idTemporada} não encontrada.");
        var etapas = await banco.Etapas.AsNoTracking().Where(e => e.IdTemporada == idTemporada).ToListAsync();
        var todas = await banco.Rodadas.AsNoTracking().Where(r => r.IdTemporada == idTemporada).ComResultados().ToListAsync();
        var ranking = CalculadoraRankingTemporada.Calcular(temporada, etapas, todas, await NomesJogadores());

        // Deck usado na última rodada da temporada (ou o mais recente cadastrado).
        var ultimoDeck = todas.OrderBy(r => r.Data).ThenBy(r => r.Numero).SelectMany(r => r.Participacoes)
            .GroupBy(p => p.IdJogador).ToDictionary(g => g.Key, g => g.Last().IdDeck);
        return ranking.Take(Math.Clamp(quantidade, 1, 16))
            .Select(l => new FinalistaSugeridoResposta(l.Posicao, l.IdJogador, l.NomeJogador, l.Pontos, l.Empatado, ultimoDeck.GetValueOrDefault(l.IdJogador)))
            .ToList();
    }

    /// <summary>Etapa com o ranking do dia e as rodadas.</summary>
    public async Task<DetalheEtapaResposta> Etapa(int idEtapa)
    {
        var etapa = await banco.Etapas.AsNoTracking().Include(e => e.Temporada).Include(e => e.Rodadas)
                        .FirstOrDefaultAsync(e => e.Id == idEtapa)
                    ?? throw new NaoEncontradoException($"Etapa {idEtapa} não encontrada.");
        var rodadas = await banco.Rodadas.AsNoTrackingWithIdentityResolution().ComTudo()
            .Where(r => r.IdEtapa == idEtapa).OrderBy(r => r.Numero).ToListAsync();
        var nomes = await NomesJogadores();

        var temporada = etapa.Temporada!;
        var porColocacao = temporada.ModoPontuacao == ModoPontuacao.ColocacaoNaEtapa;
        var ranking = CalculadoraRankingEtapa.Calcular(rodadas, nomes, temporada)
            .Select(l => new LinhaEtapaResposta(l, porColocacao ? temporada.PontosDaColocacao(l.Posicao) : null))
            .ToList();
        return new DetalheEtapaResposta(ParaResposta(etapa, temporada, rodadas, nomes), temporada.ModoPontuacao, ranking,
            rodadas.Select(r => r.ParaResposta(Agora)).ToList());
    }

    private static EtapaResposta ParaResposta(Etapa etapa, Temporada temporada, IEnumerable<Rodada> rodadas, IReadOnlyDictionary<int, string> nomes)
    {
        var lideres = CalculadoraRankingEtapa.Calcular(rodadas, nomes, temporada)
            .Where(l => l.Posicao == 1)
            .Select(l => new ReferenciaJogadorResposta(l.IdJogador, l.NomeJogador))
            .ToList();
        return new EtapaResposta(etapa.Id, etapa.IdTemporada, temporada.Nome, etapa.Numero, etapa.Data, etapa.Rodadas.Count,
            etapa.Rodadas.Count(r => r.Situacao == SituacaoRodada.Encerrada), EstaConcluida(etapa), lideres,
            etapa.Final, etapa.RodadasPrevistas);
    }
}
