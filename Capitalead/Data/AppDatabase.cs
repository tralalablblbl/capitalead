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
    }

    public DbSet<Prospect> Prospects { get; set; }
}