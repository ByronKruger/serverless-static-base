using System.Globalization;

namespace Coffeeg.Api.Results;

/// <summary>
/// The expected failures of the beverage feature, in one place.
///
/// <c>AuthService</c> keeps its one error as a private member, which is the right shape
/// when a code has a single caller. These do not: <c>beverage_type.not_found</c> is
/// returned by both beverage services, and the codes are asserted by name in the
/// application and API test suites. Writing the strings twice is how they drift.
/// </summary>
public static class BeverageErrors
{
    public static Error BeverageTypeNotFound(int beverageTypeId) =>
        Error.NotFound(
            "beverage_type.not_found",
            $"There is no beverage type with id {beverageTypeId.ToString(CultureInfo.InvariantCulture)}.");

    public static Error PreferenceNotFound(int preferenceId) =>
        Error.NotFound(
            "preference.not_found",
            $"You do not have a preference with id {preferenceId.ToString(CultureInfo.InvariantCulture)}.");

    /// <summary>
    /// One preference per beverage type per person (Docs/domain/beverage-preference.md).
    /// Checked before the insert so the caller gets this rather than a unique-index
    /// violation, though the index is what actually holds under a race.
    /// </summary>
    public static Error DuplicateBeverageType(string beverageTypeName) =>
        Error.Conflict(
            "preference.duplicate_beverage_type",
            $"You already have a {beverageTypeName} preference. Edit that one instead.");

    public static Error IngredientNotInBeverageType(string field, int ingredientId, string beverageTypeName) =>
        Error.Validation(
            "preference.ingredient_not_in_beverage_type",
            $"Ingredient {ingredientId.ToString(CultureInfo.InvariantCulture)} is not part of {beverageTypeName}.",
            Failure(field, $"That ingredient is not part of {beverageTypeName}."));

    /// <summary>
    /// A complex ingredient is a category, not a choice: milk is low-fat, raw or soy
    /// (Docs/domain/beverage-preference.md), so naming "Milk" itself is incomplete.
    /// </summary>
    public static Error IngredientRequiresVariant(string field, string ingredientName, IEnumerable<string> variants) =>
        Error.Validation(
            "preference.ingredient_requires_variant",
            $"{ingredientName} is a choice between {string.Join(", ", variants)}. Pick one of those instead.",
            Failure(field, $"Choose which {ingredientName.ToLowerInvariant()} you want."));

    public static Error AmountOutOfRange(string field, string ingredientName, string range) =>
        Error.Validation(
            "preference.amount_out_of_range",
            $"The amount of {ingredientName} must be {range}.",
            Failure(field, $"Enter an amount {range}."));

    /// <summary>
    /// A Boolean unit carries presence, not quantity, so 1 ("yes") and null ("no") are
    /// the only two things it can say.
    /// </summary>
    public static Error AmountNotBoolean(string field, string ingredientName) =>
        Error.Validation(
            "preference.amount_not_boolean",
            $"{ingredientName} is either in the drink or not, so its amount must be 1 or empty.",
            Failure(field, "Choose yes or no."));

    public static Error DuplicateIngredient(string field, string ingredientName) =>
        Error.Validation(
            "preference.duplicate_ingredient",
            $"{ingredientName} was given more than once.",
            Failure(field, "This ingredient is already in the list."));

    private static Dictionary<string, string[]> Failure(string field, string message) =>
        new() { [field] = [message] };
}
