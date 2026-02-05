namespace JpkVat7.Core.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
