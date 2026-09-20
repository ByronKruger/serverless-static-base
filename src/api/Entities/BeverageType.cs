namespace Coffeeg.Api.Entities;

/// <summary>
/// A kind of drink - coffee, whiskey, tea. Reference data owned by moderators
/// (Docs/domain/user-permissions.md); a person picks from these rather than naming one.
///
/// Seeded in <see cref="Data.Configurations.BeverageTypeConfiguration"/> because
/// moderator management of this table is assumed built and is not part of this slice.
/// </summary>
public class BeverageType
{
    public int Id { get; set; }

    /// <summary>Display name, unique across all beverage types.</summary>
    public required string Name { get; set; }

    /// <summary>Optional elaboration for the UI.</summary>
    public string? Description { get; set; }

    /// <summary>What this beverage is composed of, in display order.</summary>
    public ICollection<BeverageTypeIngredient> Ingredients { get; set; } = [];
}
