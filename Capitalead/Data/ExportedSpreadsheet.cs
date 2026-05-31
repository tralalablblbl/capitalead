using System.ComponentModel.DataAnnotations;

namespace Capitalead.Data;

public class ExportedSpreadsheet
{
    [Key]
    public Guid Id { get; set; }
    public long SheetId { get; set; }
    public string Title { get; set; }
    public long? UserId { get; set; }
    public User? User { get; set; }
    public FileForExport File { get; set; }
    public Guid FileId { get; set; }
}