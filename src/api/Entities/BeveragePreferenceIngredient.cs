namespace Coffeeg.Api.Entities;

/// <summary>
/// One line of a preference: an ingredient and how much of it.
///
/// <see cref="Amount"/> is nullable because that is the domain's way of saying "none of
/// this" - 0 teaspoons, <c>No</c>, 0% are all the same statement
/// (Docs/domain/beverage-preference.md). The unit that gives the number its meaning comes
/// from <see cref="Ingredient.UnitType"/>, not from this row, so the same ingredient is
/// always measured the same way.
/// </summary>
public class BeveragePreferenceIngredient
{
    public int Id { get; set; }

    public int BeveragePreferenceId { get; set; }

    public BeveragePreference BeveragePreference { get; set; } = null!;

    /// <summary>Always a choosable ingredient: simple, or a variant of a complex one.</summary>
    public int IngredientId { get; set; }

    public Ingredient Ingredient { get; set; } = null!;

    /// <summary>How much, in the ingredient's unit. <c>null</c> means the person does not want it.</summary>
    public decimal? Amount { get; set; }
}
