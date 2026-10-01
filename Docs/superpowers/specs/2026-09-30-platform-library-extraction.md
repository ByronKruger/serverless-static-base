# Spec: extracting the cross-cutting API code into a reusable library

Status: draft, awaiting approval
Date: 2026-09-30
Branch: `extract-api-platform-libraries`
Domain: [user-accounts.md](../../domain/user-accounts.md),
[user-permissions.md](../../domain/user-permissions.md)

## The request

> Extract common (cross-cutting concern) functionality such as user-auth, authz, problem
> details, centralised exception patterns, result/response patterns, JWT token
> configuration and token creation logic, RBAC, policies, roles, extensions, etc. Remove
> functionality that is specific to beverage preference. Do **not** apply the removal to
> the database - `master` must stay a functioning version of the beverage preferences
> project. This branch will be forked into a new repository that becomes a reusable C#
> library for common code, so future MVPs depend on the library instead of re-deriving it.

Confirmed in session on 2026-09-30:

1. **One library project**, not a split by dependency weight. Every consumer takes the
   full dependency set (ASP.NET Core, EF Core, SQL Server, Identity, AutoMapper,
   FluentValidation) whether it uses all of it or not.
2. **Product-neutral names now.** `Coffeeg.*` becomes `Platform.*` on this branch rather
   than in the fork, so the rename is one sweep instead of two.
3. **A thin reference host and trimmed tests stay runnable.** `dotnet build` and
   `dotnet test` both have to pass when this is done - that is the only real evidence the
   extraction did not quietly break the wiring.
4. **`src/web` is untouched.** This is a C#-only effort; the Angular app keeps its
   beverage features on this branch.

## What this reverses, and what it does not

### D-0002 does not survive this

`D-0002` is `Accepted` and reads, in part:

> we chose a single `Coffeeg.Api` project holding the documented folders [...]

and it explicitly rejected the alternative:

> Separate Domain / Application / Infrastructure projects: contradicts the horizontal
> layering the architecture document locks.

A second project is exactly what this spec asks for, so D-0002 cannot stand beside it.
The plan appends a superseding entry; **nothing is executed until a human accepts it**
(`AGENTS.md`, "Before you change structure, stack or contracts").

Worth being precise about *why* the old rejection does not apply. D-0002 rejected a
**vertical** split of one product's code into Domain / Application / Infrastructure layers,
each layer its own project. This is a **horizontal** split along a different seam: product
code on one side, code with no product in it on the other. The horizontal folders D-0002
protects survive intact inside the library - `Results/`, `Services/`, `Repositories/` and
the rest keep their one meaning each. What changes is that they live in two assemblies, and
which assembly a file belongs to is settled by one question: does it name a beverage?

### D-0001 is strengthened, not touched

D-0001 put registration and pipeline behind `AddCoffeegApi` / `UseCoffeegApiPipeline`
specifically so "a move to a serverless host stays a change to the host project only".
Moving those two methods into a library is that decision taken one step further: the host
shrinks to `Program.cs` plus controllers, so the cost of re-hosting drops again.

### The beverage decisions stay in the log untouched

D-0013, D-0014, D-0015 and D-0017 describe rules of a domain this branch deletes. They are
**not** edited, deprecated, or superseded: they were true, `master` still runs on them, and
`AGENTS.md` forbids rewriting history. They simply have no subject on this branch.

D-0018 (user search) and D-0016 (names required at registration, nullable in the schema)
**survive** - both are carried into the library, because both describe the user row rather
than the drink.

## The seam

One question decides where every file goes:

> Would this file still make sense in an API that has never heard of a beverage?

Three answers, three destinations.

**Yes, and it is machinery** -> `src/platform/Platform.Common`. The result pattern, the
problem-details mapping, the exception handler, the validation filter, the clock and
current-user ports, JWT options and token minting, the Identity user and role, the
`IUserRepository` port and its `UserManager` adapter, the role and policy names, and the
registration and pipeline extension methods.

**Yes, but it is an example** -> `src/api`, the reference host. `Program.cs`, the product's
`DbContext`, the migration that creates the `AspNet*` tables, and three controllers
(`Auth`, `Health`, `Users`) that show what a thin action looks like.

**No** -> deleted. Everything that names a beverage, an ingredient, or a unit.

### Why controllers stay in the host

