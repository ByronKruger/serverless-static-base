# HopeTech.Common

The library. Read the root `AGENTS.md` first; this file covers only what is specific here.
Every item below has a consumer-facing half ("pass your assembly", "do not re-implement this")
that belongs in the consuming API's own `AGENTS.md`, not this one.

## Where a file goes

Would it still make sense in an API that has never heard of any product's domain?

- **Yes, and it is machinery** - here.
- **Yes, but it is one product's** - that product's repository, shaped as an item template
  under `../src/templates/items/`.

The root `AGENTS.md` folder table is the destination list. A file that fits none of those
folders signals a missing decision, not a new folder.

## Three traps, all of them silent

1. **`featureAssemblies` is the contract, not a convenience.**
   `Extensions/ServiceCollectionExtensions.cs`. `AddHopeTechApi` prepends this assembly and
   calls `Distinct()`, so a consumer's argument is purely additive and never harmful. Do not
   drop the parameter; do not replace it with `AppDomain.CurrentDomain.GetAssemblies()`, which
   D-0009 rejected as non-deterministic. Its XML doc is the only warning a consumer gets that
   omitting it yields an unvalidated API - do not trim it. Nothing here catches a break: with
   no host in the repo (D-0010), validation and mapping still register over one fewer assembly
   and every test stays green.

2. **The base `ApplyConfigurationsFromAssembly` call is how this library's mappings reach any
   model.** `Identity/HopeTechIdentityDbContext.cs` scans this assembly; a consumer's derived
   context scans its own. The name suggests recursion; it does not recurse, so both calls are
   load-bearing.
   - Delete it and `AppUserConfiguration`/`AppRoleConfiguration` leave the model silently - no
     unique email index, no seeded roles - surfacing later as registration failing on a role
     that does not exist.
   - The library ships **no migrations** (D-0008), so any change to those configurations is a
     schema change only a consumer can resolve. The `CHANGELOG.md` entry must say so, and tell
     them to scaffold a migration.
   - `htc-host` emits the derived call with the comment explaining why both exist; changing the
     reasoning here changes that template.

3. **`InternalsVisibleTo` guards the public surface, and no test exercises it.**
   `ConfigureCorsPolicy` (`Options/`) and `BearerSecuritySchemeTransformer`
   (`Extensions/OpenApiExtensions.cs`) are `internal` on purpose, and **no test touches either
   today** - which is why the csproj item looks removable. Making one public to satisfy a test
   is a public-surface addition and a changelog entry. Commonly conflated:
   `includeInternalTypes: true` in `AddHopeTechValidation` is not about this library's
   validators (`LoginRequestValidator` and `RegisterRequestValidator` are `public`) - it exists
   so a *consumer's* `internal` validator is discovered. Dropping it breaks consumers and
   nothing here.

## Why a class library holds web code

`Microsoft.NET.Sdk` plus a `Microsoft.AspNetCore.App` `FrameworkReference`, so the Web SDK's
implicit global usings are restated as `<Using Include="..."/>` items in the csproj. A file
then compiles identically here and in a host; delete that block and moved files stop compiling
for a reason unrelated to their code (D-0008).

## The contract a consumer inherits from two calls

Each item is a promise, not an implementation detail: changing it changes behaviour in every
consumer without a line of their code changing. Each is a `CHANGELOG.md` entry; the pipeline
order is a decision entry too.

- **Default deny** - `Extensions/AuthorizationExtensions.cs`. Adding a policy beside
  `RequireAdministrator`/`RequireModerator` is additive; changing `SetFallbackPolicy`
  re-authorizes every endpoint in every consumer at once.
- **The JSON contract** - `AddHopeTechPresentation`: camelCase, string enums, nulls omitted on
  write, UTC ISO-8601. Changing the naming policy or `DefaultIgnoreCondition` is a wire-format
  break with no compile error in any consumer.
- **`ErrorType` -> status** - `Http/ResultExtensions.cs` is the only place the mapping exists,
  so the enum and that switch are **one unit**: the switch ends in `_ => 500`, so a new
  `ErrorType` member without a new arm silently becomes a 500. `AsProblemJson` forcing
  `application/problem+json` is equally deliberate - neither `Problem()` nor
  `ValidationProblem()` sets it, and without it a client cannot tell a payload from a problem.
- **Edge validation** - `Validation/FluentValidationFilter.cs`, registered in
  `AddHopeTechPresentation`. It shapes every 400 in every consumer, including ones that have
  written no validator of their own.
- **Pipeline order** - `Extensions/WebApplicationExtensions.cs`: exception handler, status code
  pages, OpenAPI (development only, anonymous), HTTPS redirection, CORS, authentication,
  authorization, controllers. Two positions are reasons, not preferences: the handler is first
  so nothing downstream leaks, and CORS precedes authentication so a rejected pre-flight keeps
  its headers. A consumer can neither reorder nor insert, so a change needs a decision entry.
- **Options fail fast** - `AddHopeTechOptions` binds `JwtOptions`, `CorsOptions` and
  `DatabaseOptions` with `ValidateDataAnnotations().ValidateOnStart()`. A new options class is
  additive; **adding `[Required]` to an existing one stops every consumer's process on
  upgrade**, breaking with nothing failing to compile or test.

No test here covers any of this end to end (D-0010). Review and the changelog are the only
gates.

## Public surface

`AddHopeTechApi`, `UseHopeTechApiPipeline`, `HopeTechIdentityDbContext`, `ApiControllerBase`,
`Result`, `Result<T>`, `Error`, `ErrorType`, `Policies`, `Roles`, the `Contracts/` records and
the `Abstractions/` ports. Renaming any of them breaks every consumer: major version, a
`CHANGELOG.md` entry, and the matching template under `../src/templates/items/` changed in the
same commit, or the templates scaffold code that no longer compiles.
