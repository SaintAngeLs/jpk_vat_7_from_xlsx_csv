using Blazored.LocalStorage;

namespace JpkVat7.LibClient.Web.Wasm.Services.LocalStorage;

public sealed class LocalStorageServiceWrapper : ILocalStorageServiceWrapper
{
    private readonly ILocalStorageService _storage;

    public LocalStorageServiceWrapper(ILocalStorageService storage)
        => _storage = storage;

    public ValueTask SetAsync<T>(string key, T value, CancellationToken ct = default)
        => _storage.SetItemAsync(key, value, ct);

    public async ValueTask<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        if (!await _storage.ContainKeyAsync(key, ct))
            return default;

        return await _storage.GetItemAsync<T>(key, ct);
    }

    public ValueTask RemoveAsync(string key, CancellationToken ct = default)
        => _storage.RemoveItemAsync(key, ct);

    public ValueTask<bool> ContainsKeyAsync(string key, CancellationToken ct = default)
        => _storage.ContainKeyAsync(key, ct);
}
