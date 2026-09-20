namespace Coffeeg.Api.Entities;

/// <summary>
/// Something a beverage is composed of. Reference data owned by moderators.
///
/// An ingredient is simple when it has no <see cref="Variants"/> - coffee is only ever
/// coffee - and complex when it has them: milk is low-fat, raw or soy milk
/// (Docs/domain/beverage-preference.md). Only a simple ingredient, or a variant of a
/// complex one, can be chosen on a preference; a complex parent is a category, and
/// choosing it is rejected with <c>preference.ingredient_requires_variant</c>.
///
/// The unit is fixed here rather than chosen per preference, so an amount always means
/// the same thing for the same ingredient.
/// </summary>
public class Ingredient
{
    public int Id { get; set; }

    /// <summary>Display name, unique among its siblings. "Milk", "Soy milk", "Honey".</summary>
    public required string Name { get; set; }

    /// <summary>Optional elaboration for the UI.</summary>
    public string? Description { get; set; }

    /// <summary>The complex ingredient this is a variant of, or <c>null</c> at the top level.</summary>
    public int? ParentIngredientId { get; set; }

    public Ingredient? ParentIngredient { get; set; }

    /// <summary>The variants this ingredient offers. Empty means simple.</summary>
    public ICollection<Ingredient> Variants { get; set; } = [];

    /// <summary>How an amount of this ingredient is measured. Required.</summary>
    public int UnitTypeId { get; set; }

    public UnitType UnitType { get; set; } = null!;

    /// <summary>The beverage types composed of this ingredient.</summary>
    public ICollection<BeverageTypeIngredient> BeverageTypes { get; set; } = [];
}
