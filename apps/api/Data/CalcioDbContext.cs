using Microsoft.EntityFrameworkCore;
using Api.Models;
using NetTopologySuite.Geometries;

namespace Api.Data;

public class CalcioDbContext : DbContext
{
    public CalcioDbContext(DbContextOptions<CalcioDbContext> options) : base(options)
    {
    }

    public DbSet<Club> Clubs { get; set; }
    public DbSet<Venue> Venues { get; set; }
    public DbSet<AgeGroup> AgeGroups { get; set; }
    public DbSet<Competition> Competitions { get; set; }
    public DbSet<Team> Teams { get; set; }
    public DbSet<Match> Matches { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure unique constraints
        modelBuilder.Entity<Club>()
            .HasIndex(c => c.ExternalId)
            .IsUnique();

        modelBuilder.Entity<Venue>()
            .HasIndex(v => v.Address)
            .IsUnique();

        modelBuilder.Entity<AgeGroup>()
            .HasIndex(ag => ag.Name)
            .IsUnique();

        modelBuilder.Entity<Competition>()
            .HasIndex(c => c.Name)
            .IsUnique();

        // Configure relationships for Match entity
        modelBuilder.Entity<Match>()
            .HasOne(m => m.HomeTeam)
            .WithMany(t => t.HomeMatches)
            .HasForeignKey(m => m.HomeTeamId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Match>()
            .HasOne(m => m.AwayTeam)
            .WithMany(t => t.AwayMatches)
            .HasForeignKey(m => m.AwayTeamId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure geometry for PostGIS
        modelBuilder.Entity<Venue>()
            .Property(v => v.Location)
            .HasColumnType("geometry(Point, 4326)");
    }
}
