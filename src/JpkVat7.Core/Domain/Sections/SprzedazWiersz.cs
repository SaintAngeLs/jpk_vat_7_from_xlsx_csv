namespace JpkVat7.Core.Domain.Sections;

/// <summary>
/// One sales row.
/// Column names tend to follow the JPK naming.
/// Add the exact columns you support.
/// </summary>
public sealed record SprzedazWiersz
{
    public int LpSprzedazy { get; init; } // matches StartColumn

    public string NrKontrahenta { get; init; } = ""; // e.g., NIP
    public string NazwaKontrahenta { get; init; } = "";
    public string DowodSprzedazy { get; init; } = "";
    public DateTime? DataWystawienia { get; init; }
    public DateTime? DataSprzedazy { get; init; }

    public decimal? K_10 { get; init; }
    public decimal? K_11 { get; init; }
    public decimal? K_12 { get; init; }

    // Example GTU/flags:
    public bool? GTU_01 { get; init; }
    public bool? TP { get; init; }
}
