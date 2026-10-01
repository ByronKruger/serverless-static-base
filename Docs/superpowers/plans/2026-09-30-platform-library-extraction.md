# Plan: extracting the cross-cutting API code into a reusable library

Status: complete
Date: 2026-09-30 (executed 2026-10-01)
Spec: [2026-09-30-platform-library-extraction.md](../specs/2026-09-30-platform-library-extraction.md)
Branch: `extract-api-platform-libraries` - **fork-only, never merged to `master`**
Scope: `src/platform/Platform.Common` extracted from `src/api`; the beverage domain deleted
from the branch; `src/api` reduced to a reference host; tests and docs trimmed to match.
Out of scope: `src/web` (untouched), any change to `master`, any change to a database.

## The invariants - read before step 1

The request names one constraint above all the others: **`master` must still function as the
complete beverage preferences project, with every architectural decision it holds today
still holding.** Everything below is subordinate to that. Six rules, each of them checkable:

1. **No git operation touches `master`.** Every command runs on
   `extract-api-platform-libraries`. No `checkout master`, `switch master`, `commit`,
   `amend`, `rebase`, `merge`, `cherry-pick`, `reset` or `push` that names `master`. Step 8's
   verification has to read `master`'s code, and did so through a **detached worktree** rather
   than a checkout: with the whole extraction sitting uncommitted in the working tree (one
   diff, as asked), `git checkout master` would have required stashing 150 changes and
   restoring them across renames and deletions. `git worktree add --detach <tmp> master`
   builds and tests `master` in a separate directory, cannot move the `master` ref, and leaves
   the working tree untouched.
2. **`master`'s decision log is unchanged by construction.** D-0002 gets its
   `Superseded by D-0021` line **on this branch only**. Because the branch is never merged,
   `master`'s `Docs/DECISIONS.md` keeps D-0001 to D-0020 byte-for-byte, D-0002 still
   `Accepted`, and every beverage entry (D-0013, D-0014, D-0015, D-0017) untouched on both
   sides. None of them is edited, deprecated, or re-argued anywhere.
3. **No migration is applied, to any database.** `dotnet ef database update` appears nowhere
   in this plan. Neither does `Database.Migrate()` in `Program.cs`, nor `EnsureCreated`
   outside the existing in-memory test factory. The only EF command run is
   `dotnet ef migrations add`, which writes C# files and does not connect. The dev SQL Server
   database, its rows and its `__EFMigrationsHistory` are not read from or written to.
4. **`src/web` is not edited.** Not one file.
5. **The branch stays revertible to nothing.** No history is rewritten, no tag moved, no
   remote pushed. Abandoning the branch leaves `master` exactly as it is now.
6. **A merge to `master` would invalidate D-0021.** On `master`, D-0002's single project is
   still the right answer, because `master` is a product, not a base. If a merge is ever
   proposed, D-0021 has to be re-decided first. This line exists so a future agent reading
   the log does not treat D-0021 as repository-wide.

Recorded in step 0 and checked again in step 8: `git rev-parse master` must return the same
SHA at the end as at the start.

## Decisions to record

Written to `Docs/DECISIONS.md` in step 7, newest at the bottom, index table updated.

D-0021 was **explicitly accepted in session** on 2026-10-01, which is what unblocked step 1.
D-0022 was not named in that acceptance, so it stays `Proposed`: `AGENTS.md` forbids an agent
marking its own entry `Accepted`. It is implemented either way - the code it describes is how
the composition root now works - which means the log currently carries a `Proposed` entry that
the code already follows. Accept or reject it when you next read it.

| ID | Title | Status |
| --- | --- | --- |
| D-0021 | Cross-cutting API code lives in one `Platform.Common` library; the product owns controllers and migrations | **Accepted** - explicitly accepted in session, 2026-10-01 |
| D-0022 | Registration is generic over the product's `DbContext` and is told the product's assembly | Proposed - never named in the go, so it stays an agent proposal |

D-0021 supersedes D-0002, so D-0002 gains one line - `Superseded by D-0021` - and nothing
else. Its Decision, Context and Consequences are not edited (`AGENTS.md`).

**D-0021 is a gate, not a note.** `AGENTS.md` says that work violating an accepted decision
stops until a human accepts the superseding entry. D-0002 is accepted and this work violates
it. So step 1 does not begin until the go includes acceptance of D-0021, or an instruction
to proceed with it left `Proposed`.

---

## Steps

Ordered so that `dotnet build` and `dotnet test` are green at **five** checkpoints rather
than one. The two changes that could each break everything - deleting a domain, and moving
every file to a new assembly - are deliberately separated by a green build, and the rename
sweep is separated from both. One commit per step, on this branch; say at go time if you
would rather have a single working-tree diff.

### 0. Baseline

- `git rev-parse master` and `git rev-parse HEAD` - record both in the plan's
  "Things that had to change along the way" section.
