using Coffeeg.Api.Entities;

namespace Coffeeg.Api.Dtos;

/// <summary>
/// How an ingredient is measured, as the client sees it. Sent as part of an ingredient
/// rather than on its own: the unit is fixed by the ingredient, so a caller never picks
/// one.
/// </summary>
/// <param name="Id">Surrogate key.</param>
/// <param name="Name">Display name. "Teaspoons", "Yes / no".</param>
/// <param name="Symbol">Short form to render beside an amount, when there is one.</param>
/// <param name="Kind">Whether an amount is a quantity or a presence flag. Serialized as a string.</param>
/// <param name="MinValue">Smallest accepted amount, or <c>null</c> when unbounded.</param>
/// <param name="MaxValue">Largest accepted amount, or <c>null</c> when unbounded.</param>
public sealed record UnitTypeResponse(
    int Id,
    string Name,
    string? Symbol,
    UnitKind Kind,
    decimal? MinValue,
    decimal? MaxValue);
