namespace Coffeeg.Api.Interfaces;

/// <summary>
/// The caller behind the current request. Services depend on this rather than on
/// <c>HttpContext</c>, which keeps the application layer ASP.NET-free.
/// </summary>
public interface ICurrentUser
{
    /// <summary>Stable user id from the token, or <c>null</c> for an anonymous request.</summary>
    string? UserId { get; }

    /// <summary>Whether the request carries an authenticated identity.</summary>
    bool IsAuthenticated { get; }

    /// <summary>Whether the caller holds the given role (see <see cref="Authorization.Roles"/>).</summary>
    bool IsInRole(string role);
}
