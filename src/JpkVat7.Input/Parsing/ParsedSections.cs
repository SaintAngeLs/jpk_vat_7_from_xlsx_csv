namespace JpkVat7.Input.Parsing;

public sealed record ParsedSections(
    IReadOnlyDictionary<string, IReadOnlyList<IReadOnlyDictionary<string, string>>> Sections
);
