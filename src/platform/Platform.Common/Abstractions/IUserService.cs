using Platform.Common.Contracts;
using Platform.Common.Results;

namespace Platform.Common.Abstractions;

/// <summary>
/// Finding other people. Open to any signed-in caller, and no response exposes an email.
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
}
