using Commander500.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Commander500.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Season> Seasons => Set<Season>();
    public DbSet<Player> Players => Set<Player>();
    public DbSet<Commander> Commanders => Set<Commander>();
    public DbSet<Deck> Decks => Set<Deck>();
    public DbSet<DeckCommander> DeckCommanders => Set<DeckCommander>();
    public DbSet<Stage> Stages => Set<Stage>();
    public DbSet<Round> Rounds => Set<Round>();
    public DbSet<RoundEntry> RoundEntries => Set<RoundEntry>();
    public DbSet<GameTable> Tables => Set<GameTable>();
    public DbSet<TableSeat> TableSeats => Set<TableSeat>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();

    protected override void ConfigureConventions(ModelConfigurationBuilder b)
    {
        b.Properties<DeckStatus>().HaveConversion<string>().HaveMaxLength(20);
        b.Properties<RoundStatus>().HaveConversion<string>().HaveMaxLength(20);
        b.Properties<TableResult>().HaveConversion<string>().HaveMaxLength(20);
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Season>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(120).IsRequired();
            e.Property(x => x.Tiebreakers).HasMaxLength(200).IsRequired();
        });

        b.Entity<Player>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(120).IsRequired();
            e.Property(x => x.Nickname).HasMaxLength(60);
        });

        b.Entity<Commander>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.NormalizedName).HasMaxLength(200).IsRequired();
            e.HasIndex(x => x.NormalizedName).IsUnique();
            e.HasIndex(x => x.ScryfallId).IsUnique().HasFilter("\"ScryfallId\" IS NOT NULL");
            e.Property(x => x.ArtUrl).HasMaxLength(500);
            e.Property(x => x.ImageUrl).HasMaxLength(500);
            e.Property(x => x.ColorIdentity).HasMaxLength(5);
        });

        b.Entity<Stage>(e =>
        {
            e.HasOne(x => x.Season).WithMany(s => s.Stages).HasForeignKey(x => x.SeasonId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.SeasonId, x.Date }).IsUnique();
            e.HasIndex(x => new { x.SeasonId, x.Number }); // not unique: renumbering by date updates rows one by one
        });

        b.Entity<Deck>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(120).IsRequired();
            e.Property(x => x.ListUrl).HasMaxLength(500);
            e.Property(x => x.ReviewNote).HasMaxLength(1000);
            e.HasOne(x => x.Player).WithMany(p => p.Decks).HasForeignKey(x => x.PlayerId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<DeckCommander>(e =>
        {
            e.HasKey(x => new { x.DeckId, x.CommanderId });
            e.HasOne(x => x.Deck).WithMany(d => d.Commanders).HasForeignKey(x => x.DeckId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Commander).WithMany().HasForeignKey(x => x.CommanderId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Round>(e =>
        {
            e.HasOne(x => x.Season).WithMany(s => s.Rounds).HasForeignKey(x => x.SeasonId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Stage).WithMany(s => s.Rounds).HasForeignKey(x => x.StageId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.SeasonId, x.Number }).IsUnique();
        });

        b.Entity<RoundEntry>(e =>
        {
            e.HasOne(x => x.Round).WithMany(r => r.Entries).HasForeignKey(x => x.RoundId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Player).WithMany().HasForeignKey(x => x.PlayerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Deck).WithMany().HasForeignKey(x => x.DeckId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.RoundId, x.PlayerId }).IsUnique();
        });

        b.Entity<GameTable>(e =>
        {
            e.ToTable("Tables");
            e.HasOne(x => x.Round).WithMany(r => r.Tables).HasForeignKey(x => x.RoundId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.WinnerEntry).WithMany().HasForeignKey(x => x.WinnerEntryId).OnDelete(DeleteBehavior.SetNull);
            e.Property(x => x.UpdatedBy).HasMaxLength(200);
            e.HasIndex(x => new { x.RoundId, x.Number }).IsUnique();
        });

        b.Entity<TableSeat>(e =>
        {
            e.HasKey(x => new { x.TableId, x.RoundEntryId });
            e.HasOne(x => x.Table).WithMany(t => t.Seats).HasForeignKey(x => x.TableId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.RoundEntry).WithMany().HasForeignKey(x => x.RoundEntryId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.TableId, x.Seat }).IsUnique();
            e.ToTable(t => t.HasCheckConstraint("CK_TableSeat_Seat", "\"Seat\" BETWEEN 1 AND 4"));
        });

        b.Entity<AdminUser>(e =>
        {
            e.Property(x => x.Email).HasMaxLength(200).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
        });
    }
}
