using Commander500.Dominio.Comum;
using Commander500.Dominio.Entidades;
using Commander500.Dominio.Enumeradores;
using Commander500.Dominio.Regras;

namespace Commander500.Testes;

public class CalculadoraRankingEtapaTestes
{
    private static readonly Dictionary<int, string> Nomes = Enumerable.Range(1, 12)
        .ToDictionary(i => i, i => $"Jogador {(char)('A' + i - 1)}");

    private static LinhaRankingEtapa Linha(IReadOnlyList<LinhaRankingEtapa> ranking, int idJogador) => ranking.Single(l => l.IdJogador == idJogador);

    [Fact]
    public void Bye_conta_como_vitoria_na_etapa()
    {
        // Exemplo combinado: Ana (1) vence 2 mesas; Bruno (5) vence 1 mesa e fica de bye em 1 → os dois com 2 vitórias.
        var rodadas = new[]
        {
            new MontadorRodada(1).Mesa(vencedor: 1, 1, 2, 3, 4).Mesa(vencedor: 5, 5, 6, 7, 8).Montar(),
            new MontadorRodada(2).Mesa(vencedor: 1, 1, 2, 3, 4).Mesa(vencedor: 6, 6, 7, 8, 9).Bye(5).Montar(),
        };

        var ranking = CalculadoraRankingEtapa.Calcular(rodadas, Nomes);

        Assert.Equal(2, Linha(ranking, 1).Vitorias);
        Assert.Equal(2, Linha(ranking, 5).Vitorias);
        Assert.Equal(1, Linha(ranking, 5).VitoriasMesa);
        Assert.Equal(1, Linha(ranking, 5).Byes);
    }

    [Fact]
    public void Com_vitorias_iguais_quem_teve_bye_fica_abaixo()
    {
        var rodadas = new[]
        {
            new MontadorRodada(1).Mesa(vencedor: 1, 1, 2, 3, 4).Mesa(vencedor: 5, 5, 6, 7, 8).Montar(),
            new MontadorRodada(2).Mesa(vencedor: 1, 1, 2, 3, 4).Mesa(vencedor: 6, 6, 7, 8, 9).Bye(5).Montar(),
        };

        var ranking = CalculadoraRankingEtapa.Calcular(rodadas, Nomes);

        Assert.Equal(1, Linha(ranking, 1).Posicao);
        Assert.Equal(2, Linha(ranking, 5).Posicao);
        Assert.False(Linha(ranking, 1).Empatado);
        Assert.Equal([1, 5], ranking.Take(2).Select(l => l.IdJogador));
    }

    [Fact]
    public void Confronto_direto_desempata_dois_jogadores()
    {
        // 1 e 5 vencem 2 mesas cada, sem bye. Na rodada 2 se enfrentaram e o 5 venceu.
        var rodadas = new[]
        {
            new MontadorRodada(1).Mesa(vencedor: 1, 1, 2, 3, 4).Mesa(vencedor: 5, 5, 6, 7, 8).Montar(),
            new MontadorRodada(2).Mesa(vencedor: 5, 1, 5, 2, 6).Mesa(vencedor: 3, 3, 4, 7, 8).Montar(),
            new MontadorRodada(3).Mesa(vencedor: 1, 1, 2, 3, 4).Mesa(vencedor: 7, 5, 6, 7, 8).Montar(),
        };

        var ranking = CalculadoraRankingEtapa.Calcular(rodadas, Nomes);

        Assert.Equal(2, Linha(ranking, 1).Vitorias);
        Assert.Equal(2, Linha(ranking, 5).Vitorias);
        Assert.Equal(1, Linha(ranking, 5).Posicao);
        Assert.Equal(2, Linha(ranking, 1).Posicao);
        Assert.Equal(1, Linha(ranking, 5).VitoriasConfrontoDireto);
        Assert.Equal(0, Linha(ranking, 1).VitoriasConfrontoDireto);
    }

    [Fact]
    public void Sem_confronto_direto_os_jogadores_dividem_a_posicao()
    {
        // 1 e 5 vencem 1 mesa cada e nunca sentaram juntos.
        var rodada = new MontadorRodada(1).Mesa(vencedor: 1, 1, 2, 3, 4).Mesa(vencedor: 5, 5, 6, 7, 8).Montar();

        var ranking = CalculadoraRankingEtapa.Calcular([rodada], Nomes);

        Assert.Equal(1, Linha(ranking, 1).Posicao);
        Assert.Equal(1, Linha(ranking, 5).Posicao);
        Assert.True(Linha(ranking, 1).Empatado);
        Assert.True(Linha(ranking, 5).Empatado);
        // O grupo seguinte começa na posição 3 (1, 1, 3).
        Assert.All(ranking.Where(l => l.Vitorias == 0), l => Assert.Equal(3, l.Posicao));
    }

