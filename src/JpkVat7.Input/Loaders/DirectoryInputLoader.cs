using JpkVat7.Core.Abstractions.Result;
using JpkVat7.Core.Domain;
using JpkVat7.Core.Services.Input;
using JpkVat7.Core.Services.Mapping;
using JpkVat7.Input.Csv;

namespace JpkVat7.Input.Loaders;

public sealed class DirectoryInputLoader : IInputLoader
{
    // Required base files (sales/purchase are required here because your pipeline expects them;
    // if you want them optional, we can relax this)
    private static readonly string[] RequiredFiles =
    [
        "naglowek.csv",
        "podmiot.csv",

        // declaration: either combined OR split (validated below)
        // "deklaracja.csv" OR split trio

        "sprzedaz.csv",
        "zakup.csv"
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

        foreach (var f in RequiredFiles)
        {
            var p = Path.Combine(path, f);
            if (!File.Exists(p))
                return Result.Fail<JpkInputBundle>(new Error("input.missing_file", $"Missing required file: {f}"));
        }

        // Declaration files (either combined or split)
        var deklaracjaCombinedPath = Path.Combine(path, "deklaracja.csv");
        var deklaracjaNagPath = Path.Combine(path, "deklaracja-naglowek.csv");
        var deklaracjaPozPath = Path.Combine(path, "deklaracja-poz-szcz.csv");
        var deklaracjaPouPath = Path.Combine(path, "deklaracja-pouczenia.csv");

        var hasCombinedDek = File.Exists(deklaracjaCombinedPath);
        var hasSplitDek = File.Exists(deklaracjaNagPath) && File.Exists(deklaracjaPozPath) && File.Exists(deklaracjaPouPath);

        if (!hasCombinedDek && !hasSplitDek)
        {
            return Result.Fail<JpkInputBundle>(new Error(
                "input.missing_deklaracja_files",
                "Missing deklaracja files. Provide either 'deklaracja.csv' or all three: 'deklaracja-naglowek.csv', 'deklaracja-poz-szcz.csv', 'deklaracja-pouczenia.csv'."));
        }

        async Task<Result<IReadOnlyList<IReadOnlyDictionary<string, string>>>> ReadAsRecords(string csvPath)
        {
            var tRes = await _csv.ReadAsync(csvPath, ct);
            if (!tRes.IsSuccess)
                return Result.Fail<IReadOnlyList<IReadOnlyDictionary<string, string>>>(tRes.Error);

            var rows = tRes.Value.Rows;
            if (rows.Count == 0)
                return Result.Ok<IReadOnlyList<IReadOnlyDictionary<string, string>>>(Array.Empty<IReadOnlyDictionary<string, string>>());

            var header = rows[0].Select(x => (x ?? "").Trim()).ToList();
            if (header.All(string.IsNullOrWhiteSpace))
                return Result.Ok<IReadOnlyList<IReadOnlyDictionary<string, string>>>(Array.Empty<IReadOnlyDictionary<string, string>>());

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

            return Result.Ok<IReadOnlyList<IReadOnlyDictionary<string, string>>>(list);
        }

        async Task<Result<IReadOnlyList<IReadOnlyDictionary<string, string>>>> ReadIfExists(string csvPath)
            => File.Exists(csvPath)
                ? await ReadAsRecords(csvPath)
                : Result.Ok<IReadOnlyList<IReadOnlyDictionary<string, string>>>(Array.Empty<IReadOnlyDictionary<string, string>>());

        var sections = new Dictionary<string, IReadOnlyList<IReadOnlyDictionary<string, string>>>(StringComparer.OrdinalIgnoreCase);

        // required
        var nag = await ReadAsRecords(Path.Combine(path, "naglowek.csv"));
        if (!nag.IsSuccess) return Result.Fail<JpkInputBundle>(nag.Error);
        sections[DefaultSectionMapper.NaglowekKey] = nag.Value;

        var pod = await ReadAsRecords(Path.Combine(path, "podmiot.csv"));
        if (!pod.IsSuccess) return Result.Fail<JpkInputBundle>(pod.Error);
        sections[DefaultSectionMapper.PodmiotKey] = pod.Value;

        // declaration: combined or split
        if (hasCombinedDek)
        {
            var dek = await ReadAsRecords(deklaracjaCombinedPath);
            if (!dek.IsSuccess) return Result.Fail<JpkInputBundle>(dek.Error);
            sections[DefaultSectionMapper.DeklaracjaKey] = dek.Value;
        }
        else
        {
            var dekNag = await ReadAsRecords(deklaracjaNagPath);
            if (!dekNag.IsSuccess) return Result.Fail<JpkInputBundle>(dekNag.Error);
            sections[DefaultSectionMapper.DeklaracjaNaglowekKey] = dekNag.Value;

            var dekPoz = await ReadAsRecords(deklaracjaPozPath);
            if (!dekPoz.IsSuccess) return Result.Fail<JpkInputBundle>(dekPoz.Error);
            sections[DefaultSectionMapper.DeklaracjaPozSzczKey] = dekPoz.Value;

            var dekPou = await ReadAsRecords(deklaracjaPouPath);
            if (!dekPou.IsSuccess) return Result.Fail<JpkInputBundle>(dekPou.Error);
            sections[DefaultSectionMapper.DeklaracjaPouczeniaKey] = dekPou.Value;
        }

        // required (rows)
        var spr = await ReadAsRecords(Path.Combine(path, "sprzedaz.csv"));
        if (!spr.IsSuccess) return Result.Fail<JpkInputBundle>(spr.Error);
        sections[DefaultSectionMapper.SprzedazWierszKey] = spr.Value;

        var zak = await ReadAsRecords(Path.Combine(path, "zakup.csv"));
        if (!zak.IsSuccess) return Result.Fail<JpkInputBundle>(zak.Error);
        sections[DefaultSectionMapper.ZakupWierszKey] = zak.Value;

        // optional ctrl
        var sprCtrl = await ReadIfExists(Path.Combine(path, "sprzedaz-ctrl.csv"));
        if (!sprCtrl.IsSuccess) return Result.Fail<JpkInputBundle>(sprCtrl.Error);
        if (sprCtrl.Value.Count > 0)
            sections[DefaultSectionMapper.SprzedazCtrlKey] = sprCtrl.Value;

        var zakCtrl = await ReadIfExists(Path.Combine(path, "zakup-ctrl.csv"));
        if (!zakCtrl.IsSuccess) return Result.Fail<JpkInputBundle>(zakCtrl.Error);
        if (zakCtrl.Value.Count > 0)
            sections[DefaultSectionMapper.ZakupCtrlKey] = zakCtrl.Value;

        return _mapper.MapToBundle(sections);
    }
}
