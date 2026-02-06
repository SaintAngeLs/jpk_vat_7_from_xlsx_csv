using JpkVat7.Input.Abstractions;

namespace JpkVat7.Input.Loaders;

public sealed class InputDetector : IInputDetector
{
    public bool IsDirectory(string path) => Directory.Exists(path);

    public bool LooksLikeSectionedFile(IReadOnlyList<IReadOnlyList<string>> rows)
        => rows.Any(r => r.Count > 0 && new[] { "Naglowek", "Podmiot", "Deklaracja" }
            .Contains((r[0] ?? "").Trim(), StringComparer.OrdinalIgnoreCase));

    public bool LooksLikeSingleHeaderFile(IReadOnlyList<string> headerRow)
        => headerRow.Any(h => (h ?? "").Contains('.'));
}
