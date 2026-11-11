namespace Commander500.Api.Domain.Services;

public record EntryInput(int PlayerId, int DeckId);

/// <summary>Who already sat together in a stage, and each player's seat in the most recent round.</summary>
public record StageHistory(IReadOnlyDictionary<(int, int), int> Pairs, IReadOnlyDictionary<int, int> LastSeat)
{
    public static StageHistory From(IEnumerable<Round> rounds)
    {
        var pairs = new Dictionary<(int, int), int>();
        var lastSeat = new Dictionary<int, int>();
        foreach (var round in rounds.Where(r => r.Status != RoundStatus.Draft).OrderBy(r => r.Number))
        {
            var byId = round.Entries.ToDictionary(e => e.Id);
            foreach (var table in round.Tables)
            {
                var seated = table.Seats
                    .Select(s => (Player: (s.RoundEntry ?? byId[s.RoundEntryId]).PlayerId, s.Seat))
                    .ToList();
                foreach (var (player, seat) in seated) lastSeat[player] = seat; // later rounds overwrite
                for (var i = 0; i < seated.Count; i++)
                    for (var j = i + 1; j < seated.Count; j++)
                    {
                        var key = TableDrawer.PairKey(seated[i].Player, seated[j].Player);
                        pairs[key] = pairs.GetValueOrDefault(key) + 1;
                    }
            }
        }
        return new StageHistory(pairs, lastSeat);
    }
}

public record RoundTimer(DateTime StartedAt, int DurationMinutes, DateTime EndsAt, int RemainingSeconds, bool Expired);

/// <summary>Round state machine (spec 4.6). Operates on an in-memory entity graph; persistence is the caller's job.</summary>
public static class RoundWorkflow
{
    public const int MaxDurationMinutes = 600;

    /// <summary>Sets who is present and with which deck. Allowed in Draft/Drawn; a drawn round goes back to Draft.</summary>
    public static void SetEntries(Round round, IReadOnlyList<EntryInput> entries)
    {
        EnsureStatus(round, "alterar os presentes", RoundStatus.Draft, RoundStatus.Drawn);
        if (entries.Select(e => e.PlayerId).Distinct().Count() != entries.Count)
            throw new DomainException("Um jogador não pode aparecer duas vezes na mesma rodada.");

        ClearDraw(round);

        var wanted = entries.ToDictionary(e => e.PlayerId);
        round.Entries.RemoveAll(e => !wanted.ContainsKey(e.PlayerId));
        foreach (var input in entries)
        {
            var existing = round.Entries.FirstOrDefault(e => e.PlayerId == input.PlayerId);
            if (existing is null)
                round.Entries.Add(new RoundEntry { PlayerId = input.PlayerId, DeckId = input.DeckId, Round = round });
            else
                existing.DeckId = input.DeckId; // FK only; EF fixes up the navigation
        }
    }

    /// <summary>Draws (or re-draws) tables and byes. Allowed in Draft/Drawn (i.e. before the timer starts).</summary>
    /// <param name="previousByesByPlayer">Byes each player already had in the season (other rounds).</param>
    /// <param name="earlierStageRounds">
    /// Other drawn rounds of the same stage (etapa). The draw avoids putting players who already shared a table
    /// together again and avoids repeating each player's seat from the most recent of them.
    /// </param>
    public static void Draw(Round round, IReadOnlyDictionary<int, int> previousByesByPlayer, IRandomSource rng,
        IEnumerable<Round>? earlierStageRounds = null)
    {
        EnsureStatus(round, "sortear", RoundStatus.Draft, RoundStatus.Drawn);

        var entries = round.Entries;
        var history = StageHistory.From(earlierStageRounds ?? []);
        var candidates = entries
            .Select((e, i) => new DrawCandidate(i, previousByesByPlayer.GetValueOrDefault(e.PlayerId),
                history.LastSeat.TryGetValue(e.PlayerId, out var seat) ? seat : null))
            .ToList();

        // Translate player-id pairs into candidate-key pairs.
        var keyOf = entries.Select((e, i) => (e.PlayerId, i)).ToDictionary(x => x.PlayerId, x => x.i);
        var pairs = new Dictionary<(int, int), int>();
        foreach (var ((a, b), count) in history.Pairs)
            if (keyOf.TryGetValue(a, out var ka) && keyOf.TryGetValue(b, out var kb))
                pairs[TableDrawer.PairKey(ka, kb)] = count;

        var result = TableDrawer.Draw(candidates, rng, pairs);

        ClearDraw(round);
        foreach (var key in result.ByeKeys) entries[key].IsBye = true;

        for (var t = 0; t < result.Tables.Count; t++)
        {
            var table = new GameTable { Number = t + 1, Round = round };
            var seats = result.Tables[t];
            for (var s = 0; s < seats.Count; s++)
            {
                var entry = entries[seats[s]];
                table.Seats.Add(new TableSeat { Table = table, RoundEntry = entry, RoundEntryId = entry.Id, Seat = s + 1 });
            }
            round.Tables.Add(table);
        }
        round.Status = RoundStatus.Drawn;
    }

