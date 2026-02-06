using JpkVat7.Core.Abstractions.Result;
using JpkVat7.Core.Domain;
using JpkVat7.Core.Services.Input;
using JpkVat7.Core.Services.Mapping;
using JpkVat7.Input.Csv;
using JpkVat7.Input.Parsing;

namespace JpkVat7.Input.Loaders;

public sealed class DirectoryInputLoader : IInputLoader
{
    private static readonly string[] Required =
    [
        "naglowek.csv", "podmiot.csv", "deklaracja.csv", "sprzedaz.csv", "zakup.csv"
    ];

    private readonly CsvWorkbookReader _csv;
    private readonly ISectionMapper _mapper;

    public DirectoryInputLoader(CsvWorkbookReader csv, ISectionMapper mapper)
    {
        _csv = csv;
        _mapper = mapper;
    }

    public async Task<Result<JpkInputBundle>> LoadAsync(string path, CancellationToken ct)
    {
        if (!Directory.Exists(path))
            return Result.Fail<JpkInputBundle>(new Error("input.not_directory", "Input path is not a directory"));

        foreach (var f in Required)
        {
            var p = Path.Combine(path, f);
            if (!File.Exists(p))
                return Result.Fail<JpkInputBundle>(new Error("input.missing_file", $"Missing required file: {f}"));
        }

        // Each CSV here is a simple: header row + one/many rows.
        async Task<Result<IReadOnlyList<IReadOnlyDictionary<string,string>>>> ReadAsRecords(string csvPath)
        {
            var tRes = await _csv.ReadAsync(csvPath, ct);
            if (!tRes.IsSuccess) return Result.Fail<IReadOnlyList<IReadOnlyDictionary<string,string>>>(tRes.Error);

            var rows = tRes.Value.Rows;
            if (rows.Count == 0)
                return Result.Ok<IReadOnlyList<IReadOnlyDictionary<string,string>>>(Array.Empty<IReadOnlyDictionary<string,string>>());

            var header = rows[0].Select(x => (x ?? "").Trim()).ToList();
            var list = new List<IReadOnlyDictionary<string, string>>();

            for (int r = 1; r < rows.Count; r++)
            {
                var row = rows[r].Select(x => (x ?? "").Trim()).ToList();
                if (row.All(string.IsNullOrWhiteSpace)) continue;

                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (int c = 0; c < header.Count; c++)
                {
                    var key = header[c];
                    if (string.IsNullOrWhiteSpace(key)) continue;
                    dict[key] = c < row.Count ? row[c] : "";
                }
                list.Add(dict);
            }

            return Result.Ok<IReadOnlyList<IReadOnlyDictionary<string,string>>>(list);
        }

        var sections = new Dictionary<string, IReadOnlyList<IReadOnlyDictionary<string, string>>>(StringComparer.OrdinalIgnoreCase);

        var nag = await ReadAsRecords(Path.Combine(path, "naglowek.csv"));
        if (!nag.IsSuccess) return Result.Fail<JpkInputBundle>(nag.Error);
        sections["Naglowek"] = nag.Value;

        var pod = await ReadAsRecords(Path.Combine(path, "podmiot.csv"));
        if (!pod.IsSuccess) return Result.Fail<JpkInputBundle>(pod.Error);
        sections["Podmiot"] = pod.Value;

        var dek = await ReadAsRecords(Path.Combine(path, "deklaracja.csv"));
        if (!dek.IsSuccess) return Result.Fail<JpkInputBundle>(dek.Error);
        sections["Deklaracja"] = dek.Value;

        var spr = await ReadAsRecords(Path.Combine(path, "sprzedaz.csv"));
        if (!spr.IsSuccess) return Result.Fail<JpkInputBundle>(spr.Error);
        sections["SprzedazWiersz"] = spr.Value;

        var zak = await ReadAsRecords(Path.Combine(path, "zakup.csv"));
        if (!zak.IsSuccess) return Result.Fail<JpkInputBundle>(zak.Error);
        sections["ZakupWiersz"] = zak.Value;

        return _mapper.MapToBundle(sections);
    }
}
