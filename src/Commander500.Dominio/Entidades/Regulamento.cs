namespace Commander500.Dominio.Entidades;

/// <summary>
/// Regulamento da liga (o "tratado"), mostrado numa página pública e editado pelo lojista num editor visual.
/// Há um só registro; o conteúdo é HTML (títulos, negrito, listas e tabelas).
/// </summary>
public class Regulamento
{
    public int Id { get; set; }
    public string Conteudo { get; set; } = "";
    public DateTime AlteradoEm { get; set; }
    public string? AlteradoPor { get; set; }
}
