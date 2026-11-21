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
    /// <summary>Mesa final do top 4 (modo <see cref="Enumeradores.ModoPontuacao.ColocacaoNaEtapa"/>). Fica fora da numeração por data
    /// e pode ter a mesma data de uma etapa normal.</summary>
    public bool Final { get; set; }
    /// <summary>Quantas rodadas os finalistas vão jogar (só na final; informativo para a tela).</summary>
    public int? RodadasPrevistas { get; set; }
    /// <summary>Foto do top da etapa (caminho no armazenamento de fotos; a Api monta o endereço público).</summary>
    public string? CaminhoFoto { get; set; }
    public string? LegendaFoto { get; set; }
    public List<Rodada> Rodadas { get; set; } = [];
}
