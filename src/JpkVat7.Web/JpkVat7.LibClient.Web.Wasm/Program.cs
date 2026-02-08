using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using MudBlazor.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using JpkVat7.LibClient.Web.Wasm;
using JpkVat7.LibClient.Web.Wasm.Services;
using JpkVat7.Grpc; // generated from your WASM proto

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");

// MudBlazor
builder.Services.AddMudServices();

// ----------------------------
// gRPC-Web client configuration
// ----------------------------

// Set this to where your API is running.
// If API is same origin as WASM host, you can use builder.HostEnvironment.BaseAddress.
// If API is different (typical), set it in wwwroot/appsettings.json as "ApiBaseUrl".
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

// Your upload wrapper service used by the Razor page
builder.Services.AddSingleton<JpkGrpcUploadClient>();

await builder.Build().RunAsync();
