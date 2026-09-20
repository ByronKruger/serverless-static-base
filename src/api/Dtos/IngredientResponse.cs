namespace Coffeeg.Api.Dtos;

/// <summary>
/// An ingredient offered by a beverage type, with everything the create form needs to
/// render a control for it.
///
/// <paramref name="Variants"/> is empty for a simple ingredient, and populated for a
/// complex one - milk offers low-fat, raw and soy. A caller chooses a variant when there
/// are any, and the ingredient itself when there are none; choosing a complex parent is
/// rejected with <c>preference.ingredient_requires_variant</c>.
/// </summary>
/// <param name="Id">Surrogate key, and what a preference line references.</param>
/// <param name="Name">Display name.</param>
/// <param name="Description">Optional elaboration.</param>
/// <param name="Unit">How an amount of this ingredient is measured.</param>
/// <param name="Variants">The choices this ingredient offers. Empty means it is itself the choice.</param>
public sealed record IngredientResponse(
    int Id,
    string Name,
    string? Description,
    UnitTypeResponse Unit,
    IReadOnlyCollection<IngredientResponse> Variants);
