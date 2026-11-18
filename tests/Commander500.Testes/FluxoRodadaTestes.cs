using Commander500.Dominio.Comum;
using Commander500.Dominio.Entidades;
using Commander500.Dominio.Enumeradores;
using Commander500.Dominio.Regras;

namespace Commander500.Testes;

public class FluxoRodadaTestes
{
    private static readonly DateTime Agora = new(2026, 11, 10, 20, 0, 0, DateTimeKind.Utc);
    private static readonly Dictionary<int, int> SemByes = new();

    /// <summary>Rodada em Rascunho com <paramref name="jogadores"/> presentes (Id da participação = 100 + id do jogador).</summary>
    private static Rodada RodadaRascunho(int jogadores, Temporada? temporada = null)
    {
        var rodada = new Rodada { Id = 1, Numero = 1, Situacao = SituacaoRodada.Rascunho, DuracaoMinutos = 60, Temporada = temporada ?? new Temporada() };
        FluxoRodada.DefinirParticipantes(rodada, Enumerable.Range(1, jogadores).Select(j => new ParticipanteInformado(j, 1000 + j)).ToList());
        foreach (var participacao in rodada.Participacoes) participacao.Id = 100 + participacao.IdJogador;
        return rodada;
    }

    private static Rodada RodadaIniciada(int jogadores = 8)
    {
        var rodada = RodadaRascunho(jogadores);
        FluxoRodada.Sortear(rodada, SemByes, new GeradorAleatorioComSemente(1));
        FluxoRodada.Iniciar(rodada, Agora);
        return rodada;
    }

    private static int AlguemSentado(Mesa mesa) => mesa.Cadeiras[0].Participacao!.Id;

    [Fact]
    public void Fluxo_completo_rascunho_sorteada_em_andamento_encerrada()
    {
        var temporada = new Temporada();
        var rodada = RodadaRascunho(9, temporada);
        Assert.Equal(SituacaoRodada.Rascunho, rodada.Situacao);

        FluxoRodada.Sortear(rodada, SemByes, new GeradorAleatorioComSemente(7));
        Assert.Equal(SituacaoRodada.Sorteada, rodada.Situacao);
        Assert.Equal(2, rodada.Mesas.Count);
        Assert.Single(rodada.Participacoes, p => p.Bye);
        Assert.All(rodada.Mesas, m => Assert.Equal([1, 2, 3, 4], m.Cadeiras.Select(c => c.Numero)));

        FluxoRodada.Iniciar(rodada, Agora, 75);
        Assert.Equal(SituacaoRodada.EmAndamento, rodada.Situacao);
        Assert.Equal(Agora, rodada.IniciadaEm);
        Assert.Equal(75, rodada.DuracaoMinutos);

        FluxoRodada.RegistrarResultado(rodada, rodada.Mesas[0], ResultadoMesa.Vitoria, AlguemSentado(rodada.Mesas[0]), Agora, "admin");
        FluxoRodada.RegistrarResultado(rodada, rodada.Mesas[1], ResultadoMesa.Empate, null, Agora, "admin");
        Assert.False(temporada.ConfiguracaoTravada);

        FluxoRodada.Encerrar(rodada, Agora.AddHours(1));
        Assert.Equal(SituacaoRodada.Encerrada, rodada.Situacao);
        Assert.Equal(Agora.AddHours(1), rodada.EncerradaEm);
        Assert.True(temporada.ConfiguracaoTravada); // spec 4.1: a configuração trava ao encerrar a primeira rodada
    }

    [Fact]
    public void Pode_sortear_de_novo_antes_de_iniciar_mas_nao_depois()
    {
        var rodada = RodadaRascunho(8);
        FluxoRodada.Sortear(rodada, SemByes, new GeradorAleatorioComSemente(1));
        var primeiro = rodada.Mesas.SelectMany(m => m.Cadeiras).Select(c => c.Participacao!.IdJogador).ToList();

        FluxoRodada.Sortear(rodada, SemByes, new GeradorAleatorioComSemente(2));
        Assert.Equal(SituacaoRodada.Sorteada, rodada.Situacao);
        Assert.Equal(2, rodada.Mesas.Count);
        Assert.NotEqual(primeiro, rodada.Mesas.SelectMany(m => m.Cadeiras).Select(c => c.Participacao!.IdJogador).ToList());

        FluxoRodada.Iniciar(rodada, Agora);
        Assert.Throws<RegraNegocioException>(() => FluxoRodada.Sortear(rodada, SemByes, new GeradorAleatorioComSemente(3)));
    }

    [Fact]
    public void Novo_sorteio_limpa_os_byes_anteriores()
    {
        var rodada = RodadaRascunho(5);
        FluxoRodada.Sortear(rodada, SemByes, new GeradorAleatorioComSemente(1));
        FluxoRodada.Sortear(rodada, SemByes, new GeradorAleatorioComSemente(2));
        Assert.Single(rodada.Participacoes, p => p.Bye);
    }

