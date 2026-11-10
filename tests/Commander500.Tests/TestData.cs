using Commander500.Api.Domain;

namespace Commander500.Tests;

/// <summary>Builds in-memory round graphs (no database) for the pure domain services.</summary>
public class RoundBuilder(int roundId, RoundStatus status = RoundStatus.Closed)
{
    private static int _nextEntryId = 1;
    private static int _nextTableId = 1;
    private readonly Round _round = new() { Id = roundId, Number = roundId, Status = status, DurationMinutes = 60 };
    private readonly Dictionary<int, Deck> _decks = new();

    public RoundBuilder WithDeck(int playerId, Deck deck) { _decks[playerId] = deck; return this; }

    /// <param name="winner">Winning player id, or null for a draw.</param>
    public RoundBuilder Table(int? winner, params int[] players) => AddTable(winner is null ? TableResult.Draw : TableResult.Win, winner, players);

    public RoundBuilder PendingTable(params int[] players) => AddTable(TableResult.Pending, null, players);

    public RoundBuilder Bye(params int[] players)
    {
        foreach (var p in players) Entry(p).IsBye = true;
        return this;
    }

    public Round Build() => _round;

    private RoundBuilder AddTable(TableResult result, int? winner, int[] players)
    {
        var table = new GameTable { Id = _nextTableId++, Number = _round.Tables.Count + 1, RoundId = _round.Id, Result = result };
        for (var i = 0; i < players.Length; i++)
        {
            var e = Entry(players[i]);
            table.Seats.Add(new TableSeat { Table = table, RoundEntry = e, RoundEntryId = e.Id, Seat = i + 1 });
            if (players[i] == winner) table.WinnerEntryId = e.Id;
        }
        _round.Tables.Add(table);
        return this;
    }

    private RoundEntry Entry(int playerId)
    {
        var deck = _decks.GetValueOrDefault(playerId);
        var e = new RoundEntry { Id = _nextEntryId++, RoundId = _round.Id, Round = _round, PlayerId = playerId, Deck = deck, DeckId = deck?.Id ?? 0 };
        _round.Entries.Add(e);
        return e;
    }
}

public static class Decks
{
    private static int _nextId = 1;
    private static readonly Dictionary<string, Commander> Commanders = new();

    public static Deck Of(params string[] commanderNames)
    {
        var deck = new Deck { Id = _nextId++, Name = string.Join("/", commanderNames) };
        foreach (var name in commanderNames)
        {
            if (!Commanders.TryGetValue(name, out var c))
                Commanders[name] = c = new Commander { Id = Commanders.Count + 1, Name = name, NormalizedName = CommanderName.Normalize(name) };
            deck.Commanders.Add(new DeckCommander { Deck = deck, DeckId = deck.Id, Commander = c, CommanderId = c.Id });
        }
        return deck;
    }
}
