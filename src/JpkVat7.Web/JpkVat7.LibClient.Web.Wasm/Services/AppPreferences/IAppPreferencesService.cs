namespace JpkVat7.LibClient.Web.Wasm.Services.Preferences;

public interface IAppPreferencesService
{
    ValueTask<AppPreferences> GetAsync(CancellationToken ct = default);
    ValueTask SetAsync(AppPreferences prefs, CancellationToken ct = default);
}
