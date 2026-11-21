using Commander500.Dominio.Comum;
using Commander500.Dominio.Entidades;
using Commander500.Dominio.Enumeradores;
using Commander500.Dominio.Regras;

namespace Commander500.Testes;

/// <summary>Fotos, link do WhatsApp, Hall da Fama e troca de deck em rodada encerrada.</summary>
public class FotosHallDaFamaTestes
{
    // ---------------------------------------------------------------- Fotos

    [Theory]
    [InlineData("image/jpeg", "jpg")]
    [InlineData("image/png", "png")]
    [InlineData("image/webp", "webp")]
    [InlineData("IMAGE/JPEG; charset=binary", "jpg")]
    public void Foto_aceita_jpg_png_e_webp(string tipo, string extensao) =>
        Assert.Equal(extensao, RegrasFoto.Validar(tipo, 300_000));

    [Fact]
    public void Foto_recusa_outros_tipos_vazia_e_grande_demais()
    {
        Assert.Throws<DadosInvalidosException>(() => RegrasFoto.Validar("image/gif", 1000));
        Assert.Throws<DadosInvalidosException>(() => RegrasFoto.Validar("application/pdf", 1000));
        Assert.Throws<DadosInvalidosException>(() => RegrasFoto.Validar(null, 1000));
        Assert.Throws<DadosInvalidosException>(() => RegrasFoto.Validar("image/jpeg", 0));
        Assert.Throws<DadosInvalidosException>(() => RegrasFoto.Validar("image/jpeg", RegrasFoto.TamanhoMaximoBytes + 1));
    }

    [Fact]
    public void Legenda_vazia_vira_nula_e_longa_demais_e_recusada()
    {
        Assert.Null(RegrasFoto.NormalizarLegenda("   "));
        Assert.Equal("Top 3 da etapa 2", RegrasFoto.NormalizarLegenda("  Top 3 da etapa 2 "));
        Assert.Throws<DadosInvalidosException>(() => RegrasFoto.NormalizarLegenda(new string('a', RegrasFoto.TamanhoMaximoLegenda + 1)));
    }

    [Fact]
    public void Caminho_da_foto_tem_a_data_para_nao_vir_do_cache() =>
        Assert.Equal("etapas/12-20261121143005.jpg",
            RegrasFoto.MontarCaminho("etapas", 12, "jpg", new DateTime(2026, 11, 21, 14, 30, 5, DateTimeKind.Utc)));

    // ---------------------------------------------------------------- WhatsApp

    [Theory]
    [InlineData("https://chat.whatsapp.com/AbCdEf123")]
    [InlineData(" https://wa.me/5534999999999 ")]
    public void Link_do_whatsapp_aceito(string link) => Assert.NotNull(RegrasLoja.NormalizarLinkWhatsapp(link));

    [Fact]
    public void Link_vazio_esconde_o_botao() => Assert.Null(RegrasLoja.NormalizarLinkWhatsapp("  "));

    [Theory]
    [InlineData("http://chat.whatsapp.com/AbCdEf123")]
    [InlineData("https://site-estranho.com/grupo")]
    [InlineData("chat.whatsapp.com/AbCdEf123")]
    [InlineData("javascript:alert(1)")]
    public void Link_que_nao_e_do_whatsapp_e_recusado(string link) =>
        Assert.Throws<DadosInvalidosException>(() => RegrasLoja.NormalizarLinkWhatsapp(link));

    // ---------------------------------------------------------------- Hall da Fama

    [Fact]
    public void Deck_do_hall_da_fama_e_o_mais_usado_e_no_empate_o_mais_recente()
    {
        // Jogador 1: deck 10 em 2 rodadas, deck 11 em 1 → 10. Jogador 2: deck 20 e 21 uma vez cada → 21 (mais recente).
        static Rodada Rodada(int numero, int dia, params (int Jogador, int Deck)[] presentes)
        {
            var rodada = new Rodada { Id = numero, Numero = numero, Data = new DateOnly(2026, 11, dia), Situacao = SituacaoRodada.Encerrada };
            foreach (var (jogador, deck) in presentes) rodada.Participacoes.Add(new Participacao { IdJogador = jogador, IdDeck = deck });
            return rodada;
        }
        var rodadas = new[]
        {
            Rodada(1, 7, (1, 10), (2, 20)),
            Rodada(2, 7, (1, 11)),
            Rodada(3, 21, (1, 10), (2, 21)),
        };

        var decks = RegrasHallDaFama.DeckMaisUsado(rodadas);

        Assert.Equal(10, decks[1]);
        Assert.Equal(21, decks[2]);
    }

    // ---------------------------------------------------------------- Troca de deck

    private static (Rodada Rodada, Participacao Participacao) RodadaEncerrada(bool temporadaAtiva)
    {
        var temporada = new Temporada { Ativa = temporadaAtiva };
        var rodada = new MontadorRodada(900).Mesa(1, 1, 2, 3, 4).Montar();
        rodada.Temporada = temporada;
        return (rodada, rodada.Participacoes.First(p => p.IdJogador == 2));
    }

    [Fact]
    public void Troca_o_deck_numa_rodada_encerrada_de_temporada_aberta()
    {
        var (rodada, participacao) = RodadaEncerrada(temporadaAtiva: true);
        var deckCerto = new Deck { Id = 77, IdJogador = 2, Nome = "Certo" };

        FluxoRodada.TrocarDeck(rodada, participacao, deckCerto);

        Assert.Equal(77, participacao.IdDeck);
    }

    [Fact]
    public void Nao_troca_deck_de_outro_jogador()
    {
        var (rodada, participacao) = RodadaEncerrada(temporadaAtiva: true);
        Assert.Throws<DadosInvalidosException>(() => FluxoRodada.TrocarDeck(rodada, participacao, new Deck { Id = 78, IdJogador = 3, Nome = "Do 3" }));
    }

    [Fact]
    public void Temporada_encerrada_nao_aceita_troca_de_deck_nem_correcao_de_resultado()
    {
        var (rodada, participacao) = RodadaEncerrada(temporadaAtiva: false);
        Assert.Throws<RegraNegocioException>(() => FluxoRodada.TrocarDeck(rodada, participacao, new Deck { Id = 77, IdJogador = 2, Nome = "Certo" }));
        Assert.Throws<RegraNegocioException>(() =>
            FluxoRodada.RegistrarResultado(rodada, rodada.Mesas[0], ResultadoMesa.Empate, null, DateTime.UtcNow, "admin"));
    }
}