- `git status` must be clean apart from the spec and this plan.
- `dotnet build` and `dotnet test` - green *before* anything changes, so a later failure is
  attributable. The branch is currently identical to `master` plus two documents.

### 1. Delete the beverage domain

Still one project. The point of doing this first is that the surface to be moved in step 3
is 44 files smaller, and the build comes back to green before any structural change.

- Delete from `src/api`, 34 files: `Controllers/BeveragePreferencesController.cs`,
  `Controllers/BeverageTypesController.cs`; `Entities/BeveragePreference.cs`,
  `BeveragePreferenceIngredient.cs`, `BeverageType.cs`, `BeverageTypeIngredient.cs`,
  `Ingredient.cs`, `UnitKind.cs`, `UnitType.cs`; the six matching
  `Data/Configurations/*.cs`; `Dtos/BeveragePreferenceResponse.cs`,
  `BeverageTypeDetailResponse.cs`, `BeverageTypeResponse.cs`,
  `CreateBeveragePreferenceRequest.cs`, `IngredientResponse.cs`, `UnitTypeResponse.cs`,
  `UserProfileResponse.cs`; `Interfaces/Repositories/IBeveragePreferenceRepository.cs`,
  `IBeverageTypeRepository.cs`; `Interfaces/Services/IBeveragePreferenceService.cs`,
  `IBeverageTypeService.cs`; `MapperProfiles/BeveragePreferenceProfile.cs`,
  `BeverageTypeProfile.cs`; `Repositories/BeveragePreferenceRepository.cs`,
  `BeverageTypeRepository.cs`; `Results/BeverageErrors.cs`;
  `Services/BeveragePreferenceService.cs`, `BeverageTypeService.cs`;
  `Validation/BeveragePreferences/CreateBeveragePreferenceRequestValidator.cs`.
- `Data/CoffeegDbContext.cs` - drop the four `DbSet` properties and their comments.
- `Extensions/PersistenceExtensions.cs` - drop the two beverage repository registrations.
- `Extensions/ServiceCollectionExtensions.cs` - drop the two beverage service registrations
  from `AddCoffeegApplicationServices`.
- `Interfaces/Services/IUserService.cs` and `Services/UserService.cs` - delete
  `GetProfileAsync`, the `IBeveragePreferenceRepository` constructor parameter, the
  `NotFound(int)` member that only it used, and the `using` for the deleted port.
  `SearchAsync`, the `ToUpperInvariant` comment and the `NoUser` constant all stay.
- `Controllers/UsersController.cs` - delete the `Get(int id)` action. Rewrite the class
  comment: it currently explains why this controller takes a user id and
  `BeveragePreferencesController` does not, and that contrast no longer exists.
- `MapperProfiles/UserProfile.cs` - delete the `CreateMap<AppUser, UserProfileResponse>`
  discussion and the `<see cref>`s to both beverage profiles from the class comment.
- Delete from `tests/`, 7 files: `Api.Tests/Controllers/BeveragePreferencesControllerTests.cs`,
  `BeverageTypesControllerTests.cs`, `Application.Tests/Services/BeveragePreferenceServiceTests.cs`,
  `BeverageTypeServiceTests.cs`, `Application.Tests/Fakes/BeverageCatalogue.cs`,
  `FakeBeveragePreferenceRepository.cs`, `FakeBeverageTypeRepository.cs`.
- `Application.Tests/Services/UserServiceTests.cs` and
  `Api.Tests/Controllers/UsersControllerTests.cs` - keep the search cases, delete the profile
  cases. The raw-JSON no-email assertion in `UsersControllerTests` **stays**: it is the only
  thing proving the absence, and a DTO assertion only proves what the DTO declares.
- `Docs/` - delete `domain/beverage-preference.md`,
  `superpowers/specs/2026-09-19-beverage-preferences.md`,
  `superpowers/plans/2026-09-20-beverage-preferences.md`.

**Checkpoint A:** `dotnet build` clean, `dotnet test` green.

The four migrations are still on disk and now describe tables the model no longer has. That
is expected and harmless - migrations are string-based `migrationBuilder` calls and compile
independently of the entity types. Do **not** run any `dotnet ef` command at this point:
against this model it would scaffold `DropTable` calls.

### 2. Delete the migrations, scaffold nothing yet

- Delete all eight migration files and the snapshot:
  `20260918163337_InitialIdentity.cs` + `.Designer.cs`,
  `20260919210119_BeveragePreferences.cs` + `.Designer.cs`,
  `20260920075655_WaterIsAnOptionalSplash.cs` + `.Designer.cs`,
  `20260920203218_UsersHaveNames.cs` + `.Designer.cs`, `CoffeegDbContextModelSnapshot.cs`.
- Keep `Data/Migrations/.editorconfig`. Without it the repo style rules fail the build on
  every scaffolded file, which is exactly what step 6 scaffolds.

