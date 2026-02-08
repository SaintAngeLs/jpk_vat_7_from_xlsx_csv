namespace JpkVat7.Core.Domain.Sections;

/// <summary>
/// Canonical section identifiers (mirror the Go constants).
/// </summary>
public static class SectionIds
{
    public const string Naglowek = "NAGLOWEK";
    public const string Podmiot = "PODMIOT";
    public const string DeklaracjaNaglowek = "DEKLARACJA-NAGLOWEK";
    public const string DeklaracjaPozSzcz = "DEKLARACJA-POZ-SZCZ";
    public const string DeklaracjaPouczenia = "DEKLARACJA-POUCZENIA";
    public const string Sprzedaz = "SPRZEDAZ";
    public const string SprzedazCtrl = "SPRZEDAZ-CTRL";
    public const string Zakup = "ZAKUP";
    public const string ZakupCtrl = "ZAKUP-CTRL";

    /// <summary>
    /// Optional: convenient list for validation / iteration.
    /// </summary>
    public static readonly IReadOnlyList<string> All =
    [
        Naglowek,
        Podmiot,
        DeklaracjaNaglowek,
        DeklaracjaPozSzcz,
        DeklaracjaPouczenia,
        Sprzedaz,
        SprzedazCtrl,
        Zakup,
        ZakupCtrl
    ];
}
