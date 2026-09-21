namespace Coffeeg.Api.Dtos;

/// <summary>
/// A saved preference. Returned both to its owner and, through
/// <see cref="UserProfileResponse"/>, to anybody signed in who looks at that person's
/// profile (D-0017) - so nothing owner-only may be added here.
/// </summary>
/// <param name="Id">Surrogate key. Stable, and safe to put in a URL.</param>
/// <param name="BeverageType">The kind of drink this preference is for.</param>
/// <param name="CreatedAtUtc">When it was saved. UTC.</param>
/// <param name="Ingredients">What goes in it, in the beverage type's display order.</param>
public sealed record BeveragePreferenceResponse(
    int Id,
    BeverageTypeResponse BeverageType,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyCollection<BeveragePreferenceIngredientResponse> Ingredients);

/// <summary>One line of a saved preference.</summary>
/// <param name="IngredientId">The chosen ingredient.</param>
/// <param name="IngredientName">Its display name, so a client need not resolve the id.</param>
/// <param name="Amount">How much, in <paramref name="Unit"/>. <c>null</c> means none of it.</param>
/// <param name="Unit">The unit the amount is expressed in, fixed by the ingredient.</param>
public sealed record BeveragePreferenceIngredientResponse(
    int IngredientId,
    string IngredientName,
    decimal? Amount,
    UnitTypeResponse Unit);
