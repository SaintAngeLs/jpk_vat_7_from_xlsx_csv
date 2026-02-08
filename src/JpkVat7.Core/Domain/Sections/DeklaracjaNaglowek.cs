namespace JpkVat7.Core.Domain.Sections;

public sealed record DeklaracjaNaglowek
{
    public string KodFormularzaDekl { get; init; } = ""; // matches StartColumn
    public string WariantFormularzaDekl { get; init; } = "";

    public string CelZlozenia { get; init; } = "";
    public int Rok { get; init; }
    public int Miesiac { get; init; }

    // Optional:
    public string KodUrzedu { get; init; } = "";
}
