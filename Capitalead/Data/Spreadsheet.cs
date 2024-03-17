using System.ComponentModel.DataAnnotations;

namespace Capitalead.Data;

public class Spreadsheet
{
    [Key]
    public long Id { get; set; }
    public string Title { get; set; }
    public string ClusterId { get; set; }
    public string ClusterName { get; set; }
    public long? UserId { get; set; }
    public User? User { get; set; }
    public DateTime? LastParsingDate { get; set; }
    public long ProspectsCount { get; set; }
    public long DisabledProspectsCount { get; set; }
    public long LeadsCount { get; set; }
    public ICollection<Prospect> Prospects { get; } = new List<Prospect>();
}