using CsvHelper.Configuration.Attributes;

namespace Capitalead.Data;

public record ProspectCsv(
    [property: Name("Civilité")]string? Civilite, string? Name, string? Phone, string? Zipcode);