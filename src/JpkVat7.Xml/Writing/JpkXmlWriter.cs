using System.Globalization;
using System.Reflection;
using System.Text;
using System.Xml;
using JpkVat7.Core.Abstractions;
using JpkVat7.Core.Abstractions.Result;
using JpkVat7.Core.Domain;
using JpkVat7.Core.Domain.Sections;
using JpkVat7.Core.Options;
using JpkVat7.Xml.Abstractions;
using Microsoft.Extensions.Options;

namespace JpkVat7.Xml.Writing;

public sealed class JpkXmlWriter : IJpkXmlWriter
{
    private readonly JpkSchemaOptions _opt;
    private readonly IClock _clock;

    public JpkXmlWriter(IOptions<JpkSchemaOptions> opt, IClock clock)
    {
        _opt = opt.Value;
        _clock = clock;
    }

    public Result<string> Write(JpkInputBundle bundle)
    {
        try
        {
            bundle.Podmiot.ValidateOrThrow();

            // IMPORTANT: write via StringWriter so XmlWriter has a real TextWriter target
            using var sw = new StringWriter(CultureInfo.InvariantCulture);

            var settings = new XmlWriterSettings
            {
                Indent = true,
                Encoding = Encoding.GetEncoding(_opt.EncodingName), // informational when writing to TextWriter
                OmitXmlDeclaration = false
            };

            using (var xw = XmlWriter.Create(sw, settings))
            {
                xw.WriteStartDocument();

                xw.WriteStartElement("tns", "JPK", JpkNamespaces.Tns);
                xw.WriteAttributeString("xmlns", "etd", null, JpkNamespaces.Etd);
                xw.WriteAttributeString("xmlns", "tns", null, JpkNamespaces.Tns);
                xw.WriteAttributeString("xmlns", "xsi", null, JpkNamespaces.Xsi);

                WriteRecordWrapped(xw, "Naglowek", bundle.Naglowek);

                xw.WriteStartElement("tns", "Podmiot1", JpkNamespaces.Tns);
                xw.WriteAttributeString("rola", "Podatnik");

                if (bundle.Podmiot.IsOsobaFizyczna())
                {
                    xw.WriteStartElement("tns", "OsobaFizyczna", JpkNamespaces.Tns);
                    WriteRecordElements(xw, bundle.Podmiot.Fizyczna!);
                    xw.WriteEndElement();
                }
                else
                {
                    xw.WriteStartElement("tns", "OsobaNiefizyczna", JpkNamespaces.Tns);
                    WriteRecordElements(xw, bundle.Podmiot.Niefizyczna!);
                    xw.WriteEndElement();
                }

                xw.WriteEndElement(); // Podmiot1

                xw.WriteStartElement("tns", "Deklaracja", JpkNamespaces.Tns);

                xw.WriteStartElement("tns", "Naglowek", JpkNamespaces.Tns);
                WriteRecordElements(xw, bundle.DeklaracjaNaglowek);
                xw.WriteEndElement();

                xw.WriteStartElement("tns", "PozycjeSzczegolowe", JpkNamespaces.Tns);
                WriteRecordElements(xw, bundle.DeklaracjaPozSzcz);
                xw.WriteEndElement();

                WriteRecordElements(xw, bundle.DeklaracjaPouczenia);

                xw.WriteEndElement(); // Deklaracja

                xw.WriteStartElement("tns", "Ewidencja", JpkNamespaces.Tns);

                foreach (var w in bundle.SprzedazWiersze)
                {
                    xw.WriteStartElement("tns", "SprzedazWiersz", JpkNamespaces.Tns);
                    WriteRecordElements(xw, w);
                    xw.WriteEndElement();
                }

                xw.WriteStartElement("tns", "SprzedazCtrl", JpkNamespaces.Tns);
                WriteRecordElements(xw, bundle.SprzedazCtrl);
                xw.WriteEndElement();

                foreach (var w in bundle.ZakupWiersze)
                {
                    xw.WriteStartElement("tns", "ZakupWiersz", JpkNamespaces.Tns);
                    WriteRecordElements(xw, w);
                    xw.WriteEndElement();
                }

                xw.WriteStartElement("tns", "ZakupCtrl", JpkNamespaces.Tns);
                WriteRecordElements(xw, bundle.ZakupCtrl);
                xw.WriteEndElement();

                xw.WriteEndElement(); // Ewidencja

                xw.WriteEndElement(); // JPK
                xw.WriteEndDocument();

                // ✅ make sure everything is written to the StringWriter
                xw.Flush();
            }

            var xml = sw.ToString();

            // ✅ fail fast if somehow empty
            if (string.IsNullOrWhiteSpace(xml))
                return Result.Fail<string>(new Error("xml.empty", "Generated XML is empty."));

            return Result.Ok(xml);
        }
        catch (Exception ex)
        {
            return Result.Fail<string>(new Error("xml.write_failed", ex.Message));
        }
    }


    /// <summary>
    /// Writes: <tns:{wrapperName}> (properties...) </tns:{wrapperName}>
    /// </summary>
    private static void WriteRecordWrapped(XmlWriter xw, string wrapperName, object record)
    {
        xw.WriteStartElement("tns", wrapperName, JpkNamespaces.Tns);
        WriteRecordElements(xw, record);
        xw.WriteEndElement();
    }

    /// <summary>
    /// Writes each property of a record as a child element.
    /// Skips null / empty string.
    ///
    /// IMPORTANT:
    /// JPK XSD often uses "sequence" so order matters.
    /// If you need strict order, see the note below to add an explicit order list.
    /// </summary>
    private static void WriteRecordElements(XmlWriter xw, object record)
    {
        var props = record.GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.CanRead)
            .ToArray();

        foreach (var p in props)
        {
            var value = p.GetValue(record);

            if (value is null) continue;

            // Convert to string
            string text = value switch
            {
                string s => s.Trim(),
                bool b => b ? "1" : "0", // often used in JPK-like formats; adjust if needed
                DateTime dt => dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                DateTimeOffset dto => dto.ToString("O", CultureInfo.InvariantCulture),
                IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString() ?? ""
            };

            if (string.IsNullOrWhiteSpace(text)) continue;

            // Default namespace is tns (matches your Go’s “99% is tns”)
            xw.WriteStartElement("tns", p.Name, JpkNamespaces.Tns);
            xw.WriteString(text);
            xw.WriteEndElement();
        }
    }
}
