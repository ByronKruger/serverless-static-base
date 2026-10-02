using Microsoft.AspNetCore.Identity;

namespace Platform.Common.Identity;

/// <summary>
/// A registered person. Backed by ASP.NET Identity, keyed by an <see cref="int"/>
/// surrogate rather than Identity's default <see cref="string"/> GUID.
///
/// Email is the natural key: it is what a person registers with, it is unique, and it
/// is the identity the business reasons about. It is not the primary key - Identity's
/// related tables (roles, claims, logins, tokens) are all generic over a single key
/// column, so the surrogate <c>Id</c> carries those relationships instead. Email
/// uniqueness is enforced by a unique index in <see cref="AppUserConfiguration"/>.
/// </summary>
public class AppUser : IdentityUser<int>
{
    /// <summary>When the account was registered. Always UTC.</summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>
    /// Given name.
    ///
    /// Nullable in the database but required by
    /// <see cref="Validation.RegisterRequestValidator"/>, so every account
    /// registered from this slice onwards has one and every account registered before it
    /// does not (D-0007). Code that reads a name must handle its absence; it is not dead
    /// defensiveness.
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>Family name. Nullable on the same terms as <see cref="FirstName"/>.</summary>
    public string? LastName { get; set; }
}
