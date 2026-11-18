using System.Globalization;
using Commander500.Dominio.Entidades;
using Commander500.Dominio.Enumeradores;

namespace Commander500.Dominio.Regras;

/// <summary>
/// Ranking da temporada nos dois modos de pontuação. Nada é gravado: tudo sai das rodadas encerradas.
/// <list type="bullet">
/// <item><see cref="ModoPontuacao.SomaDasRodadas"/>: soma dos pontos das rodadas (<see cref="CalculadoraRanking"/>).</item>
/// <item><see cref="ModoPontuacao.ColocacaoNaEtapa"/> (padrão, Art. 5º do tratado): em cada etapa concluída o jogador ganha
/// os pontos da colocação (tabela da temporada; empatados ganham os da melhor posição). A mesa final soma do mesmo jeito.
/// Desempate (Art. 6º): mais 1º lugares → mais etapas disputadas → melhor colocação na última etapa que os empatados
/// disputaram juntos → persistindo, mesma posição (a loja desempata).</item>
/// </list>
/// </summary>
public static class CalculadoraRankingTemporada
{
    private static readonly StringComparer ComparadorNome = StringComparer.Create(new CultureInfo("pt-BR"), ignoreCase: true);

    /// <param name="etapas">Etapas da temporada (inclusive a final, se houver).</param>
    /// <param name="rodadas">Todas as rodadas da temporada (as abertas servem para saber se a etapa terminou).</param>
    public static IReadOnlyList<LinhaRanking> Calcular(Temporada temporada, IEnumerable<Etapa> etapas, IEnumerable<Rodada> rodadas,
        IReadOnlyDictionary<int, string> nomesJogadores)
    {
        var todas = rodadas.ToList();
        var encerradas = todas.Where(r => r.Situacao == SituacaoRodada.Encerrada).ToList();
        if (temporada.ModoPontuacao == ModoPontuacao.SomaDasRodadas)
            return CalculadoraRanking.Calcular(temporada, encerradas, nomesJogadores);

        // Contadores de partidas (vitórias, byes…) continuam vindo das rodadas; os pontos vêm das colocações.
        var contadores = CalculadoraRanking.Calcular(temporada, encerradas, nomesJogadores).ToDictionary(l => l.IdJogador);
        var acumulados = contadores.Keys.ToDictionary(id => id, id => new Acumulado(id));
        var rodadasPorEtapa = todas.GroupBy(r => r.IdEtapa).ToDictionary(g => g.Key, g => g.ToList());

        var classificatoriasConcluidas = new List<Etapa>();
        foreach (var etapa in etapas.OrderBy(e => e.Data).ThenBy(e => e.Numero))
        {
            var rodadasDaEtapa = rodadasPorEtapa.GetValueOrDefault(etapa.Id) ?? [];
            if (!RegrasEtapa.EstaConcluida(rodadasDaEtapa)) continue;
            if (!etapa.Final) classificatoriasConcluidas.Add(etapa);

            foreach (var linha in CalculadoraRankingEtapa.Calcular(rodadasDaEtapa, nomesJogadores, temporada))
            {
                var acumulado = acumulados.TryGetValue(linha.IdJogador, out var a) ? a : acumulados[linha.IdJogador] = new Acumulado(linha.IdJogador);
                var pontos = temporada.PontosDaColocacao(linha.Posicao);
                acumulado.Pontos += pontos;
                if (etapa.Final)
                {
                    acumulado.PontosFinal = pontos;
                    continue;
                }
                acumulado.Etapas++;
                if (linha.Posicao == 1) acumulado.PrimeirosLugares++;
                acumulado.PosicaoNaEtapa[etapa.Id] = linha.Posicao;
            }
        }

        string Nome(int id) => nomesJogadores.GetValueOrDefault(id) ?? $"#{id}";
        var linhas = new List<LinhaRanking>();
        var grupos = acumulados.Values
            .GroupBy(a => (a.Pontos, a.PrimeirosLugares, a.Etapas))
            .OrderByDescending(g => g.Key.Pontos).ThenByDescending(g => g.Key.PrimeirosLugares).ThenByDescending(g => g.Key.Etapas);

        foreach (var grupo in grupos)
        {
            foreach (var subgrupo in DesempatarPelaUltimaEtapa(grupo.ToList(), classificatoriasConcluidas))
            {
                var posicao = linhas.Count + 1;
                foreach (var jogador in subgrupo.OrderBy(j => Nome(j.IdJogador), ComparadorNome).ThenBy(j => j.IdJogador))
                {
                    var c = contadores.GetValueOrDefault(jogador.IdJogador);
                    linhas.Add(new LinhaRanking(posicao, jogador.IdJogador, Nome(jogador.IdJogador), jogador.Pontos,
                        c?.Vitorias ?? 0, c?.Empates ?? 0, c?.Derrotas ?? 0, c?.Byes ?? 0, c?.Partidas ?? 0, c?.PercentualVitoria,
                        jogador.Etapas, jogador.PrimeirosLugares, jogador.PontosFinal, subgrupo.Count > 1));
                }
            }
        }
        return linhas;
    }

    /// <summary>
    /// Separa um grupo empatado pela colocação na etapa mais recente que todos disputaram.
    /// Devolve os subgrupos na ordem (quem continua empatado fica junto).
    /// </summary>
    private static IEnumerable<List<Acumulado>> DesempatarPelaUltimaEtapa(List<Acumulado> empatados, List<Etapa> classificatorias)
    {
        if (empatados.Count == 1) return [empatados];
        var ultimaEmComum = classificatorias
            .OrderByDescending(e => e.Data).ThenByDescending(e => e.Numero)
            .FirstOrDefault(e => empatados.All(j => j.PosicaoNaEtapa.ContainsKey(e.Id)));
        if (ultimaEmComum is null) return [empatados];
        return empatados.GroupBy(j => j.PosicaoNaEtapa[ultimaEmComum.Id]).OrderBy(g => g.Key).Select(g => g.ToList());
    }

    private sealed class Acumulado(int idJogador)
    {
        public int IdJogador { get; } = idJogador;
        public int Pontos, Etapas, PrimeirosLugares;
        public int? PontosFinal;
        public Dictionary<int, int> PosicaoNaEtapa { get; } = [];
    }
}
