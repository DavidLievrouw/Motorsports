using Microsoft.EntityFrameworkCore;

namespace Motorsports.Scaffolding.Core.Models {
  public partial class MotorsportsContext : DbContext {
    public MotorsportsContext(DbContextOptions<MotorsportsContext> options) : base(options) { }

    public virtual DbSet<Country> Country { get; set; }
    public virtual DbSet<Participant> Participant { get; set; }
    public virtual DbSet<Round> Round { get; set; }
    public virtual DbSet<RoundWinner> RoundWinner { get; set; }
    public virtual DbSet<Season> Season { get; set; }
    public virtual DbSet<SeasonWinner> SeasonWinner { get; set; }
    public virtual DbSet<Sport> Sport { get; set; }
    public virtual DbSet<Status> Status { get; set; }
    public virtual DbSet<Team> Team { get; set; }
    public virtual DbSet<Venue> Venue { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
      modelBuilder.Entity<Country>(
        entity => {
          entity.HasKey(e => e.Iso);

          entity.Property(e => e.Iso)
            .HasColumnName("ISO")
            .HasColumnType("char(2)")
            .ValueGeneratedNever();

          entity.Property(e => e.Iso3)
            .HasColumnName("ISO3")
            .HasColumnType("char(3)");

          entity.Property(e => e.Name)
            .HasMaxLength(80)
            .IsUnicode(false);

          entity.Property(e => e.NiceName)
            .HasMaxLength(80)
            .IsUnicode(false);
        });

      modelBuilder.Entity<Participant>(
        entity => {
          entity.Property(e => e.Country)
            .IsRequired()
            .HasColumnType("char(2)");

          entity.Property(e => e.FirstName)
            .IsRequired()
            .HasMaxLength(100);

          entity.Property(e => e.LastName)
            .IsRequired()
            .HasMaxLength(100);

          entity.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(10);

          entity.HasOne(d => d.RelatedCountry)
            .WithMany(p => p.RelatedParticipants)
            .HasForeignKey(d => d.Country)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_Country_Participant");
        });

      modelBuilder.Entity<Round>(
        entity => {
          entity.Property(e => e.Date).HasColumnType("date");

          entity.Property(e => e.Name).HasMaxLength(100);
          
          entity.Property(e => e.Note);

          entity.Property(e => e.Venue)
            .IsRequired()
            .HasMaxLength(50);

          entity.HasOne(d => d.RelatedSeason)
            .WithMany(p => p.RelatedRounds)
            .HasForeignKey(d => d.Season)
            .HasConstraintName("FK_Season_Round");

          entity.HasOne(d => d.RelatedVenue)
            .WithMany(p => p.RelatedRounds)
            .HasForeignKey(d => d.Venue)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_Venue_Round");

          entity.Property(e => e.Rain).HasColumnType("decimal(1, 0)");

          entity.Property(e => e.Rating).HasColumnType("decimal(3, 1)");

          entity.Property(e => e.Status)
            .HasDefaultValue("Scheduled")
            .IsRequired()
            .HasMaxLength(20);

          entity.HasOne(d => d.RelatedStatus)
            .WithMany(p => p.RelatedRounds)
            .HasForeignKey(d => d.Status)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_Status_Round");

          entity.HasOne(d => d.RelatedWinningTeam)
            .WithMany(p => p.RelatedRounds)
            .HasForeignKey(d => d.WinningTeam)
            .HasConstraintName("FK_Team_Round");
        });

      modelBuilder.Entity<RoundWinner>(
        entity => {
          entity.HasOne(d => d.RelatedParticipant)
            .WithMany(p => p.WonRounds)
            .HasForeignKey(d => d.Participant)
            .HasConstraintName("FK_Participant_RoundWinner");

          entity.HasOne(d => d.RelatedRound)
            .WithMany(p => p.RelatedRoundWinners)
            .HasForeignKey(d => d.Round)
            .HasConstraintName("FK_Round_RoundWinner");
        });

      modelBuilder.Entity<Season>(
        entity => {
          entity.Property(e => e.Label).HasMaxLength(100);

          entity.Property(e => e.Sport)
            .IsRequired()
            .HasMaxLength(50);

          entity.HasOne(d => d.RelatedSport)
            .WithMany(p => p.RelatedSeasons)
            .HasForeignKey(d => d.Sport)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_Sport_Season");

          entity.HasOne(d => d.RelatedWinningTeam)
            .WithMany(p => p.RelatedSeasons)
            .HasForeignKey(d => d.WinningTeam)
            .HasConstraintName("FK_Team_Season");
        });

      modelBuilder.Entity<SeasonWinner>(
        entity => {
          entity.HasOne(d => d.RelatedParticipant)
            .WithMany(p => p.WonSeasons)
            .HasForeignKey(d => d.Participant)
            .HasConstraintName("FK_Participant_SeasonWinner");

          entity.HasOne(d => d.RelatedSeason)
            .WithMany(p => p.RelatedSeasonWinners)
            .HasForeignKey(d => d.Season)
            .HasConstraintName("FK_Season_SeasonWinner");
        });

      modelBuilder.Entity<Sport>(
        entity => {
          entity.HasKey(e => e.Name);

          entity.Property(e => e.Name)
            .HasMaxLength(50)
            .ValueGeneratedNever();

          entity.Property(e => e.FullName).HasMaxLength(100);
        });

      modelBuilder.Entity<Status>(
        entity => {
          entity.HasKey(e => e.Name);

          entity.Property(e => e.Name)
            .HasMaxLength(20)
            .ValueGeneratedNever();
        });

      modelBuilder.Entity<Team>(
        entity => {
          entity.Property(e => e.Country)
            .IsRequired()
            .HasColumnType("char(2)");

          entity.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(50);

          entity.Property(e => e.Sport)
            .IsRequired()
            .HasMaxLength(50);

          entity.HasOne(d => d.RelatedCountry)
            .WithMany(p => p.RelatedTeams)
            .HasForeignKey(d => d.Country)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_Country_Team");

          entity.HasOne(d => d.RelatedSport)
            .WithMany(p => p.RelatedTeams)
            .HasForeignKey(d => d.Sport)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_Sport_Team");
        });

      modelBuilder.Entity<Venue>(
        entity => {
          entity.HasKey(e => e.Name);

          entity.Property(e => e.Name)
            .HasMaxLength(50)
            .ValueGeneratedNever();

          entity.Property(e => e.Country)
            .IsRequired()
            .HasColumnType("char(2)");

          entity.HasOne(d => d.RelatedCountry)
            .WithMany(p => p.RelatedVenues)
            .HasForeignKey(d => d.Country)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_Country_Venue");
        });
    }
  }
}