The replacement is **not** scaffolded here. It is scaffolded in step 6, after the context is
renamed - a migration generated now would bake `CoffeegDbContext` into its `[DbContext]`
attribute and its snapshot filename, and step 4 would break both.

Between here and step 6 the branch has no migrations at all. Nothing depends on them:
`Program.cs` does not migrate at startup, and `Api.Tests` uses `EnsureCreated` against the
in-memory provider, which builds from the model.

**Checkpoint B:** `dotnet build` clean, `dotnet test` green.

### 3. The library project, and the move into it

Namespaces stay `Coffeeg.Api.*` through this step. A namespace need not match its assembly,
so moving files across the project boundary compiles without touching a single `using` -
which means this step's failures are structural, and step 4's are textual, and the two never
mix.

- `src/platform/Platform.Common/Platform.Common.csproj` - `Microsoft.NET.Sdk`,
  `<FrameworkReference Include="Microsoft.AspNetCore.App" />`, `RootNamespace` and
  `AssemblyName` `Platform.Common`, `GenerateDocumentationFile`, `NoWarn` `CS1591`. Takes
  every `PackageReference` from the host except `Microsoft.EntityFrameworkCore.Design`, and
  both `InternalsVisibleTo` entries.
- `git mv` the 46 files into the layout the spec's inventory table gives. `git mv`, not
  delete-and-create, so the history of each file survives into the fork.
- `Identity/PlatformIdentityDbContext.cs` - new, the 47th file:

  ```csharp
  public abstract class PlatformIdentityDbContext(DbContextOptions options)
      : IdentityDbContext<AppUser, AppRole, int>(options)
  {
      protected override void OnModelCreating(ModelBuilder modelBuilder)
      {
          base.OnModelCreating(modelBuilder);

          // This assembly's own configurations: the email unique index, and the three
          // seeded roles. ApplyConfigurationsFromAssembly scans exactly one assembly, so
          // a product context calling it for itself does not pick these up (D-0022).
          modelBuilder.ApplyConfigurationsFromAssembly(typeof(PlatformIdentityDbContext).Assembly);
      }
  }
  ```

  `abstract`, and the constructor takes the non-generic `DbContextOptions` so that a derived
  context can pass its `DbContextOptions<TSelf>` up - which is the shape `AddDbContext<T>`
  requires.
- `src/api/Data/CoffeegDbContext.cs` -> `Data/AppDbContext.cs`, deriving from
  `PlatformIdentityDbContext`, no `DbSet` of its own, and calling
  `base.OnModelCreating(modelBuilder)` first and then
  `ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly)` for its own - which finds
  nothing today and is the line the next product edits. Comment says exactly that.
- `Extensions/ServiceCollectionExtensions.cs` - the entry point becomes:

  ```csharp
  public static IServiceCollection AddPlatformApi<TDbContext>(
      this IServiceCollection services,
      IConfiguration configuration,
      params Assembly[] featureAssemblies)
      where TDbContext : PlatformIdentityDbContext
  ```

  with `Assembly[] scanned = [typeof(ServiceCollectionExtensions).Assembly, ..featureAssemblies]`
  threaded into validator and profile discovery, and `TDbContext` threaded into
  `AddPlatformPersistence<TDbContext>` and `AddPlatformIdentity<TDbContext>`.
  `AddValidatorsFromAssembly` keeps `includeInternalTypes: true` for each.
- `src/api/Program.cs` - `AddPlatformApi<AppDbContext>(builder.Configuration, typeof(Program).Assembly)`
  and `UsePlatformApiPipeline()`. Still two statements. The comment gains one sentence on why
  the assembly is passed, because the failure it prevents is invisible.
- `src/api/Coffeeg.Api.csproj` - strip every `PackageReference` but
  `Microsoft.EntityFrameworkCore.Design`; add the `ProjectReference`; drop both
  `InternalsVisibleTo` (they moved). `UserSecretsId` was renamed to `platform-api` - see deviation 8.
- `Coffeeg.sln` - add `Platform.Common` under a new `platform` solution folder, with a fresh
  GUID.

**Checkpoint C:** `dotnet build` clean, `dotnet test` green. Green here means the three
assembly scans still find what they need - in particular `AuthControllerTests`' registration
case passing means the library's `AppRoleConfiguration` reached the model.

### 4. `Coffeeg` becomes `Platform`

One mechanical sweep, its map fixed in advance so it is checkable rather than judged
file-by-file.

