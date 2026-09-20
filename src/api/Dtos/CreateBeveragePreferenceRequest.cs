namespace Coffeeg.Api.Dtos;

/// <summary>Body of <c>POST /api/beverage-preferences</c>.</summary>
/// <param name="BeverageTypeId">
/// Which kind of drink this preference is for. Must be an existing type - a caller cannot
/// coin one (Docs/domain/user-permissions.md). The caller may hold only one preference
/// per type.
/// </param>
/// <param name="Ingredients">
/// What goes in it. An ingredient the caller does not want may be omitted, or sent with a
/// null amount; both mean the same thing.
/// </param>
public sealed record CreateBeveragePreferenceRequest(
    int BeverageTypeId,
    IReadOnlyCollection<BeveragePreferenceIngredientRequest> Ingredients);

/// <summary>One line of a new preference.</summary>
/// <param name="IngredientId">
/// A choosable ingredient: a simple one, or a variant of a complex one. It must belong to
/// the beverage type, directly or as a variant of one of its ingredients.
/// </param>
/// <param name="Amount">
/// How much, in the unit the ingredient declares - the caller does not choose a unit.
/// <c>null</c> means none of it: 0 teaspoons, <c>No</c>, 0% are the same statement. A
/// <c>Boolean</c> unit accepts only 1 or null.
/// </param>
public sealed record BeveragePreferenceIngredientRequest(int IngredientId, decimal? Amount);
