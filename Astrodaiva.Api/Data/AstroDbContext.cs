using Microsoft.EntityFrameworkCore;

namespace Astrodaiva.Api.Data;

public class AstroDbContext : DbContext
{
    public AstroDbContext(DbContextOptions<AstroDbContext> options) : base(options) { }

    public DbSet<AppDbSnapshot> AppDbSnapshots => Set<AppDbSnapshot>();

    public DbSet<AstroEventRow> AstroEvents => Set<AstroEventRow>();
    public DbSet<AstroEventPlanetRow> AstroEventPlanets => Set<AstroEventPlanetRow>();
    public DbSet<AstroPlanetEventRow> AstroPlanetEvents => Set<AstroPlanetEventRow>();

    public DbSet<PlanetInZodiacDetailsRow> PlanetInZodiacDetails => Set<PlanetInZodiacDetailsRow>();
    public DbSet<PlanetInRetrogradeDetailsRow> PlanetInRetrogradeDetails => Set<PlanetInRetrogradeDetailsRow>();
    public DbSet<MoonDayDetailsRow> MoonDayDetails => Set<MoonDayDetailsRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AstroEventPlanetRow>()
            .HasKey(x => new { x.Date, x.Planet });

        modelBuilder.Entity<AstroEventPlanetRow>()
            .HasOne<AstroEventRow>()
            .WithMany()
            .HasForeignKey(x => x.Date)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AstroPlanetEventRow>()
            .HasKey(x => new { x.Date, x.Planet1, x.Planet2, x.AspectSymbol });

        modelBuilder.Entity<AstroPlanetEventRow>()
            .HasOne<AstroEventRow>()
            .WithMany()
            .HasForeignKey(x => x.Date)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AstroPlanetEventRow>()
            .HasIndex(x => x.Date);

        modelBuilder.Entity<PlanetInZodiacDetailsRow>()
            .HasKey(x => new { x.Planet, x.ZodiacSign });

        modelBuilder.Entity<PlanetInRetrogradeDetailsRow>()
            .HasKey(x => x.PlanetInRetrograde);

        modelBuilder.Entity<MoonDayDetailsRow>()
            .HasKey(x => x.MoonDay);
    }
}