A class library *can* ship controllers - the host registers its assembly as an
`ApplicationPart` and the routes appear. This spec deliberately does not do that, and
records the rejection so a later agent does not re-propose it:

- Route templates, `[EndpointName]` values and `[ProducesResponseType]` sets are product
  surface. A library that owns them makes the product's public HTTP contract a function of
  a dependency version.
- Overriding a library-supplied action means inheriting from it or shadowing its route,
  both of which read worse than the eight lines the host writes instead.
- `ApiControllerBase` is the part actually worth sharing, and it shares fine as a base class
  with no `ApplicationPart` anywhere.

So the library ships everything *behind* a controller, and the host writes the controller.
`AuthController` is then 25 lines of binding over an `IAuthService` the library provides.

### Why migrations stay in the host

Migrations are a product's schema history, not a library's. Two consumers of the library
will have different tables and cannot share a `__EFMigrationsHistory`. The library ships
the entity types and their `IEntityTypeConfiguration<T>`s; the product scaffolds the
migration that turns them into tables.

## Composition

The one piece of real design in this extraction. `AddCoffeegApi(configuration)` works today
because it sits in the same assembly as the product's `DbContext`, validators and mapper
profiles, and reaches all three through `typeof(ServiceCollectionExtensions).Assembly`.
Split into a library, all three of those scans break **silently**: the library would scan
itself, find only its own types, and the host's validators and profiles would never be
registered. Nothing fails at startup. Requests just stop being validated.

So the entry point becomes generic and is told the host's assembly:

```csharp
// Program.cs - still two statements.
builder.Services.AddPlatformApi<AppDbContext>(builder.Configuration, typeof(Program).Assembly);

var app = builder.Build();
app.UsePlatformApiPipeline();
```

Three consequences, each a silent failure if missed:

1. **`TDbContext` is constrained to `PlatformIdentityDbContext`.** `AddIdentityCore` needs
   `.AddEntityFrameworkStores<TContext>()` and `AddDbContext` needs the concrete type; both
   become generic parameters instead of the hard-coded `CoffeegDbContext` they are today.
2. **Validator and profile discovery scans both assemblies** - the library's own, always,
   plus every assembly passed in. `AddValidatorsFromAssembly` keeps
   `includeInternalTypes: true`.
3. **`ApplyConfigurationsFromAssembly` has to be called twice.** It scans one assembly.
   `PlatformIdentityDbContext.OnModelCreating` applies the library's configurations
   (`AppUserConfiguration`, and `AppRoleConfiguration` - which is what seeds the three
   roles); the product's context calls `base` first and then applies its own. Miss this and
   the email unique index and the seeded roles vanish from the model, which surfaces as
   registration failing on a role that does not exist.

## The inventory

### `src/platform/Platform.Common` - the library

A `Microsoft.NET.Sdk` class library with
`<FrameworkReference Include="Microsoft.AspNetCore.App" />`, holding **47 C# files** plus
its `.csproj`. Folder names change where the old one only made sense inside a single
project; contents move as-is apart from namespaces and the de-coffeeing noted below.

