using Commander500.Dominio.Entidades;
using Commander500.Dominio.Enumeradores;
using Commander500.Dominio.Regras;

namespace Commander500.Testes;

/// <summary>Modo de pontuação por colocação na etapa (padrão) e soma das rodadas.</summary>
public class CalculadoraRankingTemporadaTestes
{
    // Jogadores do exemplo do lojista (etapa 2 da pré-temporada).
    private const int Pedro = 1, Joao = 2, Miguel = 3, Heitor = 4, Murilo = 5, Gabriel = 6, Laro = 7, Tony = 8, Matheus = 9;

    private static readonly Dictionary<int, string> Nomes = new()
    {
        [Pedro] = "Pedro Alexandre", [Joao] = "João Antônio", [Miguel] = "Miguel", [Heitor] = "Heitor", [Murilo] = "Murilo Silva",
        [Gabriel] = "Gabriel ND", [Laro] = "Laro", [Tony] = "Tony", [Matheus] = "Matheus Máximo",
    };

    private static int _proximaRodada = 1000;

    private static Rodada NaEtapa(MontadorRodada montador, int idEtapa)
    {
        var rodada = montador.Montar();
        rodada.IdEtapa = idEtapa;
        return rodada;
    }

    /// <summary>
    /// Etapa do exemplo: Pedro e João com 2 vitórias (6), Miguel vitória + bye (6), Heitor 1 vitória (3),
    /// Murilo e Gabriel 1 bye (3), Laro, Tony e Matheus sem pontos. Pedro e João nunca se enfrentaram.
    /// </summary>
    private static List<Rodada> EtapaDoExemplo(int idEtapa) =>
    [
        NaEtapa(new MontadorRodada(_proximaRodada++).Mesa(Pedro, Pedro, Heitor, Laro, Tony).Mesa(Miguel, Joao, Miguel, Gabriel, Matheus).Bye(Murilo), idEtapa),
        NaEtapa(new MontadorRodada(_proximaRodada++).Mesa(Pedro, Pedro, Murilo, Gabriel, Laro).Mesa(Joao, Joao, Heitor, Tony, Matheus).Bye(Miguel), idEtapa),
        NaEtapa(new MontadorRodada(_proximaRodada++).Mesa(Joao, Joao, Murilo, Laro, Tony).Mesa(Heitor, Heitor, Pedro, Miguel, Matheus).Bye(Gabriel), idEtapa),
    ];

    [Fact]
    public void Ranking_da_etapa_do_exemplo_do_lojista()
    {
        var ranking = CalculadoraRankingEtapa.Calcular(EtapaDoExemplo(1), Nomes);

        int Posicao(int id) => ranking.Single(l => l.IdJogador == id).Posicao;
        int Pontos(int id) => ranking.Single(l => l.IdJogador == id).Pontos;
        Assert.Equal((1, 6), (Posicao(Pedro), Pontos(Pedro)));
        Assert.Equal((1, 6), (Posicao(Joao), Pontos(Joao)));
        Assert.Equal((3, 6), (Posicao(Miguel), Pontos(Miguel)));   // mesmo ponto, mas teve bye
        Assert.Equal((4, 3), (Posicao(Heitor), Pontos(Heitor)));
        Assert.Equal((5, 3), (Posicao(Murilo), Pontos(Murilo)));
        Assert.Equal((5, 3), (Posicao(Gabriel), Pontos(Gabriel)));
        Assert.Equal((7, 0), (Posicao(Laro), Pontos(Laro)));
        Assert.Equal((7, 0), (Posicao(Tony), Pontos(Tony)));
        Assert.Equal((7, 0), (Posicao(Matheus), Pontos(Matheus)));
    }

    [Fact]
    public void Colocacao_da_etapa_vira_pontos_da_temporada_e_empatados_ganham_a_melhor_posicao()
    {
        var temporada = new Temporada(); // padrão: por colocação, 5-4-3-2 e demais 1
        var etapa = new Etapa { Id = 1, Numero = 1, Data = new DateOnly(2026, 11, 7) };

        var ranking = CalculadoraRankingTemporada.Calcular(temporada, [etapa], EtapaDoExemplo(etapa.Id), Nomes);

        int Pontos(int id) => ranking.Single(l => l.IdJogador == id).Pontos;
        Assert.Equal(5, Pontos(Pedro));
        Assert.Equal(5, Pontos(Joao));
        Assert.Equal(3, Pontos(Miguel));
        Assert.Equal(2, Pontos(Heitor));
        Assert.Equal(1, Pontos(Murilo));
        Assert.Equal(1, Pontos(Laro)); // presença: quem terminou a etapa ganha pelo menos 1
        Assert.Equal(1, ranking.Single(l => l.IdJogador == Pedro).PrimeirosLugares);
        Assert.Equal(1, ranking.Single(l => l.IdJogador == Pedro).Etapas);
    }

