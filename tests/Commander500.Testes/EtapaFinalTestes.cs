using Commander500.Dominio.Comum;
using Commander500.Dominio.Entidades;
using Commander500.Dominio.Enumeradores;
using Commander500.Dominio.Regras;

namespace Commander500.Testes;

/// <summary>Mesa final, configuração dos modos de pontuação e aviso da próxima etapa.</summary>
public class EtapaFinalTestes
{
    private static readonly DateOnly Dia = new(2026, 12, 5);

    private static Temporada NovaTemporada() => new() { Id = 1, Nome = "Pré-temporada", DataInicio = new DateOnly(2026, 10, 14) };

    [Fact]
    public void Final_pode_ser_no_mesmo_dia_da_ultima_etapa_e_fica_por_ultimo()
    {
        var temporada = NovaTemporada();
        var etapas = new List<Etapa>();
        var ultima = RegrasEtapa.Resolver(temporada, etapas, Dia);

        var final = RegrasEtapa.CriarFinal(temporada, etapas, [], Dia, quantidadeRodadas: 3, quantidadeFinalistas: 4);

        Assert.True(final.Final);
        Assert.Equal(3, final.RodadasPrevistas);
        Assert.Equal(2, final.Numero);
        Assert.Same(ultima, RegrasEtapa.Resolver(temporada, etapas, Dia)); // rodadas normais do dia continuam na etapa normal
    }

    [Fact]
    public void Final_exige_modo_por_colocacao_rodadas_encerradas_e_quatro_finalistas()
    {
        var soma = NovaTemporada();
        soma.ModoPontuacao = ModoPontuacao.SomaDasRodadas;
        Assert.Throws<RegraNegocioException>(() => RegrasEtapa.CriarFinal(soma, [], [], Dia, 1, 4));

        var aberta = new Rodada { Situacao = SituacaoRodada.EmAndamento };
        Assert.Throws<RegraNegocioException>(() => RegrasEtapa.CriarFinal(NovaTemporada(), [], [aberta], Dia, 1, 4));
        Assert.Throws<DadosInvalidosException>(() => RegrasEtapa.CriarFinal(NovaTemporada(), [], [], Dia, 1, 3));
        Assert.Throws<DadosInvalidosException>(() => RegrasEtapa.CriarFinal(NovaTemporada(), [], [], Dia, 0, 4));

        var etapas = new List<Etapa>();
        RegrasEtapa.CriarFinal(NovaTemporada(), etapas, [], Dia, 1, 4);
        Assert.Throws<RegraNegocioException>(() => RegrasEtapa.CriarFinal(NovaTemporada(), etapas, [], Dia, 1, 4));
    }

    [Fact]
    public void Configuracao_grava_modo_e_tabela_de_colocacao()
    {
        var temporada = NovaTemporada();
        var configuracao = ConfiguracaoTemporada.De(temporada) with
        {
            ModoPontuacao = ModoPontuacao.SomaDasRodadas, PontosDerrota = 1, PontosPorColocacao = [6, 4, 2], PontosDemaisColocados = 0,
        };

        RegrasTemporada.AplicarConfiguracao(temporada, configuracao);

        Assert.Equal(ModoPontuacao.SomaDasRodadas, temporada.ModoPontuacao);
        Assert.Equal(1, temporada.PontosDerrota);
        Assert.Equal("6,4,2", temporada.PontosPorColocacao);
        Assert.Equal(6, temporada.PontosDaColocacao(1));
        Assert.Equal(0, temporada.PontosDaColocacao(4));
    }

    [Fact]
    public void Configuracao_recusa_tabela_vazia_ou_pontos_negativos()
    {
        var temporada = NovaTemporada();
        var padrao = ConfiguracaoTemporada.De(temporada);
        Assert.Throws<DadosInvalidosException>(() => RegrasTemporada.AplicarConfiguracao(temporada, padrao with { PontosPorColocacao = [] }));
        Assert.Throws<DadosInvalidosException>(() => RegrasTemporada.AplicarConfiguracao(temporada, padrao with { PontosDerrota = -1 }));
    }

    [Fact]
    public void Padrao_e_o_modo_por_colocacao_do_tratado()
    {
        var temporada = new Temporada();
        Assert.Equal(ModoPontuacao.ColocacaoNaEtapa, temporada.ModoPontuacao);
        Assert.Equal([5, 4, 3, 2, 1, 1], Enumerable.Range(1, 6).Select(temporada.PontosDaColocacao));
    }

    [Fact]
    public void Proxima_etapa_valida_o_prazo_das_listas()
    {
        var temporada = NovaTemporada();
        var etapa = new DateTime(2026, 11, 28, 17, 0, 0, DateTimeKind.Utc);

        RegrasTemporada.DefinirProximaEtapa(temporada, etapa, new DateOnly(2026, 11, 27));
        Assert.Equal(etapa, temporada.ProximaEtapaEm);

        Assert.Throws<DadosInvalidosException>(() => RegrasTemporada.DefinirProximaEtapa(temporada, etapa, new DateOnly(2026, 11, 29)));
        Assert.Throws<DadosInvalidosException>(() => RegrasTemporada.DefinirProximaEtapa(temporada, null, new DateOnly(2026, 11, 27)));
        RegrasTemporada.DefinirProximaEtapa(temporada, null, null);
        Assert.Null(temporada.ProximaEtapaEm);
    }
}
