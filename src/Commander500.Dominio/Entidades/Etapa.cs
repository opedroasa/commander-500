namespace Commander500.Dominio.Entidades;

/// <summary>
/// Etapa: um dia de liga dentro da temporada, com uma ou mais rodadas. Tem ranking e premiação próprios
/// (ver <see cref="Regras.CalculadoraRankingEtapa"/>). É criada automaticamente pela data da rodada.
/// </summary>
public class Etapa
{
    public int Id { get; set; }
    public int IdTemporada { get; set; }
    public Temporada? Temporada { get; set; }
    /// <summary>1, 2, 3… pela ordem das datas dentro da temporada.</summary>
    public int Numero { get; set; }
    public DateOnly Data { get; set; }
    public List<Rodada> Rodadas { get; set; } = [];
}
