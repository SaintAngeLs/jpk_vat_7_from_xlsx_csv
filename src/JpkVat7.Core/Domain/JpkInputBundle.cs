using JpkVat7.Core.Domain.Sections;

namespace JpkVat7.Core.Domain;

/// <summary>
/// Domain-level container for all sections needed to generate JPK.
/// Keep it "pure": no parsing concerns, no file paths, no I/O.
/// </summary>
public sealed record JpkInputBundle(
    Naglowek Naglowek,
    Podmiot Podmiot,
    DeklaracjaNaglowek DeklaracjaNaglowek,
    DeklaracjaPozSzcz DeklaracjaPozSzcz,
    DeklaracjaPouczenia DeklaracjaPouczenia,
    IReadOnlyList<SprzedazWiersz> SprzedazWiersze,
    SprzedazCtrl SprzedazCtrl,
    IReadOnlyList<ZakupWiersz> ZakupWiersze,
    ZakupCtrl ZakupCtrl
);
