using Commander500.Dominio.Entidades;
using Commander500.Dominio.Enumeradores;
using Commander500.Dominio.Regras;

namespace Commander500.Testes;

/// <summary>Segunda rodada em diante de uma etapa: o sorteio evita repetir adversários e cadeiras.</summary>
public class SorteioNaEtapaTestes
{
    private static readonly Dictionary<int, int> SemByes = new();

    private static Rodada RodadaRascunho(int id, IEnumerable<int> jogadores)
    {
        var rodada = new Rodada { Id = id, Numero = id, Situacao = SituacaoRodada.Rascunho, DuracaoMinutos = 60, Temporada = new Temporada() };
        FluxoRodada.DefinirParticipantes(rodada, jogadores.Select(j => new ParticipanteInformado(j, 1000 + j)).ToList());
        foreach (var participacao in rodada.Participacoes) participacao.Id = id * 100 + participacao.IdJogador;
        return rodada;
    }

    private static HashSet<(int, int)> Pares(Rodada rodada) =>
        rodada.Mesas.SelectMany(m =>
        {
            var jogadores = m.Cadeiras.Select(c => c.Participacao!.IdJogador).ToList();
            return from a in jogadores from b in jogadores where a < b select (a, b);
        }).ToHashSet();

    private static Dictionary<int, int> Cadeiras(Rodada rodada) =>
        rodada.Mesas.SelectMany(m => m.Cadeiras).ToDictionary(c => c.Participacao!.IdJogador, c => c.Numero);

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(42)]
    public void Dezesseis_jogadores_ganham_adversarios_totalmente_novos(int semente)
    {
        // Rodada 1: {1-4}, {5-8}, {9-12}, {13-16}. A rodada 2 consegue misturar sem ninguém repetir adversário.
        var rodada1 = new MontadorRodada(1).Mesa(1, 1, 2, 3, 4).Mesa(5, 5, 6, 7, 8).Mesa(9, 9, 10, 11, 12).Mesa(13, 13, 14, 15, 16).Montar();
        var rodada2 = RodadaRascunho(2, Enumerable.Range(1, 16));

        FluxoRodada.Sortear(rodada2, SemByes, new GeradorAleatorioComSemente(semente), [rodada1]);

        Assert.Empty(Pares(rodada2).Intersect(Pares(rodada1)));
    }

    [Theory]
    [InlineData(3)]
    [InlineData(11)]
    public void Ninguem_repete_a_cadeira_quando_da_para_evitar(int semente)
    {
        // Só 4 jogadores: os adversários se repetem, mas as cadeiras podem mudar todas.
        var rodada1 = new MontadorRodada(1).Mesa(1, 1, 2, 3, 4).Montar();
        var rodada2 = RodadaRascunho(2, [1, 2, 3, 4]);

        FluxoRodada.Sortear(rodada2, SemByes, new GeradorAleatorioComSemente(semente), [rodada1]);

        var antes = Cadeiras(rodada1);
        Assert.All(Cadeiras(rodada2), jogadorCadeira => Assert.NotEqual(antes[jogadorCadeira.Key], jogadorCadeira.Value));
    }

    [Fact]
    public void Oito_jogadores_repetem_so_o_minimo_inevitavel()
    {
        // De {1-4},{5-8}, quaisquer duas mesas novas de 4 mantêm pelo menos 2 pares antigos cada (2+2) → 4 repetições.
        var rodada1 = new MontadorRodada(1).Mesa(1, 1, 2, 3, 4).Mesa(5, 5, 6, 7, 8).Montar();
        var rodada2 = RodadaRascunho(2, Enumerable.Range(1, 8));

        FluxoRodada.Sortear(rodada2, SemByes, new GeradorAleatorioComSemente(5), [rodada1]);

        Assert.Equal(4, Pares(rodada2).Intersect(Pares(rodada1)).Count());
    }

    [Fact]
    public void Quem_foi_embora_nao_entra_no_sorteio_e_a_rodada_ainda_evita_repeticoes()
    {
        var rodada1 = new MontadorRodada(1).Mesa(1, 1, 2, 3, 4).Mesa(5, 5, 6, 7, 8).Mesa(9, 9, 10, 11, 12).Mesa(13, 13, 14, 15, 16).Montar();
        // O jogador 16 foi embora → 15 presentes → 3 mesas + 3 byes.
        var rodada2 = RodadaRascunho(2, Enumerable.Range(1, 15));

        FluxoRodada.Sortear(rodada2, SemByes, new GeradorAleatorioComSemente(9), [rodada1]);

        Assert.Equal(3, rodada2.Mesas.Count);
        Assert.Equal(3, rodada2.Participacoes.Count(p => p.Bye));
        Assert.DoesNotContain(rodada2.Participacoes, p => p.IdJogador == 16);
        // Dependendo de quem fica de bye, uma mesa antiga pode manter 4 jogadores para só 3 mesas novas, então um par
        // repetido pode ser inevitável — mas nunca 3 jogadores da mesma mesa antiga juntos de novo.
        var mesaAntiga = (int idJogador) => (idJogador - 1) / 4;
        Assert.All(rodada2.Mesas, mesa =>
            Assert.True(mesa.Cadeiras.GroupBy(c => mesaAntiga(c.Participacao!.IdJogador)).Max(g => g.Count()) <= 2));
    }

    [Fact]
    public void Historico_conta_pares_de_todas_as_rodadas_anteriores_e_guarda_a_ultima_cadeira()
    {
        var r1 = new MontadorRodada(1).Mesa(1, 1, 2, 3, 4).Montar();
        var r2 = new MontadorRodada(2).Mesa(1, 2, 1, 4, 3).Montar();

        var historico = HistoricoEtapa.Montar([r2, r1]);

        Assert.Equal(2, historico.Pares[(1, 2)]);
        Assert.Equal(2, historico.UltimaCadeira[1]); // cadeira na rodada 2 (o jogador 1 foi o segundo)
        Assert.Equal(1, historico.UltimaCadeira[2]);
    }

    [Fact]
    public void Sem_historico_o_sorteio_e_aleatorio_simples()
    {
        var rodada = RodadaRascunho(1, Enumerable.Range(1, 8));
        FluxoRodada.Sortear(rodada, SemByes, new GeradorAleatorioComSemente(1));
        Assert.Equal(2, rodada.Mesas.Count);
        Assert.All(rodada.Mesas, m => Assert.Equal(4, m.Cadeiras.Count));
    }
}
