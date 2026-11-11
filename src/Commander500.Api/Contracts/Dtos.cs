using Commander500.Api.Domain;
using Commander500.Api.Domain.Services;

namespace Commander500.Api.Contracts;

// ---- Auth
public record LoginRequest(string Email, string Password);
public record LoginResponse(string Token, DateTime ExpiresAt, string Email);

// ---- Seasons
public record SeasonDto(
    int Id, string Name, DateOnly StartDate, DateOnly? EndDate, bool IsActive, bool ConfigLocked,
    SeasonConfig Config, int RoundsCount, int StagesCount);

public record SeasonUpsertRequest(string Name, DateOnly StartDate, DateOnly? EndDate, bool IsActive = true, SeasonConfig? Config = null);

// ---- Players / decks / commanders
public record PlayerDto(int Id, string Name, string? Nickname, bool IsActive, DateTime CreatedAt, int DeckCount);
public record PlayerUpsertRequest(string Name, string? Nickname, bool IsActive = true);

/// <param name="ArtUrl">Cropped artwork from Scryfall (null if unknown).</param>
/// <param name="ColorIdentity">WUBRG letters, "" = colorless, null = unknown.</param>
public record CommanderDto(int Id, string Name, string? ArtUrl, string? ImageUrl, string? ColorIdentity);

/// <summary>Commander chosen in the deck form: from the Scryfall search (with id) or typed by hand (name only).</summary>
public record CommanderInput(Guid? ScryfallId, string Name);

/// <summary>Scryfall search result for the commander picker.</summary>
public record ScryfallCardDto(Guid Id, string Name, string? ArtUrl, string? ImageUrl, string ColorIdentity, string TypeLine);

public record DeckDto(
    int Id, int PlayerId, string PlayerName, string Name, string? ListUrl,
    DeckStatus Status, string? ReviewNote, DateTime CreatedAt, IReadOnlyList<CommanderDto> Commanders);

/// <param name="Name">Optional: defaults to the commander name(s).</param>
/// <param name="Commanders">1 or 2 commanders (partner / background).</param>
public record DeckUpsertRequest(int PlayerId, string? Name, string? ListUrl, IReadOnlyList<CommanderInput> Commanders);
public record DeckReviewRequest(DeckStatus Status, string? Note);

// ---- Statistics
public record CommanderStatsDto(
    string Name, IReadOnlyList<CommanderDto> Commanders,
    int Matches, int Wins, int Draws, double? WinRate, int Decks, int Players);

public record PlayerStatsDto(int Rounds, int Matches, int Wins, int Draws, int Losses, int Byes, double? WinRate);
public record PlayerSeasonStatsDto(int SeasonId, string SeasonName, int? Position, int Points, PlayerStatsDto Stats);
public record SeasonRefDto(int Id, string Name);
public record StageRefDto(int Id, int SeasonId, string SeasonName, int Number, DateOnly Date);

/// <summary>Mini profile. <see cref="DeckDto.ReviewNote"/> is only filled for the admin.</summary>
public record PlayerProfileDto(
    int Id, string Name, string? Nickname, bool IsActive,
    PlayerStatsDto Overall, int SeasonsPlayed, PlayerSeasonStatsDto? Season,
    IReadOnlyList<SeasonRefDto> SeasonsWon, IReadOnlyList<StageRefDto> StagesWon,
    IReadOnlyList<DeckDto> Decks);

// ---- Stages (etapas)
public record PlayerRefDto(int Id, string Name);

/// <param name="IsComplete">All rounds closed: the ranking (and winners) are final.</param>
public record StageDto(
    int Id, int SeasonId, string SeasonName, int Number, DateOnly Date,
    int RoundsCount, int ClosedRounds, bool IsComplete, IReadOnlyList<PlayerRefDto> Leaders);

public record StageDetailDto(StageDto Stage, IReadOnlyList<StageRankingRow> Ranking, IReadOnlyList<RoundDto> Rounds);

// ---- Rounds
/// <param name="Date">Stage (etapa) date; rounds with the same date share a stage.</param>
public record CreateRoundRequest(DateOnly? Date, int? DurationMinutes);
public record StartRoundRequest(int? DurationMinutes);
public record TableResultRequest(TableResult Result, int? WinnerEntryId);

public record EntryDto(
    int Id, int PlayerId, string PlayerName, string? Nickname,
    int DeckId, string DeckName, DeckStatus DeckStatus, string Commanders, string? CommanderArtUrl, bool IsBye);

public record SeatDto(int Seat, EntryDto Entry);

public record TableDto(
    int Id, int Number, TableResult Result, int? WinnerEntryId,
    DateTime? UpdatedAt, string? UpdatedBy, IReadOnlyList<SeatDto> Seats);

public record TimerDto(DateTime StartedAt, int DurationMinutes, DateTime EndsAt, int RemainingSeconds, bool Expired);

/// <param name="Number">Sequential within the season.</param>
/// <param name="NumberInStage">1, 2, 3… within the stage (etapa).</param>
public record RoundDto(
    int Id, int SeasonId, string SeasonName, int StageId, int StageNumber, int NumberInStage, int Number, DateOnly Date,
    RoundStatus Status, string StatusLabel, int DurationMinutes, int ExtraTurnsAfterTime,
    DateTime ServerNow, TimerDto? Timer, DateTime? ClosedAt,
    IReadOnlyList<EntryDto> Entries, IReadOnlyList<EntryDto> Byes, IReadOnlyList<TableDto> Tables);
