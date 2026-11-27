using Commander500.Dominio.Entidades;
using Commander500.Dominio.Enumeradores;

namespace Commander500.Dominio.Regras;

/// <summary>Resultado de uma participação numa mesa decidida (bye não entra).</summary>
public enum ResultadoParticipacao { Vitoria, Empate, Derrota }

/// <param name="PercentualMes">Vitórias ÷ partidas do mês.</param>
/// <param name="PercentualAcumulado">Vitórias ÷ partidas de todos os meses até este.</param>
public record PontoMensal(int Ano, int Mes, int Partidas, int Vitorias, int Empates, int Derrotas, double PercentualMes, double PercentualAcumulado);

/// <param name="Mesas">Mesas decididas em que os dois sentaram juntos.</param>
/// <param name="VitoriasDoJogador">Dessas, quantas o jogador venceu.</param>
/// <param name="VitoriasDoAdversario">Dessas, quantas o adversário venceu.</param>
public record LinhaConfronto(int IdAdversario, int Mesas, int VitoriasDoJogador, int VitoriasDoAdversario);

/// <param name="PercentualVitoria">Nulo quando não jogou nenhuma mesa com o deck.</param>
public record DesempenhoDeck(int IdDeck, int Partidas, int Vitorias, int Empates, int Derrotas, double? PercentualVitoria);

/// <summary>
/// Estatísticas das páginas do jogador e do comandante: evolução mês a mês, confrontos (nêmesis / vítima favorita) e
/// desempenho por deck. Só contam mesas decididas de rodadas encerradas; bye não é partida.
/// </summary>
public static class CalculadoraDesempenho
{
    /// <summary>Cada participação numa mesa decidida, com a data da rodada e o resultado dela.</summary>
    public static IEnumerable<(Rodada Rodada, Mesa Mesa, Participacao Participacao, ResultadoParticipacao Resultado)> Partidas(IEnumerable<Rodada> rodadas)
    {
        foreach (var rodada in rodadas.Where(r => r.Situacao == SituacaoRodada.Encerrada))
        {
            var participacoesPorId = rodada.Participacoes.ToDictionary(p => p.Id);
            foreach (var mesa in rodada.Mesas.Where(m => m.Resultado != ResultadoMesa.Pendente))
                foreach (var cadeira in mesa.Cadeiras)
                {
                    var participacao = cadeira.Participacao ?? participacoesPorId[cadeira.IdParticipacao];
                    var resultado = mesa.Empatou(participacao.Id) ? ResultadoParticipacao.Empate
                        : mesa.IdParticipacaoVencedora == participacao.Id ? ResultadoParticipacao.Vitoria
                        : ResultadoParticipacao.Derrota;
                    yield return (rodada, mesa, participacao, resultado);
                }
        }
    }

