namespace Coffeeg.Api.Entities;

/// <summary>
/// How an amount measured in a <see cref="UnitType"/> behaves. This is the one enum in
/// the beverage model, and it deliberately classifies behaviour rather than vocabulary:
/// a moderator adding "Grams" adds a <see cref="UnitType"/> row, not a value here.
///
/// The web client branches on it to choose a control, and the preference service
/// branches on it to validate an amount.
/// </summary>
public enum UnitKind
{
    /// <summary>A quantity, bounded by the unit's own range. Two teaspoons, 40 percent.</summary>
    Numeric,

    /// <summary>Present or absent, with no quantity. Ice in a whiskey.</summary>
    Boolean
}
