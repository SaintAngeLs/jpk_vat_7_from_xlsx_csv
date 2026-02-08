namespace JpkVat7.Core.Domain.Sections;

public sealed record OsobaNiefizyczna
{
    public string NIP { get; init; } = "";
    public string PelnaNazwa { get; init; } = "";

    // Optional contact/address:
    public string Email { get; init; } = "";
    public string Telefon { get; init; } = "";

    public bool HasAnyData()
        => !string.IsNullOrWhiteSpace(NIP)
           || !string.IsNullOrWhiteSpace(PelnaNazwa);
}
