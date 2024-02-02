using Microsoft.EntityFrameworkCore;

namespace Capitalead.Data;

public class AppDatabase : DbContext
{
    public AppDatabase(DbContextOptions<AppDatabase> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Prospect>(u =>
        {
            u.HasIndex(x => x.Phone).IsUnique();
        });
        modelBuilder.Entity<ProcessedRun>(u =>
        {
            u.HasIndex(x => x.RunId).IsUnique();
        });
    }

    public DbSet<Prospect> Prospects { get; set; }
    public DbSet<ProcessedRun> ProcessedRuns { get; set; }
    public DbSet<DuplicateProspect> DuplicateProspects { get; set; }
}