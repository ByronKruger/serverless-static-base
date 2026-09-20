# Plan: creating a beverage preference

Status: complete
Date: 2026-09-20
Spec: [2026-09-19-beverage-preferences.md](../specs/2026-09-19-beverage-preferences.md)
Scope: a person picks an existing beverage type and saves how they like it, end to end.
Out of scope: editing and deleting a preference, moderator CRUD for the catalogue.

## What "arbitrary" turned out to mean

The brief read as though a person could coin a beverage type. Clarified in session: it
means **data-driven, not user-invented**. No hard-coded set of drinks or units anywhere;
the rows themselves stay moderator territory, and moderator management was declared
already built, so the tables are seeded instead.

## Decisions taken

`Docs/DECISIONS.md` D-0013, D-0014, D-0015 - all Accepted, each chosen explicitly in
session.

## Steps

1. **Model.** `UnitType` (+ `UnitKind`), `Ingredient` (self-referencing),
   `BeverageType`, `BeverageTypeIngredient`, `BeveragePreference`,
   `BeveragePreferenceIngredient`, with six configurations in `Data/Configurations/`.
   Unique indexes on `(UserId, BeverageTypeId)` and `(BeveragePreferenceId, IngredientId)`.
   Done.
2. **Seed.** Five unit types, eleven ingredients, two beverage types (Coffee, Whiskey),
   all literals via `HasData`, matching `AppRoleConfiguration`'s precedent. Between them
   they exercise every unit kind, both complex ingredients, and one ingredient (water)
   shared by two drinks. Done.
3. **Migration.** `BeveragePreferences`, generated and applied to LocalDB. Done.
4. **Application layer.** Two repository ports and implementations, two service ports and
   implementations, six DTOs, one validator, two AutoMapper profiles, two controllers with
   five endpoints. Done.
5. **Tests.** 32 service tests on fake ports, 20 HTTP tests through the real pipeline.
   Done.
6. **Frontend.** `features/preferences/` - store, API service, contracts, domain models, a
   list page and a create page, both lazy. Nav link in the shell, an authenticated route
   guard, 9 store tests. Done.
7. **Documentation.** Three decision entries, the domain doc rewritten with the rules and
   the unit table, the domain index, both project `AGENTS.md` files, this plan and its
   spec. Done.

## Things that had to change along the way

- **Two join tables came out singular.** EF names a table after the entity when there is
  no `DbSet`, so the first migration produced `BeverageTypeIngredient` and
  `BeveragePreferenceIngredient` beside four plural tables. `ToTable(...)` on both
  configurations, migration regenerated before it was applied anywhere.
- **`Error` could not carry both a code and field messages.** `Error.Validation` either
  took a code or took failures, never both. Domain rules need both - a stable code to
  branch on and the field to highlight - so a third overload was added. Without it these
  rules would all have reported `validation.failed`.
- **The test client could not read the API's own enums.** `ReadFromJsonAsync` defaults to
  `JsonSerializerDefaults.Web`, which refuses a string where an enum is expected, so every
  response carrying a `UnitKind` failed to parse. The API was right - the raw-JSON
  assertion proves it writes `"kind":"Boolean"` - so the fix was test-side:
  `tests/Api.Tests/ApiJson.cs`.
- **The web client read "no ice" as "yes".** Caught in the end-to-end run, not by a test:
  the API omits nulls (`WhenWritingNull`), so an absent amount arrives as `undefined`, and
  `amount === null` was false. The contract now types it `amount?: number | null` and
  normalises with `?? null`; the store spec was changed to send the shape the API actually
  sends, so it stays caught.

## Verification

```bash
dotnet build                     # succeeded, warnings as errors
dotnet test                      # 47 + 31 passed
cd src/web && npm run build      # succeeded, both pages in their own lazy chunks
cd src/web && npm test           # 16 passed
```

End to end against LocalDB, over HTTP:

- `GET /api/beverage-types` returns the two seeded drinks; `GET /api/beverage-types/1`
  returns coffee with milk's three variants and sugar's two, each with its unit and range.
- `POST /api/beverage-preferences` returned **201** with a `Location` that resolves, for
  2 tsp coffee, 40% soy milk, 1.5 tsp honey, 180 ml water.
