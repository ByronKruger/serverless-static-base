# HopeTech.Common

The library. Read the root `AGENTS.md` first; this file covers only what is specific to this
project.

## The one test for where a file goes

Would this file still make sense in an API that has never heard of any particular product's
domain?

- **Yes, and it is machinery** - it belongs here.
- **Yes, but it is one product's** - it belongs in that product's repository, and the shape
  it should take is an item template under `../src/templates/items/`.

The folder table in the root `AGENTS.md` is the destination list. A file that does not
obviously belong to one of those folders signals a missing decision, not a new folder.

## Three traps, all of them silent

These are the **maintainer's** half of each trap - what must not change in here. The same
three facts have a consumer-facing half ("pass your assembly", "keep the call in your own
context"), and that half belongs in the consuming API's own `AGENTS.md`, not this one.

1. **The `featureAssemblies` parameter is the contract, not a convenience.**
   `Extensions/ServiceCollectionExtensions.cs`. `AddHopeTechApi` builds its `scanned` array
   by prepending *this* assembly and calling `Distinct()`, so a consumer passing the
   library's own assembly is harmless and passing their own is purely additive - the shape
   exists so the parameter can never make things worse, only better.

   What must not change here: do not drop the parameter, and do not "improve" it into an
   `AppDomain.CurrentDomain.GetAssemblies()` scan - D-0009 considered and rejected exactly
   that, because what is loaded at the moment of the call is not deterministic. The XML doc
   on `featureAssemblies` is the **only** warning a consumer ever gets that omitting it
   yields an unvalidated API, so it is load-bearing prose; do not trim it.

   The reason this is a trap *in this repository specifically*: since the reference host left
   (D-0010), nothing here can fail if the contract breaks. `AddHopeTechValidation` and
   `AddHopeTechMapping` would still run, just over one fewer assembly, and every test would
   stay green. Review is the only gate.

2. **The base `ApplyConfigurationsFromAssembly` call is how this library's own mappings reach
   any model at all.** `Identity/HopeTechIdentityDbContext.cs` scans
   `typeof(HopeTechIdentityDbContext).Assembly`; a derived context in a consumer scans its
   own. The method's name suggests it recurses and it does not, so both calls exist and
   neither is redundant.

   Three consequences that are this library's problem, not a consumer's:

   - Any `IEntityTypeConfiguration<T>` added anywhere in this project reaches the model
     through that one call and nowhere else. Delete it and `AppUserConfiguration` and
     `AppRoleConfiguration` leave silently - no unique email index, no three seeded roles -
     surfacing much later as registration failing on a role that does not exist.
   - Because this library ships **no migrations** (D-0008), any change to those
     configurations is a schema change that only a consumer can resolve. It is therefore a
     `CHANGELOG.md` entry that says so in as many words, and tells them to scaffold a
     migration. An otherwise invisible change here is a deployment-blocking change there.
   - `htc-host` emits the derived context's call, with the comment explaining why both exist.
     A change to the reasoning here is a change to that template body.

3. **`InternalsVisibleTo` guards the public surface, and nothing currently exercises it.**
   `HopeTech.Common.csproj`. Two types are `internal` on purpose: `ConfigureCorsPolicy`
   (`Options/`) and `BearerSecuritySchemeTransformer` (`Extensions/OpenApiExtensions.cs`).
   **No test in `HopeTech.Common.Tests` touches either one today**, which is precisely why
   the item looks removable - and why this note exists. It is there so a test can reach them
   without widening the public surface; making either type public to satisfy a test is a
   public-surface addition and a `CHANGELOG.md` entry, not a shortcut.

   Related and commonly conflated: `includeInternalTypes: true` in `AddHopeTechValidation` is
   **not** about this library's own validators - `LoginRequestValidator` and
   `RegisterRequestValidator` are both `public`. It is what lets a *consumer's* `internal`
   validator be discovered, because the same call scans their assembly. Dropping the flag
   breaks consumers and not one thing in this repository, so nothing here would notice.

   This trap has no consumer-facing half at all.

## Why a class library holds web code

The project is `Microsoft.NET.Sdk`, not `Microsoft.NET.Sdk.Web`, and takes
`Microsoft.AspNetCore.App` as a `FrameworkReference`. The Web SDK's implicit global usings
are therefore restated as `<Using Include="..."/>` items in the csproj, so a file compiles
identically here and in a host. Delete that block and moved files stop compiling for a reason
that has nothing to do with their code (D-0008).

## Defaults a consuming host inherits - the contract, from this side

A consumer calls two methods and gets all of this, which makes every item below a promise
rather than an implementation detail. Changing any of them changes behaviour in every
consumer without a line of their code changing, so each is a `CHANGELOG.md` entry and the
pipeline order is a decision entry as well.

The consumer-facing statement of the same list - *do not re-implement any of this* - belongs
in the consuming API's own `AGENTS.md`.

- **Default deny.** `Extensions/AuthorizationExtensions.cs`. `SetFallbackPolicy` is what makes
  an endpoint authenticated unless it opts out. Adding a named policy beside
  `RequireAdministrator` and `RequireModerator` is additive and cheap. Touching the fallback
  re-authorizes **every endpoint in every consumer at once**, including ones whose authors
  never read this file - that is the single highest-blast-radius line in the library.
- **The JSON contract.** `AddHopeTechPresentation` in `Extensions/ServiceCollectionExtensions.cs`:
  camelCase, enums as strings, nulls omitted when writing, UTC ISO-8601. A change to the
  naming policy or to `DefaultIgnoreCondition` rewrites every response body every consumer
  has ever shipped. It is a wire-format break that produces no compile error anywhere, in any
  consumer, so the changelog entry is the only thing standing between it and a broken client.
- **The `ErrorType` -> status mapping.** `Http/ResultExtensions.cs` is the only place an
  `ErrorType` becomes a status code, and the enum and that switch are **one unit**. The
  switch ends in `_ => 500`, so adding a member to `ErrorType` without extending it maps the
  new category to `500 An unexpected error occurred.` - a silent, compiling bug, and the
  likeliest mistake anyone will make in this file. `AsProblemJson` forcing
  `application/problem+json` is equally deliberate: neither `Problem()` nor
  `ValidationProblem()` sets that media type, so removing the call serves problem bodies as
  `application/json` and a client can no longer tell a payload from a problem.
- **Edge validation.** `Validation/FluentValidationFilter.cs`, registered as an MVC filter in
  `AddHopeTechPresentation`. Changing when it runs, or the body it produces, changes the shape
  of every 400 in every consumer - including consumers who have never written a validator,
  because the library's own `RegisterRequestValidator` and `LoginRequestValidator` go through
  it too.
- **The request pipeline order**, fixed in `Extensions/WebApplicationExtensions.cs`: exception
  handler, status code pages, OpenAPI (development only, anonymous), HTTPS redirection, CORS,
  authentication, authorization, controllers. Two of those positions are reasons rather than
  preferences, and are the ones to defend in review: the exception handler is first so nothing
  downstream can leak an unhandled exception, and CORS precedes authentication so a rejected
  pre-flight still carries its headers. Changing this order needs a decision entry, because a
  consumer cannot reorder it and cannot insert into it.
- **Options that fail fast.** `AddHopeTechOptions` binds `JwtOptions`, `CorsOptions` and
  `DatabaseOptions` with `ValidateDataAnnotations().ValidateOnStart()`. Note which direction
  is the breaking one: adding a new options class is additive, but **adding a `[Required]` to
  an existing one stops every consumer's process on upgrade**, and that is a breaking change
  even though nothing fails to compile and nothing here fails to test.

Since the reference host left this repository (D-0010), **no test here covers any item on this
list end to end**. Each one is held in place by review and by the changelog, which is the
argument for keeping both specific.

## Public surface, and what renaming it costs

`AddHopeTechApi`, `UseHopeTechApiPipeline`, `HopeTechIdentityDbContext`, `ApiControllerBase`,
`Result`, `Error`, `ErrorType`, `Policies`, `Roles`, the `Contracts/` records and the
`Abstractions/` ports are what consumers compile against. Renaming any of them is a breaking
change for every consumer, so it belongs in `CHANGELOG.md` under a major version - and the
matching item template under `../src/templates/items/` has to change in the same commit, or
the templates scaffold code that no longer compiles.
