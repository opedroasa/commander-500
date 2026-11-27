using Commander500.Dominio.Comum;
using Commander500.Dominio.Entidades;
using Commander500.Dominio.Enumeradores;
using Commander500.Dominio.Regras;

namespace Commander500.Testes;

/// <summary>Empate parcial (só alguns da mesa empatam) e as estatísticas das páginas do jogador e do comandante.</summary>
public class EmpateParcialDesempenhoTestes
{
    private const int Pedro = 1, Murilo = 2, Gabriel = 3, Joao = 4;
    private static readonly Dictionary<int, string> Nomes = new() { [Pedro] = "Pedro", [Murilo] = "Murilo", [Gabriel] = "Gabriel", [Joao] = "João" };
    private static readonly DateTime Agora = new(2026, 11, 27, 15, 0, 0, DateTimeKind.Utc);

    private static Participacao Do(Rodada rodada, int idJogador) => rodada.Participacoes.Single(p => p.IdJogador == idJogador);

    /// <summary>Rodada em andamento com a mesa Pedro, Murilo, Gabriel e João.</summary>
    private static Rodada RodadaEmAndamento()
    {
        var rodada = new MontadorRodada(700, SituacaoRodada.EmAndamento).MesaPendente(Pedro, Murilo, Gabriel, Joao).Montar();
        rodada.Data = new DateOnly(2026, 11, 7);
        return rodada;
    }

    [Fact]
    public void Empate_entre_dois_da_pontos_de_empate_so_para_eles_e_derrota_para_os_outros()
    {
        // Pedro e Murilo morrem; Gabriel e João empatam.
        var rodada = RodadaEmAndamento();
        FluxoRodada.RegistrarResultado(rodada, rodada.Mesas[0], ResultadoMesa.Empate, null, Agora, "admin",
            idsEmpatados: [Do(rodada, Gabriel).Id, Do(rodada, Joao).Id]);
        FluxoRodada.Encerrar(rodada, Agora);

        var temporada = new Temporada { ModoPontuacao = ModoPontuacao.SomaDasRodadas, PontosEmpate = 1, PontosDerrota = 0 };
        var ranking = CalculadoraRanking.Calcular(temporada, [rodada], Nomes);
        LinhaRanking Linha(int id) => ranking.Single(l => l.IdJogador == id);

        Assert.Equal((1, 1, 0), (Linha(Gabriel).Pontos, Linha(Gabriel).Empates, Linha(Gabriel).Derrotas));
        Assert.Equal((1, 1, 0), (Linha(Joao).Pontos, Linha(Joao).Empates, Linha(Joao).Derrotas));
        Assert.Equal((0, 0, 1), (Linha(Pedro).Pontos, Linha(Pedro).Empates, Linha(Pedro).Derrotas));
        Assert.Equal((0, 0, 1), (Linha(Murilo).Pontos, Linha(Murilo).Empates, Linha(Murilo).Derrotas));
    }

    [Fact]
    public void Empate_sem_marcar_ninguem_continua_sendo_entre_todos()
    {
        var rodada = RodadaEmAndamento();
        FluxoRodada.RegistrarResultado(rodada, rodada.Mesas[0], ResultadoMesa.Empate, null, Agora, "admin");

        Assert.All(rodada.Participacoes, p => Assert.True(rodada.Mesas[0].Empatou(p.Id)));
        Assert.All(rodada.Mesas[0].Cadeiras, c => Assert.False(c.Empatou));
    }

    [Fact]
    public void Marcar_todos_como_empatados_e_o_mesmo_que_empate_geral()
    {
        var rodada = RodadaEmAndamento();
        FluxoRodada.RegistrarResultado(rodada, rodada.Mesas[0], ResultadoMesa.Empate, null, Agora, "admin",
            idsEmpatados: rodada.Participacoes.Select(p => p.Id).ToList());

        Assert.All(rodada.Mesas[0].Cadeiras, c => Assert.False(c.Empatou));
        Assert.All(rodada.Participacoes, p => Assert.True(rodada.Mesas[0].Empatou(p.Id)));
    }

    [Fact]
    public void Empate_com_um_jogador_so_ou_de_fora_da_mesa_e_recusado()
    {
        var rodada = RodadaEmAndamento();
        Assert.Throws<DadosInvalidosException>(() => FluxoRodada.RegistrarResultado(rodada, rodada.Mesas[0], ResultadoMesa.Empate, null,
            Agora, "admin", idsEmpatados: [Do(rodada, Joao).Id]));
        Assert.Throws<RegraNegocioException>(() => FluxoRodada.RegistrarResultado(rodada, rodada.Mesas[0], ResultadoMesa.Empate, null,
            Agora, "admin", idsEmpatados: [Do(rodada, Joao).Id, 99999]));
    }

