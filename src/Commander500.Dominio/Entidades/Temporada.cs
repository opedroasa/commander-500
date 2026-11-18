using Commander500.Dominio.Comum;
using Commander500.Dominio.Enumeradores;

namespace Commander500.Dominio.Entidades;

/// <summary>
/// Um circuito da liga, com início, fim e configuração de pontuação própria (spec 4.1).
/// A configuração trava quando a primeira rodada é encerrada.
/// </summary>
public class Temporada
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public DateOnly DataInicio { get; set; }
    public DateOnly? DataFim { get; set; }
    /// <summary>Aceita novas rodadas. Várias temporadas podem estar ativas ao mesmo tempo.</summary>
    public bool Ativa { get; set; } = true;
    /// <summary>Vira true ao encerrar a primeira rodada: a pontuação não pode mais mudar.</summary>
    public bool ConfiguracaoTravada { get; set; }

    // ----- Pontuação (spec 4.1)
    public int PontosVitoria { get; set; } = 3;
    /// <summary>Pontos de cada jogador da mesa em caso de empate / tempo esgotado.</summary>
    public int PontosEmpate { get; set; } = 1;
    /// <summary>Pontos de bye quando 1 jogador fica de fora.</summary>
    public int PontosBye1 { get; set; } = 3;
    /// <summary>Pontos de bye quando 2 ficam de fora. [EM ABERTO] na spec.</summary>
    public int PontosBye2 { get; set; } = 3;
    /// <summary>Pontos de bye quando 3 ficam de fora. [EM ABERTO] na spec.</summary>
    public int PontosBye3 { get; set; } = 3;
    /// <summary>Duração padrão do timer. [EM ABERTO] na spec.</summary>
    public int MinutosPadraoRodada { get; set; } = 60;
    /// <summary>Regra informativa "acabou o tempo, mais N turnos" (só aparece no timer). 0 = nenhuma.</summary>
    public int TurnosExtrasAposTempo { get; set; }
    /// <summary>Mínimo de partidas para mostrar o % de vitória de um comandante.</summary>
    public int MinimoPartidasPercentualComandante { get; set; } = 3;
    /// <summary>Lista de <see cref="CriterioDesempate"/> separada por vírgula, na ordem de aplicação.</summary>
    public string CriteriosDesempate { get; set; } = "Vitorias,MenosPartidas";

    public List<Rodada> Rodadas { get; set; } = [];
    public List<Etapa> Etapas { get; set; } = [];

    /// <summary>Pontos de bye conforme quantos jogadores ficaram de fora na rodada.</summary>
    public int PontosBye(int quantidadeByes) => quantidadeByes switch
    {
        1 => PontosBye1,
        2 => PontosBye2,
        3 => PontosBye3,
        _ => 0,
    };

    public IReadOnlyList<CriterioDesempate> ObterCriteriosDesempate() => LerCriteriosDesempate(CriteriosDesempate);

    /// <summary>Converte "Vitorias,MenosPartidas" na lista de critérios (ignora repetidos).</summary>
    public static IReadOnlyList<CriterioDesempate> LerCriteriosDesempate(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return [];
        var criterios = new List<CriterioDesempate>();
        foreach (var parte in texto.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!Enum.TryParse<CriterioDesempate>(parte, ignoreCase: true, out var criterio))
                throw new RegraNegocioException($"Critério de desempate inválido: '{parte}'.");
            if (!criterios.Contains(criterio)) criterios.Add(criterio);
        }
        return criterios;
    }
}
