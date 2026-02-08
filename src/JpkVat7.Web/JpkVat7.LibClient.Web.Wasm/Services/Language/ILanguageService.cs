namespace JpkVat7.LibClient.Web.Wasm.Services.Language;

public interface ILanguageService
{
    string CurrentCulture { get; }
    event Action? OnChanged;

    Task InitializeAsync(CancellationToken ct = default);
    Task SetCultureAsync(string culture, CancellationToken ct = default); // "pl" or "en"
}
