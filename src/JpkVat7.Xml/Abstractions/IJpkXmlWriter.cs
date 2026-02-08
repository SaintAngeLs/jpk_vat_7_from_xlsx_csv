using JpkVat7.Core.Abstractions.Result;
using JpkVat7.Core.Domain;

namespace JpkVat7.Xml.Abstractions;

public interface IJpkXmlWriter
{
    Result<string> Write(JpkInputBundle bundle);
}
