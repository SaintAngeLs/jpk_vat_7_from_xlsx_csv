using System.Globalization;
using JpkVat7.Core.Abstractions.Result;
using JpkVat7.Core.Domain;
using JpkVat7.Core.Domain.Sections;

namespace JpkVat7.Core.Services.Mapping;

public sealed class DefaultSectionMapper : ISectionMapper
{
    // Your current parser keys (JpkVat7.Input/Parsing/SectionNames.cs)
    public const string NaglowekKey = "Naglowek";
    public const string PodmiotKey = "Podmiot";
    public const string DeklaracjaKey = "Deklaracja";
    public const string SprzedazWierszKey = "SprzedazWiersz";
    public const string ZakupWierszKey = "ZakupWiersz";

    // If later you split declaration like Go (recommended long-term), mapper supports it too:
    public const string DeklaracjaNaglowekKey = "DeklaracjaNaglowek";
    public const string DeklaracjaPozSzczKey = "DeklaracjaPozSzcz";
    public const string DeklaracjaPouczeniaKey = "DeklaracjaPouczenia";

    public const string SprzedazCtrlKey = "SprzedazCtrl";
    public const string ZakupCtrlKey = "ZakupCtrl";

    public Result<JpkInputBundle> MapToBundle(
        IReadOnlyDictionary<string, IReadOnlyList<IReadOnlyDictionary<string, string>>> sections)
    {
        // --- required: Naglowek ---
        if (!TryGetSingleRow(sections, NaglowekKey, out var nagRow, out var nagErr))
            return Result.Fail<JpkInputBundle>(nagErr);

        // --- required: Podmiot ---
        if (!TryGetSingleRow(sections, PodmiotKey, out var podRow, out var podErr))
            return Result.Fail<JpkInputBundle>(podErr);

        // --- required: Deklaracja (either single combined key or split keys) ---
        // Current state: you likely have "Deklaracja" as one row.
        // If you later produce separate keys, this mapper will pick them.
        IReadOnlyDictionary<string, string>? dekCombined = null;

        TryGetSingleRow(sections, DeklaracjaKey, out dekCombined, out _);

        TryGetSingleRow(sections, DeklaracjaNaglowekKey, out var dekNag, out _);
        TryGetSingleRow(sections, DeklaracjaPozSzczKey, out var dekPoz, out _);
        TryGetSingleRow(sections, DeklaracjaPouczeniaKey, out var dekPou, out _);

        if (dekCombined is null && (dekNag is null || dekPoz is null || dekPou is null))
        {
            return Result.Fail<JpkInputBundle>(
                new Error("map.missing_deklaracja",
                    "Missing Deklaracja section. Provide either 'Deklaracja' (combined) or all: DeklaracjaNaglowek, DeklaracjaPozSzcz, DeklaracjaPouczenia"));
        }

        // --- optional: sales + purchase rows ---
        sections.TryGetValue(SprzedazWierszKey, out var sprRows);
        sections.TryGetValue(ZakupWierszKey, out var zakRows);

        // --- optional/derived: ctrl sections ---
        // If parser provides them, use them. Otherwise compute.
        TryGetSingleRow(sections, SprzedazCtrlKey, out var sprCtrlRow, out _);
        TryGetSingleRow(sections, ZakupCtrlKey, out var zakCtrlRow, out _);

        var naglowek = MapNaglowek(nagRow);

        var podmiot = MapPodmiot(podRow);

        // If declaration is combined, we map each sub-record from the same dictionary.
        // If it's split, each part uses its own dictionary.
        var deklaracjaNaglowek = MapDeklaracjaNaglowek(dekNag ?? dekCombined!);
        var deklaracjaPozSzcz = MapDeklaracjaPozSzcz(dekPoz ?? dekCombined!);
        var deklaracjaPouczenia = MapDeklaracjaPouczenia(dekPou ?? dekCombined!);

        var sprzedaz = (sprRows ?? Array.Empty<IReadOnlyDictionary<string, string>>())
            .Select(MapSprzedazWiersz)
            .ToList();

        var zakup = (zakRows ?? Array.Empty<IReadOnlyDictionary<string, string>>())
            .Select(MapZakupWiersz)
            .ToList();

        var sprzedazCtrl = sprCtrlRow is not null
            ? MapSprzedazCtrl(sprCtrlRow)
            : new SprzedazCtrl
            {
                LiczbaWierszySprzedazy = sprzedaz.Count,
                PodatekNalezny = SumDecimal(sprzedaz, w => w.K_11) // pick the field you treat as tax due
            };

        var zakupCtrl = zakCtrlRow is not null
            ? MapZakupCtrl(zakCtrlRow)
            : new ZakupCtrl
            {
                LiczbaWierszyZakupow = zakup.Count,
                PodatekNaliczony = SumDecimal(zakup, w => w.K_41) // pick the field you treat as tax input
            };

        var bundle = new JpkInputBundle(
            naglowek,
            podmiot,
            deklaracjaNaglowek,
            deklaracjaPozSzcz,
            deklaracjaPouczenia,
            sprzedaz,
            sprzedazCtrl,
            zakup,
            zakupCtrl
        );

        return Result.Ok(bundle);
    }

