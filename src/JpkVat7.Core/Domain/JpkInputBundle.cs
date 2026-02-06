using JpkVat7.Core.Domain.Sections;

namespace JpkVat7.Core.Domain;

public sealed record JpkInputBundle(
    Naglowek Naglowek,
    Podmiot Podmiot,
    Deklaracja Deklaracja,
    IReadOnlyList<SprzedazWiersz> Sprzedaz,
    IReadOnlyList<ZakupWiersz> Zakup
);
