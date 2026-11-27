using Commander500.Dominio.Entidades;
using Commander500.Dominio.Enumeradores;

namespace Commander500.Dominio.Regras;

/// <summary>Qual deck representa o jogador (a arte do comandante na lista de jogadores).</summary>
public static class RegrasDeckDoJogador
{
    /// <summary>
    /// Deck reserva para quem ainda não tem partida encerrada (sem "deck mais jogado"), para o card não ficar sem arte.
    /// Ordem de preferência:
    /// 1. situação: aprovado → pendente → reprovado;
    /// 2. com lista antes de sem lista;
    /// 3. cadastrado mais recentemente (o deck novo costuma ser o que ele vai jogar);
    /// 4. nome em ordem alfabética (e o id, para o resultado ser sempre o mesmo).
    /// </summary>
    /// <returns>O deck escolhido, ou null se o jogador não tem deck.</returns>
    public static Deck? DeckReserva(IEnumerable<Deck> decksDoJogador) =>
        decksDoJogador
            .OrderBy(d => OrdemSituacao(d.Situacao))
            .ThenBy(d => string.IsNullOrWhiteSpace(d.UrlLista) ? 1 : 0)
            .ThenByDescending(d => d.DataCadastro)
            .ThenBy(d => d.Nome, StringComparer.OrdinalIgnoreCase)
            .ThenBy(d => d.Id)
            .FirstOrDefault();

    private static int OrdemSituacao(SituacaoDeck situacao) => situacao switch
    {
        SituacaoDeck.Aprovado => 0,
        SituacaoDeck.Pendente => 1,
        _ => 2,
    };
}
