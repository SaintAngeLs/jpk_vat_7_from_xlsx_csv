namespace JpkVat7.Core.Domain.Sections;

public sealed record DeklaracjaPouczenia
{
    public string Pouczenia { get; init; } = ""; // matches StartColumn
}
