using JpkVat7.Core.Abstractions.Result;

namespace JpkVat7.Input.Parsing;

public sealed class SingleHeaderFileParser
{
    public Result<ParsedSections> Parse(Table table)
    {
        if (table.Rows.Count < 2)
            return Result.Fail<ParsedSections>(new Error("parse.too_few_rows", "File must contain header + data rows"));

        var header = table.Rows[0].Select(x => (x ?? "").Trim()).ToList();
        var sections = new Dictionary<string, List<Dictionary<string, string>>>(StringComparer.OrdinalIgnoreCase);

        for (int r = 1; r < table.Rows.Count; r++)
        {
            var row = table.Rows[r].Select(x => (x ?? "").Trim()).ToList();
            if (row.All(string.IsNullOrWhiteSpace)) continue;

            for (int c = 0; c < header.Count; c++)
            {
                var h = header[c];
                if (string.IsNullOrWhiteSpace(h)) continue;

                var parts = h.Split('.', 2);
                if (parts.Length != 2) continue;

                var sectionName = parts[0];
                var fieldName = parts[1];

                if (!sections.TryGetValue(sectionName, out var list))
                {
                    list = new List<Dictionary<string, string>>();
                    sections[sectionName] = list;
                }

                // we treat each data row as “one record” for each section
                while (list.Count < r) list.Add(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
                var record = list[r - 1];

                record[fieldName] = c < row.Count ? row[c] : "";
            }
        }

        var frozen = sections.ToDictionary(
            kv => kv.Key,
            kv => (IReadOnlyList<IReadOnlyDictionary<string, string>>)kv.Value,
            StringComparer.OrdinalIgnoreCase);

        if (frozen.Count == 0)
            return Result.Fail<ParsedSections>(new Error("parse.no_prefixed_headers", "No headers in Section.Field format found"));

        return Result.Ok(new ParsedSections(frozen));
    }
}
