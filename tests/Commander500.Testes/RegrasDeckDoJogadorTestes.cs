using Commander500.Dominio.Entidades;
using Commander500.Dominio.Enumeradores;
using Commander500.Dominio.Regras;

namespace Commander500.Testes;

/// <summary>Deck reserva: a arte de quem ainda não jogou nenhuma partida.</summary>
public class RegrasDeckDoJogadorTestes
{
    private static readonly DateTime Base = new(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc);

    private static Deck NovoDeck(int id, SituacaoDeck situacao, bool comLista = false, int diasDepois = 0, string? nome = null) => new()
    {
        Id = id,
        IdJogador = 1,
        Nome = nome ?? $"Deck {id}",
        Situacao = situacao,
        UrlLista = comLista ? $"https://exemplo.com/lista/{id}" : null,
        DataCadastro = Base.AddDays(diasDepois),
    };

    [Fact]
    public void Sem_deck_nao_tem_reserva() =>
        Assert.Null(RegrasDeckDoJogador.DeckReserva([]));

    [Fact]
    public void Um_deck_so_e_ele_mesmo_ate_reprovado() =>
        Assert.Equal(7, RegrasDeckDoJogador.DeckReserva([NovoDeck(7, SituacaoDeck.Reprovado)])!.Id);

    [Fact]
    public void Aprovado_vem_antes_de_pendente_e_reprovado_mesmo_sem_lista_e_mais_antigo()
    {
        var decks = new[]
        {
            NovoDeck(1, SituacaoDeck.Reprovado, comLista: true, diasDepois: 10),
            NovoDeck(2, SituacaoDeck.Pendente, comLista: true, diasDepois: 5),
            NovoDeck(3, SituacaoDeck.Aprovado, comLista: false, diasDepois: 0),
        };
        Assert.Equal(3, RegrasDeckDoJogador.DeckReserva(decks)!.Id);
    }

    [Fact]
    public void Pendente_vem_antes_de_reprovado()
    {
        var decks = new[] { NovoDeck(1, SituacaoDeck.Reprovado, comLista: true), NovoDeck(2, SituacaoDeck.Pendente) };
        Assert.Equal(2, RegrasDeckDoJogador.DeckReserva(decks)!.Id);
    }

    [Fact]
    public void Na_mesma_situacao_o_deck_com_lista_vem_primeiro()
    {
        var decks = new[]
        {
            NovoDeck(1, SituacaoDeck.Aprovado, comLista: false, diasDepois: 9),
            NovoDeck(2, SituacaoDeck.Aprovado, comLista: true, diasDepois: 0),
        };
        Assert.Equal(2, RegrasDeckDoJogador.DeckReserva(decks)!.Id);
    }

    [Fact]
    public void Empatando_situacao_e_lista_vale_o_cadastrado_mais_recente()
    {
        var decks = new[]
        {
            NovoDeck(1, SituacaoDeck.Pendente, diasDepois: 1),
            NovoDeck(2, SituacaoDeck.Pendente, diasDepois: 3),
            NovoDeck(3, SituacaoDeck.Pendente, diasDepois: 2),
        };
        Assert.Equal(2, RegrasDeckDoJogador.DeckReserva(decks)!.Id);
    }

    [Fact]
    public void Cadastrados_na_mesma_hora_desempata_pelo_nome()
    {
        var decks = new[]
        {
            NovoDeck(1, SituacaoDeck.Aprovado, nome: "Zumbis"),
            NovoDeck(2, SituacaoDeck.Aprovado, nome: "artefatos"),
            NovoDeck(3, SituacaoDeck.Aprovado, nome: "Elfos"),
        };
        Assert.Equal(2, RegrasDeckDoJogador.DeckReserva(decks)!.Id);
    }
}
