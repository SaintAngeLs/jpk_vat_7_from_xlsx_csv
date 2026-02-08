namespace JpkVat7.Core.Domain.Sections;

public sealed record Naglowek
{
    public string KodFormularza { get; init; } = "";
    public string WariantFormularza { get; init; } = "";

    // Common in JPK headers:
    public string CelZlozenia { get; init; } = "";
    public int Rok { get; init; }
    public int Miesiac { get; init; }

    public string KodUrzedu { get; init; } = "";
    public DateTime DataWytworzeniaJpk { get; init; } = DateTime.UtcNow;

    // Optional technical fields:
    public string NazwaSystemu { get; init; } = "";
}
