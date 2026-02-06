using JpkVat7.Core.Abstractions.Result;
using JpkVat7.Input.Parsing;

namespace JpkVat7.Input.Abstractions;

public interface IWorkbookReader
{
    bool CanRead(string path);
    Task<Result<Table>> ReadAsync(string path, CancellationToken ct);
}
