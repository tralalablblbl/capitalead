using System.ComponentModel.DataAnnotations;

namespace Capitalead.Data;

public class Import
{
    [Key]
    public Guid Id { get; set; }
    public DateTime Started { get; set; }
    public RunStatus Status { get; set; }
    public DateTime? Completed { get; set; }
    public long AddedCount { get; set; }
    public string? Error { get; set; }
    public ICollection<Prospect> Prospects { get; } = new List<Prospect>();
}