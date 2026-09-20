# Spec: creating a beverage preference

Status: agreed
Date: 2026-09-19
Domain: [beverage-preference.md](../../domain/beverage-preference.md)

## The request, and what "arbitrary" turned out to mean

> Add a feature to allow a user to create a beverage preference of arbitrary beverage
> type and allow specifying the units that can also be of arbitrary type.

"Arbitrary" here means **data-driven, not user-invented**. The feature must work against
whatever beverage types, ingredients and unit types exist in the database, with no
hard-coded enum of drinks or units anywhere in the API or the web client. It does **not**
mean a person may coin a new beverage type: `Docs/domain/user-permissions.md` gives that
to moderators, and that stays true.

Confirmed in session on 2026-09-19:

1. Beverage types, ingredients and unit types are moderator-owned reference data.
2. Moderator CRUD for that reference data is **assumed already built** and is not in this
   slice. The tables are seeded with two worked beverage types instead.
3. One preference per beverage type per user still holds.
4. `yes / no` is a genuine unit type, not a special case. Ice in a whiskey is present or
   not; any amount of sugar above zero means sugar is present.
5. Ingredients carry the full simple/complex hierarchy from the domain doc.
6. A unit is fixed by the ingredient. A person supplies an amount, not a unit.

## Model

```
UnitType 1---* Ingredient *---* BeverageType
                  |                   |
                  | (self, variants)  |
                  v                   v
         BeveragePreferenceIngredient *---1 BeveragePreference 1---* AppUser
```

**UnitType** - `Teaspoons`, `Percentage`, `Yes / no`, `Shots`, `Millilitres`. Carries a
`Kind` (`Numeric` or `Boolean`) plus optional `MinValue` / `MaxValue`. `Kind` is the only
enum in the model and it classifies *behaviour*, not vocabulary: it tells the web client
whether to render a number input or a switch, and tells the service how to validate. A
moderator adding `Grams` adds a row, not a code change - which is the whole point of
"arbitrary".

**Ingredient** - name, optional description, a required `UnitType`, and an optional
`ParentIngredient`. An ingredient with no variants is *simple* (coffee); one with variants
is *complex* (milk -> low-fat / raw / soy). Only a leaf is choosable on a preference: the
domain doc's own worked example, "2 teaspoons of honey which is a complex ingredient of
type sugar", is a preference line pointing at a child.

**BeverageType** - name, optional description, and the ingredients it is composed of,
through a `BeverageTypeIngredient` join carrying a sort order.

**BeveragePreference** - a user, a beverage type, a created stamp, and its ingredient
lines. Unique on `(UserId, BeverageTypeId)`.

**BeveragePreferenceIngredient** - a preference, a leaf ingredient, and a nullable
`Amount` (`decimal(9,3)`). Null means the person does not want it: 0 teaspoons, `No`, 0%.
Unique on `(BeveragePreferenceId, IngredientId)`.

## Rules the service enforces

Every one is an expected failure and comes back as a `Result` failure with a stable code.

| Code | Type | When |
| --- | --- | --- |
| `beverage_type.not_found` | 404 | No beverage type with that id |
| `preference.duplicate_beverage_type` | 409 | The caller already has one for this type |
| `preference.ingredient_not_in_beverage_type` | 400 | The ingredient is not composed into this type, nor a variant of one that is |
| `preference.ingredient_requires_variant` | 400 | The ingredient is complex; a variant must be chosen instead |
| `preference.amount_out_of_range` | 400 | Outside the unit's `MinValue` / `MaxValue` |
| `preference.amount_not_boolean` | 400 | A `Boolean` unit given an amount other than 1 or null |
| `preference.duplicate_ingredient` | 400 | The same ingredient sent twice in one preference |
| `preference.not_found` | 404 | Reading a preference that does not exist, or belongs to someone else |

Ownership is a service rule, not a controller rule: a preference belonging to another user
is reported as *not found*, never as forbidden, so ids cannot be probed.

## Seed data

Two beverage types, chosen to exercise every unit kind between them.

- **Coffee** - Coffee (tsp), Milk (%, complex: low-fat / raw / soy), Sugar (tsp, complex:
  white / honey), Water (ml).
- **Whiskey** - Whiskey (shots), Ice (yes/no), Water (ml).

Seeded with `HasData` and literal values only, matching `AppRoleConfiguration`: a
generated stamp would show as a model change on every scaffold.

## HTTP surface

| Method | Route | Purpose |
| --- | --- | --- |
| GET | `/api/beverage-types` | The list a person picks from |
| GET | `/api/beverage-types/{id}` | One type with its ingredients, variants and units - what the create form renders from |
| GET | `/api/beverage-preferences` | The caller's own preferences |
| GET | `/api/beverage-preferences/{id}` | One of the caller's own; also the `Location` target |
| POST | `/api/beverage-preferences` | Create - 201 + `Location` |

All authenticated by the inherited fallback policy. No new named policy: the rule is
ownership, which the service applies, not a role.

## Web

A lazy `features/preferences/` area: a list page and a create page, a store on the route,
and a data-access service whose transport types stop at that folder. The create form reads
the selected type's ingredients and renders one control per leaf - a number input bounded
by the unit's range, or a switch when the unit's kind is `Boolean`.

## Out of scope

- Editing and deleting a preference (the domain doc lists both; this slice is create/read).
- Moderator CRUD for types, ingredients and units - assumed built, seeded here.
- Ingredient hierarchies deeper than one level. The entity is self-referencing, so the
  schema permits them, but the repository loads exactly one level of variants and the
  service only offers that level as choices. A grandchild would be invisible rather than
  wrong; supporting one means a recursive load and a recursive choosable set.
