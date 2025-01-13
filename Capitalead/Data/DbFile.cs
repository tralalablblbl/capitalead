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
    public int CiviliteColumn { get; set; }
    public int FirstnameColumn { get; set; }
    public int NameColumn { get; set; }
    public int LastnameColumn { get; set; }
    public int PhoneColumn { get; set; }
    public int ZipcodeColumn { get; set; }
    public ICollection<DbProspect> Prospects { get; set; } = new List<DbProspect>();
}