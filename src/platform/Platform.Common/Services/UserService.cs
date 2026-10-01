using AutoMapper;
using Platform.Common.Contracts;
using Platform.Common.Abstractions;
using Platform.Common.Results;

namespace Platform.Common.Services;

/// <inheritdoc cref="IUserService"/>
public sealed class UserService(
    IUserRepository users,
    ICurrentUser currentUser,
    IMapper mapper) : IUserService
{
    /// <summary>
    /// Nobody's key, so excluding it excludes nobody. Used when the caller cannot be
    /// identified, which the default-deny pipeline means cannot actually happen: leaving
    /// yourself in your own results is a cosmetic flaw, not a leak, so it does not earn
    /// the <c>Result</c> ceremony that ownership rules do.
    /// </summary>
    private const int NoUser = 0;

    public async Task<Result<IReadOnlyCollection<UserSummaryResponse>>> SearchAsync(
        UserSearchRequest request,
        CancellationToken cancellationToken)
    {
        // The service owns what case-insensitive means; the repository only compares.
        // Invariant rather than current-culture: it has to agree with the upper-casing
        // Identity applied when it wrote NormalizedUserName.
        var term = request.Term.ToUpperInvariant();

        var matches = await users.SearchAsync(
            term,
            request.Limit ?? UserSearchRequest.DefaultLimit,
            currentUser.UserKey ?? NoUser,
            cancellationToken);

        return Result.Success(mapper.Map<IReadOnlyCollection<UserSummaryResponse>>(matches));
    }
}
