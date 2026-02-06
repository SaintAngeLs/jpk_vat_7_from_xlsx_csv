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
            var table = new Table();
            var cfg = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false,
                BadDataFound = null,
                MissingFieldFound = null,
                DetectDelimiter = true,
            };

            using var stream = File.OpenRead(path);
            using var reader = new StreamReader(stream);
            using var csv = new CsvReader(reader, cfg);

            while (await csv.ReadAsync())
            {
                ct.ThrowIfCancellationRequested();
                var row = new List<string>();
                for (var i = 0; csv.TryGetField<string>(i, out var field); i++)
                    row.Add(field ?? string.Empty);
                table.Rows.Add(row);
            }

            return Result.Ok(table);
        }
        catch (Exception ex)
        {
            return Result.Fail<Table>(new Error("csv.read_failed", ex.Message));
        }
    }
}
