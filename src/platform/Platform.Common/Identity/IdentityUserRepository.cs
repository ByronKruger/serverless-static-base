using Platform.Common.Abstractions;
using Platform.Common.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Platform.Common.Identity;

/// <summary>
/// ASP.NET Identity behind <see cref="IUserRepository"/>. This is the only class that
/// knows <c>UserManager</c> exists.
///
/// <c>UserManager</c>'s API predates cancellation tokens on most members, so the token
/// is honoured at the call boundary rather than threaded into the store.
/// </summary>
public sealed class IdentityUserRepository(UserManager<AppUser> userManager) : IUserRepository
{
    public async Task<AppUser?> FindByIdAsync(int userId, CancellationToken cancellationToken) =>
        await userManager.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(user => user.Id == userId, cancellationToken);

    public async Task<AppUser?> FindByEmailAsync(string email, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await userManager.FindByEmailAsync(email);
    }

    public async Task<AppUser?> FindByUserNameAsync(string userName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await userManager.FindByNameAsync(userName);
    }

    public async Task<Result<AppUser>> CreateAsync(
        AppUser user,
        string password,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = await userManager.CreateAsync(user, password);

        return result.Succeeded
            ? Result.Success(user)
            : Result.Failure<AppUser>(ToError(result));
    }

    public async Task<bool> CheckPasswordAsync(AppUser user, string password, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await userManager.CheckPasswordAsync(user, password);
    }

    public async Task<IReadOnlyCollection<string>> GetRolesAsync(AppUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return (IReadOnlyCollection<string>)await userManager.GetRolesAsync(user);
    }

    public async Task<Result> AddToRoleAsync(AppUser user, string role, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = await userManager.AddToRoleAsync(user, role);

        return result.Succeeded
            ? Result.Success()
            : Result.Failure(ToError(result));
    }

    /// <summary>
    /// Turns Identity's error list into one validation failure. Identity reports
    /// password-policy problems per rule, so they are grouped under the field the user
    /// can actually fix.
    /// </summary>
    private static Error ToError(IdentityResult result)
    {
        var messages = result.Errors.Select(error => error.Description).ToArray();

        var field = result.Errors.Any(error => error.Code.StartsWith("Password", StringComparison.Ordinal))
            ? nameof(Contracts.RegisterRequest.Password)
            : string.Empty;

        return Error.Validation(new Dictionary<string, string[]> { [field] = messages });
    }
}
