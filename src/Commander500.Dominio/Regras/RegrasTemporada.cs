using Commander500.Dominio.Comum;
using Commander500.Dominio.Entidades;
using Commander500.Dominio.Enumeradores;

namespace Commander500.Dominio.Regras;

/// <summary>Configuração de pontuação de uma temporada (spec 4.1).</summary>
/// <param name="CriteriosDesempate">Só no modo <see cref="ModoPontuacao.SomaDasRodadas"/>; o modo por colocação usa os critérios do tratado.</param>
/// <param name="PontosDerrota">Pontos de quem jogou a mesa e perdeu (presença).</param>
/// <param name="PontosPorColocacao">Modo por colocação: pontos do 1º, 2º, 3º… da etapa.</param>
/// <param name="PontosDemaisColocados">Modo por colocação: pontos de quem ficou abaixo da tabela.</param>
public record ConfiguracaoTemporada(
    int PontosVitoria, int PontosEmpate,
    int PontosBye1, int PontosBye2, int PontosBye3,
    int MinutosPadraoRodada, int TurnosExtrasAposTempo,
    int MinimoPartidasPercentualComandante,
    IReadOnlyList<CriterioDesempate> CriteriosDesempate,
    ModoPontuacao ModoPontuacao = ModoPontuacao.ColocacaoNaEtapa,
    int PontosDerrota = 0,
    IReadOnlyList<int>? PontosPorColocacao = null,
    int PontosDemaisColocados = 1)
{
    public static ConfiguracaoTemporada De(Temporada temporada) => new(
        temporada.PontosVitoria, temporada.PontosEmpate, temporada.PontosBye1, temporada.PontosBye2, temporada.PontosBye3,
        temporada.MinutosPadraoRodada, temporada.TurnosExtrasAposTempo, temporada.MinimoPartidasPercentualComandante,
        temporada.ObterCriteriosDesempate(), temporada.ModoPontuacao, temporada.PontosDerrota,
        temporada.ObterPontosPorColocacao(), temporada.PontosDemaisColocados);
}

public static class RegrasTemporada
{
    /// <summary>Aplica a configuração de pontuação. Recusada depois que a primeira rodada é encerrada (spec 4.1).</summary>
    public static void AplicarConfiguracao(Temporada temporada, ConfiguracaoTemporada configuracao)
    {
        if (temporada.ConfiguracaoTravada)
            throw new RegraNegocioException("Configuração travada: a primeira rodada da temporada já foi encerrada.");

        var pontosPorColocacao = configuracao.PontosPorColocacao ?? [5, 4, 3, 2];
        if (!Enum.IsDefined(configuracao.ModoPontuacao))
            throw new DadosInvalidosException("Modo de pontuação inválido.");
        if (pontosPorColocacao.Count is < 1 or > 16)
            throw new DadosInvalidosException("A tabela de pontos por colocação deve ter de 1 a 16 posições.");
        int[] pontos = [configuracao.PontosVitoria, configuracao.PontosEmpate, configuracao.PontosDerrota,
            configuracao.PontosBye1, configuracao.PontosBye2, configuracao.PontosBye3, configuracao.PontosDemaisColocados, .. pontosPorColocacao];
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

        temporada.ModoPontuacao = configuracao.ModoPontuacao;
        temporada.PontosDerrota = configuracao.PontosDerrota;
        temporada.PontosPorColocacao = string.Join(",", pontosPorColocacao);
        temporada.PontosDemaisColocados = configuracao.PontosDemaisColocados;
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

    /// <summary>Aviso da próxima etapa na página inicial. Pode mudar a qualquer momento (não depende da trava).</summary>
    public static void DefinirProximaEtapa(Temporada temporada, DateTime? proximaEtapaEmUtc, DateOnly? prazoListas)
    {
        if (proximaEtapaEmUtc is null && prazoListas is not null)
            throw new DadosInvalidosException("Informe a data da próxima etapa junto com o prazo das listas.");
        if (proximaEtapaEmUtc is { } em && prazoListas is { } prazo && prazo > DateOnly.FromDateTime(em))
            throw new DadosInvalidosException("O prazo das listas não pode ser depois da etapa.");
        temporada.ProximaEtapaEm = proximaEtapaEmUtc is { } data ? DateTime.SpecifyKind(data, DateTimeKind.Utc) : null;
        temporada.PrazoListas = prazoListas;
    }
}
