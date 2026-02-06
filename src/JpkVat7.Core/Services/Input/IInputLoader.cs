using JpkVat7.Core.Abstractions.Result;
using JpkVat7.Core.Domain;

namespace JpkVat7.Core.Services.Input;

public interface IInputLoader
{
    Task<Result<JpkInputBundle>> LoadAsync(string path, CancellationToken ct);
}
