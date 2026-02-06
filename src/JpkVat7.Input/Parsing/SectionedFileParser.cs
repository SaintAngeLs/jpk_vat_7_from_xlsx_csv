using JpkVat7.Core.Abstractions.Result;

namespace JpkVat7.Input.Parsing;

public sealed class SectionedFileParser
{
    // Keep these names consistent across the solution:
    private static readonly HashSet<string> Known = new(StringComparer.OrdinalIgnoreCase)
    {
        "Naglowek", "Podmiot", "Deklaracja", "SprzedazWiersz", "ZakupWiersz"
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

            // section header line: first cell = section name
            var sectionName = row[0];
            if (!Known.Contains(sectionName)) { i++; continue; }

            i++;
            if (i >= rows.Count) break;

            // next row is column header
            var header = TrimRow(rows[i]);
            i++;

            var list = new List<IReadOnlyDictionary<string, string>>();
            while (i < rows.Count)
            {
                var data = TrimRow(rows[i]);
                if (IsEmpty(data)) { i++; break; }
                if (Known.Contains(data[0])) break;

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

            sections[sectionName] = list;
        }

        if (sections.Count == 0)
            return Result.Fail<ParsedSections>(new Error("parse.no_sections", "No known sections found in file"));

        return Result.Ok(new ParsedSections(sections));
    }

    private static List<string> TrimRow(List<string> row)
        => row.Select(x => (x ?? "").Trim()).ToList();

    private static bool IsEmpty(List<string> row)
        => row.All(string.IsNullOrWhiteSpace);
}
