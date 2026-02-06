using JpkVat7.Api.Endpoints;
using JpkVat7.Core.Options;
using JpkVat7.Core.Services.Generation;
using JpkVat7.Core.Services.Mapping;
using JpkVat7.Input.Abstractions;
using JpkVat7.Input.Csv;
using JpkVat7.Input.Loaders;
using JpkVat7.Input.Parsing;
using JpkVat7.Input.Xlsx;
using JpkVat7.Xml.Abstractions;
using JpkVat7.Xml.Validation;
using JpkVat7.Xml.Writing;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JpkSchemaOptions>(builder.Configuration.GetSection("JpkSchema"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Core services
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

// Two loaders:
builder.Services.AddSingleton<DirectoryInputLoader>();
builder.Services.AddSingleton<FileInputLoader>();

// Expose as Core IInputLoader via named registration in endpoints:
// (we’ll register them separately to avoid ambiguity)
builder.Services.AddSingleton<IJpkXmlWriter, JpkXmlWriter>();
builder.Services.AddSingleton<IJpkXmlValidator, JpkXsdValidator>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/health", () => Results.Ok("OK"));

// Map endpoints using the correct loader per route
app.MapPost("/api/jpk/generate/file", async (
    IFormFile file,
    FileInputLoader loader,
    IJpkGenerator generator,
    IJpkXmlValidator validator,
    CancellationToken ct) =>
{
    if (file.Length == 0) return Results.BadRequest("Empty file.");

    var ext = Path.GetExtension(file.FileName);
    var tmp = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}{ext}");

    await using (var fs = File.Create(tmp))
        await file.CopyToAsync(fs, ct);

    var bundleRes = await loader.LoadAsync(tmp, ct);
    File.Delete(tmp);

    if (!bundleRes.IsSuccess) return Results.BadRequest(bundleRes.Error);

    var xmlRes = generator.GenerateXml(bundleRes.Value);
    if (!xmlRes.IsSuccess) return Results.Problem(xmlRes.Error.Message);

    var valRes = validator.Validate(xmlRes.Value);
    if (!valRes.IsSuccess) return Results.BadRequest(new { error = valRes.Error, xml = xmlRes.Value });

    return Results.Text(xmlRes.Value, "application/xml");
});

app.MapPost("/api/jpk/generate/directory", async (
    string path,
    DirectoryInputLoader loader,
    IJpkGenerator generator,
    IJpkXmlValidator validator,
    CancellationToken ct) =>
{
    var bundleRes = await loader.LoadAsync(path, ct);
    if (!bundleRes.IsSuccess) return Results.BadRequest(bundleRes.Error);

    var xmlRes = generator.GenerateXml(bundleRes.Value);
    if (!xmlRes.IsSuccess) return Results.Problem(xmlRes.Error.Message);

    var valRes = validator.Validate(xmlRes.Value);
    if (!valRes.IsSuccess) return Results.BadRequest(new { error = valRes.Error, xml = xmlRes.Value });

    return Results.Text(xmlRes.Value, "application/xml");
});

app.Run();
