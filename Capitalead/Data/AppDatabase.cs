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
            u.HasIndex(x => x.ProspectId).IsUnique();
            u.HasOne(x => x.Spreadsheet).WithMany(x => x.Prospects).HasForeignKey(x => x.SpreadsheetId);
            u.HasOne(x => x.Import).WithMany(x => x.Prospects).HasForeignKey(x => x.ImportId);
        });
        modelBuilder.Entity<ProcessedRun>(u =>
        {
            u.HasIndex(x => x.RunId).IsUnique();
        });
        modelBuilder.Entity<Spreadsheet>(u =>
        {
            u.HasIndex(x => x.Title).IsUnique();
            u.HasMany(x => x.Prospects).WithOne(x => x.Spreadsheet).HasForeignKey(x => x.SpreadsheetId);
            u.HasOne(x => x.User).WithMany(x => x.Spreadsheets).HasForeignKey(x => x.UserId);
        });
        modelBuilder.Entity<Import>(u =>
        {
            u.HasMany(x => x.Prospects).WithOne(x => x.Import).HasForeignKey(x => x.ImportId);
        });
        modelBuilder.Entity<User>(u =>
        {
            u.HasMany(x => x.Spreadsheets).WithOne(x => x.User).HasForeignKey(x => x.UserId);
        });
        modelBuilder.Entity<FileForExport>(u =>
        {
            u.HasIndex(x => x.FileName).IsUnique();
            u.HasMany(x => x.Spreadsheets).WithOne(x => x.File).HasForeignKey(x => x.FileId);
            u.HasMany(x => x.ProcessedSheets).WithOne(x => x.File).HasForeignKey(x => x.FileId);
        });
        modelBuilder.Entity<ExportedSpreadsheet>(u =>
        {
            u.HasOne(x => x.File).WithMany(x => x.Spreadsheets).HasForeignKey(x => x.FileId);
        });
        modelBuilder.Entity<SheetFromFile>(u =>
        {
            u.HasOne(x => x.File).WithMany(x => x.ProcessedSheets).HasForeignKey(x => x.FileId);
        });
    }

    public DbSet<Prospect> Prospects { get; set; }
    public DbSet<ProcessedRun> ProcessedRuns { get; set; }
    public DbSet<DuplicateProspect> DuplicateProspects { get; set; }
    public DbSet<Spreadsheet> Spreadsheets { get; set; }
    public DbSet<Import> Imports { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<FileForExport> FilesForExport { get; set; }
    public DbSet<ExportedSpreadsheet> ExportedSpreadsheets { get; set; }
    public DbSet<SheetFromFile> SheetsFromFiles { get; set; }
}