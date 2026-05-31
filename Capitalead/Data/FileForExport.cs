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
    public ICollection<SheetFromFile> ProcessedSheets { get; set; } = new List<SheetFromFile>();
    public ICollection<ExportedSpreadsheet> Spreadsheets { get; } = new List<ExportedSpreadsheet>();
    
}