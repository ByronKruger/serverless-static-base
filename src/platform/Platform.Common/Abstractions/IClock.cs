namespace Platform.Common.Abstractions;

/// <summary>
/// Time as a port, so services stay testable with a fake clock instead of
/// <c>DateTimeOffset.UtcNow</c>.
/// </summary>
public interface IClock
{
    /// <summary>The current instant, always UTC.</summary>
    DateTimeOffset UtcNow { get; }
}
