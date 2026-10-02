# src/platform - Platform.Common

Read the root `AGENTS.md` first. This file only maps the project; the rules live in
`Docs/ARCHITECTURE.md` and the choices in `Docs/DECISIONS.md`.

A .NET 9 class library, nullable enabled, warnings as errors. It holds the cross-cutting
machinery of an ASP.NET Core Web API and nothing else (D-0008). `src/api` is a host that
consumes it.

## The one rule

**Nothing in here names a product noun.** Not a type, not a member, not a comment, not an
example in a comment. The test is a question:

> Would this file still make sense in an API that has never heard of this product's domain?

If no, it belongs in the host. This is the whole boundary, and it is why the library can be
taken as a dependency by a product it has never seen.

## Where things go

| Folder | Holds | Never holds |
| --- | --- | --- |
| `Abstractions/` | Ports: `IClock`, `ICurrentUser`, `IAccessTokenFactory`, `IUserRepository`, `IAuthService` | Implementations |
| `Authorization/` | `Policies`, `Roles` name constants | Ad-hoc role strings anywhere else |
| `Contracts/` | The request/response types the library owns | A product's DTOs |
| `Errors/` | `GlobalExceptionHandler` | Control flow for expected misses |
| `Extensions/` | `AddPlatformApi<TDbContext>`, `UsePlatformApiPipeline`, and the `Add*` each composes | Anything a product registers |
| `Http/` | `ApiControllerBase`, `ResultExtensions`, `HttpContextCurrentUser`, `KebabCaseParameterTransformer` | Controllers (D-0008) |
| `Identity/` | `AppUser`, `AppRole`, their configurations, `PlatformIdentityDbContext`, `IdentityUserRepository` | A product's entities, or any migration |
| `Mapping/` | AutoMapper profiles for the library's own contracts | |
| `Options/` | Options classes with validation attributes, and their configurators | |
| `Results/` | `Result`, `Result<T>`, `Error`, `ErrorType` | A product's error-code classes |
| `Services/` | `AuthService` | A product's services |
| `Time/` | `SystemClock` | |
| `Tokens/` | `JwtAccessTokenFactory` | |
| `Validation/` | The library's validators, plus `FluentValidationFilter` | Validators over EF entities |

Two things the library deliberately does **not** ship, each argued in D-0008:

- **Controllers.** A library can ship them through an `ApplicationPart`, and this one does
  not: route templates, `[EndpointName]` values and `[ProducesResponseType]` sets are a
  product's public HTTP contract, and a library owning them makes that contract a function
  of a dependency version. `ApiControllerBase` is the part worth sharing.
- **Migrations.** Two consumers have different tables and cannot share a
  `__EFMigrationsHistory`. The library ships the entity types and their
  `IEntityTypeConfiguration<T>`s; the product scaffolds the migration.

## Three traps, all of them silent

These are the whole of D-0009, and every one of them fails without an error at startup.

1. **`ApplyConfigurationsFromAssembly` scans exactly one assembly.** It is not recursive and
   not additive. `PlatformIdentityDbContext.OnModelCreating` calls it for *this* assembly;
   a product's context calls `base` and then calls it for its own. Remove the call in the
   base class and the unique email index and the three seeded roles vanish from the model,
   which surfaces as registration failing on a role that does not exist.
2. **Validator and profile discovery scans one assembly at a time.** `AddPlatformApi` takes
   `params Assembly[]` and the host must pass `typeof(Program).Assembly`. A host that forgets
   gets an API whose requests are never validated. Nothing fails at startup; every route
   still answers.
3. **The Web SDK's implicit global usings are absent in a class library.**
   `Platform.Common.csproj` restates them as `<Using Include="..."/>` items. Delete that
   block and files that have not changed stop compiling, for a reason that is nowhere in
   them.

## Defaults a consuming host inherits, and should not re-implement

- Authorization is default-deny. An endpoint is authenticated unless it carries
  `[AllowAnonymous]`; use `[Authorize(Policy = Policies.RequireModerator)]` for more.
- Error responses carry `application/problem+json`, forced centrally by
  `ResultExtensions.AsProblemJson`. **Never put `[Produces(...)]` on a controller**: it is a
  result filter that overwrites `ContentTypes` on every `ObjectResult` and silently
  re-stamps error bodies as `application/json`.
- Errors are RFC 9457 problem details, with `traceId` and a stable `code` extension. Return
  a `Result` failure; do not throw for an expected miss and do not catch to 200.
- Requests are validated by `FluentValidationFilter` before the action runs. Register a
  validator; do not call one by hand.
- JSON is camelCase, enums are strings, nulls are omitted, timestamps are UTC ISO-8601.
- `Authentication:Jwt:Issuer`, `:Audience`, `:SigningKey` and `Database:ConnectionString`
  are validated at startup: if one is missing, or the key is under 32 characters, the
  process does not start.

## Users and authentication

ASP.NET Identity backs `AppUser : IdentityUser<int>` and `AppRole : IdentityRole<int>`
(D-0005). The int `Id` is the primary key; **email is the natural key and the sign-in
credential**, uniquely indexed; username is separately unique through Identity's own index
and is only a display name. Sign-in looks a user up by email, never by username.

The API issues and validates its own tokens with one symmetric key (D-0006), so
`Authentication:Jwt:SigningKey` is a real secret. The three roles in `Authorization/Roles.cs`
are seeded from `AppRoleConfiguration`, so the names in code and in the database cannot
drift, and registration grants `User`.

Identity lives behind `IUserRepository`, so `AuthService` is unit-testable without Identity,
EF, or a database. **Do not inject `UserManager` anywhere except
`Identity/IdentityUserRepository.cs`.**

`ICurrentUser` exposes the caller as **`int? UserKey`**, already parsed. The claim is a
string; parsing it is `HttpContextCurrentUser`'s job, so a service never does it. A null key
means anonymous *or* a malformed claim - the two are indistinguishable by design, and both
were already the same outcome.

`AppUser.FirstName` / `.LastName` are **required at registration and nullable in the schema**
(D-0007), so every reader must handle their absence.

## Error codes

`AuthService` keeps its one error as a private member. That is the right shape when a code has
a single caller. A code with two callers earns a named static
class in the owning project - in a product, that is `src/api/Results/`.
