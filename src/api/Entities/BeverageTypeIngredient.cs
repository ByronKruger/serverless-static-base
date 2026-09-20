namespace Coffeeg.Api.Entities;

/// <summary>
/// Composition: which ingredients a beverage type is made of. An ingredient such as milk
/// belongs to several beverage types, so this is a join rather than a foreign key on
/// <see cref="Ingredient"/>.
///
/// Composition always names the top-level ingredient. Coffee is composed of "Milk", and a
/// preference then chooses "Soy milk" from milk's variants.
/// </summary>
public class BeverageTypeIngredient
{
    public int BeverageTypeId { get; set; }

    public BeverageType BeverageType { get; set; } = null!;

    public int IngredientId { get; set; }

    public Ingredient Ingredient { get; set; } = null!;

    /// <summary>Display order within the beverage type. Ties break on name.</summary>
    public int SortOrder { get; set; }
}
