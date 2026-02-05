namespace JpkVat7.Core.Abstractions;

public sealed class  Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public IReadOnlyList<Error> Errors { get; }

    private Result(bool ok, T? value, IReadOnlyList<Error> errors)
    {
        IsSuccess = ok;
        Value = value;
        Errors = errors;
    }

    public static Result<T> ok (T value) => new(true, value, Array.Empty<Error>());
    
    oubl
}
