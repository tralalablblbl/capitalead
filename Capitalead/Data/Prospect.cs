using System.ComponentModel.DataAnnotations;

namespace Capitalead.Data;

public class Prospect
{
    [Key]
    public Guid Id { get; set; }
    public string Neighbourhood { get; set; }
    public DateTime ParsingDate { get; set; }
    public string RealEstateType { get; set; }
    public string Phone { get; set; }
    public string Rooms { get; set; }
    public string Size { get; set; }
    public string Energy { get; set; }
}