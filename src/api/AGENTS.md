# src/api - Coffeeg.Api

Read the root `AGENTS.md` first. This file only maps the project; the rules live in
`Docs/ARCHITECTURE.md` and the choices in `Docs/DECISIONS.md`.

ASP.NET Core Web API, .NET 9, nullable enabled, warnings as errors. One project,
horizontal layers, thin controllers. Not Minimal APIs (D-0001).

## Where things go

| Folder | Holds | Never holds |
| --- | --- | --- |
| `Controllers/` | Thin actions on `ApiControllerBase`, ~15 lines each | Business logic, `DbContext`, EF entities |
| `Interfaces/Services/` | `I{Resource}Service` | |
| `Services/` | `{Resource}Service` - one public method per use case | SQL, `HttpContext` |
| `Interfaces/Repositories/` | Persistence ports | |
| `Repositories/` | Port implementations over `CoffeegDbContext` | Business rules |
| `Data/` | `CoffeegDbContext`, `Configurations/`, `Migrations/` | Anything a controller can see |
| `Entities/` | EF entities, including the Identity `AppUser` / `AppRole` | API contracts |
| `Dtos/` | `Create{X}Request`, `Update{X}Request`, `{X}Response`, `[FromQuery]` models | One shared DTO per resource |
| `Results/` | `Result`, `Result<T>`, `Error`, `ErrorType` | |
| `Validation/` | One `AbstractValidator<T>` per request type, plus the MVC filter | Validators over EF entities |
| `MapperProfiles/` | AutoMapper profiles | |
| `Authorization/` | `Policies`, `Roles` name constants | Ad-hoc role strings anywhere else |
| `Options/` | Options classes with validation attributes, and their configurators | |
| `Extensions/` | Registration and pipeline composition, `Result` to `ActionResult` | |
| `Exceptions/` | Exception types and `GlobalExceptionHandler` | Control flow for expected misses |
| `Helpers/` | Adapters for platform ports (`SystemClock`, `HttpContextCurrentUser`) | Logic that belongs on a `{Resource}Service` |

## Composition

`Program.cs` is the only composition root and stays two statements:
`services.AddCoffeegApi(configuration)` and `app.UseCoffeegApiPipeline()`. Both are in
`Extensions/`. Add a registration to the right `Add*` method, never to `Program.cs`.

Pipeline order is fixed in `Extensions/WebApplicationExtensions.cs`:
exception handler, status-code pages, OpenAPI (development only), HTTPS redirection,
CORS, authentication, authorization, controllers.

## Adding an endpoint

Follow the vertical slice recipe in `Docs/ARCHITECTURE.md` - extend the existing
`{Resource}Controller` / `I{Resource}Service` pair rather than creating a class per verb.
Every endpoint needs a service unit test in `tests/Application.Tests/Services/` and an
HTTP test in `tests/Api.Tests/Controllers/`.

Defaults you inherit for free, and should not re-implement:

- Authorization is default-deny. An endpoint is authenticated unless it carries
  `[AllowAnonymous]`; use `[Authorize(Policy = Policies.RequireModerator)]` for more.
- Error responses carry `application/problem+json`, forced centrally by
  `ResultExtensions.AsProblemJson`. Never put `[Produces(...)]` on a controller: it is a
  result filter that overwrites `ContentTypes` on every `ObjectResult` and silently
  re-stamps error bodies as `application/json`.
- Errors are RFC 9457 problem details, with `traceId` and a stable `code` extension.
  Return a `Result` failure; do not throw for an expected miss and do not catch to 200.
- Requests are validated by `FluentValidationFilter` before the action runs. Register a
  validator; do not call one by hand.
- JSON is camelCase, enums are strings, nulls are omitted, timestamps are UTC ISO-8601.

## Users and authentication

ASP.NET Identity backs `AppUser : IdentityUser<int>` and `AppRole : IdentityRole<int>`
(D-0009). The int `Id` is the primary key; **email is the natural key and the sign-in
credential**, uniquely indexed; username is separately unique through Identity's own
index and is only a display name. Sign-in looks a user up by email, never by username.

The API issues and validates its own tokens with one symmetric key (D-0010), so
`Authentication:Jwt:SigningKey` is a real secret. The three roles in
`Authorization/Roles.cs` are seeded by the migration, and registration grants `User`.

Identity lives behind `IUserRepository`, so `AuthService` is unit-testable without
Identity, EF, or a database. Do not inject `UserManager` anywhere except
`Repositories/IdentityUserRepository.cs`.

## Beverage preferences

Two resources, four entities, and one rule worth internalising before you touch them.

`BeverageTypes`, `Ingredients` and `UnitTypes` are **moderator-owned reference data**
seeded by the `BeveragePreferences` migration (D-0013). There is no write endpoint for
them in this project yet. Nothing in C# names a drink, an ingredient or a unit - the
feature reads whatever rows exist, which is what "arbitrary beverage type" means here.
Do not add a `switch` on a beverage or unit name; if you find yourself wanting one, the
missing thing is a column on `UnitTypes`.

`Ingredient` is self-referencing (D-0014). An ingredient with no `Variants` is *simple*;
one with them is a *category*. `BeverageTypeIngredients` composes the **category**
("Coffee is made with Milk"), and a preference line must name a **leaf** ("Soy milk").
`BeveragePreferenceService.Choosable` is where that flattening happens, and it walks
exactly one level - a three-level ingredient would be silently unpickable, so if one is
ever seeded, that method and the include in `BeverageTypeRepository` both go recursive.

The **unit comes from the ingredient**, never from the request (D-0015). An amount is one
nullable `decimal(9,3)` read through `UnitType.Kind`: `Numeric` bounds it by the unit's
range, `Boolean` accepts only 1 (yes) or null (no). Null is always legal - it is the
domain's "none of this".

Error codes live in `Results/BeverageErrors.cs`, not inline: both services return
`beverage_type.not_found`, and both test suites assert the codes by name.

Ownership is a **service** rule, applied from `ICurrentUser`, not a policy on the action.
Every `IBeveragePreferenceRepository` method takes the user id so a query that forgets it
cannot be written, and somebody else's preference is `preference.not_found` (404), never
403.

## Commands

```bash
dotnet build                                   # from the repo root
dotnet test
dotnet run --project src/api                   # http://localhost:5237, https://localhost:7011
dotnet ef migrations add <Name> --project src/api --output-dir Data/Migrations
dotnet ef database update --project src/api
```

`Data/Migrations/` carries its own `.editorconfig` marking the scaffolded files as
generated; without it the repo style rules fail the build on every new migration.

Secrets go in user secrets (`dotnet user-secrets --project src/api set ...`), not in
`appsettings.Development.json`. `Database:ConnectionString`, `Authentication:Jwt:Issuer`,
`Authentication:Jwt:Audience` and `Authentication:Jwt:SigningKey` are validated at
startup: if one is missing, or the key is under 32 characters, the process does not start.
