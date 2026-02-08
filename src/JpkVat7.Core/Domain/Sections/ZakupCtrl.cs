namespace JpkVat7.Core.Domain.Sections;

public sealed record ZakupCtrl
{
    public int LiczbaWierszyZakupow { get; init; } // matches StartColumn
    public decimal PodatekNaliczony { get; init; }
}
