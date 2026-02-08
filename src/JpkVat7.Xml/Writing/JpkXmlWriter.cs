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

            var encoding = Encoding.GetEncoding(_opt.EncodingName);

            using var ms = new MemoryStream();
            using (var tw = new StreamWriter(ms, encoding, bufferSize: 16 * 1024, leaveOpen: true))
            {
                var settings = new XmlWriterSettings
                {
                    Indent = true,
                    Encoding = encoding,
                    OmitXmlDeclaration = false,
                    CloseOutput = false
                };

                using (var xw = XmlWriter.Create(tw, settings))
                {
                    WriteJpkDocument(xw, bundle);
                    xw.Flush();
                }

                tw.Flush();
            }

            var xml = encoding.GetString(ms.ToArray());

            if (string.IsNullOrWhiteSpace(xml))
                return Result.Fail<string>(new Error("xml.empty", "Generated XML is empty."));

            return Result.Ok(xml);
        }
        catch (Exception ex)
        {
            return Result.Fail<string>(new Error("xml.write_failed", ex.Message));
        }
    }

    private static void WriteJpkDocument(XmlWriter xw, JpkInputBundle bundle)
    {
        var nsJpk = JpkNamespaces.Tns; // this should be your JPK targetNamespace URI
        var nsEtd = JpkNamespaces.Etd;
        var nsXsi = JpkNamespaces.Xsi;

        xw.WriteStartDocument();

        // Root uses DEFAULT namespace (no prefix) => <JPK xmlns="...">
        xw.WriteStartElement("JPK", nsJpk);

        // default namespace declaration
        xw.WriteAttributeString("xmlns", null, null, nsJpk);

        // other namespace prefixes
        xw.WriteAttributeString("xmlns", "etd", null, nsEtd);
        xw.WriteAttributeString("xmlns", "xsi", null, nsXsi);

        // --- Naglowek ---
        WriteRecordWrappedDefaultNs(xw, "Naglowek", bundle.Naglowek, nsJpk);

        // --- Podmiot1 ---
        xw.WriteStartElement("Podmiot1", nsJpk);
        xw.WriteAttributeString("rola", "Podatnik");

        if (bundle.Podmiot.IsOsobaFizyczna())
        {
            xw.WriteStartElement("OsobaFizyczna", nsJpk);
            WriteRecordElementsSmartNs(xw, bundle.Podmiot.Fizyczna!, nsJpk, nsEtd);
            xw.WriteEndElement(); // OsobaFizyczna
        }
        else
        {
            xw.WriteStartElement("OsobaNiefizyczna", nsJpk);
            WriteRecordElementsSmartNs(xw, bundle.Podmiot.Niefizyczna!, nsJpk, nsEtd);
            xw.WriteEndElement(); // OsobaNiefizyczna
        }

        xw.WriteEndElement(); // Podmiot1

        // --- Deklaracja ---
        xw.WriteStartElement("Deklaracja", nsJpk);

        xw.WriteStartElement("Naglowek", nsJpk);
        WriteRecordElementsSmartNs(xw, bundle.DeklaracjaNaglowek, nsJpk, nsEtd);
        xw.WriteEndElement();

        xw.WriteStartElement("PozycjeSzczegolowe", nsJpk);
        WriteRecordElementsSmartNs(xw, bundle.DeklaracjaPozSzcz, nsJpk, nsEtd);
        xw.WriteEndElement();

        // Pouczenia is a plain element in JPK namespace
        WriteRecordElementsSmartNs(xw, bundle.DeklaracjaPouczenia, nsJpk, nsEtd);

        xw.WriteEndElement(); // Deklaracja

        // --- Ewidencja ---
        xw.WriteStartElement("Ewidencja", nsJpk);

        foreach (var w in bundle.SprzedazWiersze)
        {
            xw.WriteStartElement("SprzedazWiersz", nsJpk);
            WriteRecordElementsSmartNs(xw, w, nsJpk, nsEtd);
            xw.WriteEndElement();
        }

        xw.WriteStartElement("SprzedazCtrl", nsJpk);
        WriteRecordElementsSmartNs(xw, bundle.SprzedazCtrl, nsJpk, nsEtd);
        xw.WriteEndElement();

        foreach (var w in bundle.ZakupWiersze)
        {
            xw.WriteStartElement("ZakupWiersz", nsJpk);
            WriteRecordElementsSmartNs(xw, w, nsJpk, nsEtd);
            xw.WriteEndElement();
        }

        xw.WriteStartElement("ZakupCtrl", nsJpk);
        WriteRecordElementsSmartNs(xw, bundle.ZakupCtrl, nsJpk, nsEtd);
        xw.WriteEndElement();

        xw.WriteEndElement(); // Ewidencja

        xw.WriteEndElement(); // JPK
        xw.WriteEndDocument();
    }

    private static void WriteRecordWrappedDefaultNs(XmlWriter xw, string wrapperName, object record, string nsJpk)
    {
        xw.WriteStartElement(wrapperName, nsJpk);
        WriteRecordElementsSmartNs(xw, record, nsJpk, JpkNamespaces.Etd);
        xw.WriteEndElement();
    }

    /// <summary>
    /// Writes each property as an element, choosing namespace:
    /// - default JPK namespace for most fields (no prefix)
    /// - etd namespace for specific identity fields (etd:NIP etc.) to match common JPKs
    ///
    /// NOTE: This does NOT handle attributes like KodFormularza.kodSystemowy yet.
    /// You must add explicit writing for those nodes.
    /// </summary>
    private static void WriteRecordElementsSmartNs(XmlWriter xw, object record, string nsJpk, string nsEtd)
    {
        var props = record.GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.CanRead)
            .ToArray();

        foreach (var p in props)
        {
            var value = p.GetValue(record);
            if (value is null) continue;

            string text = value switch
            {
                string s => s.Trim(),
                bool b => b ? "1" : "0",
                DateTime dt => dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                DateTimeOffset dto => dto.ToString("O", CultureInfo.InvariantCulture),
                IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString() ?? ""
            };

            if (string.IsNullOrWhiteSpace(text)) continue;

            // Choose namespace:
            // In many JPK examples, identity fields under OsobaFizyczna are in etd namespace.
            // Add more as needed.
            var name = p.Name;
            var useEtd = name is "NIP" or "ImiePierwsze" or "Nazwisko" or "DataUrodzenia";

            if (useEtd)
            {
                xw.WriteStartElement("etd", name, nsEtd); // => <etd:NIP>...
            }
            else
            {
                xw.WriteStartElement(name, nsJpk);        // => <NazwaSystemu>... in default ns
            }

            xw.WriteString(text);
            xw.WriteEndElement();
        }
    }
}
