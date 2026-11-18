namespace Commander500.Dominio.Entidades;

/// <summary>Jogador da liga. Tem um ou mais decks.</summary>
public class Jogador
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public string? Apelido { get; set; }
    /// <summary>Inativo não aparece na lista de presentes, mas o histórico continua.</summary>
    public bool Ativo { get; set; } = true;
    public DateTime DataCadastro { get; set; }
    public List<Deck> Decks { get; set; } = [];
}
