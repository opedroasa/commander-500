using Commander500.Api.Domain;
using Commander500.Api.Domain.Services;

namespace Commander500.Api.Contracts;

// ---- Auth
public record LoginRequest(string Email, string Password);
public record LoginResponse(string Token, DateTime ExpiresAt, string Email);

// ---- Seasons
public record SeasonDto(
    int Id, string Name, DateOnly StartDate, DateOnly? EndDate, bool IsActive, bool ConfigLocked,
    SeasonConfig Config, int RoundsCount);

public record SeasonUpsertRequest(string Name, DateOnly StartDate, DateOnly? EndDate, bool IsActive = true, SeasonConfig? Config = null);

// ---- Players / decks / commanders
public record PlayerDto(int Id, string Name, string? Nickname, bool IsActive, DateTime CreatedAt, int DeckCount);
public record PlayerUpsertRequest(string Name, string? Nickname, bool IsActive = true);

public record CommanderDto(int Id, string Name);
public record CommanderUpsertRequest(string Name);

public record DeckDto(
    int Id, int PlayerId, string PlayerName, string Name, string? ListUrl,
    DeckStatus Status, string? ReviewNote, DateTime CreatedAt, IReadOnlyList<CommanderDto> Commanders);

/// <param name="Commanders">1 or 2 commander names; existing commanders are matched by normalized name, new ones are created.</param>
public record DeckUpsertRequest(int PlayerId, string Name, string? ListUrl, IReadOnlyList<string> Commanders);
public record DeckReviewRequest(DeckStatus Status, string? Note);

// ---- Rounds
public record CreateRoundRequest(DateOnly? Date, int? DurationMinutes);
public record StartRoundRequest(int? DurationMinutes);
public record TableResultRequest(TableResult Result, int? WinnerEntryId);

public record EntryDto(
    int Id, int PlayerId, string PlayerName, string? Nickname,
    int DeckId, string DeckName, DeckStatus DeckStatus, string Commanders, bool IsBye);

public record SeatDto(int Seat, EntryDto Entry);

public record TableDto(
    int Id, int Number, TableResult Result, int? WinnerEntryId,
    DateTime? UpdatedAt, string? UpdatedBy, IReadOnlyList<SeatDto> Seats);

public record TimerDto(DateTime StartedAt, int DurationMinutes, DateTime EndsAt, int RemainingSeconds, bool Expired);

public record RoundDto(
    int Id, int SeasonId, string SeasonName, int Number, DateOnly Date,
    RoundStatus Status, string StatusLabel, int DurationMinutes, int ExtraTurnsAfterTime,
    DateTime ServerNow, TimerDto? Timer, DateTime? ClosedAt,
    IReadOnlyList<EntryDto> Entries, IReadOnlyList<EntryDto> Byes, IReadOnlyList<TableDto> Tables);
