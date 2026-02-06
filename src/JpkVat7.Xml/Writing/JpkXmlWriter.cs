using System.Text;
using System.Xml;
using JpkVat7.Core.Abstractions.Result;
using JpkVat7.Core.Domain;
using JpkVat7.Core.Options;
using JpkVat7.Xml.Abstractions;
using Microsoft.Extensions.Options;

namespace JpkVat7.Xml.Writing;

public sealed class JpkXmlWriter : IJpkXmlWriter
{
    private readonly JpkSchemaOptions _opt;

    public JpkXmlWriter(IOptions<JpkSchemaOptions> opt) => _opt = opt.Value;

    public Result<string> Write(JpkInputBundle bundle)
    {
        try
        {
            var sb = new StringBuilder();
            var settings = new XmlWriterSettings
            {
                Indent = true,
                Encoding = Encoding.GetEncoding(_opt.EncodingName),
                OmitXmlDeclaration = false
            };

            using var xw = XmlWriter.Create(sb, settings);

            xw.WriteStartDocument();
            xw.WriteStartElement(_opt.RootElementName);
            xw.WriteAttributeString("xmlns", _opt.NamespaceUri);

            WriteSection(xw, "Naglowek", bundle.Naglowek.Fields);
            WriteSection(xw, "Podmiot", bundle.Podmiot.Fields);
            WriteSection(xw, "Deklaracja", bundle.Deklaracja.Fields);

            xw.WriteStartElement("Ewidencja");

            xw.WriteStartElement("Sprzedaz");
            foreach (var w in bundle.Sprzedaz)
                WriteSection(xw, "SprzedazWiersz", w.Fields);
            xw.WriteEndElement();

            xw.WriteStartElement("Zakup");
            foreach (var w in bundle.Zakup)
                WriteSection(xw, "ZakupWiersz", w.Fields);
            xw.WriteEndElement();

            xw.WriteEndElement(); // Ewidencja
            xw.WriteEndElement(); // root
            xw.WriteEndDocument();

            return Result.Ok(sb.ToString());
        }
        catch (Exception ex)
        {
            return Result.Fail<string>(new Error("xml.write_failed", ex.Message));
        }
    }

    private static void WriteSection(XmlWriter xw, string name, IReadOnlyDictionary<string,string> fields)
    {
        xw.WriteStartElement(name);
        foreach (var kv in fields)
        {
            if (string.IsNullOrWhiteSpace(kv.Key)) continue;
            if (string.IsNullOrWhiteSpace(kv.Value)) continue;
            xw.WriteElementString(kv.Key.Trim(), kv.Value.Trim());
        }
        xw.WriteEndElement();
    }
}