    [Fact]
    public void Trocar_empate_parcial_por_vitoria_limpa_as_marcacoes()
    {
        var rodada = RodadaEmAndamento();
        var mesa = rodada.Mesas[0];
        FluxoRodada.RegistrarResultado(rodada, mesa, ResultadoMesa.Empate, null, Agora, "admin", idsEmpatados: [Do(rodada, Gabriel).Id, Do(rodada, Joao).Id]);
        FluxoRodada.RegistrarResultado(rodada, mesa, ResultadoMesa.Vitoria, Do(rodada, Pedro).Id, Agora, "admin");

        Assert.All(mesa.Cadeiras, c => Assert.False(c.Empatou));
        Assert.False(mesa.Empatou(Do(rodada, Gabriel).Id));
    }

    // ---------------------------------------------------------------- Desempenho (páginas)

    private static Rodada Encerrada(int id, int ano, int mes, Action<MontadorRodada> montar)
    {
        var montador = new MontadorRodada(id);
        montar(montador);
        var rodada = montador.Montar();
        rodada.Data = new DateOnly(ano, mes, 10);
        return rodada;
    }

    [Fact]
    public void Evolucao_mensal_com_percentual_do_mes_e_acumulado()
    {
        var rodadas = new[]
        {
            Encerrada(801, 2026, 10, m => m.Mesa(Pedro, Pedro, Murilo, Gabriel, Joao)),   // out: Pedro vence
            Encerrada(802, 2026, 10, m => m.Mesa(Joao, Pedro, Murilo, Gabriel, Joao)),    // out: Pedro perde
            Encerrada(803, 2026, 11, m => m.Mesa(Pedro, Pedro, Murilo, Gabriel, Joao)),   // nov: Pedro vence
        };

        var pontos = CalculadoraDesempenho.EvolucaoMensal(rodadas, p => p.IdJogador == Pedro);

        Assert.Equal(2, pontos.Count);
        Assert.Equal((2026, 10, 2, 1, 0.5, 0.5), (pontos[0].Ano, pontos[0].Mes, pontos[0].Partidas, pontos[0].Vitorias, pontos[0].PercentualMes, pontos[0].PercentualAcumulado));
        Assert.Equal((1.0, 2.0 / 3), (pontos[1].PercentualMes, pontos[1].PercentualAcumulado));
    }

    [Fact]
    public void Nemesis_e_vitima_favorita()
    {
        var rodadas = new[]
        {
            Encerrada(811, 2026, 11, m => m.Mesa(Joao, Pedro, Murilo, Gabriel, Joao)),
            Encerrada(812, 2026, 11, m => m.Mesa(Joao, Pedro, Murilo, Gabriel, Joao)),
            Encerrada(813, 2026, 11, m => m.Mesa(Pedro, Pedro, Murilo, Gabriel, Joao)),
            Encerrada(814, 2026, 11, m => m.Mesa(Pedro, Pedro, Gabriel)),
        };

        var confrontos = CalculadoraDesempenho.Confrontos(rodadas, Pedro);

        Assert.Equal(Joao, CalculadoraDesempenho.Nemesis(confrontos)!.IdAdversario);
        Assert.Equal(Gabriel, CalculadoraDesempenho.VitimaFavorita(confrontos)!.IdAdversario); // venceu 2 mesas com o Gabriel junto
        Assert.Equal(4, confrontos.Single(c => c.IdAdversario == Gabriel).Mesas);
    }

    [Fact]
    public void Melhor_deck_exige_minimo_de_partidas()
    {
        DesempenhoDeck[] decks =
        [
            new(10, Partidas: 1, Vitorias: 1, Empates: 0, Derrotas: 0, PercentualVitoria: 1.0),
            new(11, Partidas: 5, Vitorias: 2, Empates: 0, Derrotas: 3, PercentualVitoria: 0.4),
            new(12, Partidas: 4, Vitorias: 1, Empates: 0, Derrotas: 3, PercentualVitoria: 0.25),
        ];
        Assert.Equal(11, CalculadoraDesempenho.MelhorDeck(decks)!.IdDeck);
        Assert.Equal(11, CalculadoraDesempenho.MelhorDeck(decks, minimoPartidas: 10)!.IdDeck); // ninguém no mínimo → o de mais vitórias
    }

    [Fact]
    public void Chave_dos_comandantes_nao_depende_da_ordem() =>
        Assert.Equal(CalculadoraDesempenho.ChaveComandantes([34, 12]), CalculadoraDesempenho.ChaveComandantes([12, 34]));
}
