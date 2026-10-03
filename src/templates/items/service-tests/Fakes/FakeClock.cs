using HopeTech.Common.Abstractions;

namespace HopeTech.Api.Tests.Fakes;

/// <summary>
/// Deterministic <see cref="IClock"/> for service tests. Prefer a hand-written fake over
/// a substitute for a port this small - it reads better and it fails louder.
/// </summary>
public sealed class FakeClock(DateTimeOffset? utcNow = null) : IClock
{
    public DateTimeOffset UtcNow { get; private set; } =
        utcNow ?? new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public void Advance(TimeSpan by) => UtcNow = UtcNow.Add(by);
}
