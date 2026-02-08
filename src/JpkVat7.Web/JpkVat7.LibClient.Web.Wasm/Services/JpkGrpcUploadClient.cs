using Google.Protobuf;
using Grpc.Core;
using JpkVat7.Grpc;
using Microsoft.AspNetCore.Components.Forms;

namespace JpkVat7.LibClient.Web.Wasm.Services;

public sealed class JpkGrpcUploadClient
{
    private readonly JpkService.JpkServiceClient _client;

    public JpkGrpcUploadClient(JpkService.JpkServiceClient client)
    {
        _client = client;
    }

    public async Task<GenerateReply> UploadAndConvertAsync(
        IBrowserFile file,
        CancellationToken ct = default,
        Func<string, Task>? reportStatus = null)
    {
        if (file is null)
            return new GenerateReply
            {
                Success = false,
                ErrorCode = "input.no_file",
                ErrorMessage = "No file selected."
            };

        var ext = Path.GetExtension(file.Name).ToLowerInvariant();
        if (ext is not ".csv" and not ".xlsx")
            return new GenerateReply
            {
                Success = false,
                ErrorCode = "input.bad_ext",
                ErrorMessage = "Only .csv or .xlsx files are supported."
            };

        const long maxBytes = 50 * 1024 * 1024; // align with server, adjust if needed
        const int chunkSize = 64 * 1024;        // 64KB chunks

        await reportStatus.SafeInvokeAsync("Opening file stream...");

        await using var stream = file.OpenReadStream(maxAllowedSize: maxBytes, cancellationToken: ct);

        // Client-streaming call
        using var call = _client.GenerateFromFile(cancellationToken: ct);

        // Send first chunk with file_name set
        var buffer = new byte[chunkSize];
        int read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), ct);

        if (read <= 0)
        {
            await call.RequestStream.CompleteAsync();
            return new GenerateReply
            {
                Success = false,
                ErrorCode = "input.empty",
                ErrorMessage = "Empty file."
            };
        }

        await reportStatus.SafeInvokeAsync("Uploading...");

        await call.RequestStream.WriteAsync(new UploadChunk
        {
            FileName = file.Name,
            Data = ByteString.CopyFrom(buffer, 0, read)
        });

        long uploaded = read;

        // Send remaining chunks
        while (true)
        {
            ct.ThrowIfCancellationRequested();

            read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), ct);
            if (read <= 0)
                break;

            await call.RequestStream.WriteAsync(new UploadChunk
            {
                Data = ByteString.CopyFrom(buffer, 0, read)
            });

            uploaded += read;

            // light UI update every ~512KB
            if ((uploaded % (512 * 1024)) < read)
                await reportStatus.SafeInvokeAsync($"Uploaded {(uploaded / 1024.0 / 1024.0):F1} MB...");
        }

        await call.RequestStream.CompleteAsync();

        await reportStatus.SafeInvokeAsync("Waiting for XML...");

        // Server reply contains XML or error
        var reply = await call.ResponseAsync;

        await reportStatus.SafeInvokeAsync(reply.Success ? "Done." : "Failed.");

        return reply;
    }
}