| From `src/api/` | To `Platform.Common/` | Note |
| --- | --- | --- |
| `Results/Result.cs`, `Error.cs`, `ErrorType.cs` | `Results/` | unchanged but for the namespace |
| `Interfaces/IClock.cs`, `ICurrentUser.cs`, `IAccessTokenFactory.cs` | `Abstractions/` | the `AccessToken` record travels with its port |
| `Interfaces/Repositories/IUserRepository.cs` | `Abstractions/` | keeps `SearchAsync` - see below |
| `Interfaces/Services/IAuthService.cs`, `IUserService.cs` | `Abstractions/` | `IUserService` loses `GetProfileAsync` |
| `Entities/AppUser.cs`, `AppRole.cs` | `Identity/` | |
| `Data/Configurations/AppUserConfiguration.cs`, `AppRoleConfiguration.cs` | `Identity/` | the role seed travels with the role |
| `Repositories/IdentityUserRepository.cs` | `Identity/` | still the only class that knows `UserManager` exists |
| *(new)* | `Identity/PlatformIdentityDbContext.cs` | `IdentityDbContext<AppUser, AppRole, int>`, applies the library's configurations, no `DbSet` of its own |
| `Authorization/Policies.cs`, `Roles.cs` | `Authorization/` | doc comments de-coffeed |
| `Options/JwtOptions.cs`, `CorsOptions.cs`, `DatabaseOptions.cs`, `ConfigureCorsPolicy.cs` | `Options/` | `CorsOptions.PolicyName` stops saying `CoffeegWebClient` |
| `Helpers/JwtAccessTokenFactory.cs` | `Tokens/` | |
| `Helpers/SystemClock.cs` | `Time/` | |
| `Helpers/HttpContextCurrentUser.cs`, `KebabCaseParameterTransformer.cs` | `Http/` | |
| `Controllers/ApiControllerBase.cs` | `Http/` | a base class, not a controller |
| `Extensions/ResultExtensions.cs` | `Http/` | the `Result` -> `ActionResult` map |
| `Exceptions/GlobalExceptionHandler.cs` | `Errors/` | |
| `Validation/FluentValidationFilter.cs` | `Validation/` | |
| `Validation/Authentication/*`, `Validation/Users/*` | `Validation/` | three validators, flat |
| `Dtos/RegisterRequest.cs`, `LoginRequest.cs`, `AuthenticationResponse.cs`, `UserResponse.cs`, `UserSummaryResponse.cs`, `UserSearchRequest.cs` | `Contracts/` | |
| `Services/AuthService.cs` | `Services/` | unchanged |
| `Services/UserService.cs` | `Services/` | `GetProfileAsync` deleted with its `IBeveragePreferenceRepository` dependency |
| `MapperProfiles/UserProfile.cs` | `Mapping/` | loses the paragraph about being a dependency of the beverage profiles |
| `Extensions/` (6 files) | `Extensions/` | `AddCoffeeg*` -> `AddPlatform*`, generic over `TDbContext` |

`IUserRepository.SearchAsync` stays on the library port deliberately. Its contract -
contains-match over username, first name and surname, exclude one id, cap the rows - names
nothing product-specific, and the port is the only sanctioned way to reach `UserManager`.
A product that wants to find people should not have to re-open that door. The D-0018
warnings in its doc comment (no `EF.Functions.Like`; neither test layer proves the SQL)
travel with it, because both are still true and both are still easy to trip over.

### `src/api` - the reference host

Renamed `Coffeeg.Api` -> `Platform.Api` (assembly, root namespace, `.csproj`; the folder
stays `src/api`). `Coffeeg.sln` -> `Platform.sln`. Fourteen files:

- `Program.cs` - two statements, as before.
- `Data/AppDbContext.cs` - `: PlatformIdentityDbContext`, no `DbSet` of its own, carrying
  the comment that says where a product's tables go.
- `Data/Migrations/` - one scaffolded `InitialIdentity` plus its designer and the snapshot,
  and the existing `.editorconfig` that marks them generated.
- `Controllers/AuthController.cs`, `HealthController.cs`, `UsersController.cs` - the last
  trimmed to its search action.
- `Dtos/HealthResponse.cs` - the host's one contract.
- `appsettings.json`, `appsettings.Development.json`, `Properties/launchSettings.json`,
  `Platform.Api.csproj`.

That `.csproj` keeps only `Microsoft.EntityFrameworkCore.Design` (tooling) and a
`ProjectReference` to the library. Every other `PackageReference` moves. `UserSecretsId`
becomes `platform-api` with everything else - which orphans the secrets set under the old id,
and matters only for `dotnet run`: nothing in the build, test or scaffold path reads a secret
store.

### Deleted - 44 files

`src/api` (34): two controllers (`BeveragePreferences`, `BeverageTypes`); seven entities
(`BeveragePreference`, `BeveragePreferenceIngredient`, `BeverageType`,
`BeverageTypeIngredient`, `Ingredient`, `UnitKind`, `UnitType`); six configurations; seven
DTOs, `UserProfileResponse` among them; four ports; two mapper profiles; two repositories;
`Results/BeverageErrors.cs`; two services; `CreateBeveragePreferenceRequestValidator`.

`tests/` (7): `BeveragePreferencesControllerTests`, `BeverageTypesControllerTests`,
`BeveragePreferenceServiceTests`, `BeverageTypeServiceTests`, `Fakes/BeverageCatalogue`,
`Fakes/FakeBeveragePreferenceRepository`, `Fakes/FakeBeverageTypeRepository`.

