using Microsoft.EntityFrameworkCore;
using Motorsports.Infrastructure.Entities;

namespace Motorsports.Infrastructure;

public sealed class MotorsportsDbContext(DbContextOptions<MotorsportsDbContext> options) : DbContext(options)
{
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<Participant> Participants => Set<Participant>();
    public DbSet<Round> Rounds => Set<Round>();
    public DbSet<RoundWinner> RoundWinners => Set<RoundWinner>();
    public DbSet<Season> Seasons => Set<Season>();
    public DbSet<SeasonEntry> SeasonEntries => Set<SeasonEntry>();
    public DbSet<SeasonWinner> SeasonWinners => Set<SeasonWinner>();
    public DbSet<Sport> Sports => Set<Sport>();
    public DbSet<Status> Statuses => Set<Status>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Venue> Venues => Set<Venue>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Country>(e =>
        {
            e.ToTable("Country");
            e.HasKey(x => x.Iso);
            e.Property(x => x.Iso).HasColumnName("ISO").HasColumnType("char(2)").ValueGeneratedNever();
            e.Property(x => x.Iso3).HasColumnName("ISO3").HasColumnType("char(3)");
            e.Property(x => x.NumCode).HasColumnType("smallint");
            e.Property(x => x.PhoneCode).HasColumnType("smallint");
            e.Property(x => x.Name).HasMaxLength(80);
            e.Property(x => x.NiceName).HasMaxLength(80);
        });

        modelBuilder.Entity<Sport>(e =>
        {
            e.ToTable("Sport");
            e.HasKey(x => x.Name);
            e.Property(x => x.Name).HasMaxLength(50).ValueGeneratedNever();
            e.Property(x => x.FullName).HasMaxLength(100);
            e.Property(x => x.Image).HasColumnType("longblob");
        });

        modelBuilder.Entity<Status>(e =>
        {
            e.ToTable("Status");
            e.HasKey(x => x.Name);
            e.Property(x => x.Name).HasMaxLength(20).ValueGeneratedNever();
            e.Property(x => x.Step).HasColumnType("tinyint");
        });

        modelBuilder.Entity<Venue>(e =>
        {
            e.ToTable("Venue");
            e.HasKey(x => x.Name);
            e.Property(x => x.Name).HasMaxLength(100).ValueGeneratedNever();
            e.Property(x => x.Country).HasColumnType("char(2)");
            e.HasOne(x => x.CountryRef).WithMany().HasForeignKey(x => x.Country).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Participant>(e =>
        {
            e.ToTable("Participant");
            e.Property(x => x.Title).HasMaxLength(10);
            e.Property(x => x.FirstName).HasMaxLength(100);
            e.Property(x => x.LastName).HasMaxLength(100);
            e.Property(x => x.Country).HasColumnType("char(2)");
            e.HasOne(x => x.CountryRef).WithMany().HasForeignKey(x => x.Country).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new
                {
                    x.Title,
                    x.FirstName,
                    x.LastName,
                    x.Country,
                })
                .IsUnique();
        });

        modelBuilder.Entity<Team>(e =>
        {
            e.ToTable("Team");
            e.Property(x => x.Name).HasMaxLength(50);
            e.Property(x => x.Sport).HasMaxLength(50);
            e.Property(x => x.Country).HasColumnType("char(2)");
            e.HasOne(x => x.SportRef).WithMany().HasForeignKey(x => x.Sport).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.CountryRef).WithMany().HasForeignKey(x => x.Country).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Season>(e =>
        {
            e.ToTable("Season");
            e.Property(x => x.Sport).HasMaxLength(50);
            e.Property(x => x.Label).HasMaxLength(100);
            e.HasOne(x => x.SportRef).WithMany().HasForeignKey(x => x.Sport).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Team>().WithMany().HasForeignKey(x => x.WinningTeam).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SeasonEntry>(e =>
        {
            e.ToTable("SeasonEntry");
            e.HasKey(x => new { x.Season, x.Team });
            e.Property(x => x.Name).HasMaxLength(50);
            e.HasOne(x => x.SeasonRef).WithMany().HasForeignKey(x => x.Season).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.TeamRef).WithMany().HasForeignKey(x => x.Team).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SeasonWinner>(e =>
        {
            e.ToTable("SeasonWinner");
            e.HasOne<Season>().WithMany().HasForeignKey(x => x.Season).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Participant>().WithMany().HasForeignKey(x => x.Participant).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Round>(e =>
        {
            e.ToTable("Round");
            e.Property(x => x.Date).HasColumnType("date");
            e.Property(x => x.Number).HasColumnType("smallint");
            e.Property(x => x.Name).HasMaxLength(100);
            e.Property(x => x.Venue).HasMaxLength(100);
            e.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("Scheduled");
            e.Property(x => x.Rating).HasColumnType("decimal(3,1)");
            e.Property(x => x.Rain).HasColumnType("decimal(1,0)");
            e.Property(x => x.Note).HasColumnType("text");
            e.HasOne(x => x.SeasonRef).WithMany().HasForeignKey(x => x.Season).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.StatusRef).WithMany().HasForeignKey(x => x.Status).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Venue>().WithMany().HasForeignKey(x => x.Venue).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Team>().WithMany().HasForeignKey(x => x.WinningTeam).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RoundWinner>(e =>
        {
            e.ToTable("RoundWinner");
            e.HasOne<Round>().WithMany().HasForeignKey(x => x.Round).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Participant>().WithMany().HasForeignKey(x => x.Participant).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("User");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(36);
            e.Property(x => x.Username).HasMaxLength(255);
            e.Property(x => x.PasswordHash).HasColumnType("varbinary(50)").IsRequired();
            e.Property(x => x.Salt).HasColumnType("varbinary(50)").IsRequired();
            e.Property(x => x.Prf).HasMaxLength(10);
            e.Property(x => x.Title).HasMaxLength(255);
            e.Property(x => x.GivenName).HasMaxLength(255);
            e.Property(x => x.FamilyName).HasMaxLength(255);
            e.Property(x => x.EmailAddress).HasMaxLength(255);
            e.Property(x => x.PasswordHashV3).HasMaxLength(512);
            e.Property(x => x.SecurityStamp).HasMaxLength(64);
            e.Property(x => x.ConcurrencyStamp).HasMaxLength(64);
        });
    }
}
