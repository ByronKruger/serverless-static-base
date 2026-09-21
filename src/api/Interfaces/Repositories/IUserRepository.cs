using Coffeeg.Api.Entities;
using Coffeeg.Api.Results;

namespace Coffeeg.Api.Interfaces.Repositories;

/// <summary>
/// User persistence, as a port. ASP.NET Identity's <c>UserManager</c> is the adapter
/// behind it, which keeps <c>AuthService</c> testable with a plain
/// fake instead of a mocked <c>IUserStore</c>.
/// </summary>
public interface IUserRepository
{
    /// <summary>Finds a user by their surrogate key. <c>null</c> when no such user exists.</summary>
    Task<AppUser?> FindByIdAsync(int userId, CancellationToken cancellationToken);

    /// <summary>Finds a user by email. Matching is case-insensitive.</summary>
    Task<AppUser?> FindByEmailAsync(string email, CancellationToken cancellationToken);

    /// <summary>
    /// Users whose username, first name or surname contains the term, best match first:
    /// a username starting with it, then the rest, each alphabetical by username.
    /// </summary>
    /// <param name="upperCasedTerm">
    /// The search term, <b>already upper-cased by the caller</b>. The service owns the
    /// casing so that only one layer decides what case-insensitive means; this method
    /// only compares. Passing mixed case here matches nothing.
    /// </param>
    /// <param name="limit">Maximum rows to return.</param>
    /// <param name="excludeUserId">
    /// A user to leave out - the caller, so a search for other people does not return
    /// yourself. Pass an id no user has to exclude nobody.
    /// </param>
    /// <param name="cancellationToken">Cancels the query.</param>
    Task<IReadOnlyCollection<AppUser>> SearchAsync(
        string upperCasedTerm,
        int limit,
        int excludeUserId,
        CancellationToken cancellationToken);

    /// <summary>Finds a user by username. Matching is case-insensitive.</summary>
    Task<AppUser?> FindByUserNameAsync(string userName, CancellationToken cancellationToken);

    /// <summary>
    /// Creates the user with a hashed password. Returns a validation failure when the
    /// password does not satisfy the configured policy.
    /// </summary>
    Task<Result<AppUser>> CreateAsync(AppUser user, string password, CancellationToken cancellationToken);

    /// <summary>Verifies a plain-text password against the stored hash.</summary>
    Task<bool> CheckPasswordAsync(AppUser user, string password, CancellationToken cancellationToken);

    /// <summary>The user's role names.</summary>
    Task<IReadOnlyCollection<string>> GetRolesAsync(AppUser user, CancellationToken cancellationToken);

    /// <summary>Grants a role. The role must already exist.</summary>
    Task<Result> AddToRoleAsync(AppUser user, string role, CancellationToken cancellationToken);
}
