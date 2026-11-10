using Commander500.Api.Domain;
using Commander500.Api.Domain.Services;

namespace Commander500.Tests;

public class RoundWorkflowTests
{
    private static readonly DateTime Now = new(2026, 11, 10, 20, 0, 0, DateTimeKind.Utc);
    private static readonly Dictionary<int, int> NoByes = new();

    /// <summary>A Draft round with <paramref name="players"/> entries (entry ids = 100 + player id).</summary>
    private static Round DraftRound(int players, Season? season = null)
    {
        var round = new Round { Id = 1, Number = 1, Status = RoundStatus.Draft, DurationMinutes = 60, Season = season ?? new Season() };
        RoundWorkflow.SetEntries(round, Enumerable.Range(1, players).Select(p => new EntryInput(p, 1000 + p)).ToList());
        foreach (var e in round.Entries) e.Id = 100 + e.PlayerId;
        return round;
    }

    private static Round StartedRound(int players = 8)
    {
        var round = DraftRound(players);
        RoundWorkflow.Draw(round, NoByes, new SeededRandomSource(1));
        RoundWorkflow.Start(round, Now);
        return round;
    }

    private static int AnySeatedEntry(GameTable t) => t.Seats[0].RoundEntry!.Id;

    [Fact]
    public void Full_flow_draft_drawn_inprogress_closed()
    {
        var season = new Season();
        var round = DraftRound(9, season);
        Assert.Equal(RoundStatus.Draft, round.Status);

        RoundWorkflow.Draw(round, NoByes, new SeededRandomSource(7));
        Assert.Equal(RoundStatus.Drawn, round.Status);
        Assert.Equal(2, round.Tables.Count);
        Assert.Single(round.Entries, e => e.IsBye);
        Assert.All(round.Tables, t => Assert.Equal([1, 2, 3, 4], t.Seats.Select(s => s.Seat)));

        RoundWorkflow.Start(round, Now, 75);
        Assert.Equal(RoundStatus.InProgress, round.Status);
        Assert.Equal(Now, round.StartedAt);
        Assert.Equal(75, round.DurationMinutes);

        RoundWorkflow.SetResult(round, round.Tables[0], TableResult.Win, AnySeatedEntry(round.Tables[0]), Now, "admin");
        RoundWorkflow.SetResult(round, round.Tables[1], TableResult.Draw, null, Now, "admin");
        Assert.False(season.ConfigLocked);

        RoundWorkflow.Close(round, Now.AddHours(1));
        Assert.Equal(RoundStatus.Closed, round.Status);
        Assert.Equal(Now.AddHours(1), round.ClosedAt);
        Assert.True(season.ConfigLocked); // spec 4.1: config locks once the first round is closed
    }

    [Fact]
    public void Can_redraw_before_start_but_not_after()
    {
        var round = DraftRound(8);
        RoundWorkflow.Draw(round, NoByes, new SeededRandomSource(1));
        var first = round.Tables.SelectMany(t => t.Seats).Select(s => s.RoundEntry!.PlayerId).ToList();

        RoundWorkflow.Draw(round, NoByes, new SeededRandomSource(2));
        Assert.Equal(RoundStatus.Drawn, round.Status);
        Assert.Equal(2, round.Tables.Count);
        Assert.NotEqual(first, round.Tables.SelectMany(t => t.Seats).Select(s => s.RoundEntry!.PlayerId).ToList());

        RoundWorkflow.Start(round, Now);
        Assert.Throws<DomainException>(() => RoundWorkflow.Draw(round, NoByes, new SeededRandomSource(3)));
    }

    [Fact]
    public void Redraw_clears_previous_byes()
    {
        var round = DraftRound(5);
        RoundWorkflow.Draw(round, NoByes, new SeededRandomSource(1));
        RoundWorkflow.Draw(round, NoByes, new SeededRandomSource(2));
        Assert.Single(round.Entries, e => e.IsBye);
    }

    [Fact]
    public void Draw_uses_previous_byes_of_the_season()
    {
        var round = DraftRound(5);
        var previous = Enumerable.Range(1, 5).Where(p => p != 4).ToDictionary(p => p, _ => 1);

        RoundWorkflow.Draw(round, previous, new SeededRandomSource(1));

        Assert.Equal(4, round.Entries.Single(e => e.IsBye).PlayerId);
    }

    [Fact]
    public void Cannot_draw_with_fewer_than_four_players()
    {
        var round = DraftRound(3);
        Assert.Throws<DomainException>(() => RoundWorkflow.Draw(round, NoByes, new SeededRandomSource(1)));
        Assert.Equal(RoundStatus.Draft, round.Status);
    }

    [Fact]
    public void Start_requires_a_drawn_round()
    {
        Assert.Throws<DomainException>(() => RoundWorkflow.Start(DraftRound(4), Now));
        var started = StartedRound();
        Assert.Throws<DomainException>(() => RoundWorkflow.Start(started, Now));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(601)]
    public void Start_rejects_invalid_duration(int minutes)
    {
        var round = DraftRound(4);
        RoundWorkflow.Draw(round, NoByes, new SeededRandomSource(1));
        Assert.Throws<DomainException>(() => RoundWorkflow.Start(round, Now, minutes));
    }

    [Fact]
    public void Changing_entries_after_draw_returns_to_draft()
    {
        var round = DraftRound(8);
        RoundWorkflow.Draw(round, NoByes, new SeededRandomSource(1));

        RoundWorkflow.SetEntries(round, Enumerable.Range(1, 9).Select(p => new EntryInput(p, 1000 + p)).ToList());

        Assert.Equal(RoundStatus.Draft, round.Status);
        Assert.Empty(round.Tables);
        Assert.Equal(9, round.Entries.Count);
        Assert.DoesNotContain(round.Entries, e => e.IsBye);
    }

