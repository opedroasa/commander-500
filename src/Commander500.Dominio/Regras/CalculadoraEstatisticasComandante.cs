using Commander500.Dominio.Entidades;
using Commander500.Dominio.Enumeradores;

namespace Commander500.Dominio.Regras;

/// <param name="Nome">"A" ou "A + B" para duplas de parceiros/background.</param>
/// <param name="PercentualVitoria">Nulo enquanto as partidas não chegam ao mínimo.</param>
/// <param name="Decks">Decks cadastrados com exatamente esses comandantes (só quando os decks são informados).</param>
/// <param name="Jogadores">Jogadores diferentes que jogaram com ele nas rodadas informadas.</param>
public record LinhaEstatisticaComandante(
    string Nome, IReadOnlyList<int> IdsComandantes,
    int Partidas, int Vitorias, int Empates, double? PercentualVitoria, int Decks, int Jogadores);

/// <summary>
/// Estatísticas de comandantes. O conjunto de comandantes do deck é uma linha: "Tymna + Thrasios" conta separado
/// de Tymna com outro parceiro. Bye não é partida nem vitória (spec 4.2).
/// Precisa de Deck.Comandantes.Comandante carregados nas participações e nos <paramref name="decks"/>.
/// </summary>
public static class CalculadoraEstatisticasComandante
{
    /// <param name="decks">Decks cadastrados; usados para contar <see cref="LinhaEstatisticaComandante.Decks"/>.</param>
    /// <param name="incluirSemPartidas">Também lista comandantes que têm deck mas ainda não jogaram.</param>
    public static IReadOnlyList<LinhaEstatisticaComandante> Calcular(
        IEnumerable<Rodada> rodadas, int minimoPartidasPercentual,
        IEnumerable<Deck>? decks = null, bool incluirSemPartidas = false)
    {
        var acumulados = new Dictionary<string, Acumulado>();

        Acumulado? Do(Deck? deck, bool criar)
        {
            var comandantes = deck?.Comandantes.Select(dc => dc.Comandante!).Where(c => c is not null)
                .OrderBy(c => c.Nome, StringComparer.OrdinalIgnoreCase).ToList();
            if (comandantes is null || comandantes.Count == 0) return null;
            var chave = string.Join(",", comandantes.Select(c => c.Id).Order());
            if (!acumulados.TryGetValue(chave, out var acumulado) && criar)
                acumulados[chave] = acumulado = new Acumulado(string.Join(" + ", comandantes.Select(c => c.Nome)), comandantes.Select(c => c.Id).ToList());
            return acumulado;
        }

        foreach (var rodada in rodadas.Where(r => r.Situacao == SituacaoRodada.Encerrada))
        {
            var participacoesPorId = rodada.Participacoes.ToDictionary(p => p.Id);
            foreach (var mesa in rodada.Mesas.Where(m => m.Resultado != ResultadoMesa.Pendente))
            {
                foreach (var cadeira in mesa.Cadeiras)
                {
                    var participacao = cadeira.Participacao ?? participacoesPorId[cadeira.IdParticipacao];
                    var acumulado = Do(participacao.Deck, criar: true);
                    if (acumulado is null) continue;
                    acumulado.Partidas++;
                    acumulado.IdsJogadores.Add(participacao.IdJogador);
                    if (mesa.Resultado == ResultadoMesa.Empate) acumulado.Empates++;
                    else if (mesa.IdParticipacaoVencedora == participacao.Id) acumulado.Vitorias++;
                }
            }
        }

        foreach (var deck in decks ?? [])
            if (Do(deck, criar: incluirSemPartidas) is { } acumulado) acumulado.Decks++;

        return acumulados.Values
            .OrderByDescending(a => a.Partidas).ThenByDescending(a => a.Vitorias).ThenByDescending(a => a.Decks)
            .ThenBy(a => a.Nome, StringComparer.OrdinalIgnoreCase)
            .Select(a => new LinhaEstatisticaComandante(a.Nome, a.Ids, a.Partidas, a.Vitorias, a.Empates,
                a.Partidas >= Math.Max(1, minimoPartidasPercentual) ? (double)a.Vitorias / a.Partidas : null,
                a.Decks, a.IdsJogadores.Count))
            .ToList();
    }

    private sealed class Acumulado(string nome, List<int> ids)
    {
        public string Nome { get; } = nome;
        public List<int> Ids { get; } = ids;
        public HashSet<int> IdsJogadores { get; } = [];
        public int Partidas, Vitorias, Empates, Decks;
    }
}
