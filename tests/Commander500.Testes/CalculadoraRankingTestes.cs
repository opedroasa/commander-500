using Commander500.Dominio.Entidades;
using Commander500.Dominio.Enumeradores;
using Commander500.Dominio.Regras;

namespace Commander500.Testes;

public class CalculadoraRankingTestes
{
    private static readonly Dictionary<int, string> Nomes = Enumerable.Range(1, 12)
        .ToDictionary(i => i, i => $"Jogador {(char)('A' + i - 1)}");

    private static Temporada TemporadaPadrao() => new() { Id = 1, Nome = "T1" };

    private static LinhaRanking Linha(IReadOnlyList<LinhaRanking> ranking, int idJogador) => ranking.Single(l => l.IdJogador == idJogador);

    [Fact]
    public void Na_vitoria_so_o_vencedor_pontua()
    {
        var rodada = new MontadorRodada(1).Mesa(vencedor: 2, 1, 2, 3, 4).Montar();

        var ranking = CalculadoraRanking.Calcular(TemporadaPadrao(), [rodada], Nomes);

        Assert.Equal(3, Linha(ranking, 2).Pontos);
        Assert.Equal(1, Linha(ranking, 2).Vitorias);
        Assert.All(new[] { 1, 3, 4 }, jogador =>
        {
            Assert.Equal(0, Linha(ranking, jogador).Pontos);
            Assert.Equal(1, Linha(ranking, jogador).Derrotas);
            Assert.Equal(1, Linha(ranking, jogador).Partidas);
        });
        Assert.Equal(2, ranking[0].IdJogador);
    }

    [Fact]
    public void Empate_da_pontos_de_empate_para_os_quatro()
    {
        var rodada = new MontadorRodada(1).Mesa(vencedor: null, 1, 2, 3, 4).Montar();

        var ranking = CalculadoraRanking.Calcular(TemporadaPadrao(), [rodada], Nomes);

        Assert.All(ranking, linha =>
        {
            Assert.Equal(1, linha.Pontos);
            Assert.Equal(1, linha.Empates);
            Assert.Equal(0, linha.Vitorias);
        });
    }

    [Theory]
    [InlineData(1, 5)]
    [InlineData(2, 7)]
    [InlineData(3, 9)]
    public void Pontos_de_bye_dependem_de_quantos_ficaram_de_fora(int quantidadeByes, int pontosEsperados)
    {
        var temporada = new Temporada { PontosBye1 = 5, PontosBye2 = 7, PontosBye3 = 9 };
        var rodada = new MontadorRodada(1).Mesa(vencedor: 1, 1, 2, 3, 4).Bye(Enumerable.Range(5, quantidadeByes).ToArray()).Montar();

        var ranking = CalculadoraRanking.Calcular(temporada, [rodada], Nomes);

        Assert.All(ranking.Where(l => l.IdJogador >= 5), linha =>
        {
            Assert.Equal(pontosEsperados, linha.Pontos);
            Assert.Equal(1, linha.Byes);
            Assert.Equal(0, linha.Partidas); // bye não é partida
            Assert.Null(linha.PercentualVitoria);
        });
    }

    [Fact]
    public void Percentual_de_vitoria_ignora_byes()
    {
        var rodadas = new[]
        {
            new MontadorRodada(1).Mesa(vencedor: 1, 1, 2, 3, 4).Montar(),
            new MontadorRodada(2).Mesa(vencedor: 2, 1, 2, 3, 4).Montar(),
            new MontadorRodada(3).Mesa(vencedor: 2, 2, 3, 4, 5).Bye(1).Montar(),
        };

        var jogador1 = Linha(CalculadoraRanking.Calcular(TemporadaPadrao(), rodadas, Nomes), 1);

        Assert.Equal(2, jogador1.Partidas);
        Assert.Equal(1, jogador1.Byes);
        Assert.Equal(0.5, jogador1.PercentualVitoria);
        Assert.Equal(3 + 3, jogador1.Pontos);
    }

    [Fact]
    public void Rodadas_nao_encerradas_nao_contam()
    {
        var rodadas = new[]
        {
            new MontadorRodada(1).Mesa(vencedor: 1, 1, 2, 3, 4).Montar(),
            new MontadorRodada(2, SituacaoRodada.EmAndamento).Mesa(vencedor: 2, 1, 2, 3, 4).Montar(),
            new MontadorRodada(3, SituacaoRodada.Sorteada).MesaPendente(1, 2, 3, 4).Bye(5).Montar(),
        };

        var ranking = CalculadoraRanking.Calcular(TemporadaPadrao(), rodadas, Nomes);

        Assert.Equal(4, ranking.Count);
        Assert.Equal(3, Linha(ranking, 1).Pontos);
        Assert.Equal(0, Linha(ranking, 2).Pontos);
    }