| From | To |
| --- | --- |
| `Coffeeg.Api.Results` | `Platform.Common.Results` |
| `Coffeeg.Api.Interfaces`, `.Interfaces.Repositories`, `.Interfaces.Services` | `Platform.Common.Abstractions` |
| `Coffeeg.Api.Entities`, `.Data.Configurations`, `.Repositories` | `Platform.Common.Identity` |
| `Coffeeg.Api.Authorization` | `Platform.Common.Authorization` |
| `Coffeeg.Api.Options` | `Platform.Common.Options` |
| `Coffeeg.Api.Helpers` | `Platform.Common.Tokens` / `.Time` / `.Http`, per file |
| `Coffeeg.Api.Exceptions` | `Platform.Common.Errors` |
| `Coffeeg.Api.Validation`, `.Validation.Authentication`, `.Validation.Users` | `Platform.Common.Validation` |
| `Coffeeg.Api.Dtos` | `Platform.Common.Contracts` |
| `Coffeeg.Api.Services` | `Platform.Common.Services` |
| `Coffeeg.Api.MapperProfiles` | `Platform.Common.Mapping` |
| `Coffeeg.Api.Extensions` | `Platform.Common.Extensions` |
| `Coffeeg.Api.Controllers.ApiControllerBase` | `Platform.Common.Http.ApiControllerBase` |
| `Coffeeg.Api` (host assembly, root namespace, `.csproj`) | `Platform.Api` |
| `Coffeeg.Application.Tests`, `Coffeeg.Api.Tests` | `Platform.Application.Tests`, `Platform.Api.Tests` |
| `Coffeeg.sln` | `Platform.sln` |
| `AddCoffeegApi`, `UseCoffeegApiPipeline` | `AddPlatformApi`, `UsePlatformApiPipeline` |
| `AddCoffeegOptions/Presentation/Validation/Mapping/Persistence/Identity/Authentication/Authorization/Cors/OpenApi/ApplicationServices` | `AddPlatform` + the same suffix |
| `AddCoffeegPlatformServices` | `AddPlatformPorts` |
| `CoffeegApiFactory` | `PlatformApiFactory` |
| `CorsOptions.PolicyName` = `"CoffeegWebClient"` | `"PlatformWebClient"` |

`AddCoffeegPlatformServices` is the one name that cannot be transformed by rule:
`AddPlatformPlatformServices` is absurd, and the method registers `IClock`, `ICurrentUser`
and `IAccessTokenFactory` - ports. Hence `AddPlatformPorts`.

Also in this step:
- Prose de-coffeeing in doc comments and `Directory.Packages.props`: the AutoMapper comment
  says "a revenue threshold that Coffeeg sits under"; `AuthenticationExtensions` says "this
  API is its own issuer" (fine) but names Coffeeg elsewhere. Grep, do not guess:
  `grep -rin 'coffeeg\|coffee\|beverage\|ingredient\|drink' --include=*.cs --include=*.csproj --include=*.props --include=*.json src/api src/platform tests`
  must come back empty but for `UserSecretsId`.
- `appsettings.json` / `appsettings.Development.json` - any Coffeeg-named issuer, audience or
  origin becomes Platform-named. These hold no secrets; the real values are in user secrets.
- `tests/Api.Tests/ApiJson.cs` - its comment cites `UnitKind` as the enum that would fail to
  parse. Reworded to name no type, since none is left.

**Checkpoint D:** `dotnet build` clean, `dotnet test` green, and the grep above empty.

### 5. Renaming the test projects and folders

Split out from step 4 only because it moves files rather than editing them:
`tests/Application.Tests/Coffeeg.Application.Tests.csproj` ->
`Platform.Application.Tests.csproj`, same for `Api.Tests`, and the `InternalsVisibleTo`
entries in the library point at the new assembly names. Folder names
(`tests/Application.Tests`, `tests/Api.Tests`) stay - `Docs/ARCHITECTURE.md` names them, and
they carry no product noun.

**Checkpoint D still applies:** build and test again after the rename; a stale
`InternalsVisibleTo` fails as a missing-internal error, not as a naming one, so it is worth
the second run.

### 6. One fresh migration

- `dotnet ef migrations add InitialIdentity --project src/api --output-dir Data/Migrations`
- Read the generated `.cs` before anything else. It must contain: `CreateTable` for
  `AspNetRoles`, `AspNetUsers`, `AspNetRoleClaims`, `AspNetUserClaims`, `AspNetUserLogins`,
  `AspNetUserRoles`, `AspNetUserTokens`; `FirstName` and `LastName` as `nvarchar(64)`,
  nullable, on `AspNetUsers`; `CreatedAtUtc` non-nullable; a **unique** `EmailIndex` on
  `NormalizedEmail`; and three `InsertData` rows for Administrator, Moderator and User with
  the literal concurrency stamps from `AppRoleConfiguration`.
- It must contain **no** `DropTable`, and no table named for a beverage, ingredient or unit.
  If it does, the step-2 deletion missed the snapshot.
- The new snapshot is `AppDbContextModelSnapshot.cs`, and the designer carries
  `[DbContext(typeof(AppDbContext))]`.

