using Commander500.Dominio.Regras;

namespace Commander500.Testes;

public class CalculadoraEstatisticasComandanteTestes
{
    [Fact]
    public void Dupla_de_parceiros_e_uma_linha_separada_do_comandante_sozinho()
    {
        var tymnaThrasios = FabricaDecks.Com("Tymna the Weaver", "Thrasios, Triton Hero");
        var tymnaKraum = FabricaDecks.Com("Tymna the Weaver", "Kraum, Ludevic's Opus");
        var atraxa = FabricaDecks.Com("Atraxa, Praetors' Voice");
        var urDragon = FabricaDecks.Com("The Ur-Dragon");

        var rodada = new MontadorRodada(1)
            .ComDeck(1, tymnaThrasios).ComDeck(2, tymnaKraum).ComDeck(3, atraxa).ComDeck(4, urDragon)
            .Mesa(vencedor: 1, 1, 2, 3, 4).Montar();

        var estatisticas = CalculadoraEstatisticasComandante.Calcular([rodada], minimoPartidasPercentual: 1);

        Assert.Equal(4, estatisticas.Count);
        var dupla = estatisticas.Single(e => e.IdsComandantes.Count == 2 && e.Nome.Contains("Thrasios"));
        Assert.Equal("Thrasios, Triton Hero + Tymna the Weaver", dupla.Nome);
        Assert.Equal(1, dupla.Vitorias);
        Assert.Equal(0, estatisticas.Single(e => e.Nome.Contains("Kraum")).Vitorias);
    }

    [Fact]
    public void Conta_decks_cadastrados_e_jogadores_diferentes()
    {
        var kinnan = FabricaDecks.Com("Kinnan, Bonder Prodigy");
        var kinnan2 = FabricaDecks.Com("Kinnan, Bonder Prodigy");
        var urza = FabricaDecks.Com("Urza, Lord High Artificer");
        var nuncaJogado = FabricaDecks.Com("Tivit, Seller of Secrets");
        var rodada = new MontadorRodada(1)
            .ComDeck(1, kinnan).ComDeck(2, kinnan2).ComDeck(3, urza).ComDeck(4, urza)
            .Mesa(vencedor: 1, 1, 2, 3, 4).Montar();

        var visaoTemporada = CalculadoraEstatisticasComandante.Calcular([rodada], 1, [kinnan, kinnan2, urza, nuncaJogado]);
        var linhaKinnan = visaoTemporada.Single(e => e.Nome.StartsWith("Kinnan"));
        Assert.Equal(2, linhaKinnan.Decks);
        Assert.Equal(2, linhaKinnan.Jogadores);
        Assert.Equal(2, linhaKinnan.Partidas);
        Assert.DoesNotContain(visaoTemporada, e => e.Nome.StartsWith("Tivit")); // sem partidas não aparece por padrão

        var visaoGeral = CalculadoraEstatisticasComandante.Calcular([rodada], 1, [kinnan, kinnan2, urza, nuncaJogado], incluirSemPartidas: true);
        var tivit = visaoGeral.Single(e => e.Nome.StartsWith("Tivit"));
        Assert.Equal((0, 1, 0), (tivit.Partidas, tivit.Decks, tivit.Jogadores));
        Assert.Null(tivit.PercentualVitoria);
    }

    [Fact]
    public void Bye_nao_e_partida_nem_vitoria()
    {
        var atraxa = FabricaDecks.Com("Atraxa, Praetors' Voice");
        var outro = FabricaDecks.Com("Teysa Karlov");
        var rodada = new MontadorRodada(1)
            .ComDeck(1, outro).ComDeck(2, outro).ComDeck(3, outro).ComDeck(4, outro).ComDeck(5, atraxa)
            .Mesa(vencedor: 1, 1, 2, 3, 4).Bye(5).Montar();

        var estatisticas = CalculadoraEstatisticasComandante.Calcular([rodada], 1);

        Assert.DoesNotContain(estatisticas, e => e.Nome.StartsWith("Atraxa"));
        var teysa = Assert.Single(estatisticas);
        Assert.Equal(4, teysa.Partidas);
        Assert.Equal(1, teysa.Vitorias);
    }

    [Fact]
    public void Percentual_de_vitoria_so_aparece_depois_do_minimo_de_partidas()
    {
        var a = FabricaDecks.Com("Commander A");
        var b = FabricaDecks.Com("Commander B");
        var rodadas = Enumerable.Range(1, 3).Select(i =>
        {
            var montador = new MontadorRodada(i).ComDeck(1, a).ComDeck(2, b).ComDeck(3, b).ComDeck(4, b);
            return montador.Mesa(vencedor: i == 1 ? 1 : null, 1, 2, 3, 4).Montar();
        }).ToList();

        var comDuas = CalculadoraEstatisticasComandante.Calcular(rodadas.Take(2), minimoPartidasPercentual: 3);
        Assert.Null(comDuas.Single(e => e.Nome == "Commander A").PercentualVitoria);

        var comTres = CalculadoraEstatisticasComandante.Calcular(rodadas, minimoPartidasPercentual: 3);
        var linhaA = comTres.Single(e => e.Nome == "Commander A");
        Assert.Equal(3, linhaA.Partidas);
        Assert.Equal(1, linhaA.Vitorias);
        Assert.Equal(2, linhaA.Empates);
        Assert.Equal(1.0 / 3, linhaA.PercentualVitoria!.Value, 5);
        Assert.Equal("Commander B", comTres[0].Nome); // o mais jogado primeiro (9 partidas)
    }
}
