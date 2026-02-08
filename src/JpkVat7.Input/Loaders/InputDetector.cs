using System.Text;
using JpkVat7.Core.Domain.Sections;      // SectionIds + SectionCatalog
using JpkVat7.Core.Services.Mapping;     // DefaultSectionMapper
using JpkVat7.Input.Abstractions;

namespace JpkVat7.Input.Loaders;

public sealed class InputDetector : IInputDetector
{
    // Mapper keys (what your pipeline expects)
    private static readonly HashSet<string> MapperNames = new(StringComparer.OrdinalIgnoreCase)
    {
        DefaultSectionMapper.NaglowekKey,
        DefaultSectionMapper.PodmiotKey,

        DefaultSectionMapper.DeklaracjaKey,
        DefaultSectionMapper.DeklaracjaNaglowekKey,
        DefaultSectionMapper.DeklaracjaPozSzczKey,
        DefaultSectionMapper.DeklaracjaPouczeniaKey,

        DefaultSectionMapper.SprzedazWierszKey,
        DefaultSectionMapper.SprzedazCtrlKey,
        DefaultSectionMapper.ZakupWierszKey,
        DefaultSectionMapper.ZakupCtrlKey,
    };

    // Go-style IDs (what some files may contain)
    private static readonly HashSet<string> GoIds = new(StringComparer.OrdinalIgnoreCase)
    {
        SectionIds.Naglowek,
        SectionIds.Podmiot,
        SectionIds.DeklaracjaNaglowek,
        SectionIds.DeklaracjaPozSzcz,
        SectionIds.DeklaracjaPouczenia,
        SectionIds.Sprzedaz,
        SectionIds.SprzedazCtrl,
        SectionIds.Zakup,
        SectionIds.ZakupCtrl,
    };

    // Strong signals: the "start column" names that appear as header rows in sectioned files
    private static readonly HashSet<string> StartColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        // from your SectionCatalog
        "KodFormularza",
        "typPodmiotu",
        "KodFormularzaDekl",
        "P_10",
        "Pouczenia",
        "LpSprzedazy",
        "LiczbaWierszySprzedazy",
        "LpZakupu",
        "LiczbaWierszyZakupow"
    };

    public bool IsDirectory(string path) => Directory.Exists(path);

    public bool LooksLikeSectionedFile(IReadOnlyList<IReadOnlyList<string>> rows)
{
    if (rows is null || rows.Count == 0) return false;

    // scan first ~200 rows (skip any titles)
    var limit = Math.Min(rows.Count, 200);

    for (int ri = 0; ri < limit; ri++)
    {
        var r = rows[ri];
        if (r is null || r.Count == 0) continue;

        // Your real format: SEKCJA,<SECTION-ID>
        var first = NormalizeToken(r[0] ?? "");
        if (!first.Equals("SEKCJA", StringComparison.OrdinalIgnoreCase))
            continue;

        // second cell contains section id
        var second = r.Count > 1 ? NormalizeToken(r[1] ?? "") : "";
        if (string.IsNullOrWhiteSpace(second)) continue;

        // Accept any known Go section IDs
        if (IsKnownGoSectionId(second)) return true;
    }

    return false;
}

private static bool IsKnownGoSectionId(string id)
{
    // matches what you showed in CSV
    return id.Equals("NAGLOWEK", StringComparison.OrdinalIgnoreCase)
        || id.Equals("PODMIOT", StringComparison.OrdinalIgnoreCase)
        || id.Equals("DEKLARACJA-NAGLOWEK", StringComparison.OrdinalIgnoreCase)
        || id.Equals("DEKLARACJA-POZ-SZCZ", StringComparison.OrdinalIgnoreCase)
        || id.Equals("DEKLARACJA-POUCZENIA", StringComparison.OrdinalIgnoreCase)
        || id.Equals("SPRZEDAZ", StringComparison.OrdinalIgnoreCase)
        || id.Equals("SPRZEDAZ-CTRL", StringComparison.OrdinalIgnoreCase)
        || id.Equals("ZAKUP", StringComparison.OrdinalIgnoreCase)
        || id.Equals("ZAKUP-CTRL", StringComparison.OrdinalIgnoreCase);
}



    public bool LooksLikeSingleHeaderFile(IReadOnlyList<string> headerRow)
    {
        if (headerRow is null || headerRow.Count == 0) return false;

        foreach (var h in headerRow)
        {
            var s = NormalizeToken(h ?? "");
            if (s.Length == 0) continue;

            var dot = s.IndexOf('.');
            if (dot <= 0 || dot >= s.Length - 1) continue;

            var prefix = s[..dot].Trim();

            // if prefix matches mapper name or Go id -> strong yes
            if (MapperNames.Contains(prefix) || GoIds.Contains(prefix))
                return true;

            // otherwise still accept Section.Field pattern
            return true;
        }

        return false;
    }

    private static bool RowContainsAnyKnownHeader(IReadOnlyList<string> row)
    {
        // Look for any cell that matches one of the known start columns
        for (int i = 0; i < row.Count; i++)
        {
            var s = NormalizeToken(row[i] ?? "");
            if (s.Length == 0) continue;
            if (StartColumns.Contains(s)) return true;
        }
        return false;
    }

    private static bool TryGetFirstNonEmptyCell(IReadOnlyList<string> row, out string value)
    {
        for (int i = 0; i < row.Count; i++)
        {
            var s = row[i] ?? "";
            if (!string.IsNullOrWhiteSpace(s))
            {
                value = s;
                return true;
            }
        }

        value = "";
        return false;
    }

    private static string NormalizeToken(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "";

        var s = input.Trim();

        // remove UTF-8 BOM if present in first cell of file
        s = s.TrimStart('\uFEFF');

        // common wrappers: "[Naglowek]" / "Sekcja: Naglowek"
        s = s.Trim('[', ']', '{', '}', '(', ')');
        s = s.Replace("Sekcja:", "", StringComparison.OrdinalIgnoreCase)
             .Replace("Section:", "", StringComparison.OrdinalIgnoreCase)
             .Trim();

        // collapse whitespace
        s = string.Join(' ', s.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        return s;
    }
}