`migrations add` builds the host and constructs the context to read the model. It does not
connect, so no database is touched - but `AddDbContext`'s callback does run, which means
`UseSqlServer` needs a non-empty connection string. `appsettings.Development.json` already
supply one. If they ever do not, `Database__ConnectionString` set to any syntactically valid
string in the environment is enough; the value is never dialled.

**Checkpoint E:** `dotnet build` clean, `dotnet test` green, migration read and matching the
list above. **`dotnet ef database update` is not run.**

### 7. Documentation

- `Docs/DECISIONS.md` - append D-0021 and D-0022 at the bottom, both `Proposed`, both filled
  against the template already in the file (Context, Decision as a Y-statement, Options
  considered, Consequences). Add both to the index table. Add `Superseded by D-0021` to
  D-0002's Status line and change nothing else about it. D-0021's Consequences carry
  invariant 6 above: on `master` D-0002 still holds, and a merge would need D-0021 re-decided.
- `Docs/ARCHITECTURE.md` - `src/platform/Platform.Common/` added to the target tree with its
  folders; the "One project, horizontal folders (D-0002)" line re-pointed at D-0021; the
  persistence lock gains the sentence that the product owns its `DbContext` and migrations
  and the library owns the Identity entities. The Angular half is not touched.
- `AGENTS.md` (root) - the repository map gains an `src/platform` row; the `Docs/domain`
  row's beverage mention goes; the Required Reading pointer to `Docs/domain/README.md` stays.
- `src/api/AGENTS.md` - the biggest rewrite. It currently teaches one project. It has to
  teach two, leading with the one question that decides where a file goes, and keeping the
  "Defaults you inherit for free" list, which is entirely still true and is now the library's
  contract. Its "Users and authentication" section survives nearly intact. Its "Beverage
  preferences" section and the beverage half of "Users, search and profiles" go. The D-0018
  warnings about `EF.Functions.Like` and about neither test layer proving the SQL survive,
  moved next to the port.
- `src/platform/AGENTS.md` - new sibling, per the root file's rule that a project folder
  carries one. What belongs in the library, what does not, and the three silent-failure traps
  from D-0022.
- `Docs/domain/README.md` - the beverage row goes. `user-accounts.md` and
  `user-permissions.md` stay; the former loses its profile and preferences paragraphs, and
  its search paragraphs stay.
- `Docs/superpowers/specs/2026-09-20-user-search-and-profiles.md` and its plan - one line at
  the top of each: the profile half was removed on this branch, search survives in the
  library. Their bodies are not rewritten; they are history.
- `README.md` - rewritten. It describes a beverage preferences app; it should describe a base
  with auth, the error model and the result pattern already wired, and say plainly that
  `src/web` is still the beverage client and is the fork's first job.
- This plan - `Status: complete`, plus the two sections the house format expects: "Traps
  found while executing" and "Things that had to change along the way".

### 8. Proving `master` is untouched

The last step, and the one the request is really about.

- `git rev-parse master` - must equal the SHA recorded in step 0.
- `git log master --oneline -1` - still `e36b3c9 Add user search + detailed view`.
- `git diff master --stat -- src/web Docs/DECISIONS.md` - `src/web` must show **zero** files;
  `DECISIONS.md` shows the branch's additions, which is expected and never merged.
- `git worktree add --detach <tmp> master`, then `dotnet build` and `dotnet test` **in that
  worktree** - green, with the beverage suite present and passing. Then
  `git worktree remove --force <tmp>`. This writes nothing to `master`: no commit, no file
  edit, and a detached HEAD cannot move the ref. It is worth the two minutes because it is the
  only direct evidence that `master` still functions, rather than an argument that it must.
- A worktree also sidesteps cross-branch build artefacts entirely: it has its own `bin/` and
  `obj/`, so there is nothing stale to clean.

**Result:** `master` built clean and all **121** tests passed (70 service, 51 API) - the same
counts as the step 0 baseline, which is what "still the complete beverage preferences project"
means in numbers.

---

## Traps found while planning

1. **Three assembly scans that fail silently.** Written up as D-0022, and the reason step 3's
   checkpoint matters more than it looks. `AddValidatorsFromAssembly`, AutoMapper's
   `AddMaps`, and `ApplyConfigurationsFromAssembly` all resolve through
   `typeof(ServiceCollectionExtensions).Assembly` today. Split into a library, each finds only
   the library. The validation one is the dangerous member of the set: the API starts, every
   route answers, and nothing is validated.
2. **`ApplyConfigurationsFromAssembly` is not recursive and not additive across assemblies.**
   It scans the one assembly you hand it. Two calls, one per assembly, in
   `PlatformIdentityDbContext` and in `AppDbContext`.
3. **The model snapshot outlives the migrations.** Delete the two beverage migrations and keep
   `CoffeegDbContextModelSnapshot.cs`, and the next `migrations add` in any forked product
   scaffolds `DropTable` for tables that product never had. Hence step 2 deletes all four
   migrations and the snapshot.
