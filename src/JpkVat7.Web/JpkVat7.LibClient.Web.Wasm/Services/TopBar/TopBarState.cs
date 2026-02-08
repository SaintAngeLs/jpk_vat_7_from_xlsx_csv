namespace JpkVat7.LibClient.Web.Wasm.Services.TopBar;

public sealed class TopBarState
{
    public string Title { get; set; } = "JPK VAT-7";
    public string? Subtitle { get; set; } = null;

    // if you want to show/hide burger button etc.
    public bool ShowMenuToggle { get; set; } = true;
}
