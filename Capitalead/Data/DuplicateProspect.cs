using System.ComponentModel.DataAnnotations;

namespace Capitalead.Data;

public class DuplicateProspect
{
    [Key]
    public Guid Id { get; set; }
    public string[] Content { get; set; }
    public string Phone { get; set; }
    public long SheetId { get; set; }
    public long ProspectId { get; set; }
    public bool Deleted { get; set; }
}