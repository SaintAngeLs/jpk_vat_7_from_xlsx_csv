namespace JpkVat7.Core.Domain.Sections;

public sealed record ZakupWiersz
{
    public int LpZakupu { get; init; } // matches StartColumn

    public string NrDostawcy { get; init; } = ""; // e.g., NIP
    public string NazwaDostawcy { get; init; } = "";
    public string DowodZakupu { get; init; } = "";
    public DateTime? DataZakupu { get; init; }
    public DateTime? DataWplywu { get; init; }

    public decimal? K_40 { get; init; }
    public decimal? K_41 { get; init; }
    public decimal? K_42 { get; init; }

    // Example flags:
    public bool? MPP { get; init; }
}
