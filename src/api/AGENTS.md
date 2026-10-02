# src/api - Platform.Api

Read the root `AGENTS.md` first, then `src/platform/AGENTS.md` - almost everything this
project relies on is defined there. The rules live in `Docs/ARCHITECTURE.md` and the choices
in `Docs/DECISIONS.md`.

ASP.NET Core Web API, .NET 9, nullable enabled, warnings as errors. Not Minimal APIs
(D-0001). This project is **a reference host over `Platform.Common`, and the template for a
product**: it has no domain of its own, and the two controllers it carries exist to show
what a thin action looks like over a library service.

## Which project does a new file go in?

One question, and it is the whole boundary (D-0008):

> Would this file still make sense in an API that has never heard of this product's domain?

**Yes, and it is machinery** - `src/platform/Platform.Common`. **Yes, but it is this
product's** - here. A product noun in a type name, a member, or even a comment means it
belongs here, not there.

## Where things go

The library owns every folder in the left column of `src/platform/AGENTS.md`. This project
owns these, and only these:

| Folder | Holds | Never holds |
| --- | --- | --- |
| `Controllers/` | Thin actions on `ApiControllerBase`, ~15 lines each | Business logic, `DbContext`, EF entities |
| `Data/` | `AppDbContext`, `Configurations/`, `Migrations/` | Anything a controller can see |
| `Dtos/` | This product's `Create{X}Request`, `Update{X}Request`, `{X}Response`, `[FromQuery]` models | One shared DTO per resource |
| `Entities/` | This product's EF entities | API contracts |
| `Interfaces/` | This product's ports, in `Repositories/` and `Services/` | |
| `MapperProfiles/` | This product's AutoMapper profiles | |
| `Repositories/` | Port implementations over `AppDbContext` | Business rules |
| `Results/` | This product's error-code classes - only once a code has two callers | `Result`, `Error`, `ErrorType`, which are the library's |
| `Services/` | `{Resource}Service` - one public method per use case | SQL, `HttpContext` |
| `Validation/` | One `AbstractValidator<T>` per request type | Validators over EF entities |

Only `Controllers/`, `Data/` and `Dtos/` exist today. The rest appear the first time this
host grows a domain, and each means here exactly what it means in the library.

## Composition

`Program.cs` is the only composition root and stays two statements:

```csharp
builder.Services.AddPlatformApi<AppDbContext>(builder.Configuration, typeof(Program).Assembly);
// ...
app.UsePlatformApiPipeline();
```

**That second argument is load-bearing.** Validator and AutoMapper discovery scan one
assembly at a time, so without it the library scans only itself: this host's validators and
profiles are never registered, every route still answers, and nothing fails at startup to
say so (D-0009). The same shape of trap applies to entity configurations - see the three
traps in `src/platform/AGENTS.md`.

A product adds registrations to the library's `Add*` methods only if they are cross-cutting.
Its own services and repositories go in its own `Add*` extension in this project. Never in
`Program.cs`.

Pipeline order is fixed in the library's `Extensions/WebApplicationExtensions.cs`:
exception handler, status-code pages, OpenAPI (development only), HTTPS redirection, CORS,
authentication, authorization, controllers.

## Adding an endpoint

Follow the vertical slice recipe in `Docs/ARCHITECTURE.md` - extend the existing
`{Resource}Controller` / `I{Resource}Service` pair rather than creating a class per verb.
Every endpoint needs a service unit test in `tests/Application.Tests/Services/` and an HTTP
test in `tests/Api.Tests/Controllers/`.

Everything in "Defaults a consuming host inherits" in `src/platform/AGENTS.md` applies and
should not be re-implemented - the default-deny fallback policy, the problem-details error
model, the validation filter, the JSON contract.

## The two controllers here

- `AuthController` - register and sign in, both `[AllowAnonymous]` by necessity: they are how
  a caller obtains the token every other endpoint requires. Both return 200 rather than 201;
  what comes back is a session, not a representation of a created resource.
- `HealthController` - anonymous, and deliberately free of business meaning. It exists so
  hosts, smoke tests and the API suite have a stable endpoint.

Both are ~25 lines of binding over a library service. They are the shape to copy.

## The database

`AppDbContext : PlatformIdentityDbContext` and holds no `DbSet`. The product's tables go
here: a `DbSet` per aggregate, entities in `Entities/`, mappings in `Data/Configurations/`.

`Data/Migrations/` carries one scaffolded `InitialIdentity` creating the `AspNet*` tables and
seeding the three roles. Its `.editorconfig` marks the scaffolded files as generated; without
it the repo style rules fail the build on every new migration.

`dotnet ef migrations add` prints `No instantiatable types implementing
IEntityTypeConfiguration were found while scanning assembly 'Platform.Api'` while this host
has no tables of its own. That warning is the product's own configuration scan working, not
a fault.

## Commands

```bash
dotnet build                                   # from the repo root
dotnet test
dotnet run --project src/api                   # http://localhost:5237, https://localhost:7011
dotnet ef migrations add <Name> --project src/api --output-dir Data/Migrations
dotnet ef database update --project src/api
```

Secrets go in user secrets (`dotnet user-secrets --project src/api set ...`), not in
`appsettings.Development.json`. `Database:ConnectionString`, `Authentication:Jwt:Issuer`,
`Authentication:Jwt:Audience` and `Authentication:Jwt:SigningKey` are validated at startup:
if one is missing, or the key is under 32 characters, the process does not start.

`UserSecretsId` is `platform-api`, renamed with everything else. **The secrets set under the
old `coffeeg-api` id are not visible to this project**, so a fresh
`dotnet user-secrets set "Authentication:Jwt:SigningKey" "<32+ characters>"` is required
before `dotnet run` will start. `appsettings.Development.json` supplies the issuer, audience
and a `PlatformAI` connection string, so the signing key is the only one that must come from
secrets.

Nothing in the build or test path needs it: `dotnet build`, `dotnet test` and
`dotnet ef migrations add` all work without a secret store, because the test factory supplies
its own configuration in memory and `migrations add` never connects.
