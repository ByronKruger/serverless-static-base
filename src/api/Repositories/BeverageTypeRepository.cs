using Coffeeg.Api.Data;
using Coffeeg.Api.Entities;
using Coffeeg.Api.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Coffeeg.Api.Repositories;

/// <summary>
/// <see cref="IBeverageTypeRepository"/> over <see cref="CoffeegDbContext"/>. Both reads
/// are no-tracking: the catalogue is reference data that this slice never mutates.
/// </summary>
public sealed class BeverageTypeRepository(CoffeegDbContext dbContext) : IBeverageTypeRepository
{
    public async Task<IReadOnlyCollection<BeverageType>> ListAsync(CancellationToken cancellationToken) =>
        await dbContext.BeverageTypes
            .AsNoTracking()
            .OrderBy(type => type.Name)
            .ToListAsync(cancellationToken);

    public async Task<BeverageType?> FindWithIngredientsAsync(
        int beverageTypeId,
        CancellationToken cancellationToken) =>
        await dbContext.BeverageTypes
            .AsNoTracking()
            // The service needs the variants and both levels of unit to validate a
            // choice, so the whole composition graph is loaded in one go rather than
            // lazily - the port returns entities, never an IQueryable to finish off.
            .Include(type => type.Ingredients.OrderBy(composition => composition.SortOrder))
                .ThenInclude(composition => composition.Ingredient)
                    .ThenInclude(ingredient => ingredient.UnitType)
            .Include(type => type.Ingredients)
                .ThenInclude(composition => composition.Ingredient)
                    .ThenInclude(ingredient => ingredient.Variants.OrderBy(variant => variant.Name))
                        .ThenInclude(variant => variant.UnitType)
            .SingleOrDefaultAsync(type => type.Id == beverageTypeId, cancellationToken);
}
