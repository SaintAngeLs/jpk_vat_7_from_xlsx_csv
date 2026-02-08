namespace JpkVat7.Core.Domain.Sections;

/// <summary>
/// Mirrors Go logic:
/// if s.podmiot.OsobaFizyczna() => write OsobaFizyczna else OsobaNiefizyczna.
/// </summary>
public sealed record Podmiot
{
    /// <summary>
    /// In your inputs you already have a "typPodmiotu" start column (Go: StartCol "typPodmiotu").
    /// Common values: "FIZYCZNA" / "NIEFIZYCZNA" (or similar).
    /// </summary>
    public string TypPodmiotu { get; init; } = "";

    public OsobaFizyczna? Fizyczna { get; init; }
    public OsobaNiefizyczna? Niefizyczna { get; init; }

    public bool IsOsobaFizyczna()
    {
        // Primary: explicit typ
        if (!string.IsNullOrWhiteSpace(TypPodmiotu))
        {
            var t = TypPodmiotu.Trim().ToUpperInvariant();
            if (t is "FIZYCZNA" or "OSOBAFIZYCZNA" or "OSOBA_FIZYCZNA") return true;
            if (t is "NIEFIZYCZNA" or "OSOBANIEFIZYCZNA" or "OSOBA_NIEFIZYCZNA") return false;
        }

        // Fallback: based on which object is populated
        if (Fizyczna is not null && Niefizyczna is null) return true;
        if (Niefizyczna is not null && Fizyczna is null) return false;

        // If ambiguous, prefer Fizyczna if it has any meaningful content
        return Fizyczna?.HasAnyData() == true;
    }

    public void ValidateOrThrow()
    {
        var isFiz = IsOsobaFizyczna();
        if (isFiz && Fizyczna is null)
            throw new InvalidOperationException("TypPodmiotu indicates OsobaFizyczna, but Fizyczna is null.");

        if (!isFiz && Niefizyczna is null)
            throw new InvalidOperationException("TypPodmiotu indicates OsobaNiefizyczna, but Niefizyczna is null.");
    }
}