    /// <summary>Starts the timer. Drawn → InProgress.</summary>
    public static void Start(Round round, DateTime nowUtc, int? durationMinutes = null)
    {
        EnsureStatus(round, "iniciar", RoundStatus.Drawn);
        var duration = durationMinutes ?? round.DurationMinutes;
        if (duration is <= 0 or > MaxDurationMinutes)
            throw new DomainException($"Duração deve estar entre 1 e {MaxDurationMinutes} minutos.");

        round.DurationMinutes = duration;
        round.StartedAt = nowUtc;
        round.Status = RoundStatus.InProgress;
    }

    /// <summary>Records (or corrects, after closing) a table result.</summary>
    public static void SetResult(Round round, GameTable table, TableResult result, int? winnerEntryId, DateTime nowUtc, string? updatedBy)
    {
        EnsureStatus(round, "registrar resultado", RoundStatus.InProgress, RoundStatus.Closed);
        if (!round.Tables.Contains(table))
            throw new DomainException("A mesa não pertence a esta rodada.");

        switch (result)
        {
            case TableResult.Win:
                if (winnerEntryId is null || table.Seats.All(s => SeatEntryId(s) != winnerEntryId))
                    throw new DomainException("O vencedor precisa ser um jogador sentado nesta mesa.");
                table.WinnerEntryId = winnerEntryId;
                break;
            case TableResult.Draw:
                table.WinnerEntryId = null;
                table.WinnerEntry = null;
                break;
            default:
                throw new DomainException("Resultado deve ser vitória (com vencedor) ou empate.");
        }

        table.Result = result;
        table.UpdatedAt = nowUtc;
        table.UpdatedBy = updatedBy;
    }

    /// <summary>InProgress → Closed. Every table needs a result. Locks the season's scoring config.</summary>
    public static void Close(Round round, DateTime nowUtc)
    {
        EnsureStatus(round, "encerrar", RoundStatus.InProgress);
        var pending = round.Tables.Where(t => t.Result == TableResult.Pending).Select(t => t.Number).OrderBy(n => n).ToList();
        if (pending.Count > 0)
            throw new DomainException($"Mesas sem resultado: {string.Join(", ", pending)}.");

        round.Status = RoundStatus.Closed;
        round.ClosedAt = nowUtc;
        if (round.Season is not null) round.Season.ConfigLocked = true;
    }

    public static void EnsureDeletable(Round round) =>
        EnsureStatus(round, "excluir", RoundStatus.Draft, RoundStatus.Drawn);

    public static RoundTimer? GetTimer(Round round, DateTime nowUtc)
    {
        if (round.StartedAt is not { } started) return null;
        var ends = started.AddMinutes(round.DurationMinutes);
        var remaining = (int)Math.Max(0, Math.Ceiling((ends - nowUtc).TotalSeconds));
        return new RoundTimer(started, round.DurationMinutes, ends, remaining, remaining == 0);
    }

    /// <summary>Byes per player in the season, ignoring <paramref name="excludingRoundId"/> and undrawn rounds.</summary>
    public static Dictionary<int, int> CountPreviousByes(IEnumerable<Round> seasonRounds, int excludingRoundId) =>
        seasonRounds
            .Where(r => r.Id != excludingRoundId && r.Status != RoundStatus.Draft)
            .SelectMany(r => r.Entries)
            .Where(e => e.IsBye)
            .GroupBy(e => e.PlayerId)
            .ToDictionary(g => g.Key, g => g.Count());

    private static int SeatEntryId(TableSeat s) => s.RoundEntry?.Id ?? s.RoundEntryId;

    private static void ClearDraw(Round round)
    {
        round.Tables.Clear();
        foreach (var e in round.Entries) e.IsBye = false;
        round.Status = RoundStatus.Draft;
    }

    private static void EnsureStatus(Round round, string action, params RoundStatus[] allowed)
    {
        if (!allowed.Contains(round.Status))
            throw new DomainException($"Não é possível {action}: rodada está em '{StatusLabel(round.Status)}'.");
    }

    public static string StatusLabel(RoundStatus s) => s switch
    {
        RoundStatus.Draft => "Rascunho",
        RoundStatus.Drawn => "Sorteada",
        RoundStatus.InProgress => "Em andamento",
        RoundStatus.Closed => "Encerrada",
        _ => s.ToString(),
    };
}
