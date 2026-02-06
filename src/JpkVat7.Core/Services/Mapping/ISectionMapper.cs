using JpkVat7.Core.Abstractions.Result;
using JpkVat7.Core.Domain;

namespace JpkVat7.Core.Services.Mapping;

public interface ISectionMapper
{
    Result<JpkInputBundle> MapToBundle(
        IReadOnlyDictionary<string, IReadOnlyList<IReadOnlyDictionary<string,string>>> sections);
}
