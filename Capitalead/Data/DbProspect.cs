using System.ComponentModel.DataAnnotations;

namespace Capitalead.Data;

public class DbProspect
{
    [Key]
    public Guid Id { get; set; }
    public string SheetName { get; set; }
    public long RowNumber { get; set; }
    public string? Civilite { get; set; }
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public string? Zipcode { get; set; }
    public DbFile File { get; set; }
    public Guid FileId { get; set; }
}