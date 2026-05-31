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
    public long? SpreadsheetId { get; set; }
    public Spreadsheet? Spreadsheet { get; set; }
    public Guid ImportId { get; set; }
    public Import Import { get; set; }
    public long? ProspectId { get; set; }
    public bool Disabled { get; set; }
    public long? LeadId { get; set; }
}