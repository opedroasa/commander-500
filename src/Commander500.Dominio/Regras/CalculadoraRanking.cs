using System.Globalization;
using Commander500.Dominio.Entidades;
using Commander500.Dominio.Enumeradores;

namespace Commander500.Dominio.Regras;

/// <param name="Partidas">Mesas jogadas de fato (bye não conta).</param>
/// <param name="PercentualVitoria">Vitórias ÷ partidas; nulo quando não jogou nenhuma.</param>
/// <param name="Etapas">Modo por colocação: etapas concluídas que o jogador disputou (sem contar a final).</param>
/// <param name="PrimeirosLugares">Modo por colocação: etapas em que terminou em 1º (empatados contam).</param>
/// <param name="PontosFinal">Modo por colocação: pontos ganhos na mesa final (nulo = não jogou a final).</param>
/// <param name="Empatado">Modo por colocação: outro jogador tem a mesma posição (o tratado manda a loja desempatar).</param>
public record LinhaRanking(
    int Posicao, int IdJogador, string NomeJogador,
    int Pontos, int Vitorias, int Empates, int Derrotas, int Byes,
    int Partidas, double? PercentualVitoria,
    int Etapas = 0, int PrimeirosLugares = 0, int? PontosFinal = null, bool Empatado = false);

/// <summary>
/// Ranking da temporada no modo <see cref="ModoPontuacao.SomaDasRodadas"/> (spec 4.1) e contadores de partidas
/// usados pelos dois modos. Os pontos nunca são gravados: são sempre calculados a partir dos
/// resultados das rodadas encerradas + configuração da temporada. Corrigir um resultado recalcula tudo.
/// </summary>
public static class CalculadoraRanking
{
    private static readonly StringComparer ComparadorNome = StringComparer.Create(new CultureInfo("pt-BR"), ignoreCase: true);

    public static IReadOnlyList<LinhaRanking> Calcular(Temporada temporada, IEnumerable<Rodada> rodadas, IReadOnlyDictionary<int, string> nomesJogadores)
    {
        var acumulados = new Dictionary<int, Acumulado>();
        Acumulado Do(int idJogador) => acumulados.TryGetValue(idJogador, out var a) ? a : acumulados[idJogador] = new Acumulado(idJogador);

        foreach (var rodada in rodadas.Where(r => r.Situacao == SituacaoRodada.Encerrada))
        {
            var byes = rodada.Participacoes.Where(p => p.Bye).ToList();
            var pontosBye = temporada.PontosBye(byes.Count);
            foreach (var bye in byes)
            {
                var acumulado = Do(bye.IdJogador);
                acumulado.Byes++;
                acumulado.Pontos += pontosBye;
            }

            var participacoesPorId = rodada.Participacoes.ToDictionary(p => p.Id);
            foreach (var mesa in rodada.Mesas.Where(m => m.Resultado != ResultadoMesa.Pendente))
            {
                foreach (var cadeira in mesa.Cadeiras)
                {
                    var participacao = cadeira.Participacao ?? participacoesPorId[cadeira.IdParticipacao];
                    var acumulado = Do(participacao.IdJogador);
                    acumulado.Partidas++;
                    if (mesa.Empatou(participacao.Id))
                    {
                        acumulado.Empates++;
                        acumulado.Pontos += temporada.PontosEmpate;
                    }
                    else if (mesa.IdParticipacaoVencedora == participacao.Id)
                    {
                        acumulado.Vitorias++;
                        acumulado.Pontos += temporada.PontosVitoria;
                    }
                    else
                    {
                        acumulado.Derrotas++;
                        acumulado.Pontos += temporada.PontosDerrota; // presença de quem jogou e perdeu
                    }
                }
            }
        }

        foreach (var acumulado in acumulados.Values)
            acumulado.Nome = nomesJogadores.TryGetValue(acumulado.IdJogador, out var nome) ? nome : $"#{acumulado.IdJogador}";

        // Pontos primeiro, depois os critérios configurados na temporada e, por último, o nome.
        IOrderedEnumerable<Acumulado> ordenados = acumulados.Values.OrderByDescending(a => a.Pontos);
        foreach (var criterio in temporada.ObterCriteriosDesempate())
        {
            ordenados = criterio switch
            {
                CriterioDesempate.Vitorias => ordenados.ThenByDescending(a => a.Vitorias),
                CriterioDesempate.MenosPartidas => ordenados.ThenBy(a => a.Partidas),
                CriterioDesempate.MaisPartidas => ordenados.ThenByDescending(a => a.Partidas),
                CriterioDesempate.Empates => ordenados.ThenByDescending(a => a.Empates),
                CriterioDesempate.PercentualVitoria => ordenados.ThenByDescending(a => a.PercentualVitoria ?? -1),
                _ => ordenados,
            };
        }
        ordenados = ordenados.ThenBy(a => a.Nome, ComparadorNome).ThenBy(a => a.IdJogador);

        return ordenados
            .Select((a, indice) => new LinhaRanking(indice + 1, a.IdJogador, a.Nome, a.Pontos, a.Vitorias, a.Empates,
                a.Derrotas, a.Byes, a.Partidas, a.PercentualVitoria))
            .ToList();
    }

    private sealed class Acumulado(int idJogador)
    {
        public int IdJogador { get; } = idJogador;
        public string Nome { get; set; } = "";
        public int Pontos, Vitorias, Empates, Derrotas, Byes, Partidas;
        public double? PercentualVitoria => Partidas == 0 ? null : (double)Vitorias / Partidas;
    }
}