`Docs/` (3): `domain/beverage-preference.md`,
`superpowers/specs/2026-09-19-beverage-preferences.md`,
`superpowers/plans/2026-09-20-beverage-preferences.md`.

`Docs/superpowers/` keeps `2026-09-17-project-setup.md` and `2026-09-18-auth-slice.md`,
which document the code the library retains. The `user-search-and-profiles` spec and plan
stay too - search survives - each gaining one line at the top saying its profile half was
removed on this branch. They are history; they are not rewritten.

### Migrations: all four are replaced by one

The two beverage migrations (`BeveragePreferences`, `WaterIsAnOptionalSplash`) obviously go.
The recommendation is to delete `InitialIdentity` and `UsersHaveNames` **as well** and
scaffold a single fresh `InitialIdentity` from the trimmed model.

The reason is the model snapshot. `CoffeegDbContextModelSnapshot.cs` describes the model as
of the last migration - beverage tables included. Keep it and the next
`dotnet ef migrations add` in any forked product scaffolds a migration that **drops tables
the product never had**. Hand-editing a generated snapshot to fix that is worse than
regenerating it.

Nobody forks this repository to upgrade an existing coffee database, so there is no history
worth preserving here - and `master` keeps all four migrations untouched, which is the thing
the request actually protects. `UsersHaveNames` is not lost: `FirstName` and `LastName` sit
on `AppUser` and in `AppUserConfiguration`, so the fresh migration creates them as columns
from the start.

