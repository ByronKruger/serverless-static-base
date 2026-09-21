---
title: Beverage Preference Management
domain: Beverage Preferences
topics:
  - Create beverage preference
  - Edit beverage preference
  - Delete a beverage preference
codeReferences:
  - src/api/Services/BeveragePreferenceService.cs
  - src/api/Services/BeverageTypeService.cs
  - src/api/Entities/Ingredient.cs
  - src/api/Data/Configurations
  - src/web/src/app/features/preferences
generatedDate: 2026-04-24
lastVerified: 2026-09-21
---

# Overview of Beverage Preferences

Each user can save their own beverage preference. A beverage preference has a beverage
type (e.g. coffee, whiskey, tea, etc.). Each beverage type is composed of ingredients. An
ingredient is itself either simple (has a description only) or is complex: a description
and a related set of the complex ingredients. For instance, coffee is a simple ingredient
of only one type, whereas milk ingredient is complex and can either be low-fat milk, raw
milk, soy milk, etc. For any preference, an amount per ingredient is saved (e.g. tea has
milk, sugar and water and for each ingredient an amount: `yes` milk, 2 teaspoons of honey
which is a complex ingredient of type sugar). Each ingredient must also have an associated
unit type (E.g. percentage, <`yes`|`no`>, teaspoons, shot e.g. whiskey). A user is only
allowed to have one preference with a specific beverage type. If a user does not want a
specific ingredient in their beverage they specify a null/empty value and that means
(0 teaspoons, `No`, 0% etc.)

## Who may see a preference

A person owns their preferences and is the only one who may create them. Since D-0017,
anybody signed in may **read** them, because the point of finding another person is to see
how they take their drinks - see [user-accounts.md](user-accounts.md) for how people are
found. There is no way to opt out of that, and the preferences saved before D-0017 shipped
were saved under the opposite promise.

## Who owns what

Beverage types, ingredients and unit types are **reference data owned by moderators**
(see [user-permissions.md](user-permissions.md)). A person creating a preference picks
from what exists; they never coin a beverage type or a unit. Nothing in the API or the web
client names a drink, an ingredient or a unit - the whole feature is driven by the rows,
so a moderator adding "Tea" or "Grams" needs no code change. See `Docs/DECISIONS.md`
D-0013.

Moderator management screens are **not built yet**. Until they are, the catalogue is
whatever the `BeveragePreferences` migration seeds: two beverage types (Coffee, Whiskey),
eleven ingredients, and five unit types.

### The seeded catalogue

| Beverage | Ingredient | Unit | Notes |
| --- | --- | --- | --- |
| Coffee | Coffee | Teaspoons | |
| Coffee | Milk | Percentage | Category: low-fat / raw / soy |
| Coffee | Sugar | Teaspoons | Category: white sugar / honey |
| Whiskey | Whiskey | Shots | |
| Whiskey | Ice | Yes / no | |
| Whiskey | Water | Yes / no | The optional splash |

**Coffee deliberately does not name water.** It is an essential ingredient, assumed to be
in there, so there is nothing useful for a person to say about it and it is not part of
the composition. Asking for water in a coffee is
`preference.ingredient_not_in_beverage_type`.

Water is **yes / no**, not a volume, because the only question a whiskey drinker is asked
about it is whether they want a splash. Since the unit lives on the ingredient (D-0015)
and water now belongs to whiskey alone, that reads the same everywhere it appears - but
note the shape of the constraint: if a third beverage ever wants water in millilitres, one
of the two has to give, because a shared ingredient carries a single unit.

`Millilitres` remains a seeded unit type even though no ingredient currently uses it. It
is a catalogue of units available to a moderator, not a list of units in play.

## Rules

1. A person may hold **at most one preference per beverage type**. Enforced by a unique
   index on `(UserId, BeverageTypeId)`; the service checks first so the caller gets
   `preference.duplicate_beverage_type` (409) rather than a constraint violation.
2. A preference line must name a **choosable** ingredient: a simple one, or a variant of a
   complex one. Naming a complex parent fails with
   `preference.ingredient_requires_variant`, which lists the variants to pick from.
   Composition names the parent ("Coffee is made with Milk"); the preference names the
   variant ("Soy milk"). See D-0014.
3. The ingredient must belong to that beverage type, directly or as a variant of one of
   its ingredients. Otherwise `preference.ingredient_not_in_beverage_type`.
4. An ingredient may appear **once** per preference - `preference.duplicate_ingredient`.
5. The **unit is fixed by the ingredient**, never chosen per preference, so an amount
   always means the same thing. See D-0015.
6. An amount outside its unit's range fails with `preference.amount_out_of_range`; the
   message is built from the unit row, so it names no unit in code.
7. A unit whose kind is `Boolean` (`yes / no`) carries presence, not quantity: the only
   legal amounts are **1** (yes) and **null** (no). Anything else is
   `preference.amount_not_boolean`.
8. A **null or omitted** amount means the person does not want that ingredient. 0
   teaspoons, `No` and 0% are one statement, and an omitted line says the same as a line
   with a null amount.
9. A preference is **readable by any signed-in person and writable only by its owner**
   (`Docs/DECISIONS.md` D-0017). Reading somebody else's happens through their profile -
   `GET /api/users/{id}`, which names the subject in the route. The owner-scoped routes
   still mean *mine*: `GET /api/beverage-preferences/{id}` reports a preference belonging
   to anybody else as `preference.not_found` (404), never 403, so ids cannot be probed
   there. Anonymous callers get nothing either way.

## Units

A unit type is a row, not an enum value. It carries a name, an optional symbol, a range,
and a `Kind` that says how an amount behaves:

| Kind | Meaning | Amount | Rendered as |
| --- | --- | --- | --- |
| `Numeric` | A quantity, bounded by the unit's range | a number, or null for none | number input |
| `Boolean` | Presence - it is in the drink or it is not | 1 for yes, null for no | switch |

`Kind` is the one enum in the model, and it classifies *behaviour*, not vocabulary: there
are two ways an amount can behave, and each needs its own control and its own validation.
Adding "Grams" is a row; adding a third kind would be a code change, which is correct,
because a new behaviour needs new UI anyway.

## Not in scope yet

- **Hiding your preferences.** D-0017 made them readable by anybody signed in, with no
  opt-out. The opt-in `discoverable` flag that entry rejected is what this would become.
- **Editing and deleting** a preference. This slice is create and read only.
- **Moderator CRUD** for beverage types, ingredients and unit types (D-0013).
- **Ingredient hierarchies deeper than one level**. The schema is self-referencing so it
  permits them, but the repository loads one level of variants and only that level is
  offered as a choice - a grandchild would be silently unpickable (D-0014).
- **An ingredient measurable in more than one unit** (sugar in teaspoons or grams). That
  is the alternative D-0015 rejected; picking it up means superseding that entry.
