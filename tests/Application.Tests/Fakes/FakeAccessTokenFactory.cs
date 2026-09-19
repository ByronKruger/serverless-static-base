using Coffeeg.Api.Entities;
using Coffeeg.Api.Interfaces;

namespace Coffeeg.Application.Tests.Fakes;

/// <summary>
/// Returns a predictable token instead of signing one, so service tests never need a
/// signing key. What matters here is who the token was asked for, not its bytes.
/// </summary>
public sealed class FakeAccessTokenFactory(DateTimeOffset? expiresAt = null) : IAccessTokenFactory
{
    private readonly DateTimeOffset _expiresAt =
        expiresAt ?? new DateTimeOffset(2026, 1, 1, 1, 0, 0, TimeSpan.Zero);

    /// <summary>The user the last token was issued for, or <c>null</c> if none was.</summary>
    public AppUser? IssuedFor { get; private set; }

    /// <summary>The roles baked into the last issued token.</summary>
    public IReadOnlyCollection<string> IssuedRoles { get; private set; } = [];

    public AccessToken Create(AppUser user, IReadOnlyCollection<string> roles)
    {
        IssuedFor = user;
        IssuedRoles = roles;

        return new AccessToken($"token-for-{user.Id}", _expiresAt);
    }
}
