namespace JpkVat7.LibClient.Web.Wasm.Services.TopBar;

public interface ITopBarService
{
    TopBarState State { get; }
    event Action? OnChanged;

    void SetTitle(string title, string? subtitle = null);
    void Reset();
}
