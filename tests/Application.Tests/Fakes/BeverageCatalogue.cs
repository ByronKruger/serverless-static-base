using Coffeeg.Api.Entities;

namespace Coffeeg.Application.Tests.Fakes;

/// <summary>
/// The seeded catalogue, as an in-memory entity graph. Ids match
/// <c>Data/Configurations</c> exactly, so a service test and an API test are talking
/// about the same coffee.
///
/// It exists because every rule in <c>BeveragePreferenceService</c> is a statement about
/// this graph - which ingredients belong to a drink, which are categories, what unit each
/// is measured in - so the fake repositories have to serve a real one.
/// </summary>
public static class BeverageCatalogue
{
    /// <summary>Seeded unit ids.</summary>
    public static class Units
    {
        public const int Teaspoons = 1;
        public const int Percentage = 2;
        public const int YesNo = 3;
        public const int Shots = 4;
        /// <summary>Seeded, but no ingredient uses it since water became yes/no.</summary>
        public const int Millilitres = 5;
    }

    /// <summary>
    /// Seeded ingredient ids. Milk and Sugar are categories; the rest are choices.
    /// Water belongs to whiskey only - coffee assumes it rather than naming it.
    /// </summary>
    public static class Ingredients
    {
        public const int Coffee = 1;
        public const int Milk = 2;
        public const int LowFatMilk = 3;
        public const int RawMilk = 4;
        public const int SoyMilk = 5;
        public const int Sugar = 6;
        public const int WhiteSugar = 7;
        public const int Honey = 8;
        public const int Water = 9;
        public const int Whiskey = 10;
        public const int Ice = 11;
    }

    /// <summary>Seeded beverage type ids.</summary>
    public static class Types
    {
        public const int Coffee = 1;
        public const int Whiskey = 2;
    }

    /// <summary>A fresh graph. Each call builds its own, so one test cannot mutate another's.</summary>
    public static List<BeverageType> Build()
    {
        var teaspoons = Unit(Units.Teaspoons, "Teaspoons", "tsp", UnitKind.Numeric, 0m, 20m);
        var percentage = Unit(Units.Percentage, "Percentage", "%", UnitKind.Numeric, 0m, 100m);
        var yesNo = Unit(Units.YesNo, "Yes / no", null, UnitKind.Boolean, 1m, 1m);
        var shots = Unit(Units.Shots, "Shots", "shot", UnitKind.Numeric, 0m, 10m);

        var coffee = Simple(Ingredients.Coffee, "Coffee", teaspoons);
        var milk = Complex(
            Ingredients.Milk,
            "Milk",
            percentage,
            Simple(Ingredients.LowFatMilk, "Low-fat milk", percentage),
            Simple(Ingredients.RawMilk, "Raw milk", percentage),
            Simple(Ingredients.SoyMilk, "Soy milk", percentage));
        var sugar = Complex(
            Ingredients.Sugar,
            "Sugar",
            teaspoons,
            Simple(Ingredients.WhiteSugar, "White sugar", teaspoons),
            Simple(Ingredients.Honey, "Honey", teaspoons));
        var water = Simple(Ingredients.Water, "Water", yesNo);
        var whiskey = Simple(Ingredients.Whiskey, "Whiskey", shots);
        var ice = Simple(Ingredients.Ice, "Ice", yesNo);

        return
        [
            Type(Types.Coffee, "Coffee", coffee, milk, sugar),
            Type(Types.Whiskey, "Whiskey", whiskey, ice, water)
        ];
    }

    private static UnitType Unit(int id, string name, string? symbol, UnitKind kind, decimal? min, decimal? max) =>
        new() { Id = id, Name = name, Symbol = symbol, Kind = kind, MinValue = min, MaxValue = max };

    private static Ingredient Simple(int id, string name, UnitType unit) =>
        new() { Id = id, Name = name, UnitTypeId = unit.Id, UnitType = unit };

    private static Ingredient Complex(int id, string name, UnitType unit, params Ingredient[] variants)
    {
        var ingredient = Simple(id, name, unit);

        foreach (var variant in variants)
        {
            variant.ParentIngredientId = id;
            variant.ParentIngredient = ingredient;
            ingredient.Variants.Add(variant);
        }

        return ingredient;
    }

    private static BeverageType Type(int id, string name, params Ingredient[] ingredients)
    {
        var beverageType = new BeverageType { Id = id, Name = name };

        for (var order = 0; order < ingredients.Length; order++)
        {
            beverageType.Ingredients.Add(new BeverageTypeIngredient
            {
                BeverageTypeId = id,
                BeverageType = beverageType,
                IngredientId = ingredients[order].Id,
                Ingredient = ingredients[order],
                SortOrder = order
            });
        }

        return beverageType;
    }
}
