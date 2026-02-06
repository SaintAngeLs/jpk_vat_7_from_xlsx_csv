using JpkVat7.Core.Abstractions.Result;
using JpkVat7.Core.Domain;
using JpkVat7.Xml.Abstractions;

namespace JpkVat7.Core.Services.Generation;

public sealed class JpkGenerator : IJpkGenerator
{
    private readonly IJpkXmlWriter _writer;

    public JpkGenerator(IJpkXmlWriter writer) => _writer = writer;

    public Result<string> GenerateXml(JpkInputBundle bundle)
        => _writer.Write(bundle);
}