    [Fact]
    public void Desempate_padrao_e_pontos_vitorias_menos_partidas_e_nome()
    {
        // J1: 1 vitória em 1 partida            = 3 pts, 1 vitória, 1 partida
        // J5: 3 empates em 3 partidas           = 3 pts, 0 vitórias, 3 partidas
        // J9: 1 vitória em 2 partidas           = 3 pts, 1 vitória, 2 partidas
        // J10: 3 pts por um bye                 = 3 pts, 0 vitórias, 0 partidas
        var rodadas = new[]
        {
            new MontadorRodada(1).Mesa(vencedor: 1, 1, 2, 3, 4).Mesa(vencedor: null, 5, 6, 7, 8).Bye(10).Montar(),
            new MontadorRodada(2).Mesa(vencedor: null, 5, 6, 7, 8).Mesa(vencedor: 9, 9, 2, 3, 4).Montar(),
            new MontadorRodada(3).Mesa(vencedor: null, 5, 6, 7, 8).Mesa(vencedor: 2, 9, 2, 3, 4).Montar(),
        };

        var ranking = CalculadoraRanking.Calcular(TemporadaPadrao(), rodadas, Nomes);
        var ordem = ranking.Where(l => l.Pontos == 3).Select(l => l.IdJogador).ToList();

        // J2 também termina com 3 pts: 1 vitória em 3 partidas.
        // vitórias ↓: {1, 9, 2} antes de {5..8, 10}; entre eles, menos partidas primeiro: 1 (1), 9 (2), 2 (3)
        // entre os sem vitória: 10 (0 partidas) antes de 5..8 (3 partidas cada), depois 5..8 pelo nome
        Assert.Equal([1, 9, 2, 10, 5, 6, 7, 8], ordem);
        Assert.Equal(Enumerable.Range(1, ranking.Count), ranking.Select(l => l.Posicao));
    }

    [Fact]
    public void Criterios_de_desempate_sao_configuraveis()
    {
        var rodadas = new[]
        {
            new MontadorRodada(1).Mesa(vencedor: 1, 1, 2, 3, 4).Mesa(vencedor: null, 5, 6, 7, 8).Montar(),
            new MontadorRodada(2).Mesa(vencedor: null, 5, 6, 7, 8).Montar(),
            new MontadorRodada(3).Mesa(vencedor: null, 5, 6, 7, 8).Montar(),
        };
        // J1 e J5..8 têm 3 pontos. Com "Empates" primeiro, quem empatou fica antes de J1.
        var temporada = new Temporada { CriteriosDesempate = "Empates,Vitorias" };

        var ranking = CalculadoraRanking.Calcular(temporada, rodadas, Nomes);

        Assert.Equal([5, 6, 7, 8, 1], ranking.Where(l => l.Pontos == 3).Select(l => l.IdJogador));
    }

    [Fact]
    public void Nome_desempata_o_que_sobrar()
    {
        var nomes = new Dictionary<int, string> { [1] = "Zeca", [2] = "Ana", [3] = "Ícaro", [4] = "bruno" };
        var rodada = new MontadorRodada(1).Mesa(vencedor: null, 1, 2, 3, 4).Montar();

        var ranking = CalculadoraRanking.Calcular(TemporadaPadrao(), [rodada], nomes);

        Assert.Equal(["Ana", "bruno", "Ícaro", "Zeca"], ranking.Select(l => l.NomeJogador));
    }

    [Fact]
    public void Corrigir_um_resultado_recalcula_o_ranking()
    {
        var rodada = new MontadorRodada(1).Mesa(vencedor: 1, 1, 2, 3, 4).Montar();
        var temporada = TemporadaPadrao();
        Assert.Equal(1, CalculadoraRanking.Calcular(temporada, [rodada], Nomes)[0].IdJogador);

        var mesa = rodada.Mesas[0];
        var participacaoJogador3 = rodada.Participacoes.Single(p => p.IdJogador == 3);
        FluxoRodada.RegistrarResultado(rodada, mesa, ResultadoMesa.Vitoria, participacaoJogador3.Id, DateTime.UtcNow, "admin");

        var ranking = CalculadoraRanking.Calcular(temporada, [rodada], Nomes);
        Assert.Equal(3, ranking[0].IdJogador);
        Assert.Equal(0, Linha(ranking, 1).Pontos);
    }

    [Fact]
    public void Pontuacao_usa_a_configuracao_da_temporada()
    {
        var temporada = new Temporada { PontosVitoria = 5, PontosEmpate = 2 };
        var rodadas = new[]
        {
            new MontadorRodada(1).Mesa(vencedor: 1, 1, 2, 3, 4).Montar(),
            new MontadorRodada(2).Mesa(vencedor: null, 1, 2, 3, 4).Montar(),
        };

        var ranking = CalculadoraRanking.Calcular(temporada, rodadas, Nomes);

        Assert.Equal(7, Linha(ranking, 1).Pontos);
        Assert.Equal(2, Linha(ranking, 2).Pontos);
    }
}