4. **Scaffolding before renaming bakes in the old name.** A migration generated while the
   context is still `CoffeegDbContext` carries `[DbContext(typeof(CoffeegDbContext))]` and a
   `CoffeegDbContextModelSnapshot` filename. That is why step 6 sits after step 4 and step 2
   scaffolds nothing.
5. **Broken `<see cref>` is a build error here, not a warning.**
   `GenerateDocumentationFile` is on and `TreatWarningsAsErrors` is on, so a cref to a deleted
   type is CS1574 and fails the build. `MapperProfiles/UserProfile.cs` and
   `Controllers/UsersController.cs` both point at types step 1 deletes, and the namespace
   moves in step 4 invalidate more of them. `grep -rn 'cref=' --include=*.cs src/ tests/` after
   both steps, and let the build find the rest.
6. **`[Produces]` must not appear on a controller.** Already in `src/api/AGENTS.md` and worth
   repeating because the host's controllers get rewritten in this work: it is a result filter
   that overwrites `ContentTypes` on every `ObjectResult`, which re-stamps error bodies as
   `application/json` and silently breaks the RFC 9457 media type.
7. **`InternalsVisibleTo` is load-bearing.** `ConfigureCorsPolicy` and
   `BearerSecuritySchemeTransformer` are `internal`, and validators are discovered with
   `includeInternalTypes: true`. The entries move to the library and have to name the renamed
   test assemblies, or step 5 fails with missing-internal errors that look unrelated.
8. **`UserSecretsId` was planned to stay `coffeeg-api`.** The thinking was that renaming it
   orphans the secrets already set locally, which step 6 was assumed to need in order to
   construct the context. **That assumption turned out to be false** - see deviation 8 below.
   It was renamed with everything else, step 6 worked anyway, and the documents were corrected
   to match the code rather than the other way round.
9. **Running the branch against `master`'s database would be a write.** The branch's app
   boots fine against the existing dev database, because the `AspNet*` tables are there and
   nothing migrates at startup. But registering a test user through it would insert a row
   into the database `master` uses. No step does this, and nothing in the verification needs
   it - the automated suite covers the same path against the in-memory provider.
10. **`Roles` and `Policies` carry beverage prose, not beverage code.** `Roles.Moderator`'s
    comment says "Can manage beverage types and their ingredients". The constant is generic;
    only the sentence is not. Same for the `Error` doc comment's
    `preference.duplicate_beverage_type` example. Text-only fixes, in step 4's grep.

## Verification

Per-step checkpoints A-E above, then step 8. In full, from the repo root:

```bash
dotnet build                  # clean; TreatWarningsAsErrors is on
dotnet test                   # both projects green
dotnet run --project src/api  # boots; GET /api/health returns 200 anonymously
```

Plus the two inspections that no test performs:

- The generated migration matches step 6's list - in particular the **unique** `EmailIndex`,
  which the in-memory provider does not enforce and therefore no test can prove.
- The grep from step 4 is empty.

What is **not** verified, stated plainly:

- **Nothing runs against SQL Server.** The search predicate's SQL translation is unproven
  here, as it was before; `src/api/AGENTS.md` keeps the warning.
- **The library is not packed.** No `dotnet pack`, no NuGet id, no version policy. The fork
  consumes it by `ProjectReference` until someone decides otherwise - which is a decision
  entry, in the fork.
- **`src/web` is not built.** Untouched, so its state is whatever `master` has.

## Open questions for the human

1. **Accept D-0021, or proceed with it `Proposed`?** Step 1 is gated on this. D-0002 is
   `Accepted` and this work contradicts it, and `AGENTS.md` says that stops until a human
   accepts the superseding entry.
2. **Commits per step, or one diff?** Eight commits on the branch make each checkpoint
   revertible, which is worth something on a move this size. Neither touches `master`.
3. **Does `src/api` stay as the reference host in the fork, or get deleted there?** Keeping it
   on this branch is settled - it is what makes the extraction verifiable. Whether the fork
   keeps a reference host or expects each product to write its own `Program.cs` is the fork's
   question, and worth answering before someone answers it by accident.

---

## Things that had to change along the way

Baseline recorded at step 0: `master` and `HEAD` both at
`e36b3c92cc9104f54abb58a680653ec05c3aa96d`, working tree clean but for the spec and this
plan, `dotnet build` clean and 121 tests green (70 service, 51 API).

Nine deviations from the plan as written. Four were predicted traps firing on schedule; five
were not foreseen.

**1. The class library does not inherit the Web SDK's implicit global usings.** Not
anticipated, and it stopped the first build of step 3 dead with ~40 `CS0246` errors for
`IServiceCollection`, `HttpContext`, `ILogger<>`, `WebApplication` and `StatusCodes` - in
files that had not been edited at all. `Microsoft.NET.Sdk.Web` adds eight global usings that
`Microsoft.NET.Sdk` does not.

