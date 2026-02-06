using JpkVat7.Core.Abstractions.Result;

namespace JpkVat7.Xml.Abstractions;

public interface IJpkXmlValidator
{
    Result Validate(string xml);
}
