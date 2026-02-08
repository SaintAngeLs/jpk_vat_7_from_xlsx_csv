namespace JpkVat7.LibClient.Web.Wasm.Services.LocalStorage;

public interface ILocalStorageServiceWrapper
{
    ValueTask SetAsync<T>(string key, T value, CancellationToken ct = default);
    ValueTask<T?> GetAsync<T>(string key, CancellationToken ct = default);
    ValueTask RemoveAsync(string key, CancellationToken ct = default);
    ValueTask<bool> ContainsKeyAsync(string key, CancellationToken ct = default);
}
