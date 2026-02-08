using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Components.Forms;
using JpkVat7.Core.Abstractions.Result;
using JpkVat7.Input.Parsing;

namespace JpkVat7.LibClient.Web.Wasm.Services;

// Alias to keep generics readable and avoid >>> parsing weirdness
using SectionsMap =
    System.Collections.Generic.IReadOnlyDictionary<
        string,
        System.Collections.Generic.IReadOnlyList<
            System.Collections.Generic.IReadOnlyDictionary<string, string>>>;

public sealed class BrowserFileInputLoader
{
    // OPTIONAL: allow UI progress updates from the Razor page
    // Usage from Razor: Loader.LoadSectionsAsync(_file, cts.Token, SetStatusAsync);
    public async Task<Result<SectionsMap>> LoadSectionsAsync(
        IBrowserFile file,
        CancellationToken ct = default,
        Func<string, Task>? reportStatus = null)
    {
        if (file is null)
            return Result.Fail<SectionsMap>(new Error("input.no_file", "No file selected"));

        var ext = Path.GetExtension(file.Name).ToLowerInvariant();
        if (ext is not ".csv" and not ".xlsx")
            return Result.Fail<SectionsMap>(new Error("input.bad_ext", "Only .csv or .xlsx files are supported"));

        const long maxBytes = 50 * 1024 * 1024;

        try
        {
            await reportStatus.SafeInvokeAsync("Opening stream...");

            await using var uploaded = file.OpenReadStream(maxAllowedSize: maxBytes, cancellationToken: ct);

            Table table;

            if (ext == ".csv")
            {
                await reportStatus.SafeInvokeAsync("Reading CSV...");
                table = await ReadCsvTableAsync(uploaded, ct, reportStatus);
            }
            else
            {
                await reportStatus.SafeInvokeAsync("Reading XLSX...");
                table = await ReadXlsxTableAsync(uploaded, ct, reportStatus); // TODO
            }

            await reportStatus.SafeInvokeAsync("Parsing sections...");

            // NOTE: Parse(...) is synchronous (CPU work). In WASM it can freeze the UI.
            // We can't offload to a background thread reliably in WASM,
            // so we at least yield *before/after* and try to keep earlier loops yielding.
            await Task.Yield();

            Result<ParsedSections> parsedRes = ext == ".csv"
                ? new SingleHeaderFileParser().Parse(table)
                : new SectionedFileParser().Parse(table);

            // Don’t rely on Console for UI feedback; return errors or use reportStatus.
            // Console.WriteLine(parsedRes);

            if (!parsedRes.IsSuccess)
                return Result.Fail<SectionsMap>(parsedRes.Error);

            await reportStatus.SafeInvokeAsync("Sections ready.");

            return Result.Ok((SectionsMap)parsedRes.Value.Sections);
        }
        catch (OperationCanceledException)
        {
            return Result.Fail<SectionsMap>(
                new Error("input.cancelled", "Operation cancelled or timed out while reading/parsing the file."));
        }
        catch (Exception ex)
        {
            return Result.Fail<SectionsMap>(new Error("input.read_failed", ex.Message));
        }
    }
private const int YieldEveryNRows = 25;
private const int MaxRows = 200_000; // adjust; huge CSVs will kill WASM memory

private static async Task<Table> ReadCsvTableAsync(
    Stream stream,
    CancellationToken ct,
    Func<string, Task>? reportStatus = null)
{
    await reportStatus.SafeInvokeAsync("Buffering upload in memory...");

    // Copy uploaded stream -> MemoryStream (seekable, stable in WASM)
    await using var ms = new MemoryStream();

    // ✅ Copy in chunks + yield to keep UI responsive
    var buffer = new byte[128 * 1024];
    int read;
    long total = 0;

    while ((read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), ct)) > 0)
    {
        await ms.WriteAsync(buffer.AsMemory(0, read), ct);
        total += read;

        // yield often during big uploads
        if ((total % (512 * 1024)) < read) // roughly every 512KB
        {
            await reportStatus.SafeInvokeAsync($"Buffered {(total / 1024.0 / 1024.0):F1} MB...");
            await Task.Yield();
        }
    }

    ms.Position = 0;

    // Read first line to choose delimiter ; vs ,
    string delimiter = ";";
    using (var peek = new StreamReader(ms, leaveOpen: true))
    {
        var firstLine = await peek.ReadLineAsync();
        if (!string.IsNullOrEmpty(firstLine))
        {
            var semi = firstLine.Count(c => c == ';');
            var comma = firstLine.Count(c => c == ',');
            delimiter = semi >= comma ? ";" : ",";
        }
    }
    ms.Position = 0;

    await reportStatus.SafeInvokeAsync($"CSV delimiter detected: '{delimiter}'");

    var table = new Table();

    var cfg = new CsvConfiguration(CultureInfo.InvariantCulture)
    {
        HasHeaderRecord = false,
        BadDataFound = null,
        MissingFieldFound = null,
        DetectDelimiter = false,
        Delimiter = delimiter
    };

    using var reader = new StreamReader(ms, leaveOpen: true);
    using var csv = new CsvReader(reader, cfg);

    int rowCount = 0;

    await reportStatus.SafeInvokeAsync("Reading rows...");

    while (await csv.ReadAsync())
    {
        ct.ThrowIfCancellationRequested();

        rowCount++;
        if (rowCount > MaxRows)
            throw new InvalidOperationException($"CSV too large for browser processing (>{MaxRows:n0} rows).");

        // Collect row fields
        var row = new List<string>(32);
        for (var i = 0; csv.TryGetField<string>(i, out var field); i++)
            row.Add(field ?? string.Empty);

        table.Rows.Add(row);

        // ✅ yield very frequently
        if ((rowCount % YieldEveryNRows) == 0)
        {
            await reportStatus.SafeInvokeAsync($"Read {rowCount:n0} rows...");
            await Task.Yield();
        }
    }

    await reportStatus.SafeInvokeAsync($"Finished reading CSV: {rowCount:n0} rows.");
    return table;
}

    // ------------------------
    // XLSX -> Table (TODO)
    // ------------------------
    private static async Task<Table> ReadXlsxTableAsync(
        Stream stream,
        CancellationToken ct,
        Func<string, Task>? reportStatus = null)
    {
        await reportStatus.SafeInvokeAsync("XLSX upload is not implemented yet (WASM stream reader needed).");
        await Task.CompletedTask;

        throw new NotSupportedException(
            "XLSX upload is not implemented for WASM yet. CSV works. Paste XlsxWorkbookReader.cs and I will provide a stream-based implementation.");
    }
}