    [Fact]
    public void Confronto_direto_circular_continua_empatado()
    {
        // 1 vence 2, 2 vence 3, 3 vence 1 — cada um com 1 vitória e 1 vitória de confronto direto.
        var rodadas = new[]
        {
            new MontadorRodada(1).Mesa(vencedor: 1, 1, 2, 10, 11).Montar(),
            new MontadorRodada(2).Mesa(vencedor: 2, 2, 3, 10, 11).Montar(),
            new MontadorRodada(3).Mesa(vencedor: 3, 3, 1, 10, 11).Montar(),
        };

        var ranking = CalculadoraRankingEtapa.Calcular(rodadas, Nomes);

        Assert.All(new[] { 1, 2, 3 }, jogador =>
        {
            Assert.Equal(1, Linha(ranking, jogador).Posicao);
            Assert.True(Linha(ranking, jogador).Empatado);
            Assert.Equal(1, Linha(ranking, jogador).VitoriasConfrontoDireto);
        });
    }

    [Fact]
    public void Confronto_direto_conta_vitorias_contra_qualquer_um_do_grupo_empatado()
    {
        // 1, 2 e 3 terminam com 1 vitória. O 1 venceu 2 e 3 na mesma mesa; 2 e 3 venceram contra quem não está empatado.
        var rodadas = new[]
        {
            new MontadorRodada(1).Mesa(vencedor: 1, 1, 2, 3, 10).Montar(),
            new MontadorRodada(2).Mesa(vencedor: 2, 2, 9, 10, 11).Montar(),
            new MontadorRodada(3).Mesa(vencedor: 3, 3, 9, 10, 11).Montar(),
        };

        var ranking = CalculadoraRankingEtapa.Calcular(rodadas, Nomes);

        Assert.Equal(1, Linha(ranking, 1).Posicao);
        Assert.False(Linha(ranking, 1).Empatado);
        Assert.Equal(2, Linha(ranking, 2).Posicao);
        Assert.Equal(2, Linha(ranking, 3).Posicao);
        Assert.True(Linha(ranking, 2).Empatado);
    }

    [Fact]
    public void Rodadas_abertas_nao_contam()
    {
        var rodadas = new[]
        {
            new MontadorRodada(1).Mesa(vencedor: 1, 1, 2, 3, 4).Montar(),
            new MontadorRodada(2, SituacaoRodada.EmAndamento).Mesa(vencedor: 2, 1, 2, 3, 4).Montar(),
        };

        var ranking = CalculadoraRankingEtapa.Calcular(rodadas, Nomes);

        Assert.Equal(1, Linha(ranking, 1).Vitorias);
        Assert.Equal(0, Linha(ranking, 2).Vitorias);
        Assert.Equal(1, ranking[0].IdJogador);
    }

    [Fact]
    public void Empate_de_mesa_nao_e_vitoria()
    {
        var rodada = new MontadorRodada(1).Mesa(vencedor: null, 1, 2, 3, 4).Montar();

        var ranking = CalculadoraRankingEtapa.Calcular([rodada], Nomes);

        Assert.All(ranking, linha =>
        {
            Assert.Equal(0, linha.Vitorias);
            Assert.Equal(1, linha.Empates);
            Assert.Equal(1, linha.Posicao);
        });
    }
}

public class RegrasEtapaTestes
{
    private static Temporada Temporada() => new() { Id = 1, DataInicio = new DateOnly(2026, 11, 1), DataFim = new DateOnly(2026, 12, 15) };

    [Fact]
    public void Mesma_data_reaproveita_a_etapa()
    {
        var temporada = Temporada();
        var etapas = new List<Etapa>();

        var a = RegrasEtapa.Resolver(temporada, etapas, new DateOnly(2026, 12, 8));
        var b = RegrasEtapa.Resolver(temporada, etapas, new DateOnly(2026, 12, 8));

        Assert.Same(a, b);
        Assert.Single(etapas);
        Assert.Equal(1, a.Numero);
    }

    [Fact]
    public void Etapas_sao_numeradas_pela_data_mesmo_criadas_fora_de_ordem()
    {
        var temporada = Temporada();
        var etapas = new List<Etapa>();

        var dia8Dez = RegrasEtapa.Resolver(temporada, etapas, new DateOnly(2026, 12, 8));
        var dia15Nov = RegrasEtapa.Resolver(temporada, etapas, new DateOnly(2026, 11, 15));
        var dia1Dez = RegrasEtapa.Resolver(temporada, etapas, new DateOnly(2026, 12, 1));

        Assert.Equal((1, 2, 3), (dia15Nov.Numero, dia1Dez.Numero, dia8Dez.Numero));
    }

    [Theory]
    [InlineData(2026, 10, 31)]
    [InlineData(2026, 12, 16)]
    public void Data_precisa_estar_dentro_da_temporada(int ano, int mes, int dia) =>
        Assert.Throws<DadosInvalidosException>(() => RegrasEtapa.Resolver(Temporada(), [], new DateOnly(ano, mes, dia)));

    [Fact]
    public void Temporada_sem_fim_aceita_qualquer_data_posterior()
    {
        var temporada = Temporada();
        temporada.DataFim = null;
        Assert.Equal(1, RegrasEtapa.Resolver(temporada, [], new DateOnly(2027, 3, 1)).Numero);
    }
}