    [Fact]
    public void Sorteio_usa_os_byes_anteriores_da_temporada()
    {
        var rodada = RodadaRascunho(5);
        var anteriores = Enumerable.Range(1, 5).Where(j => j != 4).ToDictionary(j => j, _ => 1);

        FluxoRodada.Sortear(rodada, anteriores, new GeradorAleatorioComSemente(1));

        Assert.Equal(4, rodada.Participacoes.Single(p => p.Bye).IdJogador);
    }

    [Fact]
    public void Nao_sorteia_com_menos_de_quatro_jogadores()
    {
        var rodada = RodadaRascunho(3);
        Assert.Throws<RegraNegocioException>(() => FluxoRodada.Sortear(rodada, SemByes, new GeradorAleatorioComSemente(1)));
        Assert.Equal(SituacaoRodada.Rascunho, rodada.Situacao);
    }

    [Fact]
    public void Iniciar_exige_rodada_sorteada()
    {
        Assert.Throws<RegraNegocioException>(() => FluxoRodada.Iniciar(RodadaRascunho(4), Agora));
        var iniciada = RodadaIniciada();
        Assert.Throws<RegraNegocioException>(() => FluxoRodada.Iniciar(iniciada, Agora));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(601)]
    public void Iniciar_recusa_duracao_invalida(int minutos)
    {
        var rodada = RodadaRascunho(4);
        FluxoRodada.Sortear(rodada, SemByes, new GeradorAleatorioComSemente(1));
        Assert.Throws<RegraNegocioException>(() => FluxoRodada.Iniciar(rodada, Agora, minutos));
    }

    [Fact]
    public void Alterar_presentes_depois_do_sorteio_volta_para_rascunho()
    {
        var rodada = RodadaRascunho(8);
        FluxoRodada.Sortear(rodada, SemByes, new GeradorAleatorioComSemente(1));

        FluxoRodada.DefinirParticipantes(rodada, Enumerable.Range(1, 9).Select(j => new ParticipanteInformado(j, 1000 + j)).ToList());

        Assert.Equal(SituacaoRodada.Rascunho, rodada.Situacao);
        Assert.Empty(rodada.Mesas);
        Assert.Equal(9, rodada.Participacoes.Count);
        Assert.DoesNotContain(rodada.Participacoes, p => p.Bye);
    }

    [Fact]
    public void Definir_presentes_mantem_quem_ja_estava_troca_o_deck_e_remove_quem_saiu()
    {
        var rodada = RodadaRascunho(5);
        var participacaoJogador1 = rodada.Participacoes.Single(p => p.IdJogador == 1);

        FluxoRodada.DefinirParticipantes(rodada, [new(1, 2001), new(2, 1002), new(3, 1003), new(4, 1004)]);

        Assert.Same(participacaoJogador1, rodada.Participacoes.Single(p => p.IdJogador == 1));
        Assert.Equal(2001, participacaoJogador1.IdDeck);
        Assert.DoesNotContain(rodada.Participacoes, p => p.IdJogador == 5);
    }

    [Fact]
    public void Presentes_nao_mudam_depois_de_iniciar_nem_aceitam_repetidos()
    {
        Assert.Throws<RegraNegocioException>(() => FluxoRodada.DefinirParticipantes(RodadaIniciada(), [new(1, 1)]));
        Assert.Throws<RegraNegocioException>(() => FluxoRodada.DefinirParticipantes(RodadaRascunho(0), [new(1, 1), new(1, 2)]));
    }

    [Fact]
    public void Resultado_exige_rodada_iniciada()
    {
        var rodada = RodadaRascunho(4);
        FluxoRodada.Sortear(rodada, SemByes, new GeradorAleatorioComSemente(1));
        var mesa = rodada.Mesas[0];
        Assert.Throws<RegraNegocioException>(() => FluxoRodada.RegistrarResultado(rodada, mesa, ResultadoMesa.Empate, null, Agora, "admin"));
    }

    [Fact]
    public void Vencedor_precisa_estar_sentado_na_mesa()
    {
        var rodada = RodadaIniciada(8);
        var participacaoDeOutraMesa = AlguemSentado(rodada.Mesas[1]);

        Assert.Throws<RegraNegocioException>(() => FluxoRodada.RegistrarResultado(rodada, rodada.Mesas[0], ResultadoMesa.Vitoria, participacaoDeOutraMesa, Agora, "a"));
        Assert.Throws<RegraNegocioException>(() => FluxoRodada.RegistrarResultado(rodada, rodada.Mesas[0], ResultadoMesa.Vitoria, null, Agora, "a"));
        Assert.Throws<RegraNegocioException>(() => FluxoRodada.RegistrarResultado(rodada, rodada.Mesas[0], ResultadoMesa.Pendente, null, Agora, "a"));
    }

