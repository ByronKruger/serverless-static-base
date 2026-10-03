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

1. **The assembly argument.** `AddHopeTechApi<TDbContext>(configuration, params Assembly[])`
   scans the assemblies it is handed for validators, AutoMapper profiles and entity
   configurations. Each of those APIs takes one assembly at a time, so a host that forgets
   `typeof(Program).Assembly` gets an API that starts, answers every route, and never
   validates a request. Nothing fails at startup to say so (D-0009). Do not "simplify" the
   signature by dropping the parameter.

2. **Two `ApplyConfigurationsFromAssembly` calls.** `HopeTechIdentityDbContext` calls it for
   *this* assembly; a product's context calls it for its own. The method's name suggests it
   recurses and it does not. Remove either call and the loss is silent - the library's
   version loses the unique email index and the three seeded roles, which surfaces much
   later as registration failing on a role that does not exist. The `htc-host` template
   emits the derived context's call with a comment saying why both exist; a change here is
   also a change there.

3. **`InternalsVisibleTo` is load-bearing.** `ConfigureCorsPolicy` and the OpenAPI security
   scheme transformer are `internal`, and `AddValidatorsFromAssembly` is called with
   `includeInternalTypes: true`. The `InternalsVisibleTo` item in the csproj is not a
   convenience for the test project; removing it changes what gets registered. This one has
   no consumer-facing half at all.

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

- **Default deny.** The fallback authorization policy requires an authenticated user, so an
  endpoint is authenticated unless it opts out with `[AllowAnonymous]`.
- **The JSON contract.** camelCase, enums as strings, nulls omitted, UTC ISO-8601.
- **RFC 9457 problem details** for every failure, with `traceId` and a stable machine-readable
  `code` extension. `ResultExtensions` is the only place an `ErrorType` becomes a status code.
- **Edge validation** through `FluentValidationFilter`, producing a 400 with per-field
  messages before the action runs.
- **The request pipeline order**, fixed in `Extensions/WebApplicationExtensions.cs`: exception
  handler, status code pages, OpenAPI (development only, anonymous), HTTPS redirection, CORS,
  authentication, authorization, controllers. The exception handler is first so nothing
  downstream can leak; CORS precedes authentication so a rejected pre-flight keeps its
  headers. Changing this order needs a decision entry.
- **Options that fail fast.** `JwtOptions`, `CorsOptions` and `DatabaseOptions` are bound,
  validated by data annotations, and `ValidateOnStart`. A missing secret stops the process
  rather than failing on the first request.

## Public surface, and what renaming it costs

`AddHopeTechApi`, `UseHopeTechApiPipeline`, `HopeTechIdentityDbContext`, `ApiControllerBase`,
`Result`, `Error`, `ErrorType`, `Policies`, `Roles`, the `Contracts/` records and the
`Abstractions/` ports are what consumers compile against. Renaming any of them is a breaking
change for every consumer, so it belongs in `CHANGELOG.md` under a major version - and the
matching item template under `../src/templates/items/` has to change in the same commit, or
the templates scaffold code that no longer compiles.