    [Fact]
    public void Set_entries_keeps_existing_rows_updates_deck_and_removes_absent()
    {
        var round = DraftRound(5);
        var entry1 = round.Entries.Single(e => e.PlayerId == 1);

        RoundWorkflow.SetEntries(round, [new(1, 2001), new(2, 1002), new(3, 1003), new(4, 1004)]);

        Assert.Same(entry1, round.Entries.Single(e => e.PlayerId == 1));
        Assert.Equal(2001, entry1.DeckId);
        Assert.DoesNotContain(round.Entries, e => e.PlayerId == 5);
    }

    [Fact]
    public void Entries_cannot_change_once_started_or_have_duplicates()
    {
        Assert.Throws<DomainException>(() => RoundWorkflow.SetEntries(StartedRound(), [new(1, 1)]));
        Assert.Throws<DomainException>(() => RoundWorkflow.SetEntries(DraftRound(0), [new(1, 1), new(1, 2)]));
    }

    [Fact]
    public void Result_requires_started_round()
    {
        var round = DraftRound(4);
        RoundWorkflow.Draw(round, NoByes, new SeededRandomSource(1));
        var table = round.Tables[0];
        Assert.Throws<DomainException>(() => RoundWorkflow.SetResult(round, table, TableResult.Draw, null, Now, "admin"));
    }

    [Fact]
    public void Winner_must_be_seated_at_the_table()
    {
        var round = StartedRound(8);
        var otherTableEntry = AnySeatedEntry(round.Tables[1]);

        Assert.Throws<DomainException>(() => RoundWorkflow.SetResult(round, round.Tables[0], TableResult.Win, otherTableEntry, Now, "a"));
        Assert.Throws<DomainException>(() => RoundWorkflow.SetResult(round, round.Tables[0], TableResult.Win, null, Now, "a"));
        Assert.Throws<DomainException>(() => RoundWorkflow.SetResult(round, round.Tables[0], TableResult.Pending, null, Now, "a"));
    }

    [Fact]
    public void Draw_result_clears_winner()
    {
        var round = StartedRound(4);
        var table = round.Tables[0];
        RoundWorkflow.SetResult(round, table, TableResult.Win, AnySeatedEntry(table), Now, "a");
        RoundWorkflow.SetResult(round, table, TableResult.Draw, null, Now, "a");
        Assert.Null(table.WinnerEntryId);
        Assert.Equal(TableResult.Draw, table.Result);
    }

    [Fact]
    public void Cannot_close_with_pending_tables()
    {
        var round = StartedRound(8);
        RoundWorkflow.SetResult(round, round.Tables[0], TableResult.Draw, null, Now, "a");

        var ex = Assert.Throws<DomainException>(() => RoundWorkflow.Close(round, Now));
        Assert.Contains("2", ex.Message);
        Assert.Equal(RoundStatus.InProgress, round.Status);
    }

    [Fact]
    public void Cannot_close_a_round_that_was_not_started()
    {
        var round = DraftRound(4);
        RoundWorkflow.Draw(round, NoByes, new SeededRandomSource(1));
        Assert.Throws<DomainException>(() => RoundWorkflow.Close(round, Now));
    }

    [Fact]
    public void Results_can_be_corrected_after_closing_with_audit()
    {
        var round = StartedRound(4);
        var table = round.Tables[0];
        RoundWorkflow.SetResult(round, table, TableResult.Draw, null, Now, "admin");
        RoundWorkflow.Close(round, Now);

        var later = Now.AddDays(1);
        var winner = table.Seats[2].RoundEntry!.Id;
        RoundWorkflow.SetResult(round, table, TableResult.Win, winner, later, "lojista@loja");

        Assert.Equal(RoundStatus.Closed, round.Status);
        Assert.Equal(winner, table.WinnerEntryId);
        Assert.Equal(later, table.UpdatedAt);
        Assert.Equal("lojista@loja", table.UpdatedBy);
    }

    [Fact]
    public void Only_unstarted_rounds_can_be_deleted()
    {
        RoundWorkflow.EnsureDeletable(DraftRound(4));
        Assert.Throws<DomainException>(() => RoundWorkflow.EnsureDeletable(StartedRound()));
    }

    [Fact]
    public void Timer_is_derived_from_start_and_duration()
    {
        var round = StartedRound();
        Assert.Null(RoundWorkflow.GetTimer(DraftRound(4), Now));

        var t = RoundWorkflow.GetTimer(round, Now.AddMinutes(15))!;
        Assert.Equal(Now.AddMinutes(60), t.EndsAt);
        Assert.Equal(45 * 60, t.RemainingSeconds);
        Assert.False(t.Expired);

        var expired = RoundWorkflow.GetTimer(round, Now.AddMinutes(90))!;
        Assert.Equal(0, expired.RemainingSeconds);
        Assert.True(expired.Expired);
    }

    [Fact]
    public void Previous_byes_count_other_drawn_rounds_only()
    {
        var r1 = new RoundBuilder(1).Table(1, 1, 2, 3, 4).Bye(5).Build();
        var r2 = new RoundBuilder(2, RoundStatus.Drawn).PendingTable(1, 2, 3, 5).Bye(4).Build();
        var r3 = new RoundBuilder(3, RoundStatus.Drawn).PendingTable(1, 2, 3, 4).Bye(5).Build();
        var draft = new RoundBuilder(4, RoundStatus.Draft).Bye(1).Build(); // stale flag in a draft: ignored

        var counts = RoundWorkflow.CountPreviousByes([r1, r2, r3, draft], excludingRoundId: 3);

        Assert.Equal(1, counts[5]);
        Assert.Equal(1, counts[4]);
        Assert.False(counts.ContainsKey(1));
    }
}
