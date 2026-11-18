using Commander500.Dominio.Comum;
using Commander500.Dominio.Entidades;
using Commander500.Dominio.Enumeradores;
using Commander500.Dominio.Regras;
using Commander500.Infraestrutura.Dados;

namespace Commander500.Testes;

public class RegrasTemporadaTestes
{
    private static ConfiguracaoTemporada Configuracao(int pontosVitoria = 3) =>
        new(pontosVitoria, 1, 3, 3, 3, 60, 0, 3, [CriterioDesempate.Vitorias, CriterioDesempate.MenosPartidas]);

    [Fact]
    public void Valores_padrao_seguem_a_spec()
    {
        var t = new Temporada();
        Assert.Equal((3, 1, 3, 3, 3, 60), (t.PontosVitoria, t.PontosEmpate, t.PontosBye1, t.PontosBye2, t.PontosBye3, t.MinutosPadraoRodada));
        Assert.Equal([CriterioDesempate.Vitorias, CriterioDesempate.MenosPartidas], t.ObterCriteriosDesempate());
        Assert.Equal(3, t.MinimoPartidasPercentualComandante);
    }

    [Fact]
    public void Configuracao_pode_mudar_ate_travar()
    {
        var temporada = new Temporada();
        RegrasTemporada.AplicarConfiguracao(temporada, Configuracao(pontosVitoria: 4));
        Assert.Equal(4, temporada.PontosVitoria);

        temporada.ConfiguracaoTravada = true;
        Assert.Throws<RegraNegocioException>(() => RegrasTemporada.AplicarConfiguracao(temporada, Configuracao(pontosVitoria: 5)));
        Assert.Equal(4, temporada.PontosVitoria);
    }

    [Fact]
    public void Configuracao_invalida_e_recusada()
    {
        var temporada = new Temporada();
        Assert.Throws<DadosInvalidosException>(() => RegrasTemporada.AplicarConfiguracao(temporada, Configuracao(pontosVitoria: -1)));
        Assert.Throws<DadosInvalidosException>(() => RegrasTemporada.AplicarConfiguracao(temporada,
            Configuracao() with { CriteriosDesempate = [CriterioDesempate.MenosPartidas, CriterioDesempate.MaisPartidas] }));
        Assert.Throws<DadosInvalidosException>(() => RegrasTemporada.AplicarConfiguracao(temporada, Configuracao() with { MinutosPadraoRodada = 0 }));
    }

    [Fact]
    public void Criterio_de_desempate_desconhecido_e_recusado() =>
        Assert.Throws<RegraNegocioException>(() => Temporada.LerCriteriosDesempate("Vitorias,Sorte"));
}

public class NomeComandanteTestes
{
    [Theory]
    [InlineData("Atraxa, Praetors' Voice", "  atraxa,   PRAETORS’ voice ")]
    [InlineData("Éowyn, Fearless Knight", "eowyn, fearless knight")]
    public void Grafias_equivalentes_geram_a_mesma_chave(string a, string b) =>
        Assert.Equal(NomeComandante.Normalizar(a), NomeComandante.Normalizar(b));

    [Fact]
    public void Limpar_mantem_maiusculas_e_tira_espacos_sobrando() =>
        Assert.Equal("The Ur-Dragon", NomeComandante.Limpar("  The   Ur-Dragon "));
}

public class StringConexaoTestes
{
    [Fact]
    public void Endereco_postgres_e_convertido()
    {
        var resultado = StringConexao.Normalizar("postgresql://usuario:s%40nha@ep-x.neon.tech/banco?sslmode=require");
        Assert.Contains("Host=ep-x.neon.tech", resultado);
        Assert.Contains("Database=banco", resultado);
        Assert.Contains("Username=usuario", resultado);
        Assert.Contains("Password=s@nha", resultado);
        Assert.Contains("SSL Mode=Require", resultado);
    }

    [Fact]
    public void Formato_do_npgsql_e_mantido() =>
        Assert.Equal("Host=localhost;Database=x", StringConexao.Normalizar("Host=localhost;Database=x"));
}
