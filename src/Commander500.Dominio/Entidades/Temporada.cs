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
    /// <summary>Soma dos pontos das rodadas ou pontos pela colocação em cada etapa (padrão).</summary>
    public ModoPontuacao ModoPontuacao { get; set; } = ModoPontuacao.ColocacaoNaEtapa;
    public int PontosVitoria { get; set; } = 3;
    /// <summary>Pontos de cada jogador da mesa em caso de empate / tempo esgotado.</summary>
    public int PontosEmpate { get; set; } = 1;
    /// <summary>Pontos de quem jogou e perdeu (presença). 0 = só quem ganha pontua; quem faltou nunca pontua.</summary>
    public int PontosDerrota { get; set; }
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
    /// <summary>Modo por colocação: pontos do 1º, 2º, 3º… lugar da etapa, separados por vírgula ("5,4,3,2").</summary>
    public string PontosPorColocacao { get; set; } = "5,4,3,2";
    /// <summary>Modo por colocação: pontos de quem ficou depois das posições da tabela (presença).</summary>
    public int PontosDemaisColocados { get; set; } = 1;

    // ----- Aviso da próxima etapa (aparece na página inicial; pode mudar a qualquer momento)
    /// <summary>Data e hora (UTC) da próxima etapa marcada.</summary>
    public DateTime? ProximaEtapaEm { get; set; }
    /// <summary>Último dia para enviar as listas dos decks.</summary>
    public DateOnly? PrazoListas { get; set; }

    public List<Rodada> Rodadas { get; set; } = [];
    public List<Etapa> Etapas { get; set; } = [];

    /// <summary>Pontos de bye conforme quantos jogadores ficaram de fora na rodada.</summary>
    public int PontosBye(int quantidadeByes) => quantidadeByes switch
    {
        <= 0 => 0,
        1 => PontosBye1,
        2 => PontosBye2,
        _ => PontosBye3, // mesas montadas à mão podem deixar mais de 3 de fora
    };

    /// <summary>Pontos de temporada de quem terminou a etapa na <paramref name="posicao"/> (1 = primeiro).</summary>
    public int PontosDaColocacao(int posicao)
    {
        var tabela = ObterPontosPorColocacao();
        return posicao >= 1 && posicao <= tabela.Count ? tabela[posicao - 1] : PontosDemaisColocados;
    }

    public IReadOnlyList<int> ObterPontosPorColocacao() => LerPontosPorColocacao(PontosPorColocacao);

    /// <summary>Converte "5,4,3,2" em [5, 4, 3, 2].</summary>
    public static IReadOnlyList<int> LerPontosPorColocacao(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return [];
        var pontos = new List<int>();
        foreach (var parte in texto.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!int.TryParse(parte, out var valor))
                throw new RegraNegocioException($"Pontuação por colocação inválida: '{parte}'.");
            pontos.Add(valor);
        }
        return pontos;
    }

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
