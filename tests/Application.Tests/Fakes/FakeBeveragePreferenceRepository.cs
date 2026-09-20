using Coffeeg.Api.Entities;
using Coffeeg.Api.Interfaces.Repositories;

namespace Coffeeg.Application.Tests.Fakes;

/// <summary>
/// In-memory <see cref="IBeveragePreferenceRepository"/>.
///
/// <see cref="AddAsync"/> resolves the navigations the same way the real repository does
/// by re-reading the row: without that the mapper would see a null beverage type and the
/// test would pass against a shape the database never produces.
/// </summary>
public sealed class FakeBeveragePreferenceRepository : IBeveragePreferenceRepository
{
    private readonly List<BeverageType> _catalogue = BeverageCatalogue.Build();
    private readonly List<BeveragePreference> _preferences = [];

    private int _nextId = 1;

    /// <summary>Everything saved, for assertions.</summary>
    public IReadOnlyList<BeveragePreference> Saved => _preferences;

    /// <summary>Puts an existing preference in place, e.g. to provoke the duplicate conflict.</summary>
    public BeveragePreference Seed(int userId, int beverageTypeId, params (int IngredientId, decimal? Amount)[] lines)
    {
        var preference = new BeveragePreference
        {
            Id = _nextId++,
            UserId = userId,
            BeverageTypeId = beverageTypeId,
            CreatedAtUtc = DateTimeOffset.UnixEpoch,
            Ingredients = [.. lines.Select(line => new BeveragePreferenceIngredient
            {
                IngredientId = line.IngredientId,
                Amount = line.Amount
            })]
        };

        Attach(preference);
        _preferences.Add(preference);

        return preference;
    }

    public Task<bool> ExistsForUserAsync(int userId, int beverageTypeId, CancellationToken cancellationToken) =>
        Task.FromResult(_preferences.Any(preference =>
            preference.UserId == userId && preference.BeverageTypeId == beverageTypeId));

    public Task<IReadOnlyCollection<BeveragePreference>> ListForUserAsync(
        int userId,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyCollection<BeveragePreference>>(_preferences
            .Where(preference => preference.UserId == userId)
            .OrderByDescending(preference => preference.CreatedAtUtc)
            .ToList());

    public Task<BeveragePreference?> FindForUserAsync(
        int userId,
        int preferenceId,
        CancellationToken cancellationToken) =>
        Task.FromResult(_preferences.SingleOrDefault(preference =>
            preference.Id == preferenceId && preference.UserId == userId));

    public Task<BeveragePreference> AddAsync(BeveragePreference preference, CancellationToken cancellationToken)
    {
        preference.Id = _nextId++;

        Attach(preference);
        _preferences.Add(preference);

        return Task.FromResult(preference);
    }

    /// <summary>Stands in for the includes the real repository re-reads the row with.</summary>
    private void Attach(BeveragePreference preference)
    {
        preference.BeverageType = _catalogue.Single(type => type.Id == preference.BeverageTypeId);

        foreach (var line in preference.Ingredients)
        {
            line.Ingredient = FindIngredient(line.IngredientId);
        }
    }

    private Ingredient FindIngredient(int ingredientId) => _catalogue
        .SelectMany(type => type.Ingredients.Select(composition => composition.Ingredient))
        .SelectMany(ingredient => ingredient.Variants.Append(ingredient))
        .First(ingredient => ingredient.Id == ingredientId);
}
