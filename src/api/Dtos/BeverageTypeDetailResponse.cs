namespace Coffeeg.Api.Dtos;

/// <summary>
/// One beverage type with what it is composed of. This is what the create-preference
/// form renders from: each ingredient carries its unit, and a complex one carries the
/// variants to choose between.
/// </summary>
/// <param name="Id">Surrogate key.</param>
/// <param name="Name">Display name.</param>
/// <param name="Description">Optional elaboration.</param>
/// <param name="Ingredients">Top-level ingredients, in the order a moderator arranged them.</param>
public sealed record BeverageTypeDetailResponse(
    int Id,
    string Name,
    string? Description,
    IReadOnlyCollection<IngredientResponse> Ingredients);
