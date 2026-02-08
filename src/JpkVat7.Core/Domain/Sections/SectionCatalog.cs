using System.Collections.Immutable;

namespace JpkVat7.Core.Domain.Sections;

/// <summary>
/// Equivalent of Go's `var SAFTSections = []SAFTSection{...}`.
/// Central place that defines section ordering and the "StartColumn"
/// used by your parsers/mappers.
/// </summary>
public static class SectionCatalog
{
    // Keep this in the exact same order as in Go if you want identical behavior.
    public static readonly ImmutableArray<SectionDefinition> All =
    [
        new SectionDefinition(SectionIds.Naglowek,            "KodFormularza"),
        new SectionDefinition(SectionIds.Podmiot,             "typPodmiotu"),
        new SectionDefinition(SectionIds.DeklaracjaNaglowek,  "KodFormularzaDekl"),
        new SectionDefinition(SectionIds.DeklaracjaPozSzcz,   "P_10"),
        new SectionDefinition(SectionIds.DeklaracjaPouczenia, "Pouczenia"),
        new SectionDefinition(SectionIds.Sprzedaz,            "LpSprzedazy"),
        new SectionDefinition(SectionIds.SprzedazCtrl,        "LiczbaWierszySprzedazy"),
        new SectionDefinition(SectionIds.Zakup,               "LpZakupu"),
        new SectionDefinition(SectionIds.ZakupCtrl,           "LiczbaWierszyZakupow"),
    ];

    private static readonly ImmutableDictionary<string, SectionDefinition> ById =
        All.ToImmutableDictionary(s => s.Id, s => s, StringComparer.OrdinalIgnoreCase);

    public static bool TryGet(string sectionId, out SectionDefinition def)
        => ById.TryGetValue(sectionId, out def);

    public static SectionDefinition GetRequired(string sectionId)
        => TryGet(sectionId, out var def)
            ? def
            : throw new KeyNotFoundException($"Unknown section id '{sectionId}'.");
}
