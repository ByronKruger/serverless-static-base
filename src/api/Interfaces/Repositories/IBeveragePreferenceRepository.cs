using Coffeeg.Api.Entities;

namespace Coffeeg.Api.Interfaces.Repositories;

/// <summary>
/// Preference persistence, as a port. Every method is scoped to one user: ownership is
/// a domain rule, and keeping the user id in the signature means a query that forgets it
/// cannot be written.
/// </summary>
public interface IBeveragePreferenceRepository
{
    /// <summary>Whether the user already has a preference for this beverage type.</summary>
    Task<bool> ExistsForUserAsync(int userId, int beverageTypeId, CancellationToken cancellationToken);

    /// <summary>The user's preferences, newest first, with their beverage types and lines loaded.</summary>
    Task<IReadOnlyCollection<BeveragePreference>> ListForUserAsync(int userId, CancellationToken cancellationToken);

    /// <summary>
    /// One of the user's preferences. Returns <c>null</c> when it does not exist or
    /// belongs to somebody else - the caller cannot tell the two apart, which is what
    /// stops ids being probed.
    /// </summary>
    Task<BeveragePreference?> FindForUserAsync(int userId, int preferenceId, CancellationToken cancellationToken);

    /// <summary>
    /// Saves a new preference and returns it with its beverage type, ingredients and
    /// units loaded, ready to map.
    /// </summary>
    Task<BeveragePreference> AddAsync(BeveragePreference preference, CancellationToken cancellationToken);
}
