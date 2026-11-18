using System.Globalization;
using Commander500.Dominio.Entidades;
using Commander500.Dominio.Enumeradores;

namespace Commander500.Dominio.Regras;

/// <param name="Posicao">Igual para jogadores empatados (1, 1, 3…).</param>
/// <param name="Vitorias">Vitórias em mesa + byes (na etapa o bye conta como vitória).</param>
/// <param name="VitoriasConfrontoDireto">Vitórias em mesas contra outros empatados em vitórias/byes (0 quando não empatou).</param>
/// <param name="Empatado">True quando outro jogador tem a mesma posição.</param>
public record LinhaRankingEtapa(
    int Posicao, int IdJogador, string NomeJogador,
    int Vitorias, int VitoriasMesa, int Byes, int Empates, int Derrotas, int Partidas,
    int VitoriasConfrontoDireto, bool Empatado);

/// <summary>
/// Ranking da etapa (prêmio do dia). Ordem:
/// 1. vitórias ↓ (o bye conta como vitória);
/// 2. byes ↑ — entre vitórias iguais, quem teve bye fica abaixo;
/// 3. confronto direto ↓ — entre os que continuam empatados, vitórias em mesas em que se enfrentaram;
/// 4. persistindo o empate → mesma posição (o lojista decide a premiação).
/// Só contam rodadas encerradas.
/// </summary>
public static class CalculadoraRankingEtapa
{
    private static readonly StringComparer ComparadorNome = StringComparer.Create(new CultureInfo("pt-BR"), ignoreCase: true);

    public static IReadOnlyList<LinhaRankingEtapa> Calcular(IEnumerable<Rodada> rodadasDaEtapa, IReadOnlyDictionary<int, string> nomesJogadores)
    {
        var encerradas = rodadasDaEtapa.Where(r => r.Situacao == SituacaoRodada.Encerrada).ToList();
        var acumulados = new Dictionary<int, Acumulado>();
        Acumulado Do(int idJogador) => acumulados.TryGetValue(idJogador, out var a) ? a
            : acumulados[idJogador] = new Acumulado(idJogador, nomesJogadores.GetValueOrDefault(idJogador) ?? $"#{idJogador}");

        // Mesas decididas: quem sentou e quem venceu, para calcular o confronto direto depois.
        var mesasDecididas = new List<(int[] Jogadores, int? Vencedor)>();

        foreach (var rodada in encerradas)
        {
            foreach (var bye in rodada.Participacoes.Where(p => p.Bye)) Do(bye.IdJogador).Byes++;

            var participacoesPorId = rodada.Participacoes.ToDictionary(p => p.Id);
            foreach (var mesa in rodada.Mesas.Where(m => m.Resultado != ResultadoMesa.Pendente))
            {
                var sentados = mesa.Cadeiras.Select(c => c.Participacao ?? participacoesPorId[c.IdParticipacao]).ToList();
                int? vencedor = null;
                foreach (var participacao in sentados)
                {
                    var acumulado = Do(participacao.IdJogador);
                    acumulado.Partidas++;
                    if (mesa.Resultado == ResultadoMesa.Empate) acumulado.Empates++;
                    else if (mesa.IdParticipacaoVencedora == participacao.Id) { acumulado.VitoriasMesa++; vencedor = participacao.IdJogador; }
                    else acumulado.Derrotas++;
                }
                mesasDecididas.Add((sentados.Select(p => p.IdJogador).ToArray(), vencedor));
            }
        }

        var linhas = new List<LinhaRankingEtapa>();
        var grupos = acumulados.Values
            .GroupBy(a => (a.Vitorias, a.Byes))
            .OrderByDescending(g => g.Key.Vitorias).ThenBy(g => g.Key.Byes);

        foreach (var grupo in grupos)
        {
            var empatados = grupo.ToList();
            var idsEmpatados = empatados.Select(e => e.IdJogador).ToHashSet();
            if (empatados.Count > 1)
                foreach (var jogador in empatados)
                    jogador.ConfrontoDireto = mesasDecididas.Count(m => m.Vencedor == jogador.IdJogador
                        && m.Jogadores.Any(outro => outro != jogador.IdJogador && idsEmpatados.Contains(outro)));

            var ordenados = empatados.OrderByDescending(e => e.ConfrontoDireto).ThenBy(e => e.Nome, ComparadorNome).ThenBy(e => e.IdJogador).ToList();
            var inicioDoGrupo = linhas.Count;
            foreach (var jogador in ordenados)
            {
                // Posição = 1 + jogadores estritamente à frente (grupos anteriores + melhor confronto direto neste grupo).
                var posicao = inicioDoGrupo + 1 + ordenados.Count(o => o.ConfrontoDireto > jogador.ConfrontoDireto);
                var empatado = ordenados.Count(o => o.ConfrontoDireto == jogador.ConfrontoDireto) > 1;
                linhas.Add(new LinhaRankingEtapa(posicao, jogador.IdJogador, jogador.Nome, jogador.Vitorias, jogador.VitoriasMesa,
                    jogador.Byes, jogador.Empates, jogador.Derrotas, jogador.Partidas,
                    empatados.Count > 1 ? jogador.ConfrontoDireto : 0, empatado));
            }
        }
        return linhas;
    }

    private sealed class Acumulado(int idJogador, string nome)
    {
        public int IdJogador { get; } = idJogador;
        public string Nome { get; } = nome;
        public int VitoriasMesa, Byes, Empates, Derrotas, Partidas, ConfrontoDireto;
        public int Vitorias => VitoriasMesa + Byes;
    }
}
