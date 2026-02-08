using System.Diagnostics;
using System.Security.Cryptography;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using JpkVat7.Core.Services.Generation;
using JpkVat7.Input.Loaders;
using JpkVat7.Xml.Abstractions;
using JpkVat7.Grpc;

namespace JpkVat7.Api.Grpc;

public sealed class JpkGrpcService : JpkService.JpkServiceBase
{
    private readonly FileInputLoader _loader;
    private readonly IJpkGenerator _generator;
    private readonly IJpkXmlValidator _validator;
    private readonly ILogger<JpkGrpcService> _log;

    public JpkGrpcService(
        FileInputLoader loader,
        IJpkGenerator generator,
        IJpkXmlValidator validator,
        ILogger<JpkGrpcService> log)
    {
        _loader = loader;
        _generator = generator;
        _validator = validator;
        _log = log;
    }

    public override async Task<GenerateReply> GenerateFromFile(
        IAsyncStreamReader<UploadChunk> requestStream,
        ServerCallContext context)
    {
        var swTotal = Stopwatch.StartNew();
        string? tmpPath = null;

        // useful request identifiers
        var peer = context.Peer;
        var traceId = Convert.ToHexString(RandomNumberGenerator.GetBytes(8)).ToLowerInvariant();

        using var scope = _log.BeginScope(new Dictionary<string, object>
        {
            ["traceId"] = traceId,
            ["peer"] = peer
        });

        _log.LogInformation("GenerateFromFile started. Peer={Peer}", peer);

        try
        {
            // Read first chunk (needed for filename + content)
            var swRead = Stopwatch.StartNew();
            if (!await requestStream.MoveNext(context.CancellationToken))
            {
                _log.LogWarning("Empty stream (no chunks).");
                return Fail("input.empty_stream", "No data received.");
            }

            var first = requestStream.Current;
            var fileName = string.IsNullOrWhiteSpace(first.FileName) ? "upload.bin" : first.FileName;
            var ext = Path.GetExtension(fileName).ToLowerInvariant();

            _log.LogInformation("First chunk received in {ElapsedMs} ms. FileName={FileName} Ext={Ext} FirstChunkBytes={Bytes}",
                swRead.ElapsedMilliseconds, fileName, ext, first.Data.Length);

            if (ext is not ".csv" and not ".xlsx")
            {
                _log.LogWarning("Rejected file extension: {Ext}", ext);
                return Fail("input.bad_ext", "Only .csv or .xlsx files are supported.");
            }

            tmpPath = CreateTempPath(ext);
            _log.LogInformation("Writing upload to temp file: {TmpPath}", tmpPath);

            long totalBytes = 0;
            int chunkCount = 0;

            var swWrite = Stopwatch.StartNew();
            await using (var fs = new FileStream(
                             tmpPath,
                             FileMode.CreateNew,
                             FileAccess.Write,
                             FileShare.None,
                             bufferSize: 128 * 1024,
                             useAsync: true))
            {
                if (first.Data is { Length: > 0 })
                {
                    await fs.WriteAsync(first.Data.Memory, context.CancellationToken);
                    totalBytes += first.Data.Length;
                }
                chunkCount++;

                while (await requestStream.MoveNext(context.CancellationToken))
                {
                    var chunk = requestStream.Current;
                    chunkCount++;

                    if (chunk.Data is { Length: > 0 })
                    {
                        await fs.WriteAsync(chunk.Data.Memory, context.CancellationToken);
                        totalBytes += chunk.Data.Length;
                    }

                    // log every ~50 chunks so you can see progress on large files
                    if (chunkCount % 50 == 0)
                    {
                        _log.LogDebug("Upload progress: chunks={Chunks} bytes={Bytes}", chunkCount, totalBytes);
                    }
                }

                await fs.FlushAsync(context.CancellationToken);
            }

            _log.LogInformation("Upload written. Chunks={Chunks} Bytes={Bytes} WriteTimeMs={Ms}",
                chunkCount, totalBytes, swWrite.ElapsedMilliseconds);

            if (totalBytes == 0)
            {
                _log.LogWarning("Upload had 0 bytes after writing.");
                return Fail("input.empty_file", "Uploaded file contained no data.");
            }

            // Load -> bundle
            _log.LogInformation("Loading input via FileInputLoader...");
            var swLoad = Stopwatch.StartNew();
            var bundleRes = await _loader.LoadAsync(tmpPath, context.CancellationToken);
            _log.LogInformation("Load finished in {Ms} ms. Success={Success}",
                swLoad.ElapsedMilliseconds, bundleRes.IsSuccess);

            if (!bundleRes.IsSuccess)
            {
                _log.LogWarning("Load failed. Code={Code} Message={Message}",
                    bundleRes.Error?.Code, bundleRes.Error?.Message);

                return Fail(bundleRes.Error?.Code ?? "bundle.failed",
                            bundleRes.Error?.Message ?? "Failed to load input.");
            }

            // Generate XML
            _log.LogInformation("Generating XML...");
            var swGen = Stopwatch.StartNew();
            var xmlRes = _generator.GenerateXml(bundleRes.Value);
            _log.LogInformation("GenerateXml finished in {Ms} ms. Success={Success} XmlLength={Len}",
                swGen.ElapsedMilliseconds, xmlRes.IsSuccess, xmlRes.Value?.Length ?? 0);

            if (!xmlRes.IsSuccess)
            {
                _log.LogWarning("XML generation failed. Code={Code} Message={Message}",
                    xmlRes.Error?.Code, xmlRes.Error?.Message);

                return Fail(xmlRes.Error?.Code ?? "xml.generate_failed",
                            xmlRes.Error?.Message ?? "Failed to generate XML.");
            }

            // Validate XML
            _log.LogInformation("Validating XML (XSD)...");
            var swVal = Stopwatch.StartNew();
            var valRes = _validator.Validate(xmlRes.Value);
            _log.LogInformation("Validation finished in {Ms} ms. Success={Success}",
                swVal.ElapsedMilliseconds, valRes.IsSuccess);

            if (!valRes.IsSuccess)
            {
                _log.LogWarning("Validation failed. Code={Code} Message={Message}",
                    valRes.Error?.Code, valRes.Error?.Message);

                return new GenerateReply
                {
                    Success = false,
                    Xml = xmlRes.Value, // handy for debugging
                    ErrorCode = valRes.Error?.Code ?? "xml.validation_failed",
                    ErrorMessage = valRes.Error?.Message ?? "XML validation failed.",
                    ValidationMessage = valRes.Error?.Message ?? ""
                };
            }

            _log.LogInformation("GenerateFromFile succeeded. TotalTimeMs={Ms}", swTotal.ElapsedMilliseconds);

            return new GenerateReply
            {
                Success = true,
                Xml = xmlRes.Value
            };
        }
        catch (OperationCanceledException)
        {
            _log.LogWarning("Request cancelled by client. TotalTimeMs={Ms}", swTotal.ElapsedMilliseconds);
            return Fail("request.cancelled", "Request was cancelled.");
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Unhandled exception in GenerateFromFile. TotalTimeMs={Ms}", swTotal.ElapsedMilliseconds);
            return Fail("server.exception", ex.Message);
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(tmpPath))
            {
                try
                {
                    File.Delete(tmpPath);
                    _log.LogDebug("Temp file deleted: {TmpPath}", tmpPath);
                }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, "Failed to delete temp file: {TmpPath}", tmpPath);
                }
            }

            _log.LogInformation("GenerateFromFile finished. TotalTimeMs={Ms}", swTotal.ElapsedMilliseconds);
        }
    }

    private static GenerateReply Fail(string code, string message) => new()
    {
        Success = false,
        ErrorCode = code,
        ErrorMessage = message
    };

    private static string CreateTempPath(string ext)
    {
        var name = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
        return Path.Combine(Path.GetTempPath(), $"{name}{ext}");
    }
}
