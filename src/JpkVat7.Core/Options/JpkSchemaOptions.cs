namespace JpkVat7.Core.Options;

public sealed class JpkSchemaOptions
{
    public string RootElementName { get; init; } = "JPK";
    public string NamespaceUri { get; init; } = "http://jpk.mf.gov.pl/wzor/2020/05/08/9393/";
    public string EncodingName { get; init; } = "utf-8";
    public string? XsdPath { get; init; }
}
