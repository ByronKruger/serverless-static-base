using Coffeeg.Api.Data;
using Coffeeg.Api.Entities;
using Coffeeg.Api.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Coffeeg.Api.Repositories;

/// <summary>
/// <see cref="IBeveragePreferenceRepository"/> over <see cref="CoffeegDbContext"/>.
/// Every query filters on the user id it was given, so a preference belonging to
/// somebody else is simply not found.
/// </summary>
public sealed class BeveragePreferenceRepository(CoffeegDbContext dbContext) : IBeveragePreferenceRepository
{
    public async Task<bool> ExistsForUserAsync(
        int userId,
        int beverageTypeId,
        CancellationToken cancellationToken) =>
        await dbContext.BeveragePreferences
            .AsNoTracking()
            .AnyAsync(
                preference => preference.UserId == userId && preference.BeverageTypeId == beverageTypeId,
                cancellationToken);

    public async Task<IReadOnlyCollection<BeveragePreference>> ListForUserAsync(
        int userId,
        CancellationToken cancellationToken) =>
        await Readable()
            .Where(preference => preference.UserId == userId)
            .OrderByDescending(preference => preference.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<BeveragePreference?> FindForUserAsync(
        int userId,
        int preferenceId,
        CancellationToken cancellationToken) =>
        await Readable()
            .SingleOrDefaultAsync(
                preference => preference.Id == preferenceId && preference.UserId == userId,
                cancellationToken);

    public async Task<BeveragePreference> AddAsync(
        BeveragePreference preference,
        CancellationToken cancellationToken)
    {
        dbContext.BeveragePreferences.Add(preference);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Saving populates the keys but not the navigations the response is mapped from,
        // so the row is read back through the same projection every other read uses.
        return await FindForUserAsync(preference.UserId, preference.Id, cancellationToken)
            ?? throw new InvalidOperationException(
                $"Preference {preference.Id} was saved but could not be read back.");
    }

    /// <summary>
    /// The read shape shared by every query: the beverage type for the header, and each
    /// line's ingredient and unit so an amount can be rendered with its meaning.
    /// </summary>
    private IQueryable<BeveragePreference> Readable() =>
        dbContext.BeveragePreferences
            .AsNoTracking()
            .Include(preference => preference.BeverageType)
            .Include(preference => preference.Ingredients)
                .ThenInclude(line => line.Ingredient)
                    .ThenInclude(ingredient => ingredient.UnitType);
}
