using CsvHelper;
using CsvHelper.Configuration;
using JpkVat7.Core.Abstractions.Result;
using JpkVat7.Input.Abstractions;
using JpkVat7.Input.Parsing;
using System.Globalization;

namespace JpkVat7.Input.Csv;

public sealed class CsvWorkbookReader : IWorkbookReader
{
    public bool CanRead(string path)
        => Path.GetExtension(path).Equals(".csv", StringComparison.OrdinalIgnoreCase);

    public async Task<Result<Table>> ReadAsync(string path, CancellationToken ct)
    {
        try
        {
            // Try ';' first (very common in PL CSV), then ','.
            var t1 = await ReadWithDelimiter(path, ";", ct);
            if (t1.IsSuccess && LooksReasonable(t1.Value))
                return t1;

            var t2 = await ReadWithDelimiter(path, ",", ct);
            if (t2.IsSuccess && LooksReasonable(t2.Value))
                return t2;

            // If both look "weird" just return first success, or combine error messages
            if (t1.IsSuccess) return t1;
            if (t2.IsSuccess) return t2;

            return Result.Fail<Table>(new Error("csv.read_failed", $"{t1.Error?.Message}; {t2.Error?.Message}"));
        }
        catch (Exception ex)
        {
            return Result.Fail<Table>(new Error("csv.read_failed", ex.Message));
        }
    }

    private static async Task<Result<Table>> ReadWithDelimiter(string path, string delimiter, CancellationToken ct)
    {
        try
        {
            var table = new Table();

            var cfg = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false,
                BadDataFound = null,
                MissingFieldFound = null,
                Delimiter = delimiter,
                DetectDelimiter = false,     // ✅ critical: disable auto detection
                TrimOptions = TrimOptions.Trim,
                IgnoreBlankLines = true,
                // Helps with messy CSV
                Mode = CsvMode.RFC4180
            };

            using var stream = File.OpenRead(path);
            using var reader = new StreamReader(stream);
            using var csv = new CsvReader(reader, cfg);

            // ✅ Much safer way to read fields:
            // after ReadAsync(), csv.Parser.Record contains the whole row as string[]
            while (await csv.ReadAsync())
            {
                ct.ThrowIfCancellationRequested();

                var record = csv.Parser.Record;
                if (record is null)
                    continue;

                table.Rows.Add(record.Select(x => x ?? string.Empty).ToList());
            }

            return Result.Ok(table);
        }
        catch (Exception ex)
        {
            return Result.Fail<Table>(new Error("csv.read_failed", $"Delimiter='{delimiter}': {ex.Message}"));
        }
    }

    private static bool LooksReasonable(Table t)
    {
        // heuristic: if there are many rows and the average column count is >1, it's likely correct
        if (t.Rows.Count == 0) return false;
        var firstNonEmpty = t.Rows.FirstOrDefault(r => r.Any(s => !string.IsNullOrWhiteSpace(s)));
        if (firstNonEmpty is null) return false;

        // if everything is in one column, delimiter likely wrong
        return firstNonEmpty.Count > 1;
    }
}
