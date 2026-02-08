using JpkVat7.LibClient.Web.Wasm.Services.LocalStorage;

namespace JpkVat7.LibClient.Web.Wasm.Services.Preferences;

public sealed class AppPreferencesService : IAppPreferencesService
{
    private const string Key = "app:prefs";
    private readonly ILocalStorageServiceWrapper _storage;

    public AppPreferencesService(ILocalStorageServiceWrapper storage)
        => _storage = storage;

    public async ValueTask<AppPreferences> GetAsync(CancellationToken ct = default)
        => await _storage.GetAsync<AppPreferences>(Key, ct) ?? new AppPreferences();

    public ValueTask SetAsync(AppPreferences prefs, CancellationToken ct = default)
        => _storage.SetAsync(Key, prefs, ct);
}
