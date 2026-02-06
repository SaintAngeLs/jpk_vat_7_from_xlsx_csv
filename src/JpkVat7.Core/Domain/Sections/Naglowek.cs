namespace JpkVat7.Core.Domain.Sections;

public sealed record Naglowek(IReadOnlyDictionary<string, string> Fields);

public sealed record Podmiot(IReadOnlyDictionary<string, string> Fields);

public sealed record Deklaracja(IReadOnlyDictionary<string, string> Fields);

public sealed record SprzedazWiersz(IReadOnlyDictionary<string, string> Fields);

public sealed record ZakupWiersz(IReadOnlyDictionary<string, string> Fields);
