using ClosedXML.Excel;
using JpkVat7.Core.Abstractions.Result;
using JpkVat7.Input.Abstractions;
using JpkVat7.Input.Parsing;

namespace JpkVat7.Input.Xlsx;

public sealed class XlsxWorkbookReader : IWorkbookReader
{
    public bool CanRead(string path)
        => Path.GetExtension(path).Equals(".xlsx", StringComparison.OrdinalIgnoreCase);

    public Task<Result<Table>> ReadAsync(string path, CancellationToken ct)
    {
        try
        {
            var table = new Table();

            using var wb = new XLWorkbook(path);
            var ws = wb.Worksheets.First();

            var range = ws.RangeUsed();
            if (range == null)
                return Task.FromResult(Result.Ok(table));

            foreach (var row in range.Rows())
            {
                ct.ThrowIfCancellationRequested();
                var list = new List<string>();
                foreach (var cell in row.Cells())
                    list.Add(cell.GetFormattedString() ?? string.Empty);
                table.Rows.Add(list);
            }

            return Task.FromResult(Result.Ok(table));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Fail<Table>(new Error("xlsx.read_failed", ex.Message)));
        }
    }
}
