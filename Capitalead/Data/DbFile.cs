using System.ComponentModel.DataAnnotations;

namespace Capitalead.Data;

public class DbFile
{
    [Key]
    public Guid Id { get; set; }
    public string FileName { get; set; }
    public bool Exported { get; set; }
    public bool ReadyForExport { get; set; }
    public DateTime Created { get; set; }
    public DateTime? CompletedDate { get; set; }
    public IList<int> CiviliteColumns { get; set; } = new List<int>();
    public IList<int> FirstnameColumns { get; set; } = new List<int>();
    public IList<int> NameColumns { get; set; } = new List<int>();
    public IList<int> LastnameColumns { get; set; } = new List<int>();
    public IList<int> PhoneColumns { get; set; } = new List<int>();
    public IList<int> ZipcodeColumns { get; set; } = new List<int>();
    public ICollection<DbProspect> Prospects { get; set; } = new List<DbProspect>();
}