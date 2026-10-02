using Platform.Common.Identity;
using Platform.Common.Results;

namespace Platform.Common.Abstractions;

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