    [Fact]
    public void Empate_limpa_o_vencedor()
    {
        var rodada = RodadaIniciada(4);
        var mesa = rodada.Mesas[0];
        FluxoRodada.RegistrarResultado(rodada, mesa, ResultadoMesa.Vitoria, AlguemSentado(mesa), Agora, "a");
        FluxoRodada.RegistrarResultado(rodada, mesa, ResultadoMesa.Empate, null, Agora, "a");
        Assert.Null(mesa.IdParticipacaoVencedora);
        Assert.Equal(ResultadoMesa.Empate, mesa.Resultado);
    }

    [Fact]
    public void Nao_encerra_com_mesa_sem_resultado()
    {
        var rodada = RodadaIniciada(8);
        FluxoRodada.RegistrarResultado(rodada, rodada.Mesas[0], ResultadoMesa.Empate, null, Agora, "a");

        var erro = Assert.Throws<RegraNegocioException>(() => FluxoRodada.Encerrar(rodada, Agora));
        Assert.Contains("2", erro.Message);
        Assert.Equal(SituacaoRodada.EmAndamento, rodada.Situacao);
    }

    [Fact]
    public void Nao_encerra_rodada_que_nao_comecou()
    {
        var rodada = RodadaRascunho(4);
        FluxoRodada.Sortear(rodada, SemByes, new GeradorAleatorioComSemente(1));
        Assert.Throws<RegraNegocioException>(() => FluxoRodada.Encerrar(rodada, Agora));
    }

    [Fact]
    public void Resultado_pode_ser_corrigido_depois_de_encerrar_com_auditoria()
    {
        var rodada = RodadaIniciada(4);
        var mesa = rodada.Mesas[0];
        FluxoRodada.RegistrarResultado(rodada, mesa, ResultadoMesa.Empate, null, Agora, "admin");
        FluxoRodada.Encerrar(rodada, Agora);

        var depois = Agora.AddDays(1);
        var vencedor = mesa.Cadeiras[2].Participacao!.Id;
        FluxoRodada.RegistrarResultado(rodada, mesa, ResultadoMesa.Vitoria, vencedor, depois, "lojista@loja");

        Assert.Equal(SituacaoRodada.Encerrada, rodada.Situacao);
        Assert.Equal(vencedor, mesa.IdParticipacaoVencedora);
        Assert.Equal(depois, mesa.AlteradaEm);
        Assert.Equal("lojista@loja", mesa.AlteradaPor);
    }

    [Fact]
    public void So_rodada_nao_iniciada_pode_ser_excluida()
    {
        FluxoRodada.GarantirQuePodeExcluir(RodadaRascunho(4));
        Assert.Throws<RegraNegocioException>(() => FluxoRodada.GarantirQuePodeExcluir(RodadaIniciada()));
    }

    [Fact]
    public void Cronometro_e_calculado_pelo_inicio_e_duracao()
    {
        var rodada = RodadaIniciada();
        Assert.Null(FluxoRodada.ObterCronometro(RodadaRascunho(4), Agora));

        var cronometro = FluxoRodada.ObterCronometro(rodada, Agora.AddMinutes(15))!;
        Assert.Equal(Agora.AddMinutes(60), cronometro.TerminaEm);
        Assert.Equal(45 * 60, cronometro.SegundosRestantes);
        Assert.False(cronometro.Esgotado);

        var esgotado = FluxoRodada.ObterCronometro(rodada, Agora.AddMinutes(90))!;
        Assert.Equal(0, esgotado.SegundosRestantes);
        Assert.True(esgotado.Esgotado);
    }

    [Fact]
    public void Byes_anteriores_contam_so_outras_rodadas_ja_sorteadas()
    {
        var r1 = new MontadorRodada(1).Mesa(1, 1, 2, 3, 4).Bye(5).Montar();
        var r2 = new MontadorRodada(2, SituacaoRodada.Sorteada).MesaPendente(1, 2, 3, 5).Bye(4).Montar();
        var r3 = new MontadorRodada(3, SituacaoRodada.Sorteada).MesaPendente(1, 2, 3, 4).Bye(5).Montar();
        var rascunho = new MontadorRodada(4, SituacaoRodada.Rascunho).Bye(1).Montar(); // bye esquecido num rascunho: ignorado

        var contagem = FluxoRodada.ContarByesAnteriores([r1, r2, r3, rascunho], idRodadaIgnorada: 3);

        Assert.Equal(1, contagem[5]);
        Assert.Equal(1, contagem[4]);
        Assert.False(contagem.ContainsKey(1));
    }
}
