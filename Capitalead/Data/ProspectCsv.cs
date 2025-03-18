using CsvHelper.Configuration.Attributes;

namespace Capitalead.Data;

public record ProspectCsv(
    [property: Name("Civilité"), Index(1)]string? Civilite, [property: Index(0)]string? Name, [property: Name("Téléphone")]string? Phone, string? Zipcode);