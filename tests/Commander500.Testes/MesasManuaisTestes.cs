using Commander500.Dominio.Comum;
using Commander500.Dominio.Entidades;
using Commander500.Dominio.Enumeradores;
using Commander500.Dominio.Regras;

namespace Commander500.Testes;

/// <summary>Mesas montadas à mão, rodadas retroativas, quem começa a mesa e bye repetido no mesmo dia.</summary>
public class MesasManuaisTestes
{
    private static readonly DateTime Agora = new(2026, 11, 18, 15, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Hoje = new(2026, 11, 18);

    /// <summary>Rodada em rascunho com <paramref name="quantidade"/> presentes (participação i = jogador i).</summary>
    private static Rodada RodadaComPresentes(int quantidade, DateOnly? data = null)
    {
        var rodada = new Rodada { Id = 1, Numero = 1, Data = data ?? Hoje, DuracaoMinutos = 60, Situacao = SituacaoRodada.Rascunho };
        for (var i = 1; i <= quantidade; i++)
            rodada.Participacoes.Add(new Participacao { Id = i, IdJogador = i, IdDeck = i, Rodada = rodada });
        return rodada;
    }

    [Fact]
    public void Montar_mesas_a_mao_respeita_a_ordem_e_quem_sobra_fica_de_bye()
    {
        var rodada = RodadaComPresentes(9);

        FluxoRodada.MontarMesas(rodada, [[3, 1, 2, 4], [5, 6, 7]]);

        Assert.Equal(SituacaoRodada.Sorteada, rodada.Situacao);
        Assert.Equal(2, rodada.Mesas.Count);
        Assert.Equal([3, 1, 2, 4], rodada.Mesas[0].Cadeiras.OrderBy(c => c.Numero).Select(c => c.IdParticipacao));
        Assert.Equal(3, rodada.Mesas[1].Cadeiras.Count);
        Assert.Equal([8, 9], rodada.Participacoes.Where(p => p.Bye).Select(p => p.IdJogador));
    }

    [Fact]
    public void Montar_mesas_recusa_jogador_repetido_mesa_pequena_e_quem_nao_esta_presente()
    {
        Assert.Throws<DadosInvalidosException>(() => FluxoRodada.MontarMesas(RodadaComPresentes(8), [[1, 2, 3, 4], [4, 5, 6, 7]]));
        Assert.Throws<DadosInvalidosException>(() => FluxoRodada.MontarMesas(RodadaComPresentes(8), [[1]]));
        Assert.Throws<DadosInvalidosException>(() => FluxoRodada.MontarMesas(RodadaComPresentes(4), [[1, 2, 3, 99]]));
        Assert.Throws<DadosInvalidosException>(() => FluxoRodada.MontarMesas(RodadaComPresentes(4), []));
    }

    [Fact]
    public void Trocar_quem_comeca_gira_as_cadeiras_mantendo_a_ordem()
    {
        var rodada = RodadaComPresentes(4);
        FluxoRodada.MontarMesas(rodada, [[1, 2, 3, 4]]);
        var mesa = rodada.Mesas[0];

        FluxoRodada.DefinirQuemComeca(rodada, mesa, idParticipacao: 3);

        Assert.Equal([3, 4, 1, 2], mesa.Cadeiras.OrderBy(c => c.Numero).Select(c => c.IdParticipacao));
        Assert.Throws<RegraNegocioException>(() => FluxoRodada.DefinirQuemComeca(rodada, mesa, idParticipacao: 99));
    }

    [Fact]
    public void Rodada_retroativa_recebe_resultado_e_encerra_sem_timer()
    {
        var rodada = RodadaComPresentes(4, data: Hoje.AddDays(-10));
        FluxoRodada.MontarMesas(rodada, [[1, 2, 3, 4]]);

        FluxoRodada.RegistrarResultado(rodada, rodada.Mesas[0], ResultadoMesa.Vitoria, 2, Agora, "admin", Hoje);
        FluxoRodada.Encerrar(rodada, Agora, Hoje);

        Assert.Equal(SituacaoRodada.Encerrada, rodada.Situacao);
        Assert.Null(rodada.IniciadaEm);
    }

    [Fact]
    public void Rodada_de_hoje_continua_exigindo_o_timer()
    {
        var rodada = RodadaComPresentes(4, data: Hoje);
        FluxoRodada.MontarMesas(rodada, [[1, 2, 3, 4]]);

        Assert.Throws<RegraNegocioException>(() =>
            FluxoRodada.RegistrarResultado(rodada, rodada.Mesas[0], ResultadoMesa.Vitoria, 2, Agora, "admin", Hoje));
        Assert.Throws<RegraNegocioException>(() => FluxoRodada.Encerrar(rodada, Agora, Hoje));
    }

    [Fact]
    public void Hoje_usa_o_fuso_da_loja()
    {
        // 01:00 UTC do dia 19 ainda é dia 18 em Brasília (22:00).
        Assert.Equal(new DateOnly(2026, 11, 18), FusoLoja.Hoje(new DateTime(2026, 11, 19, 1, 0, 0, DateTimeKind.Utc)));
    }

    [Fact]
    public void Quem_ja_ficou_de_bye_no_dia_nao_fica_de_novo_enquanto_houver_outro()
    {
        // 5 presentes → 1 bye. O jogador 1 já ficou de bye nesta etapa; mesmo tendo menos byes na temporada,
        // ele não pode ser o bye de novo.
        for (var semente = 0; semente < 200; semente++)
        {
            var candidatos = new List<CandidatoSorteio>
            {
                new(1, ByesAnteriores: 1, ByesNaEtapa: 1),
                new(2, ByesAnteriores: 3),
                new(3, ByesAnteriores: 3),
                new(4, ByesAnteriores: 3),
                new(5, ByesAnteriores: 3),
            };
            var resultado = SorteadorMesas.Sortear(candidatos, new GeradorAleatorioComSemente(semente));
            Assert.DoesNotContain(1, resultado.ChavesBye);
        }
    }

    [Fact]
    public void Sorteio_na_etapa_conta_os_byes_das_rodadas_anteriores_do_dia()
    {
        var anterior = new MontadorRodada(50, SituacaoRodada.Encerrada).Mesa(1, 1, 2, 3, 4).Bye(5).Montar();
        for (var semente = 0; semente < 100; semente++)
        {
            var rodada = RodadaComPresentes(5);
            FluxoRodada.Sortear(rodada, new Dictionary<int, int>(), new GeradorAleatorioComSemente(semente), [anterior]);
            Assert.DoesNotContain(rodada.Participacoes, p => p.Bye && p.IdJogador == 5);
        }
    }
}