    [Fact]
    public void Etapa_com_rodada_aberta_ainda_nao_pontua()
    {
        var temporada = new Temporada();
        var etapa = new Etapa { Id = 1, Numero = 1, Data = new DateOnly(2026, 11, 7) };
        var rodadas = EtapaDoExemplo(etapa.Id);
        rodadas.Add(NaEtapa(new MontadorRodada(_proximaRodada++, SituacaoRodada.EmAndamento).MesaPendente(Pedro, Joao, Miguel, Heitor), etapa.Id));

        var ranking = CalculadoraRankingTemporada.Calcular(temporada, [etapa], rodadas, Nomes);

        Assert.All(ranking, l => Assert.Equal(0, l.Pontos));
        Assert.Equal(2, ranking.Single(l => l.IdJogador == Pedro).Vitorias); // os contadores de partidas continuam
    }

    [Fact]
    public void Mesa_final_soma_os_pontos_da_colocacao()
    {
        var temporada = new Temporada();
        var etapa = new Etapa { Id = 1, Numero = 1, Data = new DateOnly(2026, 11, 7) };
        var final = new Etapa { Id = 2, Numero = 2, Data = new DateOnly(2026, 12, 5), Final = true, RodadasPrevistas = 2 };
        var rodadas = EtapaDoExemplo(etapa.Id);
        // Final: Heitor vence as duas → 1º (5); os outros três empatam → 2º (4 cada).
        rodadas.Add(NaEtapa(new MontadorRodada(_proximaRodada++).Mesa(Heitor, Pedro, Joao, Miguel, Heitor), final.Id));
        rodadas.Add(NaEtapa(new MontadorRodada(_proximaRodada++).Mesa(Heitor, Heitor, Miguel, Joao, Pedro), final.Id));

        var ranking = CalculadoraRankingTemporada.Calcular(temporada, [etapa, final], rodadas, Nomes);

        var heitor = ranking.Single(l => l.IdJogador == Heitor);
        Assert.Equal(2 + 5, heitor.Pontos);
        Assert.Equal(5, heitor.PontosFinal);
        Assert.Equal(1, heitor.Etapas); // a final não conta como etapa disputada
        Assert.Equal(5 + 4, ranking.Single(l => l.IdJogador == Pedro).Pontos);
        Assert.Null(ranking.Single(l => l.IdJogador == Laro).PontosFinal);
        // Campeão = maior total depois da final: Pedro e João com 9 (empate total → mesma posição).
        Assert.Equal([1, 1], ranking.Where(l => l.IdJogador is Pedro or Joao).Select(l => l.Posicao));
        Assert.True(ranking.First().Empatado);
    }

    [Fact]
    public void Empate_em_pontos_1os_lugares_e_etapas_usa_a_colocacao_na_ultima_etapa_em_comum()
    {
        var temporada = new Temporada { PontosPorColocacao = "5,4,3,2", PontosDemaisColocados = 1 };
        var e1 = new Etapa { Id = 1, Numero = 1, Data = new DateOnly(2026, 10, 24) };
        var e2 = new Etapa { Id = 2, Numero = 2, Data = new DateOnly(2026, 11, 7) };
        // Etapa 1: jogador 1 vence (5), jogador 2 vence a outra mesa mas teve bye antes → 2º (4).
        // Etapa 2: jogador 2 vence (5), jogador 1 2º (4). Total 9 x 9, um 1º lugar cada, 2 etapas cada.
        // Última etapa em comum = etapa 2 → jogador 2 (1º lá) fica na frente.
        var rodadas = new List<Rodada>
        {
            NaEtapa(new MontadorRodada(_proximaRodada++).Mesa(1, 1, 3, 4, 5).Bye(2), 1),
            NaEtapa(new MontadorRodada(_proximaRodada++).Mesa(1, 1, 3, 4, 2).Bye(5), 1),
            NaEtapa(new MontadorRodada(_proximaRodada++).Mesa(2, 2, 3, 4, 5).Bye(1), 2),
            NaEtapa(new MontadorRodada(_proximaRodada++).Mesa(2, 2, 3, 4, 1).Bye(5), 2),
        };

        var ranking = CalculadoraRankingTemporada.Calcular(temporada, [e1, e2], rodadas, Nomes);

        var um = ranking.Single(l => l.IdJogador == 1);
        var dois = ranking.Single(l => l.IdJogador == 2);
        Assert.Equal(um.Pontos, dois.Pontos);
        Assert.Equal(1, dois.Posicao);
        Assert.Equal(2, um.Posicao);
        Assert.False(dois.Empatado);
    }

    [Fact]
    public void Soma_das_rodadas_da_pontos_de_presenca_para_quem_perdeu()
    {
        // Exemplo do lojista: vitória 3, bye 2, derrota (presença) 1; quem faltou fica com 0.
        var temporada = new Temporada { ModoPontuacao = ModoPontuacao.SomaDasRodadas, PontosVitoria = 3, PontosBye1 = 2, PontosDerrota = 1 };
        var rodada = new MontadorRodada(_proximaRodada++).Mesa(Joao, Pedro, Murilo, Gabriel, Joao).Bye(Miguel).Montar();

        var ranking = CalculadoraRankingTemporada.Calcular(temporada, [], [rodada], Nomes);

        int Pontos(int id) => ranking.Single(l => l.IdJogador == id).Pontos;
        Assert.Equal(3, Pontos(Joao));
        Assert.Equal(2, Pontos(Miguel));
        Assert.Equal(1, Pontos(Pedro));
        Assert.Equal(1, Pontos(Murilo));
        Assert.DoesNotContain(ranking, l => l.IdJogador == Laro);
    }
}