    // -----------------------------
    // REQUIRED SECTION HELPERS
    // -----------------------------
    private static bool TryGetSingleRow(
        IReadOnlyDictionary<string, IReadOnlyList<IReadOnlyDictionary<string, string>>> sections,
        string key,
        out IReadOnlyDictionary<string, string>? row,
        out Error error)
    {
        row = null;
        error = new Error("map.missing_section", $"Missing section: {key}");

        if (!sections.TryGetValue(key, out var rows) || rows.Count == 0)
            return false;

        row = rows[0];
        return true;
    }

    // -----------------------------
    // MAPPING: Naglowek
    // -----------------------------
    private static Naglowek MapNaglowek(IReadOnlyDictionary<string, string> row)
        => new()
        {
            KodFormularza = Get(row, "KodFormularza"),
            WariantFormularza = Get(row, "WariantFormularza"),
            CelZlozenia = Get(row, "CelZlozenia"),
            Rok = GetInt(row, "Rok"),
            Miesiac = GetInt(row, "Miesiac"),
            KodUrzedu = Get(row, "KodUrzedu"),
            NazwaSystemu = Get(row, "NazwaSystemu"),
            DataWytworzeniaJpk = GetDateTimeOrUtcNow(row, "DataWytworzeniaJpk")
        };

    // -----------------------------
    // MAPPING: Podmiot (OsobaFizyczna vs OsobaNiefizyczna)
    // -----------------------------
    private static Podmiot MapPodmiot(IReadOnlyDictionary<string, string> row)
    {
        var typ = Get(row, "typPodmiotu");

        // You may have different input column naming.
        // Common patterns:
        // - Fizyczna: ImiePierwsze, Nazwisko
        // - Niefizyczna: PelnaNazwa
        var fiz = new OsobaFizyczna
        {
            NIP = FirstNonEmpty(Get(row, "NIP"), Get(row, "NrId")),
            ImiePierwsze = Get(row, "ImiePierwsze"),
            Nazwisko = Get(row, "Nazwisko"),
            DataUrodzenia = Get(row, "DataUrodzenia"),
            Email = Get(row, "Email"),
            Telefon = Get(row, "Telefon"),
        };

        var nie = new OsobaNiefizyczna
        {
            NIP = FirstNonEmpty(Get(row, "NIP"), Get(row, "NrId")),
            PelnaNazwa = FirstNonEmpty(Get(row, "PelnaNazwa"), Get(row, "NazwaPelna"), Get(row, "Nazwa")),
            Email = Get(row, "Email"),
            Telefon = Get(row, "Telefon"),
        };

        // Decide based on typPodmiotu first, otherwise based on which has data
        var podmiot = new Podmiot
        {
            TypPodmiotu = typ,
            Fizyczna = fiz.HasAnyData() ? fiz : null,
            Niefizyczna = nie.HasAnyData() ? nie : null
        };

        // Ensure exactly one branch is present according to rules in Podmiot
        // (will throw if inconsistent; caught by caller and returned as Result error)
        podmiot.ValidateOrThrow();

        // Normalize: keep only the chosen variant to avoid ambiguity later
        if (podmiot.IsOsobaFizyczna())
            return podmiot with { Niefizyczna = null };
        return podmiot with { Fizyczna = null };
    }

    // -----------------------------
    // MAPPING: Deklaracja parts
    // -----------------------------
    private static DeklaracjaNaglowek MapDeklaracjaNaglowek(IReadOnlyDictionary<string, string> row)
        => new()
        {
            KodFormularzaDekl = Get(row, "KodFormularzaDekl"),
            WariantFormularzaDekl = Get(row, "WariantFormularzaDekl"),
            CelZlozenia = Get(row, "CelZlozeniaDekl"),
            Rok = GetInt(row, "RokDekl", fallbackKey: "Rok"),
            Miesiac = GetInt(row, "MiesiacDekl", fallbackKey: "Miesiac"),
            KodUrzedu = Get(row, "KodUrzeduDekl", fallbackKey: "KodUrzedu"),
        };

    private static DeklaracjaPozSzcz MapDeklaracjaPozSzcz(IReadOnlyDictionary<string, string> row)
        => new()
        {
            P_10 = GetDecimalNullable(row, "P_10"),
            P_11 = GetDecimalNullable(row, "P_11"),
            P_12 = GetDecimalNullable(row, "P_12"),
            P_68 = GetBoolNullable(row, "P_68"),
            P_69 = GetBoolNullable(row, "P_69"),
        };

    private static DeklaracjaPouczenia MapDeklaracjaPouczenia(IReadOnlyDictionary<string, string> row)
        => new()
        {
            Pouczenia = Get(row, "Pouczenia")
        };

