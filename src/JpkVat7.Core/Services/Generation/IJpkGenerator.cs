using JpkVat7.Core.Abstractions.Result;
using JpkVat7.Core.Domain;

namespace JpkVat7.Core.Services.Generation;

public interface IJpkGenerator
{
    Result<string> GenerateXml(JpkInputBundle bundle);
}
