using Commander500.Dominio.Enumeradores;

namespace Commander500.Dominio.Entidades;

/// <summary>
/// Deck de um jogador, com 1 ou 2 comandantes (parceiros/background). O orçamento é conferido fora do sistema;
/// aqui só ficam o link da lista e a situação da revisão (spec 4.5).
/// </summary>
public class Deck
{
    public int Id { get; set; }
    public int IdJogador { get; set; }
    public Jogador? Jogador { get; set; }
    /// <summary>Opcional na tela: quando vazio vira o nome do(s) comandante(s). As estatísticas usam sempre os comandantes.</summary>
    public string Nome { get; set; } = "";
    public string? UrlLista { get; set; }
    public SituacaoDeck Situacao { get; set; } = SituacaoDeck.Pendente;
    /// <summary>Observação do lojista na revisão (obrigatória ao reprovar). Não aparece no site público.</summary>
    public string? ObservacaoRevisao { get; set; }
    public DateTime DataCadastro { get; set; }
    public List<DeckComandante> Comandantes { get; set; } = [];
}

/// <summary>Ligação deck ↔ comandante (1 ou 2 por deck).</summary>
public class DeckComandante
{
    public int IdDeck { get; set; }
    public Deck? Deck { get; set; }
    public int IdComandante { get; set; }
    public Comandante? Comandante { get; set; }
}