    /// <summary>Evolução mês a mês das participações escolhidas pelo <paramref name="filtro"/> (ex.: de um jogador ou de um comandante).</summary>
    public static IReadOnlyList<PontoMensal> EvolucaoMensal(IEnumerable<Rodada> rodadas, Func<Participacao, bool> filtro)
    {
        var pontos = new List<PontoMensal>();
        int partidasAcumuladas = 0, vitoriasAcumuladas = 0;
        var porMes = Partidas(rodadas).Where(p => filtro(p.Participacao))
            .GroupBy(p => (p.Rodada.Data.Year, p.Rodada.Data.Month))
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month);
        foreach (var mes in porMes)
        {
            var partidas = mes.Count();
            var vitorias = mes.Count(p => p.Resultado == ResultadoParticipacao.Vitoria);
            partidasAcumuladas += partidas;
            vitoriasAcumuladas += vitorias;
            pontos.Add(new PontoMensal(mes.Key.Year, mes.Key.Month, partidas, vitorias,
                mes.Count(p => p.Resultado == ResultadoParticipacao.Empate), mes.Count(p => p.Resultado == ResultadoParticipacao.Derrota),
                (double)vitorias / partidas, (double)vitoriasAcumuladas / partidasAcumuladas));
        }
        return pontos;
    }

    /// <summary>Com quem o jogador dividiu mesa e quem venceu quem. Ordenado por mesas juntos.</summary>
    public static IReadOnlyList<LinhaConfronto> Confrontos(IEnumerable<Rodada> rodadas, int idJogador)
    {
        var linhas = new Dictionary<int, (int Mesas, int DoJogador, int DoAdversario)>();
        foreach (var grupo in Partidas(rodadas).GroupBy(p => p.Mesa))
        {
            var sentados = grupo.ToList();
            var eu = sentados.FirstOrDefault(p => p.Participacao.IdJogador == idJogador);
            if (eu.Participacao is null) continue;
            var vencedor = sentados.FirstOrDefault(p => p.Resultado == ResultadoParticipacao.Vitoria).Participacao?.IdJogador;
            foreach (var outro in sentados.Where(p => p.Participacao.IdJogador != idJogador))
            {
                var atual = linhas.GetValueOrDefault(outro.Participacao.IdJogador);
                linhas[outro.Participacao.IdJogador] = (atual.Mesas + 1,
                    atual.DoJogador + (vencedor == idJogador ? 1 : 0),
                    atual.DoAdversario + (vencedor == outro.Participacao.IdJogador ? 1 : 0));
            }
        }
        return linhas.Select(l => new LinhaConfronto(l.Key, l.Value.Mesas, l.Value.DoJogador, l.Value.DoAdversario))
            .OrderByDescending(l => l.Mesas).ThenBy(l => l.IdAdversario).ToList();
    }

    /// <summary>Nêmesis: o adversário que mais venceu mesas com o jogador junto (empate: o que dividiu mais mesas). Nulo se ninguém venceu.</summary>
    public static LinhaConfronto? Nemesis(IReadOnlyList<LinhaConfronto> confrontos) =>
        confrontos.Where(c => c.VitoriasDoAdversario > 0)
            .OrderByDescending(c => c.VitoriasDoAdversario).ThenByDescending(c => c.Mesas).FirstOrDefault();

    /// <summary>Vítima favorita: o adversário de quem o jogador mais venceu (empate: o que dividiu mais mesas).</summary>
    public static LinhaConfronto? VitimaFavorita(IReadOnlyList<LinhaConfronto> confrontos) =>
        confrontos.Where(c => c.VitoriasDoJogador > 0)
            .OrderByDescending(c => c.VitoriasDoJogador).ThenByDescending(c => c.Mesas).FirstOrDefault();

    /// <summary>Desempenho de cada deck nas participações escolhidas pelo <paramref name="filtro"/>.</summary>
    public static IReadOnlyList<DesempenhoDeck> PorDeck(IEnumerable<Rodada> rodadas, Func<Participacao, bool> filtro) =>
        Partidas(rodadas).Where(p => filtro(p.Participacao))
            .GroupBy(p => p.Participacao.IdDeck)
            .Select(g =>
            {
                var partidas = g.Count();
                var vitorias = g.Count(p => p.Resultado == ResultadoParticipacao.Vitoria);
                return new DesempenhoDeck(g.Key, partidas, vitorias, g.Count(p => p.Resultado == ResultadoParticipacao.Empate),
                    g.Count(p => p.Resultado == ResultadoParticipacao.Derrota), partidas == 0 ? null : (double)vitorias / partidas);
            })
            .OrderByDescending(d => d.Vitorias).ThenByDescending(d => d.Partidas).ToList();

    /// <summary>
    /// Melhor deck: maior % de vitória entre os decks com pelo menos <paramref name="minimoPartidas"/> partidas
    /// (empate: mais vitórias). Se nenhum chegou no mínimo, o de mais vitórias.
    /// </summary>
    public static DesempenhoDeck? MelhorDeck(IReadOnlyList<DesempenhoDeck> decks, int minimoPartidas = 3) =>
        decks.Where(d => d.Partidas >= minimoPartidas)
            .OrderByDescending(d => d.PercentualVitoria).ThenByDescending(d => d.Vitorias).FirstOrDefault()
        ?? decks.Where(d => d.Vitorias > 0).OrderByDescending(d => d.Vitorias).ThenByDescending(d => d.Partidas).FirstOrDefault();

    /// <summary>"12-34": ids dos comandantes em ordem, identificando o conjunto (dupla de parceiros = uma chave).</summary>
    public static string ChaveComandantes(IEnumerable<int> idsComandantes) => string.Join('-', idsComandantes.Order());
}
