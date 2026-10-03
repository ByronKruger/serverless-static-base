using HopeTech.Common.Abstractions;

namespace HopeTech.Api.Tests.Fakes;

/// <summary>
/// In-memory <see cref="ICurrentUser"/> so ownership rules can be tested directly.
///
/// The key is an <c>int?</c> because the port is: parsing the token's claim belongs to
/// the library's <c>HttpContextCurrentUser</c>, so "the token carried something that is
/// not a number" and "the token carried nothing" arrive here as the same absent key.
/// </summary>
public sealed class FakeCurrentUser(int? userKey = 1, params string[] roles) : ICurrentUser
{
    private readonly HashSet<string> _roles = new(roles, StringComparer.OrdinalIgnoreCase);

    public int? UserKey { get; } = userKey;

    public bool IsAuthenticated => UserKey is not null;

    public bool IsInRole(string role) => _roles.Contains(role);

    public static FakeCurrentUser Anonymous() => new(null);
}
