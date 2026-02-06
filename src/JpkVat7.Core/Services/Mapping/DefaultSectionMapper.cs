using JpkVat7.Core.Abstractions.Result;
using JpkVat7.Core.Domain;
using JpkVat7.Core.Domain.Sections;

namespace JpkVat7.Core.Services.Mapping;

public sealed class DefaultSectionMapper : ISectionMapper
{
    // Supported keys (your parsers will produce these keys)
    public const string NaglowekKey = "Naglowek";
    public const string PodmiotKey = "Podmiot";
    public const string DeklaracjaKey = "Deklaracja";
    public const string SprzedazKey = "SprzedazWiersz";
    public const string ZakupKey = "ZakupWiersz";

    public Result<JpkInputBundle> MapToBundle(
        IReadOnlyDictionary<string, IReadOnlyList<IReadOnlyDictionary<string,string>>> sections)
    {
        if (!sections.TryGetValue(NaglowekKey, out var nag) || nag.Count == 0)
            return Result.Fail<JpkInputBundle>(new Error("map.missing_naglowek", "Missing Naglowek section"));

        if (!sections.TryGetValue(PodmiotKey, out var pod) || pod.Count == 0)
            return Result.Fail<JpkInputBundle>(new Error("map.missing_podmiot", "Missing Podmiot section"));

        if (!sections.TryGetValue(DeklaracjaKey, out var dek) || dek.Count == 0)
            return Result.Fail<JpkInputBundle>(new Error("map.missing_deklaracja", "Missing Deklaracja section"));

        sections.TryGetValue(SprzedazKey, out var spr);
        sections.TryGetValue(ZakupKey, out var zak);

        var bundle = new JpkInputBundle(
            new Naglowek(nag[0]),
            new Podmiot(pod[0]),
            new Deklaracja(dek[0]),
            (spr ?? Array.Empty<IReadOnlyDictionary<string,string>>()).Select(x => new SprzedazWiersz(x)).ToList(),
            (zak ?? Array.Empty<IReadOnlyDictionary<string,string>>()).Select(x => new ZakupWiersz(x)).ToList()
        );

        return Result.Ok(bundle);
    }
}