    // -----------------------------
    // MAPPING: rows + ctrl
    // -----------------------------
    private static SprzedazWiersz MapSprzedazWiersz(IReadOnlyDictionary<string, string> row)
        => new()
        {
            LpSprzedazy = GetInt(row, "LpSprzedazy"),
            NrKontrahenta = Get(row, "NrKontrahenta"),
            NazwaKontrahenta = Get(row, "NazwaKontrahenta"),
            DowodSprzedazy = Get(row, "DowodSprzedazy"),
            DataWystawienia = GetDateNullable(row, "DataWystawienia"),
            DataSprzedazy = GetDateNullable(row, "DataSprzedazy"),
            K_10 = GetDecimalNullable(row, "K_10"),
            K_11 = GetDecimalNullable(row, "K_11"),
            K_12 = GetDecimalNullable(row, "K_12"),
            GTU_01 = GetBoolNullable(row, "GTU_01"),
            TP = GetBoolNullable(row, "TP"),
        };

    private static ZakupWiersz MapZakupWiersz(IReadOnlyDictionary<string, string> row)
        => new()
        {
            LpZakupu = GetInt(row, "LpZakupu"),
            NrDostawcy = Get(row, "NrDostawcy"),
            NazwaDostawcy = Get(row, "NazwaDostawcy"),
            DowodZakupu = Get(row, "DowodZakupu"),
            DataZakupu = GetDateNullable(row, "DataZakupu"),
            DataWplywu = GetDateNullable(row, "DataWplywu"),
            K_40 = GetDecimalNullable(row, "K_40"),
            K_41 = GetDecimalNullable(row, "K_41"),
            K_42 = GetDecimalNullable(row, "K_42"),
            MPP = GetBoolNullable(row, "MPP"),
        };

    private static SprzedazCtrl MapSprzedazCtrl(IReadOnlyDictionary<string, string> row)
        => new()
        {
            LiczbaWierszySprzedazy = GetInt(row, "LiczbaWierszySprzedazy"),
            PodatekNalezny = GetDecimal(row, "PodatekNalezny")
        };

    private static ZakupCtrl MapZakupCtrl(IReadOnlyDictionary<string, string> row)
        => new()
        {
            LiczbaWierszyZakupow = GetInt(row, "LiczbaWierszyZakupow"),
            PodatekNaliczony = GetDecimal(row, "PodatekNaliczony")
        };

    // -----------------------------
    // Small helpers
    // -----------------------------
    private static string Get(IReadOnlyDictionary<string, string> row, string key, string? fallbackKey = null)
    {
        if (row.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v))
            return v.Trim();

        if (fallbackKey is not null && row.TryGetValue(fallbackKey, out var v2) && !string.IsNullOrWhiteSpace(v2))
            return v2.Trim();

        return "";
    }

    private static int GetInt(IReadOnlyDictionary<string, string> row, string key, string? fallbackKey = null)
    {
        var s = Get(row, key, fallbackKey);
        if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
            return i;
        return 0;
    }

    private static decimal GetDecimal(IReadOnlyDictionary<string, string> row, string key, string? fallbackKey = null)
    {
        var s = Get(row, key, fallbackKey);
        if (TryParseDecimal(s, out var d))
            return d;
        return 0m;
    }

    private static decimal? GetDecimalNullable(IReadOnlyDictionary<string, string> row, string key)
    {
        var s = Get(row, key);
        if (string.IsNullOrWhiteSpace(s)) return null;
        return TryParseDecimal(s, out var d) ? d : null;
    }

    private static bool? GetBoolNullable(IReadOnlyDictionary<string, string> row, string key)
    {
        var s = Get(row, key);
        if (string.IsNullOrWhiteSpace(s)) return null;

        s = s.Trim().ToUpperInvariant();
        return s switch
        {
            "1" or "T" or "TRUE" or "TAK" or "YES" => true,
            "0" or "N" or "FALSE" or "NIE" or "NO" => false,
            _ => null
        };
    }

    private static DateTime GetDateTimeOrUtcNow(IReadOnlyDictionary<string, string> row, string key)
    {
        var s = Get(row, key);
        if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dt))
            return dt;

        // fallback to now, but you can also choose DateTime.MinValue
        return DateTime.UtcNow;
    }

    private static DateTime? GetDateNullable(IReadOnlyDictionary<string, string> row, string key)
    {
        var s = Get(row, key);
        if (string.IsNullOrWhiteSpace(s)) return null;

        // Most JPK dates are yyyy-MM-dd
        if (DateTime.TryParseExact(s.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return dt;

        if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
            return dt;

        return null;
    }

    private static bool TryParseDecimal(string s, out decimal d)
    {
        // accept "1234.56" and "1234,56"
        var normalized = s.Trim().Replace(',', '.');
        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out d);
    }

    private static string FirstNonEmpty(params string[] values)
        => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim() ?? "";

    private static decimal SumDecimal<T>(IEnumerable<T> rows, Func<T, decimal?> selector)
    {
        decimal sum = 0m;
        foreach (var r in rows)
        {
            var v = selector(r);
            if (v.HasValue) sum += v.Value;
        }
        return sum;
    }
}
