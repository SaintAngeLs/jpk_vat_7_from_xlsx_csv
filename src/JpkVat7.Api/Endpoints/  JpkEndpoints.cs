using JpkVat7.Core.Services.Generation;
using JpkVat7.Core.Services.Input;
using JpkVat7.Input.Abstractions;
using JpkVat7.Xml.Abstractions;

namespace JpkVat7.Api.Endpoints;

public static class JpkEndpoints
{
    public static void MapJpk(this IEndpointRouteBuilder app)
    {
        // 1) Generate from uploaded file (.csv/.xlsx)
        app.MapPost("/api/jpk/generate/file", async (
            IFormFile file,
            IInputLoader fileLoader,
            IJpkGenerator generator,
            IJpkXmlValidator validator,
            CancellationToken ct) =>
        {
            if (file.Length == 0) return Results.BadRequest("Empty file.");

            var ext = Path.GetExtension(file.FileName);
            var tmp = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}{ext}");

            await using (var fs = File.Create(tmp))
                await file.CopyToAsync(fs, ct);

            var bundleRes = await fileLoader.LoadAsync(tmp, ct);
            File.Delete(tmp);

            if (!bundleRes.IsSuccess)
                return Results.BadRequest(bundleRes.Error);

            var xmlRes = generator.GenerateXml(bundleRes.Value);
            if (!xmlRes.IsSuccess)
                return Results.Problem(xmlRes.Error.Message);

            var valRes = validator.Validate(xmlRes.Value);
            if (!valRes.IsSuccess)
                return Results.BadRequest(new { error = valRes.Error, xml = xmlRes.Value });

            return Results.Text(xmlRes.Value, "application/xml");
        });

        // 2) Generate from directory path on server (your kali box)
        app.MapPost("/api/jpk/generate/directory", async (
            string path,
            IInputLoader directoryLoader,
            IJpkGenerator generator,
            IJpkXmlValidator validator,
            CancellationToken ct) =>
        {
            var bundleRes = await directoryLoader.LoadAsync(path, ct);
            if (!bundleRes.IsSuccess)
                return Results.BadRequest(bundleRes.Error);

            var xmlRes = generator.GenerateXml(bundleRes.Value);
            if (!xmlRes.IsSuccess)
                return Results.Problem(xmlRes.Error.Message);

            var valRes = validator.Validate(xmlRes.Value);
            if (!valRes.IsSuccess)
                return Results.BadRequest(new { error = valRes.Error, xml = xmlRes.Value });

            return Results.Text(xmlRes.Value, "application/xml");
        });
    }
}