The fix had two candidates: add explicit `using` directives to each moved file, or restate
the Web SDK's set as `<Using Include="..."/>` items in `Platform.Common.csproj`. The second
was chosen because step 3's whole purpose was a move with no textual edits, and because the
library genuinely *is* web code living in a class library - the SDK is the thing that is
unusual here, not the files. It is written up as a Consequence of D-0021, because a future
agent deleting that block will see unrelated files stop compiling.

**2. Migration scaffolding moved from step 2 to step 6.** Planned, and worth restating as the
reason the plan has eight steps rather than seven: a migration generated before the rename
would have carried `[DbContext(typeof(CoffeegDbContext))]` and a
`CoffeegDbContextModelSnapshot` filename, and step 4 would have broken both. Between step 2
and step 6 the branch has no migrations at all, and nothing depends on them - `Program.cs`
does not migrate at startup and `Api.Tests` uses `EnsureCreated` against the model.

**3. The CS1574 trap fired on five crefs, not the two that were obvious.** Predicted as trap
5. `GenerateDocumentationFile` plus `TreatWarningsAsErrors` makes an unresolvable cref a build
error, and five doc comments named a namespace that had moved:
`Contracts/UserSearchRequest.cs` (`Validation.Users.UserSearchRequestValidator`),
`Identity/AppUser.cs` (two: `Data.Configurations.AppUserConfiguration` and
`Validation.Authentication.RegisterRequestValidator`), `Results/Error.cs`
(`Exceptions.GlobalExceptionHandler`), `Results/ErrorType.cs` (`Extensions.ResultExtensions`).
These are *relative* crefs, so the sweep over fully-qualified `Coffeeg.Api.*` strings never
touched them. The build found every one; nothing had to be hunted by hand.

**4. Two real references broke on the rename, both of them the trap in a different costume.**
`IdentityUserRepository.ToError` used `nameof(Dtos.RegisterRequest.Password)` - a
namespace-qualified reference in code rather than in a comment - and
`FluentValidationFilter` called `AsProblemJson`, which moved from `Extensions/` to `Http/`
while the file's `using` was rewritten to `Platform.Common.Extensions` by the blanket map.
Both are the same underlying thing: the two files whose destination folder disagreed with
their old namespace.

**5. The using-rewrite produced duplicate and self-referential usings.** Foreseeable, not
foreseen. Collapsing three namespaces into `Platform.Common.Abstractions` and three into
`Platform.Common.Identity` left files with the same `using` twice - `CS0105`, an error here -
and left `Identity/IdentityUserRepository.cs` importing the namespace it now lives in. A
dedupe pass over every library file, dropping duplicates and any using matching the file's own
namespace, was added between the namespace sweep and the method sweep.

**6. `src/api/Data/Migrations/` keeps its `.editorconfig` through the deletion.** Planned, and
the reason is worth keeping visible: without it the repo style rules fail the build on every
scaffolded file, and step 6 scaffolds three.

**7. `appsettings.Development.json` points at a new database name.** Not in the plan. It held
`Database=CoffeegAI`, which is the database `master` uses. Renaming it to `PlatformAI` is
product-neutral *and* strengthens invariant 3: nothing run from this branch can reach
`master`'s data even by accident. Note the limit of that - **user secrets override
appsettings**, and an older secret store on this machine may still supply the
`CoffeegAI` string. Nothing in this plan connects to a database, so it did not matter here;
anyone who later runs the branch should check `dotnet user-secrets list` first.

**8. `UserSecretsId` was renamed after all, against trap 8, and the docs were corrected to
match rather than the code reverted.** The plan said keep `coffeeg-api` so the secrets step 6
needed stayed reachable. The step 4 sweep renamed it to `platform-api` along with everything
else, and this was not noticed until the final reference sweep - by which point step 6 had
already succeeded, because the connection string came from `appsettings.Development.json`
rather than from the secret store. So the premise of trap 8 was wrong: nothing in the build,
test or scaffold path needs a secret at all.

Given that, `platform-api` is the better end state for a neutral base, and the fix was to
correct the four documents that claimed otherwise (`src/api/AGENTS.md`, `README.md` twice, and
this plan). **The consequence is real and worth stating: `dotnet run` on this branch will not
start until `Authentication:Jwt:SigningKey` is set under the new id.** The old `coffeeg-api`
store is untouched and still serves `master`.

**9a. The final prose sweep found four beverage comments inside the library, and broke a test
fixing them.** The library's own rule is that nothing in it names a product noun, comments
included, and four places still did: `Roles.Moderator` ("can manage beverage types and their
ingredients"), `KebabCaseParameterTransformer` (its worked example was `BeverageTypes` ->
`beverage-types`), and two in `Error.cs` (an example code `preference.duplicate_beverage_type`,
and a rule phrased as "that ingredient needs a variant chosen"). Two test fixtures in
`ResultTests` used beverage error codes as sample data as well.

