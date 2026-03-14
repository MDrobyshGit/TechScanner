using Microsoft.EntityFrameworkCore;
using TechScanner.Core.Entities;

namespace TechScanner.Infrastructure.Data;

public class TechScannerDbContext : DbContext
{
    public TechScannerDbContext(DbContextOptions<TechScannerDbContext> options)
        : base(options) { }

    public DbSet<Scan> Scans => Set<Scan>();
    public DbSet<ScanTechnology> ScanTechnologies => Set<ScanTechnology>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Scan>(entity =>
        {
            entity.ToTable("Scans");
            entity.HasKey(s => s.Id);
            entity.Property(s => s.SourceType).HasConversion<string>();
            entity.Property(s => s.Status).HasConversion<string>();
            entity.HasIndex(s => s.CreatedAt).IsDescending();
            entity.HasMany(s => s.Technologies)
                  .WithOne(t => t.Scan)
                  .HasForeignKey(t => t.ScanId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ScanTechnology>(entity =>
        {
            entity.ToTable("ScanTechnologies");
            entity.HasKey(t => t.Id);
            entity.Property(t => t.SupportStatus).HasConversion<string>();
            entity.HasIndex(t => t.ScanId);
        });
    }
}
