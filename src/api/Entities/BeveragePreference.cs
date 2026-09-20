namespace Coffeeg.Api.Entities;

/// <summary>
/// How one person likes one kind of drink. A person may hold at most one preference per
/// beverage type (Docs/domain/beverage-preference.md), enforced by a unique index on
/// <c>(UserId, BeverageTypeId)</c> in
/// <see cref="Data.Configurations.BeveragePreferenceConfiguration"/>.
/// </summary>
public class BeveragePreference
{
    public int Id { get; set; }

    /// <summary>The owner. Preferences are private to the person who created them.</summary>
    public int UserId { get; set; }

    public AppUser User { get; set; } = null!;

    public int BeverageTypeId { get; set; }

    public BeverageType BeverageType { get; set; } = null!;

    /// <summary>When the preference was saved. Always UTC.</summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>The chosen ingredients and how much of each.</summary>
    public ICollection<BeveragePreferenceIngredient> Ingredients { get; set; } = [];
}
