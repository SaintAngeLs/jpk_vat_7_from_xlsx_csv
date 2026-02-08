using System.Globalization;
using JpkVat7.LibClient.Web.Wasm.Services.Preferences;

namespace JpkVat7.LibClient.Web.Wasm.Services.Language;

public sealed class LanguageService : ILanguageService
{
    private readonly IAppPreferencesService _prefs;
    private string _current = "en";

    public LanguageService(IAppPreferencesService prefs)
        => _prefs = prefs;

    public string CurrentCulture => _current;

    public event Action? OnChanged;

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        var p = await _prefs.GetAsync(ct);
        await ApplyCultureAsync(p.Culture, persist: false, ct);
    }

    public Task SetCultureAsync(string culture, CancellationToken ct = default)
        => ApplyCultureAsync(culture, persist: true, ct);

    private async Task ApplyCultureAsync(string culture, bool persist, CancellationToken ct)
    {
        culture = culture is "pl" or "en" ? culture : "en";
        _current = culture;

        var ci = new CultureInfo(culture);

        // IMPORTANT for WASM runtime changes
        CultureInfo.CurrentCulture = ci;
        CultureInfo.CurrentUICulture = ci;

        CultureInfo.DefaultThreadCurrentCulture = ci;
        CultureInfo.DefaultThreadCurrentUICulture = ci;

        if (persist)
        {
            var p = await _prefs.GetAsync(ct);
            p.Culture = culture;
            await _prefs.SetAsync(p, ct);
        }

        OnChanged?.Invoke();
    }
}
