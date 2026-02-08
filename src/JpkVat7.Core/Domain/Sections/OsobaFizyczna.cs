namespace JpkVat7.Core.Domain.Sections;

/// <summary>
/// Minimal set — extend with all fields you support in input/config.
/// </summary>
public sealed record OsobaFizyczna
{
    // Usually: NIP + name parts
    public string NIP { get; init; } = "";

    public string ImiePierwsze { get; init; } = "";
    public string Nazwisko { get; init; } = "";
    public string DataUrodzenia { get; init; } = ""; // keep as string if your source is string

    // Optional contact/address:
    public string Email { get; init; } = "";
    public string Telefon { get; init; } = "";

    public bool HasAnyData()
        => !string.IsNullOrWhiteSpace(NIP)
           || !string.IsNullOrWhiteSpace(ImiePierwsze)
           || !string.IsNullOrWhiteSpace(Nazwisko);
}