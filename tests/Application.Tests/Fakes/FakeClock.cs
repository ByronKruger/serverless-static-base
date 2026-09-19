using Coffeeg.Api.Interfaces;

namespace Coffeeg.Application.Tests.Fakes;

/// <summary>
/// Deterministic <see cref="IClock"/> for service tests. Service tests use fake
/// ports only - no ASP.NET, no DbContext, no database.
/// </summary>
public sealed class FakeClock(DateTimeOffset? utcNow = null) : IClock
{
    public DateTimeOffset UtcNow { get; private set; } =
        utcNow ?? new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public void Advance(TimeSpan by) => UtcNow = UtcNow.Add(by);
}