- Every rule was provoked and answered with its own code: duplicate beverage type (409),
  the milk category rather than a variant (400 `ingredient_requires_variant`, body naming
  `ingredients[0].amount`), whiskey inside a coffee (400
  `ingredient_not_in_beverage_type`), 150% soy milk (400 `amount_out_of_range`), the same
  ingredient twice (400 `duplicate_ingredient`), 3 units of ice (400 `amount_not_boolean`),
  an unknown beverage type (404).
- A whiskey saved with `ice: 1` and `water: null` came back with ice at 1 and water with no
  amount at all - which is the omission that produced the web bug above.
- A second account sees only its own preference in `GET /api/beverage-preferences`, and
  the first account's preference id answers **404 `preference.not_found`**, not 403.

Note: the API instance used for this run was already listening on :5237 - it was not
started or stopped by this work.

## Open questions for the human

- **Editing a preference** is listed in the domain doc's topics and is not built. It is
  the obvious next slice, and the 409 on a duplicate currently tells people to "edit that
  one instead" - which they cannot yet do.
- **Moderator CRUD** for the catalogue (D-0013). Until it exists, a new beverage type
  needs a migration.
- **`AmountOutOfRange` reports a range, not the offending value.** Deliberate, so the
  message reads the same for min and max violations. Say if you would rather it named the
  value.

---

## Amendment, 2026-09-20: water becomes an optional splash

Requested after the first pass: whiskey's water changes from millilitres to yes/no
because it is an optional component, and coffee stops naming water at all because it is
essential and assumed to be in there. Ice (yes/no), whiskey (shots) and milk (percentage)
were already correct and were left alone.

### Changes

- `IngredientConfiguration`: water moves to unit 3 (`Yes / no`) and gains a description,
  matching ice.
- `BeverageTypeIngredientConfiguration`: the coffee-water composition row is deleted.
  Coffee's remaining sort orders stay contiguous at 0, 1, 2.
- Migration `WaterIsAnOptionalSplash`.

### The migration had to repair data, not just seed

The scaffold moved the seed and warned about data loss, which was real: three saved
preferences had water lines, two of them on coffees (180 ml and 0 ml) whose beverage no
longer composes water, and a volume is not a legal amount for a Boolean unit. Two
hand-written statements were added to `Up`, both written against the invariant rather than
against water so they stay correct if a composition is edited again:

1. Delete any preference line whose ingredient is no longer part of its beverage type,
   directly or as a variant of one that is.
2. Normalise any amount under a `Boolean` unit - 0 becomes null (no), anything else
   becomes 1 (yes).

`Down` restores the catalogue but says plainly that it cannot restore the data: the
volumes are collapsed and the deleted coffee lines are gone.

### Verification

```bash
dotnet build                     # succeeded, warnings as errors
dotnet test                      # 50 + 34 passed (six new)
```

Against LocalDB, before and after the migration:

| Preference | Before | After |
| --- | --- | --- |
| 1 Coffee | Coffee 2 tsp, Honey 1.5 tsp, Soy milk 40%, **Water 180 ml** | Coffee, Honey, Soy milk - water line deleted |
| 2 Whiskey | Ice 1, **Water null (ml)**, Whiskey 2 | Ice 1, **Water null (Yes / no)**, Whiskey 2 |
| 3 Coffee | Coffee 2 tsp, Raw milk null, **Water 0 ml** | Coffee, Raw milk - water line deleted |

Over HTTPS against the migrated database: coffee's ingredients are Coffee, Milk, Sugar
with no water; whiskey's water is `Yes / no` / `Boolean` with the new description; water in
a coffee is 400 `ingredient_not_in_beverage_type`; 200 ml of water in a whiskey is 400
`amount_not_boolean`; a whiskey with a splash saves as 201 with `amount=1, unit=Yes / no`.

### Worth knowing

- `Millilitres` is now a seeded unit type no ingredient uses. Left in place deliberately -
  it is a catalogue of units available to a moderator, not a list of units in play.
- This is the first live instance of the D-0015 constraint: the unit lives on the
  ingredient, so water carries one unit everywhere. It is fine today because water belongs
  to whiskey alone. A third beverage wanting water in millilitres would force the choice
  D-0015 recorded - either split the ingredient or supersede that entry.
- The API had to be stopped to build: a running `Coffeeg.Api` holds
  `bin/Debug/net9.0/Coffeeg.Api.exe` and `.dll`, and no MSBuild flag works around it.
