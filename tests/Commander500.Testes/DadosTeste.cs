using Commander500.Dominio.Comum;
using Commander500.Dominio.Entidades;
using Commander500.Dominio.Enumeradores;

namespace Commander500.Testes;

/// <summary>Monta rodadas em memória (sem banco) para testar as regras do domínio.</summary>
public class MontadorRodada(int idRodada, SituacaoRodada situacao = SituacaoRodada.Encerrada)
{
    private static int _proximoIdParticipacao = 1;
    private static int _proximoIdMesa = 1;
    private readonly Rodada _rodada = new() { Id = idRodada, Numero = idRodada, Situacao = situacao, DuracaoMinutos = 60 };
    private readonly Dictionary<int, Deck> _decks = new();

    public MontadorRodada ComDeck(int idJogador, Deck deck) { _decks[idJogador] = deck; return this; }

    /// <param name="vencedor">Id do jogador vencedor, ou null para empate.</param>
    public MontadorRodada Mesa(int? vencedor, params int[] jogadores) =>
        AdicionarMesa(vencedor is null ? ResultadoMesa.Empate : ResultadoMesa.Vitoria, vencedor, jogadores);

    public MontadorRodada MesaPendente(params int[] jogadores) => AdicionarMesa(ResultadoMesa.Pendente, null, jogadores);

    public MontadorRodada Bye(params int[] jogadores)
    {
        foreach (var jogador in jogadores) Participacao(jogador).Bye = true;
        return this;
    }

    public Rodada Montar() => _rodada;

    private MontadorRodada AdicionarMesa(ResultadoMesa resultado, int? vencedor, int[] jogadores)
    {
        var mesa = new Mesa { Id = _proximoIdMesa++, Numero = _rodada.Mesas.Count + 1, IdRodada = _rodada.Id, Resultado = resultado };
        for (var i = 0; i < jogadores.Length; i++)
        {
            var participacao = Participacao(jogadores[i]);
            mesa.Cadeiras.Add(new Cadeira { Mesa = mesa, Participacao = participacao, IdParticipacao = participacao.Id, Numero = i + 1 });
            if (jogadores[i] == vencedor) mesa.IdParticipacaoVencedora = participacao.Id;
        }
        _rodada.Mesas.Add(mesa);
        return this;
    }

    private Participacao Participacao(int idJogador)
    {
        var deck = _decks.GetValueOrDefault(idJogador);
        var participacao = new Participacao
        {
            Id = _proximoIdParticipacao++, IdRodada = _rodada.Id, Rodada = _rodada, IdJogador = idJogador, Deck = deck, IdDeck = deck?.Id ?? 0,
        };
        _rodada.Participacoes.Add(participacao);
        return participacao;
    }
}

/// <summary>Cria decks com comandantes (o mesmo nome reaproveita o mesmo comandante).</summary>
public static class FabricaDecks
{
    private static int _proximoId = 1;
    private static readonly Dictionary<string, Comandante> Comandantes = new();

    public static Deck Com(params string[] nomesComandantes)
    {
        var deck = new Deck { Id = _proximoId++, Nome = string.Join("/", nomesComandantes) };
        foreach (var nome in nomesComandantes)
        {
            if (!Comandantes.TryGetValue(nome, out var comandante))
                Comandantes[nome] = comandante = new Comandante { Id = Comandantes.Count + 1, Nome = nome, NomeNormalizado = NomeComandante.Normalizar(nome) };
            deck.Comandantes.Add(new DeckComandante { Deck = deck, IdDeck = deck.Id, Comandante = comandante, IdComandante = comandante.Id });
        }
        return deck;
    }
}
