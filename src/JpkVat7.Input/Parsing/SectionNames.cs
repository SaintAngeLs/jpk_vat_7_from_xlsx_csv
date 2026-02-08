namespace JpkVat7.Input.Parsing;

public static class SectionNames
{
    public const string Naglowek = "Naglowek";
    public const string Podmiot = "Podmiot";

    // Combined declaration (current)
    public const string Deklaracja = "Deklaracja";

    // Optional split declaration (supported by DefaultSectionMapper)
    public const string DeklaracjaNaglowek = "DeklaracjaNaglowek";
    public const string DeklaracjaPozSzcz  = "DeklaracjaPozSzcz";
    public const string DeklaracjaPouczenia = "DeklaracjaPouczenia";

    public const string SprzedazWiersz = "SprzedazWiersz";
    public const string SprzedazCtrl  = "SprzedazCtrl";

    public const string ZakupWiersz = "ZakupWiersz";
    public const string ZakupCtrl  = "ZakupCtrl";

    public static readonly HashSet<string> Known = new(StringComparer.OrdinalIgnoreCase)
    {
        Naglowek,
        Podmiot,

        Deklaracja,
        DeklaracjaNaglowek,
        DeklaracjaPozSzcz,
        DeklaracjaPouczenia,

        SprzedazWiersz,
        SprzedazCtrl,

        ZakupWiersz,
        ZakupCtrl
    };

    public static readonly HashSet<string> Required = new(StringComparer.OrdinalIgnoreCase)
    {
        Naglowek,
        Podmiot,
        // keep requiring the combined form for now
        Deklaracja

        // If you switch to split declaration later, change Required to:
        // DeklaracjaNaglowek, DeklaracjaPozSzcz, DeklaracjaPouczenia
    };
}
