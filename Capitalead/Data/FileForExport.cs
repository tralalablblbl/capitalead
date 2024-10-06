using System.ComponentModel.DataAnnotations;

namespace Capitalead.Data;

public class FileForExport
{
    [Key]
    public Guid Id { get; set; }
    public string FileId { get; set; }
    public string FileName { get; set; }
    public string MimeType { get; set; }
    public bool Exported { get; set; }
    public DateTime Created { get; set; }
    public DateTime? CompletedDate { get; set; }
    public long ProcessedCount { get; set; }
    public ICollection<ExportedSpreadsheet> Spreadsheets { get; } = new List<ExportedSpreadsheet>();
    
}