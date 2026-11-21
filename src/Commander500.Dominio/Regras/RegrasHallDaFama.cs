using Commander500.Dominio.Entidades;

namespace Commander500.Dominio.Regras;

/// <summary>Hall da Fama: ranking final das temporadas encerradas, com o deck de cada jogador.</summary>
public static class RegrasHallDaFama
{
    /// <summary>
    /// Deck que representa cada jogador na temporada: o que ele usou em mais rodadas (bye conta, porque ele estava
    /// presente com o deck). Empate: o usado mais recentemente.
    /// </summary>
    /// <returns>Id do jogador → id do deck.</returns>
    public static Dictionary<int, int> DeckMaisUsado(IEnumerable<Rodada> rodadasDaTemporada) =>
        rodadasDaTemporada
            .SelectMany(r => r.Participacoes.Select(p => (p.IdJogador, p.IdDeck, r.Data, r.Numero)))
            .GroupBy(p => p.IdJogador)
            .ToDictionary(
                jogador => jogador.Key,
                jogador => jogador
                    .GroupBy(p => p.IdDeck)
                    .OrderByDescending(deck => deck.Count())
                    .ThenByDescending(deck => deck.Max(p => (p.Data, p.Numero)))
                    .First().Key);
}
