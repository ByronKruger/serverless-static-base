using Coffeeg.Api.Dtos;
using Coffeeg.Api.Results;

namespace Coffeeg.Api.Interfaces.Services;

/// <summary>
/// Finding other people, and reading one of them. Both are open to any signed-in caller
/// (D-0017); neither exposes an email.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Autocomplete matches for a term, best match first. The caller is left out. An
    /// empty result is a success, not a miss.
    /// </summary>
    Task<Result<IReadOnlyCollection<UserSummaryResponse>>> SearchAsync(
        UserSearchRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// One person with their saved preferences. <c>user.not_found</c> when no such
    /// account exists.
    /// </summary>
    Task<Result<UserProfileResponse>> GetProfileAsync(int userId, CancellationToken cancellationToken);
}
