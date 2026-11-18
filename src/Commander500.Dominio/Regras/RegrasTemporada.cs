using Commander500.Dominio.Comum;
using Commander500.Dominio.Entidades;
using Commander500.Dominio.Enumeradores;

namespace Commander500.Dominio.Regras;

/// <summary>Configuração de pontuação de uma temporada (spec 4.1).</summary>
public record ConfiguracaoTemporada(
    int PontosVitoria, int PontosEmpate,
    int PontosBye1, int PontosBye2, int PontosBye3,
    int MinutosPadraoRodada, int TurnosExtrasAposTempo,
    int MinimoPartidasPercentualComandante,
    IReadOnlyList<CriterioDesempate> CriteriosDesempate)
{
    public static ConfiguracaoTemporada De(Temporada temporada) => new(
        temporada.PontosVitoria, temporada.PontosEmpate, temporada.PontosBye1, temporada.PontosBye2, temporada.PontosBye3,
        temporada.MinutosPadraoRodada, temporada.TurnosExtrasAposTempo, temporada.MinimoPartidasPercentualComandante,
        temporada.ObterCriteriosDesempate());
}

public static class RegrasTemporada
{
    /// <summary>Aplica a configuração de pontuação. Recusada depois que a primeira rodada é encerrada (spec 4.1).</summary>
    public static void AplicarConfiguracao(Temporada temporada, ConfiguracaoTemporada configuracao)
    {
        if (temporada.ConfiguracaoTravada)
            throw new RegraNegocioException("Configuração travada: a primeira rodada da temporada já foi encerrada.");

        int[] pontos = [configuracao.PontosVitoria, configuracao.PontosEmpate, configuracao.PontosBye1, configuracao.PontosBye2, configuracao.PontosBye3];
        if (pontos.Any(p => p is < 0 or > 100))
            throw new DadosInvalidosException("Pontos devem estar entre 0 e 100.");
        if (configuracao.MinutosPadraoRodada is < 1 or > FluxoRodada.DuracaoMaximaMinutos)
            throw new DadosInvalidosException($"Duração padrão deve estar entre 1 e {FluxoRodada.DuracaoMaximaMinutos} minutos.");
        if (configuracao.TurnosExtrasAposTempo is < 0 or > 20)
            throw new DadosInvalidosException("Turnos extras devem estar entre 0 e 20.");
        if (configuracao.MinimoPartidasPercentualComandante is < 1 or > 100)
            throw new DadosInvalidosException("Mínimo de partidas para % de vitória deve estar entre 1 e 100.");
        var criterios = configuracao.CriteriosDesempate ?? [];
        if (criterios.Distinct().Count() != criterios.Count)
            throw new DadosInvalidosException("Critérios de desempate repetidos.");
        if (criterios.Contains(CriterioDesempate.MenosPartidas) && criterios.Contains(CriterioDesempate.MaisPartidas))
            throw new DadosInvalidosException("Use apenas um entre 'menos partidas' e 'mais partidas'.");

        temporada.PontosVitoria = configuracao.PontosVitoria;
        temporada.PontosEmpate = configuracao.PontosEmpate;
        temporada.PontosBye1 = configuracao.PontosBye1;
        temporada.PontosBye2 = configuracao.PontosBye2;
        temporada.PontosBye3 = configuracao.PontosBye3;
        temporada.MinutosPadraoRodada = configuracao.MinutosPadraoRodada;
        temporada.TurnosExtrasAposTempo = configuracao.TurnosExtrasAposTempo;
        temporada.MinimoPartidasPercentualComandante = configuracao.MinimoPartidasPercentualComandante;
        temporada.CriteriosDesempate = string.Join(",", criterios);
    }
}
