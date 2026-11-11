namespace Commander500.Api.Domain;

public enum DeckStatus { Pending, Approved, Rejected }

/// <summary>Draft → Drawn → InProgress → Closed (spec 4.6: Rascunho → Sorteada → EmAndamento → Encerrada).</summary>
public enum RoundStatus { Draft, Drawn, InProgress, Closed }

public enum TableResult { Pending, Win, Draw }

/// <summary>Tiebreak criteria applied after points (spec 6, [EM ABERTO]). Player name is always the final tiebreak.</summary>
public enum TiebreakCriterion { Wins, FewerMatches, MoreMatches, Draws, WinRate }

public class Season
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public bool ConfigLocked { get; set; }

    // Scoring config (spec 4.1). Locked once the first round of the season is closed.
    public int PointsWin { get; set; } = 3;
    public int PointsDraw { get; set; } = 1;
    public int PointsBye1 { get; set; } = 3;
    public int PointsBye2 { get; set; } = 3; // [EM ABERTO]
    public int PointsBye3 { get; set; } = 3; // [EM ABERTO]
    public int DefaultRoundMinutes { get; set; } = 60; // [EM ABERTO]
    /// <summary>Informational "time is up, play N more turns" rule (spec 9.3). 0 = none.</summary>
    public int ExtraTurnsAfterTime { get; set; }
    /// <summary>Minimum matches before a commander's win rate is shown (spec 6).</summary>
    public int MinMatchesForCommanderWinRate { get; set; } = 3;
    /// <summary>Comma-separated <see cref="TiebreakCriterion"/> list ([EM ABERTO]).</summary>
    public string Tiebreakers { get; set; } = "Wins,FewerMatches";

    public List<Round> Rounds { get; set; } = [];
    public List<Stage> Stages { get; set; } = [];

    public int ByePoints(int byeCount) => byeCount switch
    {
        1 => PointsBye1,
        2 => PointsBye2,
        3 => PointsBye3,
        _ => 0,
    };

    public IReadOnlyList<TiebreakCriterion> GetTiebreakers() => ParseTiebreakers(Tiebreakers);

    public static IReadOnlyList<TiebreakCriterion> ParseTiebreakers(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return [];
        var result = new List<TiebreakCriterion>();
        foreach (var part in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!Enum.TryParse<TiebreakCriterion>(part, ignoreCase: true, out var c))
                throw new DomainException($"Critério de desempate inválido: '{part}'.");
            if (!result.Contains(c)) result.Add(c);
        }
        return result;
    }
}

public class Player
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Nickname { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public List<Deck> Decks { get; set; } = [];
}

public class Commander
{
    public int Id { get; set; }
    /// <summary>Display name, as first registered.</summary>
    public string Name { get; set; } = "";
    /// <summary>Unique key, see <see cref="CommanderName.Normalize"/>.</summary>
    public string NormalizedName { get; set; } = "";

    // Card data cached from Scryfall (null when typed by hand / Scryfall unavailable; filled later by the enrichment job).
    public Guid? ScryfallId { get; set; }
    /// <summary>Cropped artwork (banners, thumbnails).</summary>
    public string? ArtUrl { get; set; }
    /// <summary>Full card image.</summary>
    public string? ImageUrl { get; set; }
    /// <summary>Color identity letters in WUBRG order, e.g. "WUB". Empty = colorless.</summary>
    public string? ColorIdentity { get; set; }
    /// <summary>Last Scryfall lookup attempt, to avoid retrying unknown names on every startup.</summary>
    public DateTime? ScryfallCheckedAt { get; set; }
}

public class Deck
{
    public int Id { get; set; }
    public int PlayerId { get; set; }
    public Player? Player { get; set; }
    public string Name { get; set; } = "";
    public string? ListUrl { get; set; }
    public DeckStatus Status { get; set; } = DeckStatus.Pending;
    public string? ReviewNote { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<DeckCommander> Commanders { get; set; } = [];
}

public class DeckCommander
{
    public int DeckId { get; set; }
    public Deck? Deck { get; set; }
    public int CommanderId { get; set; }
    public Commander? Commander { get; set; }
}

/// <summary>
/// Etapa: one league day inside a season, with one or more rounds. Has its own ranking and prize
/// (see <see cref="Services.StageRankingCalculator"/>). Created automatically by the round date.
/// </summary>
public class Stage
{
    public int Id { get; set; }
    public int SeasonId { get; set; }
    public Season? Season { get; set; }
    /// <summary>1, 2, 3… in date order within the season.</summary>
    public int Number { get; set; }
    public DateOnly Date { get; set; }
    public List<Round> Rounds { get; set; } = [];
}

public class Round
{
    public int Id { get; set; }
    public int SeasonId { get; set; }
    public Season? Season { get; set; }
    public int StageId { get; set; }
    public Stage? Stage { get; set; }
    /// <summary>Sequential within the season (1, 2, 3…). The position inside the stage is derived.</summary>
    public int Number { get; set; }
    /// <summary>Same as the stage date (kept for queries/history).</summary>
    public DateOnly Date { get; set; }
    public RoundStatus Status { get; set; } = RoundStatus.Draft;
    public DateTime? StartedAt { get; set; }
    public int DurationMinutes { get; set; }
    public DateTime? ClosedAt { get; set; }
    public List<RoundEntry> Entries { get; set; } = [];
    public List<GameTable> Tables { get; set; } = [];
}

/// <summary>A player's presence in a round, with the deck they play.</summary>
public class RoundEntry
{
    public int Id { get; set; }
    public int RoundId { get; set; }
    public Round? Round { get; set; }
    public int PlayerId { get; set; }
    public Player? Player { get; set; }
    public int DeckId { get; set; }
    public Deck? Deck { get; set; }
    public bool IsBye { get; set; }
}

/// <summary>A pod of 4 (spec "Table"). Named GameTable to avoid clashing with EF/SQL terms.</summary>
public class GameTable
{
    public int Id { get; set; }
    public int RoundId { get; set; }
    public Round? Round { get; set; }
    public int Number { get; set; }
    public TableResult Result { get; set; } = TableResult.Pending;
    public int? WinnerEntryId { get; set; }
    public RoundEntry? WinnerEntry { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public List<TableSeat> Seats { get; set; } = [];
}

public class TableSeat
{
    public int TableId { get; set; }
    public GameTable? Table { get; set; }
    public int RoundEntryId { get; set; }
    public RoundEntry? RoundEntry { get; set; }
    /// <summary>1..4; seat 1 plays first.</summary>
    public int Seat { get; set; }
}

public class AdminUser
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
}
