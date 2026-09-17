# Requirements

Load this file when work touches users, beverage preferences, beverage types, ingredients, amounts, or units.

## Product
A user can save personal beverage preferences. The product records *what they want in a drink*, not recipes for a catalog.

## Glossary
- **User** - a registered person.
- **Beverage type** - the drink being preferred (coffee, tea, whiskey, ...).
- **Preference** - one user's saved choices for one beverage type.
- **Ingredient** - a component of a beverage type.
- **Simple ingredient** - description only (e.g. coffee).
- **Complex ingredient** - description plus a set of related variants (e.g. milk -> low-fat, raw, soy).
- **Amount** - how much of an ingredient is in that preference.
- **Unit** - how an amount is measured. Examples: `yes`/`no`, teaspoons, shot, percent.

## Invariants
1. A user may have at most one preference per beverage type.
2. Every beverage type is a set of ingredients.
3. Every ingredient is either simple or complex, never both.
4. A complex ingredient's variants are themselves ingredients (simple or complex).
5. Every ingredient has exactly one unit.
6. A preference stores an amount for each ingredient on that beverage type.
7. Empty / null amount means zero of that unit (`No`, `0` teaspoons, `0%`, `0` shots). It does not mean "unset" or "inherit default."

## Examples (normative)
- Tea may include milk, sugar, and water. A preference might be: milk = `yes`, honey = `2` teaspoons (honey is a complex variant of sugar), water = as defined by its unit.
- Coffee as an ingredient can be simple.
- Milk as an ingredient is complex.

## Out of scope (day 1)
- Sharing preferences between users
- Default "house" recipes
- Ordering, inventory, or pricing
- Multiple concurrent preferences for the same beverage type

## Open questions
- Canonical list of beverage types and units: product-owned; do not invent new types in code without updating this file.