Rewriting those fixtures changed an `Error.NotFound` code in the arrange but not the
assertion three lines below it, and `ResultTests.Failure_exposes_its_error` went red -
caught by the final `dotnet test`, fixed, re-run green. Worth recording because it is the only
point in the whole extraction where a checkpoint caught a regression that the author
introduced rather than one the plan predicted.

**9. Two historical plans got a drift note that the plan did not budget for.**
`2026-09-17-project-setup.md` and `2026-09-18-auth-slice.md` were kept as the record of how
the retained code came to be, but both describe `src/api/...` paths and `Coffeeg.*` names that
no longer exist. The root `AGENTS.md` rule - when docs and code disagree, fix the docs and
mention the drift - makes a one-line header note the minimal honest fix. Their bodies are not
rewritten.

## What the verification actually showed

**Test count: 121 -> 56.** 70 service tests became 32 and 51 API tests became 24. The whole of
the drop is the deleted beverage suite plus the five profile cases in `UserServiceTests` and
the three in `UsersControllerTests`. Nothing that tests retained code was removed.

**The generated migration matched the step 6 checklist exactly**: seven `AspNet*`
`CreateTable` calls, `CreatedAtUtc` non-nullable, `FirstName` and `LastName` as
`nvarchar(64)` nullable, a **unique** `EmailIndex` on `NormalizedEmail`, and three
`InsertData` rows carrying the literal concurrency stamps from `AppRoleConfiguration`. `Up()`
spans lines 14-217 and every `DropTable` sits in `Down()` from line 218, which is a
migration's own rollback and not a drop of anything that existed. No beverage table appears
anywhere in `Data/Migrations/`.

**The spec's claim about manual verification was wrong, and is corrected in the spec.** It
said the `ApplyConfigurationsFromAssembly` trap needed a manual registration against real SQL
Server because no in-memory test could catch it. That is not so: `EnsureCreated` seeds from
the *model*, the three roles reach the model only through `AppRoleConfiguration`, and if the
library's scan were missed there would be no roles and `AddToRoleAsync(user, Roles.User)`
would fail. `AuthControllerTests`' registration case covers it, and it passed at checkpoint C
- which is the moment the split actually became trustworthy. What remains genuinely unproven
is the unique email index, since the in-memory provider does not enforce unique indexes;
reading the generated migration is what closes that, and it was read.

**`dotnet ef migrations add` warns, correctly.** It prints `No instantiatable types
implementing IEntityTypeConfiguration were found while scanning assembly 'Platform.Api'`.
That is the *product's* own configuration scan running and finding nothing, because this host
has added no tables - evidence that the double scan of D-0022 is wired, not evidence of a
fault. Recorded in both `src/api/AGENTS.md` and D-0022 so nobody tries to silence it.

## Master, at the end

- `git rev-parse master` - `e36b3c92cc9104f54abb58a680653ec05c3aa96d`, unchanged from step 0.
- `git log master --oneline -1` - `e36b3c9 Add user search + detailed view`, unchanged.
- No commit was made on any branch: the whole extraction is one working-tree diff, as asked.
- No file under `src/web` was edited.
- No `dotnet ef database update`, no `Database.Migrate()`, no `EnsureCreated` outside the
  in-memory test factory. No database was read from or written to at any point.
- `master`'s `Docs/DECISIONS.md` is untouched, because nothing was committed and nothing will
  be merged. D-0002 is marked `Superseded by D-0021` **in the working tree only**, which is
  where this branch lives.

The one thing a reader should carry away: D-0021 is scoped to this branch and the fork it
becomes. On `master`, D-0002's single project is still the right answer, because `master` is
a product rather than a base. A merge would require re-deciding D-0021 first.

## Left for the fork

- **`src/web` is untouched**, by choice. It is still the beverage client, and stripping it to
  a reusable shell is its own effort in the new repository.
- **One inert drift that came out of leaving it untouched:** `src/web/public/config.json` (and
  the five `*.spec.ts` fixtures that mirror it) advertise `audience: "coffeeg-api"`, while this
  branch's API now issues and validates `platform-api`. Nothing breaks - the API validates the
  `aud` claim it signed itself and the client never sends one (D-0010) - so this is a reading
  hazard rather than a bug. It resolves itself whenever `src/web` is stripped or deleted; if
  the Angular app is kept, change those six values in the same pass.
- **`UserSecretsId` is `platform-api`** and nothing is set under it. `dotnet run` needs
  `Authentication:Jwt:SigningKey` before it will start. Build, test and `migrations add` do not.
- **D-0022 is `Proposed` while the code already follows it.** Accept or reject it.
