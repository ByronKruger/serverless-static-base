namespace HopeTech.Api.Entities;

/// <summary>
/// An EF Core entity. Never an API contract: nothing outside <c>Repositories/</c> hands
/// one of these to a caller, so a column rename is not a breaking change to the API.
///
/// Mapping lives in <c>Data/Configurations/WidgetConfiguration.cs</c>, not in attributes
/// here and not in <c>OnModelCreating</c>.
/// </summary>
public class Widget
{
    /// <summary>Surrogate key.</summary>
    public int Id { get; set; }

    /// <summary>What the widget is called. Unique.</summary>
    public required string Name { get; set; }

    /// <summary>Optional free text.</summary>
    public string? Description { get; set; }

    /// <summary>When the row was created, UTC. Set from <c>IClock</c>, never from <c>DateTime.UtcNow</c>.</summary>
    public DateTimeOffset CreatedAtUtc { get; set; }
}
