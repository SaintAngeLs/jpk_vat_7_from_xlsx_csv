using JpkVat7.Core.Abstractions.Result;
using JpkVat7.Core.Services.Mapping;

namespace JpkVat7.Input.Parsing;

public sealed class SectionedFileParser
{
    // Map Go IDs (from your CSV) -> mapper keys (what DefaultSectionMapper expects)
    private static readonly Dictionary<string, string> SectionIdMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["NAGLOWEK"] = DefaultSectionMapper.NaglowekKey,
        ["PODMIOT"] = DefaultSectionMapper.PodmiotKey,

        ["DEKLARACJA-NAGLOWEK"] = DefaultSectionMapper.DeklaracjaNaglowekKey,
        ["DEKLARACJA-POZ-SZCZ"] = DefaultSectionMapper.DeklaracjaPozSzczKey,
        ["DEKLARACJA-POUCZENIA"] = DefaultSectionMapper.DeklaracjaPouczeniaKey,

        ["SPRZEDAZ"] = DefaultSectionMapper.SprzedazWierszKey,
        ["SPRZEDAZ-CTRL"] = DefaultSectionMapper.SprzedazCtrlKey,

        ["ZAKUP"] = DefaultSectionMapper.ZakupWierszKey,
        ["ZAKUP-CTRL"] = DefaultSectionMapper.ZakupCtrlKey,
    };

    public Result<ParsedSections> Parse(Table table)
    {
        var rows = table.Rows;

        var sections = new Dictionary<string, IReadOnlyList<IReadOnlyDictionary<string, string>>>(
            StringComparer.OrdinalIgnoreCase);

        int i = 0;
        while (i < rows.Count)
        {
            var row = TrimRow(rows[i]);
            if (IsEmpty(row)) { i++; continue; }

            // Expect: SEKCJA,<ID>,...
            if (!IsSectionHeaderRow(row, out var mappedSectionName))
            {
                i++;
                continue;
            }

            // Move to next row(s) until we find the column header row (first non-empty row)
            i++;
            while (i < rows.Count && IsEmpty(TrimRow(rows[i]))) i++;

            if (i >= rows.Count) break;

            // Column header row
            var header = TrimRow(rows[i]);
            i++;

            if (IsEmpty(header))
            {
                // section exists but no columns
                sections[mappedSectionName] = Array.Empty<IReadOnlyDictionary<string, string>>();
                continue;
            }

            var list = new List<IReadOnlyDictionary<string, string>>();

            // Data rows until blank OR next section header
            while (i < rows.Count)
            {
                var data = TrimRow(rows[i]);

                if (IsEmpty(data)) { i++; break; }

                if (IsSectionHeaderRow(data, out _))
                    break;

                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (int c = 0; c < header.Count; c++)
                {
                    var key = header[c];
                    if (string.IsNullOrWhiteSpace(key)) continue;

                    dict[key] = c < data.Count ? data[c] : "";
                }

                list.Add(dict);
                i++;
            }

            sections[mappedSectionName] = list;
        }

        if (sections.Count == 0)
            return Result.Fail<ParsedSections>(new Error("parse.no_sections", "No sections found (expected 'SEKCJA,<ID>' rows)."));

        return Result.Ok(new ParsedSections(sections));
    }

    private static bool IsSectionHeaderRow(IReadOnlyList<string> row, out string mappedSectionName)
    {
        mappedSectionName = "";

        if (row.Count < 2) return false;

        var first = Normalize(row[0]);
        if (!first.Equals("SEKCJA", StringComparison.OrdinalIgnoreCase))
            return false;

        var rawId = Normalize(row[1]);
        if (string.IsNullOrWhiteSpace(rawId))
            return false;

        // Map Go ID -> mapper key
        if (SectionIdMap.TryGetValue(rawId, out var mapped))
        {
            mappedSectionName = mapped;
            return true;
        }

        // Also allow direct mapper keys if someone provides them
        if (rawId.Equals(DefaultSectionMapper.NaglowekKey, StringComparison.OrdinalIgnoreCase) ||
            rawId.Equals(DefaultSectionMapper.PodmiotKey, StringComparison.OrdinalIgnoreCase) ||
            rawId.Equals(DefaultSectionMapper.DeklaracjaKey, StringComparison.OrdinalIgnoreCase) ||
            rawId.Equals(DefaultSectionMapper.DeklaracjaNaglowekKey, StringComparison.OrdinalIgnoreCase) ||
            rawId.Equals(DefaultSectionMapper.DeklaracjaPozSzczKey, StringComparison.OrdinalIgnoreCase) ||
            rawId.Equals(DefaultSectionMapper.DeklaracjaPouczeniaKey, StringComparison.OrdinalIgnoreCase) ||
            rawId.Equals(DefaultSectionMapper.SprzedazWierszKey, StringComparison.OrdinalIgnoreCase) ||
            rawId.Equals(DefaultSectionMapper.SprzedazCtrlKey, StringComparison.OrdinalIgnoreCase) ||
            rawId.Equals(DefaultSectionMapper.ZakupWierszKey, StringComparison.OrdinalIgnoreCase) ||
            rawId.Equals(DefaultSectionMapper.ZakupCtrlKey, StringComparison.OrdinalIgnoreCase))
        {
            mappedSectionName = rawId;
            return true;
        }

        return false;
    }

    private static List<string> TrimRow(List<string> row)
        => row.Select(x => (x ?? "").Trim()).ToList();

    private static bool IsEmpty(IReadOnlyList<string> row)
        => row.Count == 0 || row.All(string.IsNullOrWhiteSpace);

    private static string Normalize(string s)
        => (s ?? "").Trim().TrimStart('\uFEFF');
}
