using System.ComponentModel.DataAnnotations;

namespace Capitalead.Data;

public class SheetFromFile
{
    [Key]
    public Guid Id { get; set; }
    public string SheetName { get; set; }
    public long ProcessedCount { get; set; }
    public FileForExport File { get; set; }
    public Guid FileId { get; set; }
}