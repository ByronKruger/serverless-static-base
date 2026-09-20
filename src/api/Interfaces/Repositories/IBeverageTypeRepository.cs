using Coffeeg.Api.Entities;

namespace Coffeeg.Api.Interfaces.Repositories;

/// <summary>
/// Read access to the beverage catalogue. Read-only on purpose: moderators own these
/// rows (Docs/domain/user-permissions.md), and nothing in this slice writes them.
/// </summary>
public interface IBeverageTypeRepository
{
    /// <summary>Every beverage type, by name. Ingredients are not loaded.</summary>
    Task<IReadOnlyCollection<BeverageType>> ListAsync(CancellationToken cancellationToken);

    /// <summary>
    /// One beverage type with the whole graph a preference is validated against: its
    /// ingredients, each ingredient's variants, and the unit each of them is measured in.
    /// Returns <c>null</c> when there is no such type.
    /// </summary>
    Task<BeverageType?> FindWithIngredientsAsync(int beverageTypeId, CancellationToken cancellationToken);
}
