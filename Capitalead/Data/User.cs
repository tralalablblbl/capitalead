using System.ComponentModel.DataAnnotations;

namespace Capitalead.Data;

public class User
{
    [Key]
    public long Id { get; set; }
    public string Email { get; set; }
    public string Lastname { get; set; }
    public string Firstname { get; set; }
    public string Phone { get; set; }
    public string MobilePhone { get; set; }
    public ICollection<Spreadsheet> Spreadsheets { get; } = new List<Spreadsheet>();
}