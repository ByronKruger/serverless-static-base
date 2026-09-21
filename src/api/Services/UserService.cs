using System.Globalization;
using AutoMapper;
using Coffeeg.Api.Dtos;
using Coffeeg.Api.Interfaces;
using Coffeeg.Api.Interfaces.Repositories;
using Coffeeg.Api.Interfaces.Services;
using Coffeeg.Api.Results;

namespace Coffeeg.Api.Services;

/// <inheritdoc cref="IUserService"/>
public sealed class UserService(
    IUserRepository users,
    IBeveragePreferenceRepository preferences,
    ICurrentUser currentUser,
    IMapper mapper) : IUserService
{
    /// <summary>
    /// One caller, so it stays a private member here rather than joining
    /// <see cref="BeverageErrors"/> - which exists because each of its codes has two
    /// callers. <c>AuthService</c> sets the same precedent.
    /// </summary>
    private static Error NotFound(int userId) =>
        Error.NotFound(
            "user.not_found",
            $"There is no person with id {userId.ToString(CultureInfo.InvariantCulture)}.");

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

    public async Task<Result<UserProfileResponse>> GetProfileAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return NotFound(userId);
        }

        // The existing owner-scoped query, handed the subject's id instead of the
        // caller's. Every method on the port still takes the user it reads for, so the
        // invariant that a query cannot forget the user holds - what changed is who is
        // allowed to ask (D-0017).
        var theirs = await preferences.ListForUserAsync(userId, cancellationToken);

        // Composed here rather than through a mapper context key like UserProfile.RolesKey:
        // the preferences are entities needing their own map, and nesting that inside a
        // member mapping reads worse than one line of composition.
        var summary = mapper.Map<UserSummaryResponse>(user);
        var saved = mapper.Map<IReadOnlyCollection<BeveragePreferenceResponse>>(theirs);

        return new UserProfileResponse(
            summary.Id,
            summary.UserName,
            summary.FirstName,
            summary.LastName,
            user.CreatedAtUtc,
            saved);
    }
}
