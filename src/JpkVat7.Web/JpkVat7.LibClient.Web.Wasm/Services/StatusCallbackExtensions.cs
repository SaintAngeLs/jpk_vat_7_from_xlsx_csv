namespace JpkVat7.LibClient.Web.Wasm.Services;

internal static class StatusCallbackExtensions
{
    public static Task SafeInvokeAsync(this Func<string, Task>? cb, string message)
        => cb is null ? Task.CompletedTask : cb(message);
}
