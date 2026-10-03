using HopeTech.Common.Abstractions;

namespace HopeTech.Common.Time;

/// <summary>Wall-clock implementation of <see cref="IClock"/>.</summary>
public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
