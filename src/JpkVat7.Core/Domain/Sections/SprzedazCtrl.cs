namespace JpkVat7.Core.Domain.Sections;

public sealed record SprzedazCtrl
{
    public int LiczbaWierszySprzedazy { get; init; } // matches StartColumn
    public decimal PodatekNalezny { get; init; }
}
