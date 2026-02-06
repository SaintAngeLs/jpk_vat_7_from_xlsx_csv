namespace JpkVat7.Input.Abstractions;

public interface IInputDetector
{
    bool IsDirectory(string path);
    bool LooksLikeSectionedFile(IReadOnlyList<IReadOnlyList<string>> rows);
    bool LooksLikeSingleHeaderFile(IReadOnlyList<string> headerRow);
}
