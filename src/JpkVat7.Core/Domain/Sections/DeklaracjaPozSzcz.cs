namespace JpkVat7.Core.Domain.Sections;

public sealed record DeklaracjaPozSzcz
{
    // The Go StartColumn is "P_10" — so keep the same naming convention.
    // Add as many P_XX fields as you need.

    public decimal? P_10 { get; init; }
    public decimal? P_11 { get; init; }
    public decimal? P_12 { get; init; }

    // Example flags/fields frequently present:
    public bool? P_68 { get; init; }
    public bool? P_69 { get; init; }
}
