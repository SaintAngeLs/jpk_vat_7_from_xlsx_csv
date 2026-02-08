namespace JpkVat7.LibClient.Web.Wasm.Services.TopBar;

public sealed class TopBarService : ITopBarService
{
    public TopBarState State { get; } = new();

    public event Action? OnChanged;

    public void SetTitle(string title, string? subtitle = null)
    {
        State.Title = title;
        State.Subtitle = subtitle;
        OnChanged?.Invoke();
    }

    public void Reset()
    {
        State.Title = "JPK VAT-7";
        State.Subtitle = null;
        State.ShowMenuToggle = true;
        OnChanged?.Invoke();
    }
}