**This changes no database.** `dotnet ef migrations add` writes C# files; `dotnet ef database
update` is the command that touches SQL Server, and the plan will not run it. The `master`
database keeps its rows and its `__EFMigrationsHistory`.

### Tests - what is kept, and what each one is worth

| Kept | Proves |
| --- | --- |
| `Application.Tests/ResultTests` | the result pattern's invariants |
| `Application.Tests/Services/AuthServiceTests` | registration conflicts, the identical-failure rule for bad credentials, the role grant, token issuance |
| `Application.Tests/Services/UserServiceTests` | search only - the upper-casing, the limit default, self-exclusion. Profile tests deleted |
| `Application.Tests/Fakes/` | `FakeUserRepository`, `FakeAccessTokenFactory`, `FakeClock`, `FakeCurrentUser` |
| `Api.Tests/CoffeegApiFactory` -> `PlatformApiFactory` | the real pipeline over HTTP, in-memory provider, `EnsureCreated` seeding the roles |
| `Api.Tests/Controllers/AuthControllerTests` | 200 / 400 / 409 shapes, `problem+json`, `traceId`, `code` |
| `Api.Tests/Controllers/HealthControllerTests` | anonymous access through a default-deny pipeline |
| `Api.Tests/Controllers/UsersControllerTests` | search only, including the raw-JSON assertion that no response carries an email. Profile tests deleted |

Test projects are renamed to `Platform.Application.Tests` and `Platform.Api.Tests`.

`ApiJson.cs` keeps its `JsonStringEnumConverter` even with no enum left in any response: the
library still writes enums as strings, and the next product DTO carrying one would
otherwise fail to parse for a reason that has nothing to do with it.

The `InternalsVisibleTo` entries move to the library's `.csproj`, pointed at the renamed test
assemblies. They are load-bearing: `ConfigureCorsPolicy` and `BearerSecuritySchemeTransformer`
are `internal`, and `AddValidatorsFromAssembly` is called with `includeInternalTypes: true`.

## Documentation

- `Docs/DECISIONS.md` - the two proposed entries in the appendix below, appended at the
  bottom, index table updated, D-0002 marked `Superseded by D-0021`.
- `Docs/ARCHITECTURE.md` - the target folder tree gains `src/platform/`; the "One project,
  horizontal folders (D-0002)" line points at the superseding entry instead. The Angular
  half is untouched.
- `AGENTS.md` (root) - the repository map gains the library row; the beverage references go.
- `src/api/AGENTS.md` - rewritten. It currently explains one project; it has to explain two
  and say which side a new file belongs on. Its "Beverage preferences" section goes; its
  "Users and authentication" section survives nearly intact, because all of it is still true
  of the library.
- `Docs/domain/` - `beverage-preference.md` deleted and de-indexed. `user-accounts.md` and
  `user-permissions.md` stay: they describe the library's own domain. `user-accounts.md`
  loses its profile and preferences paragraphs.
- `README.md` - describes a beverage preferences app today. Rewritten to describe the base.

## Verification

In order, from the repo root:

```bash
dotnet build                  # must be clean: TreatWarningsAsErrors is on
dotnet test                   # both projects green
dotnet run --project src/api  # boots; GET /api/health returns 200
```

Then, because a green in-memory suite is no proof the composition works against a real
provider (the `src/api/AGENTS.md` warning about the search predicate is the precedent):

```bash
dotnet ef migrations add InitialIdentity --project src/api --output-dir Data/Migrations
```

must scaffold **only** `AspNet*` table creation and the three role inserts - no `DropTable`,
no beverage table. That, plus a `git diff master --stat` showing nothing under `src/web`
changed, is the evidence.

No database is touched to verify any of this. `dotnet ef database update` is not run, here
or anywhere in the plan.

The `ApplyConfigurationsFromAssembly` trap is already covered by a test that exists:
`AuthControllerTests`' registration case. `EnsureCreated` seeds from the **model**, and the
three roles reach the model only through `AppRoleConfiguration`. Miss the library's
configuration scan and there are no roles, so `AddToRoleAsync(user, Roles.User)` fails and
that test goes red. Nothing manual is needed.

What genuinely stays unproven is the **unique email index**: the in-memory provider does not
enforce unique indexes, so no test distinguishes `AppUserConfiguration` being applied from
it being skipped - `auth.email_taken` comes from the service's own up-front check either
way. That gap is closed by reading the generated migration, which either contains the
unique `EmailIndex` or does not. It is also not new: the index was equally unproven before
this change.

## Answered on 2026-09-30

1. **`Platform` is the name.** Confirmed as the placeholder. A later rename is another full
   sweep, and the fork is where to do it if a real name arrives.
2. **This branch is fork-only and is never merged to `master`.** Confirmed. That settles the
   scope of both decision entries: they describe the forked repository's architecture, not
   this one's. On `master`, D-0002 remains `Accepted` and remains correct, because `master`
   remains the complete beverage preferences project.
3. **`master` must keep working, unchanged.** Confirmed as a hard constraint, not a
   preference. It is written up as an invariant list at the top of the plan, and the last
   verification step is a build and test run on `master` itself to prove it.
4. **No migration is applied.** Confirmed. `dotnet ef database update` appears nowhere in
   the plan; the only EF command run is `migrations add`, which writes C# files.

## Still open

1. **`src/web`.** Untouched by choice. The fork inherits a beverage-shaped Angular app, so
   stripping it is a separate effort in the new repository, and worth a plan of its own
   there.

## Appendix: proposed decision entries

Drafted here; appended to `Docs/DECISIONS.md` as `Proposed` when the plan runs. Agents may
not mark them `Accepted` (`AGENTS.md`).

**D-0021 - Cross-cutting API code lives in one `Platform.Common` library; the product owns
controllers and migrations.** Supersedes D-0002.

> In the context of turning this repository into a base for future MVPs, facing a single
> project in which product code and reusable machinery share every folder, we chose to split
> it along one horizontal seam - a `Platform.Common` class library for everything that names
> no product noun, and a host project holding `Program.cs`, the product `DbContext`, its
> migrations and its controllers - to achieve a dependency a new product can take instead of
> re-deriving auth, the error model and the result pattern, accepting that the library
> carries the full dependency set (ASP.NET Core, EF Core, SQL Server, Identity, AutoMapper,
> FluentValidation) for every consumer, and that D-0002's single project no longer holds.

**D-0022 - Registration is generic over the product's `DbContext` and is told the product's
assembly.**

> In the context of moving `AddCoffeegApi` into a library, facing three same-assembly scans
> (validators, mapper profiles, entity configurations) that would silently find nothing once
> split, we chose `AddPlatformApi<TDbContext>(configuration, params Assembly[])` with
> `TDbContext : PlatformIdentityDbContext` and a second `ApplyConfigurationsFromAssembly`
> call inside the library's own context, to achieve a composition root that still reads as
> two statements, accepting that a host which forgets to pass its assembly gets an API whose
> requests are never validated and whose failure is invisible at startup.
