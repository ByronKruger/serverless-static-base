# src/templates - Platform.Api???

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

## Adding an endpoint TODO: goes in consumer.

TODO: no vertial slicing - we do horizontal slice (so use that for template). Follow the vertical slice recipe in `Docs/ARCHITECTURE.md` - extend the existing
`{Resource}Controller` / `I{Resource}Service` pair rather than creating a class per verb.
Every endpoint needs a service unit test in `tests/Application.Tests/Services/` and an HTTP
test in `tests/Api.Tests/Controllers/`.

Everything in "Defaults a consuming host inherits" in `src/platform/AGENTS.md` applies and
should not be re-implemented - the default-deny fallback policy, the problem-details error
model, the validation filter, the JSON contract.

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
TODO: remove, not useful here this is consumer specific

```bash
dotnet build                                   # from the repo root
dotnet test
dotnet run --project src/api                   # http://localhost:5237, https://localhost:7011
dotnet ef migrations add <Name> --project src/api --output-dir Data/Migrations
dotnet ef database update --project src/api
```

TODO: move secrets' content to consumer and specify that the lib expects it to be populated by consumers on app init.Secrets go in user secrets (`dotnet user-secrets --project src/api set ...`), not in
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
