using Commander500.Dominio.Comum;
using Commander500.Dominio.Regras;

namespace Commander500.Testes;

public class SorteadorMesasTestes
{
    private static List<CandidatoSorteio> Candidatos(int quantidade, Func<int, int>? byesAnteriores = null) =>
        Enumerable.Range(1, quantidade).Select(chave => new CandidatoSorteio(chave, byesAnteriores?.Invoke(chave) ?? 0)).ToList();

    [Theory]
    [InlineData(4, 1, 0)]
    [InlineData(5, 1, 1)]
    [InlineData(6, 1, 2)]
    [InlineData(7, 1, 3)]
    [InlineData(8, 2, 0)]
    [InlineData(13, 3, 1)]
    [InlineData(23, 5, 3)]
    public void Distribui_os_jogadores_em_mesas_de_quatro_e_byes(int quantidade, int mesasEsperadas, int byesEsperados)
    {
        var resultado = SorteadorMesas.Sortear(Candidatos(quantidade), new GeradorAleatorioComSemente(quantidade));

        Assert.Equal(mesasEsperadas, resultado.Mesas.Count);
        Assert.All(resultado.Mesas, mesa => Assert.Equal(4, mesa.Count));
        Assert.Equal(byesEsperados, resultado.ChavesBye.Count);

        var todos = resultado.Mesas.SelectMany(m => m).Concat(resultado.ChavesBye).Order().ToList();
        Assert.Equal(Enumerable.Range(1, quantidade), todos); // cada jogador exatamente uma vez
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public void Menos_de_quatro_jogadores_nao_pode_sortear(int quantidade)
    {
        var erro = Assert.Throws<RegraNegocioException>(() => SorteadorMesas.Sortear(Candidatos(quantidade), new GeradorAleatorioComSemente(1)));
        Assert.Contains("4", erro.Message);
    }

    [Fact]
    public void Candidatos_duplicados_sao_recusados() =>
        Assert.Throws<RegraNegocioException>(() =>
            SorteadorMesas.Sortear([new(1, 0), new(1, 0), new(2, 0), new(3, 0), new(4, 0)], new GeradorAleatorioComSemente(1)));

    [Fact]
    public void Bye_vai_para_o_unico_jogador_sem_bye_anterior()
    {
        // 5 presentes → 1 bye. O jogador 3 nunca teve bye; todos os outros já tiveram um.
        for (var semente = 0; semente < 200; semente++)
        {
            var resultado = SorteadorMesas.Sortear(Candidatos(5, chave => chave == 3 ? 0 : 1), new GeradorAleatorioComSemente(semente));
            Assert.Equal([3], resultado.ChavesBye);
        }
    }

    [Fact]
    public void Byes_preferem_quem_tem_menos_byes_depois_o_proximo_nivel()
    {
        // 7 presentes → 3 byes. Jogadores 1 e 2 têm 0 byes, 3 e 4 têm 1, o resto tem 2.
        // Os dois sem bye ficam de fora, mais um dos que têm 1 bye.
        for (var semente = 0; semente < 200; semente++)
        {
            var resultado = SorteadorMesas.Sortear(Candidatos(7, chave => chave switch { 1 or 2 => 0, 3 or 4 => 1, _ => 2 }), new GeradorAleatorioComSemente(semente));
            Assert.Contains(1, resultado.ChavesBye);
            Assert.Contains(2, resultado.ChavesBye);
            Assert.Single(resultado.ChavesBye, chave => chave is 3 or 4);
        }
    }

    [Fact]
    public void Empate_na_prioridade_de_bye_e_decidido_aleatoriamente()
    {
        var byesPorJogador = new Dictionary<int, int>();
        for (var semente = 0; semente < 500; semente++)
            foreach (var chave in SorteadorMesas.Sortear(Candidatos(5), new GeradorAleatorioComSemente(semente)).ChavesBye)
                byesPorJogador[chave] = byesPorJogador.GetValueOrDefault(chave) + 1;

        Assert.Equal(5, byesPorJogador.Count); // todos os empatados ficaram de bye alguma vez
        Assert.All(byesPorJogador.Values, quantidade => Assert.InRange(quantidade, 50, 150)); // mais ou menos uniforme (esperado 100)
    }

    [Fact]
    public void Ordem_das_cadeiras_muda_entre_sorteios()
    {
        var quemFicouNaCadeira1 = Enumerable.Range(0, 100)
            .Select(semente => SorteadorMesas.Sortear(Candidatos(8), new GeradorAleatorioComSemente(semente)).Mesas[0][0])
            .Distinct().Count();
        Assert.True(quemFicouNaCadeira1 > 4, "a cadeira 1 não deve ir sempre para os mesmos jogadores");
    }

    [Fact]
    public void Gerador_criptografico_produz_sorteio_valido()
    {
        var resultado = SorteadorMesas.Sortear(Candidatos(10), new GeradorAleatorioCriptografico());
        Assert.Equal(2, resultado.Mesas.Count);
        Assert.Equal(2, resultado.ChavesBye.Count);
    }
}
