namespace HopeTech.Api.Dtos;

/// <summary>
/// The representation returned for a widget. A contract, not an entity: the EF type
/// never crosses this boundary, so a column rename is not a breaking API change.
/// </summary>
/// <param name="Id">Surrogate key.</param>
/// <param name="Name">What the widget is called.</param>
/// <param name="Description">Optional free text.</param>
/// <param name="CreatedAtUtc">When it was created, UTC.</param>
public sealed record WidgetResponse(
    int Id,
    string Name,
    string? Description,
    DateTimeOffset CreatedAtUtc);
