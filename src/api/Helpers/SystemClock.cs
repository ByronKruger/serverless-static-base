using Coffeeg.Api.Interfaces;

namespace Coffeeg.Api.Helpers;

/// <summary>Wall-clock implementation of <see cref="IClock"/>.</summary>
public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
