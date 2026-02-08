using System.Globalization;
using Blazored.LocalStorage;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using JpkVat7.LibClient.Web.Wasm;
using JpkVat7.LibClient.Web.Wasm.Services;
using JpkVat7.LibClient.Web.Wasm.Services.Language;
using JpkVat7.LibClient.Web.Wasm.Services.LocalStorage;
using JpkVat7.LibClient.Web.Wasm.Services.Preferences;
using JpkVat7.LibClient.Web.Wasm.Services.TopBar;
using JpkVat7.Grpc;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");

// MudBlazor
builder.Services.AddMudServices();

// Localization resources path
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

// Local storage + prefs + language
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<ILocalStorageServiceWrapper, LocalStorageServiceWrapper>();
builder.Services.AddScoped<IAppPreferencesService, AppPreferencesService>();
builder.Services.AddScoped<ILanguageService, LanguageService>();

// TopBar
builder.Services.AddScoped<ITopBarService, TopBarService>();

// gRPC-Web
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress;

builder.Services.AddSingleton(sp =>
{
    var handler = new GrpcWebHandler(GrpcWebMode.GrpcWebText, new HttpClientHandler());
    var channel = GrpcChannel.ForAddress(apiBaseUrl, new GrpcChannelOptions
    {
        HttpHandler = handler
    });
    return new JpkService.JpkServiceClient(channel);
});

builder.Services.AddSingleton<JpkGrpcUploadClient>();

// ✅ Build host first
var host = builder.Build();

// ✅ Apply culture BEFORE first render
var langService = host.Services.GetRequiredService<ILanguageService>();
await langService.InitializeAsync();

var cultureName = langService.CurrentCulture;
var culture = new CultureInfo(cultureName);

CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;
CultureInfo.CurrentCulture = culture;
CultureInfo.CurrentUICulture = culture;

await host.RunAsync();
