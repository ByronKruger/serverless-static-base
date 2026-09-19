using Coffeeg.Api.Interfaces;

namespace Coffeeg.Application.Tests.Fakes;

/// <summary>In-memory <see cref="ICurrentUser"/> so ownership rules can be tested directly.</summary>
public sealed class FakeCurrentUser(string? userId = "test-user", params string[] roles) : ICurrentUser
{
    private readonly HashSet<string> _roles = new(roles, StringComparer.OrdinalIgnoreCase);

    public string? UserId { get; } = userId;

    public bool IsAuthenticated => UserId is not null;

    public bool IsInRole(string role) => _roles.Contains(role);

    public static FakeCurrentUser Anonymous() => new(null);
}
