using JpkVat7.Core.Abstractions.Result;
using JpkVat7.Core.Domain;
using JpkVat7.Core.Services.Input;
using JpkVat7.Core.Services.Mapping;
using JpkVat7.Input.Abstractions;
using JpkVat7.Input.Parsing;

namespace JpkVat7.Input.Loaders;

public sealed class FileInputLoader : IInputLoader
{
    private readonly IEnumerable<IWorkbookReader> _readers;
    private readonly IInputDetector _detector;
    private readonly SectionedFileParser _sectioned;
    private readonly SingleHeaderFileParser _singleHeader;
    private readonly ISectionMapper _mapper;

    public FileInputLoader(
        IEnumerable<IWorkbookReader> readers,
        IInputDetector detector,
        SectionedFileParser sectioned,
        SingleHeaderFileParser singleHeader,
        ISectionMapper mapper)
    {
        _readers = readers;
        _detector = detector;
        _sectioned = sectioned;
        _singleHeader = singleHeader;
        _mapper = mapper;
    }

    public async Task<Result<JpkInputBundle>> LoadAsync(string path, CancellationToken ct)
    {
        if (!File.Exists(path))
            return Result.Fail<JpkInputBundle>(new Error("input.not_found", "Input file not found"));

        var reader = _readers.FirstOrDefault(r => r.CanRead(path));
        if (reader == null)
            return Result.Fail<JpkInputBundle>(new Error("input.unsupported", "Only .csv or .xlsx supported"));

        var tRes = await reader.ReadAsync(path, ct);
        if (!tRes.IsSuccess) return Result.Fail<JpkInputBundle>(tRes.Error);

        var table = tRes.Value;

        // Detect mode
        if (table.Rows.Count == 0)
            return Result.Fail<JpkInputBundle>(new Error("input.empty", "Input file is empty"));

        ParsedSections parsed;
        if (_detector.LooksLikeSectionedFile(table.Rows))
        {
            var p = _sectioned.Parse(table);
            if (!p.IsSuccess) return Result.Fail<JpkInputBundle>(p.Error);
            parsed = p.Value;
        }
        else
        {
            var header = table.Rows[0];
            if (!_detector.LooksLikeSingleHeaderFile(header))
                return Result.Fail<JpkInputBundle>(new Error("input.unknown_format",
                    "Unknown file format. Use sectioned format or Section.Field header format."));

            var p = _singleHeader.Parse(table);
            if (!p.IsSuccess) return Result.Fail<JpkInputBundle>(p.Error);
            parsed = p.Value;
        }

        return _mapper.MapToBundle(parsed.Sections);
    }
}
