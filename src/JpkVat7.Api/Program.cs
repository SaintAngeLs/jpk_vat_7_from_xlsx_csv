using Grpc.AspNetCore.Web;
using JpkVat7.Api.Grpc;
using JpkVat7.Core.Abstractions;
using JpkVat7.Core.Abstractions.DateTime;
using JpkVat7.Core.Options;
using JpkVat7.Core.Services.Generation;
using JpkVat7.Core.Services.Mapping;
using JpkVat7.Input.Abstractions;
using JpkVat7.Input.Csv;
using JpkVat7.Input.Loaders;
using JpkVat7.Input.Parsing;
using JpkVat7.Input.Xlsx;
using JpkVat7.Xml.Abstractions;
using JpkVat7.Xml.Generation;
using JpkVat7.Xml.Validation;
using JpkVat7.Xml.Writing;

var builder = WebApplication.CreateBuilder(args);

// Options
builder.Services.Configure<JpkSchemaOptions>(builder.Configuration.GetSection("JpkSchema"));

// CORS (dev)
builder.Services.AddCors();

// gRPC
builder.Services.AddGrpc();

// Core services
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton<ISectionMapper, DefaultSectionMapper>();
builder.Services.AddSingleton<IJpkGenerator, JpkGenerator>();

// Input readers + parsers
builder.Services.AddSingleton<CsvWorkbookReader>();
builder.Services.AddSingleton<XlsxWorkbookReader>();
builder.Services.AddSingleton<IEnumerable<IWorkbookReader>>(sp => new IWorkbookReader[]
{
    sp.GetRequiredService<CsvWorkbookReader>(),
    sp.GetRequiredService<XlsxWorkbookReader>()
});
builder.Services.AddSingleton<IInputDetector, InputDetector>();
builder.Services.AddSingleton<SectionedFileParser>();
builder.Services.AddSingleton<SingleHeaderFileParser>();

// Loaders
builder.Services.AddSingleton<DirectoryInputLoader>();
builder.Services.AddSingleton<FileInputLoader>();

// XML
builder.Services.AddSingleton<IJpkXmlWriter, JpkXmlWriter>();
builder.Services.AddSingleton<IJpkXmlValidator, JpkXsdValidator>();

var app = builder.Build();

// If you want to redirect HTTP -> HTTPS once cert is trusted, enable this.
// app.UseHttpsRedirection();

app.MapGet("/health", () => Results.Ok("OK"));

// Dev CORS: allow browser gRPC-Web
app.UseCors(policy =>
    policy
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod()
        .WithExposedHeaders("Grpc-Status", "Grpc-Message", "Grpc-Encoding", "Grpc-Accept-Encoding")
);

// gRPC-Web middleware
app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true });

// gRPC endpoint
app.MapGrpcService<JpkGrpcService>()
   .EnableGrpcWeb();

app.Run();
