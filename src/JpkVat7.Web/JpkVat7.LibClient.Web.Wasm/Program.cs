using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using MudBlazor.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Localization;
using Blazored.LocalStorage;

using JpkVat7.LibClient.Web.Wasm;
using JpkVat7.LibClient.Web.Wasm.Services;
using JpkVat7.LibClient.Web.Wasm.Services.LocalStorage;
using JpkVat7.LibClient.Web.Wasm.Services.Preferences;
using JpkVat7.LibClient.Web.Wasm.Services.Language;
using JpkVat7.Grpc;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");

// MudBlazor
builder.Services.AddMudServices();

// Localization
builder.Services.AddLocalization();

// LocalStorage
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<ILocalStorageServiceWrapper, LocalStorageServiceWrapper>();
builder.Services.AddScoped<IAppPreferencesService, AppPreferencesService>();
builder.Services.AddScoped<ILanguageService, LanguageService>();

// gRPC-Web
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress;

builder.Services.AddSingleton(sp =>
{
    var handler = new GrpcWebHandler(GrpcWebMode.GrpcWebText, new HttpClientHandler());
    var channel = GrpcChannel.ForAddress(apiBaseUrl, new GrpcChannelOptions { HttpHandler = handler });
    return new JpkService.JpkServiceClient(channel);
});

builder.Services.AddSingleton<JpkGrpcUploadClient>();

await builder.Build().RunAsync();
