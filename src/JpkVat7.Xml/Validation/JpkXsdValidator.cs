using System.Xml;
using System.Xml.Schema;
using JpkVat7.Core.Abstractions.Result;
using JpkVat7.Core.Options;
using JpkVat7.Xml.Abstractions;
using Microsoft.Extensions.Options;

namespace JpkVat7.Xml.Validation;

public sealed class JpkXsdValidator : IJpkXmlValidator
{
    private readonly JpkSchemaOptions _opt;

    public JpkXsdValidator(IOptions<JpkSchemaOptions> opt) => _opt = opt.Value;

    public Result Validate(string xml)
    {
        if (string.IsNullOrWhiteSpace(_opt.XsdPath))
            return Result.Ok(); // validation disabled

        if (!File.Exists(_opt.XsdPath))
            return Result.Fail(new Error("xsd.not_found", $"XSD not found: {_opt.XsdPath}"));

        try
        {
            var errors = new List<string>();
            var schemas = new XmlSchemaSet();
            schemas.Add(null, _opt.XsdPath);

            var settings = new XmlReaderSettings
            {
                ValidationType = ValidationType.Schema,
                Schemas = schemas
            };
            settings.ValidationEventHandler += (_, e) => errors.Add(e.Message);

            using var sr = new StringReader(xml);
            using var reader = XmlReader.Create(sr, settings);
            while (reader.Read()) { }

            return errors.Count == 0
                ? Result.Ok()
                : Result.Fail(new Error("xsd.invalid", string.Join("\n", errors)));
        }
        catch (Exception ex)
        {
            return Result.Fail(new Error("xsd.validate_failed", ex.Message));
        }
    }
}
