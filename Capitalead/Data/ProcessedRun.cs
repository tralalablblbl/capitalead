using System.ComponentModel.DataAnnotations;

namespace Capitalead.Data;

public class ProcessedRun
{
    [Key]
    public Guid Id { get; set; }
    public string RunId { get; set; }
    public long ProspectsCount { get; set; }
    public DateTime ProcessedDate { get; set; }
}