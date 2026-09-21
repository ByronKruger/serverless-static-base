namespace Coffeeg.Api.Interfaces;

/// <summary>
/// The caller behind the current request. Services depend on this rather than on
/// <c>HttpContext</c>, which keeps the application layer ASP.NET-free.
/// </summary>
public interface ICurrentUser
{
    /// <summary>
    /// The caller's surrogate key, or <c>null</c> when the request is anonymous or the
    /// token's id claim is not an integer.
    ///
    /// Deliberately an <c>int?</c> rather than the raw claim string: the key is what
    /// every table joins on, so parsing it once in the adapter is what stops each service
    /// growing its own copy of the parse.
    /// </summary>
    int? UserKey { get; }

    /// <summary>Whether the request carries an authenticated identity.</summary>
    bool IsAuthenticated { get; }

    /// <summary>Whether the caller holds the given role (see <see cref="Authorization.Roles"/>).</summary>
    bool IsInRole(string role);
}
