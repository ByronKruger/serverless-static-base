using Coffeeg.Api.Entities;

namespace Coffeeg.Api.Interfaces;

/// <summary>
/// Mints signed access tokens. A port so the service layer can assert on what it asked
/// for without a real signing key.
/// </summary>
public interface IAccessTokenFactory
{
    /// <summary>Issues a token for the user, carrying their roles.</summary>
    AccessToken Create(AppUser user, IReadOnlyCollection<string> roles);
}

/// <summary>A signed token and the moment it stops being valid.</summary>
/// <param name="Value">The encoded JWT.</param>
/// <param name="ExpiresAtUtc">Expiry, UTC.</param>
public sealed record AccessToken(string Value, DateTimeOffset ExpiresAtUtc);
