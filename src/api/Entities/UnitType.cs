namespace Coffeeg.Api.Entities;

/// <summary>
/// A way of measuring an ingredient: teaspoons, percentage, shots, yes/no. Reference
/// data owned by moderators (Docs/domain/user-permissions.md) and seeded in
/// <see cref="Data.Configurations.UnitTypeConfiguration"/>.
///
/// Units are rows rather than an enum so the set is open: the API never names a unit in
/// code, which is what lets a preference be expressed in any unit that exists.
/// </summary>
public class UnitType
{
    public int Id { get; set; }

    /// <summary>Display name, unique. "Teaspoons", "Percentage", "Yes / no".</summary>
    public required string Name { get; set; }

    /// <summary>Short form shown next to an amount, when the unit has one. "tsp", "%".</summary>
    public string? Symbol { get; set; }

    /// <summary>Whether an amount is a quantity or a presence flag.</summary>
    public UnitKind Kind { get; set; }

    /// <summary>Smallest accepted amount, or <c>null</c> when unbounded below.</summary>
    public decimal? MinValue { get; set; }

    /// <summary>Largest accepted amount, or <c>null</c> when unbounded above.</summary>
    public decimal? MaxValue { get; set; }

    /// <summary>Ingredients measured in this unit.</summary>
    public ICollection<Ingredient> Ingredients { get; set; } = [];
}
