namespace JpkVat7.Core.Domain.Sections;

/// <summary>
/// Mirrors the Go struct:
/// type SAFTSection struct { Id string; StartCol string }
/// </summary>
public sealed record SectionDefinition(
    string Id,
    string StartColumn
);
