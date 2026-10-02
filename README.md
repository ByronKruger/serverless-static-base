# Platform

A base for new API projects. `src/platform/Platform.Common` is a reusable C# library holding
the code every ASP.NET Core API re-derives - authentication, authorization, the error model,
the result pattern, validated options, the request pipeline - and `src/api` is a reference
host that consumes it and shows what a product writes for itself (D-0008).

What arrives as a dependency, already wired and tested:

- **Accounts and sign-in** on ASP.NET Identity, keyed by an int surrogate with email as the
  natural key and the sign-in credential.
- **Self-issued JWTs**: one symmetric key signs and validates, with issuer, audience,
  lifetime and claim types as validated configuration.
- **RBAC**: three seeded roles, named policies, and a default-deny fallback so an endpoint is
  authenticated unless it opts out.
- **RFC 9457 problem details** for every failure, with `traceId` and a stable machine-readable
  `code`.
- **The result pattern**: expected failure is data (`Result<T>`, `Error`, `ErrorType`),
  unexpected failure is an exception caught once at the edge.
- **Edge validation** through a FluentValidation MVC filter, and the camelCase / string-enums
  / UTC JSON contract.
- **User search** over username, first name and surname.

What a product writes: `Program.cs` (two statements), a `DbContext`, its migrations, and its
controllers.

> `src/web/` has **not** been extracted. It is still the Angular client of the beverage
> preferences project this base was carved out of, and stripping it to a reusable shell is
> the first job in a fork. See `Docs/superpowers/specs/2026-09-30-platform-library-extraction.md`.

## Layout

```
src/platform/Platform.Common/  The reusable library                -> src/platform/AGENTS.md
src/api/                       Reference ASP.NET host (.NET 9)      -> src/api/AGENTS.md
src/web/                       Angular 22 SPA - not yet extracted   -> src/web/AGENTS.md
tests/Application.Tests/       Service tests, fake ports only
tests/Api.Tests/               HTTP tests over the real pipeline
Docs/ARCHITECTURE.md           The locks every change must respect
Docs/DECISIONS.md              Why the stack looks like this
Docs/domain/                   Business rules, indexed by its README
AGENTS.md                      Conventions for agents and humans
```

## Prerequisites

| Tool | Version | Notes |
| --- | --- | --- |
| .NET SDK | 9.0.3xx | `net9.0` is pinned in `Directory.Build.props` |
| Node.js | 22.22.3+ | Angular 22 refuses to run below this |
| npm | 11+ | npm 10 cannot resolve this workspace's peer graph |
| SQL Server | LocalDB or any instance | Needed to run the API; not needed to build or test it |

With `nvm` for Windows:

```bash
nvm install 22
nvm use 22
npm install -g npm@11
```

## Running it

```bash
# API - http://localhost:5237, https://localhost:7011, OpenAPI at /openapi/v1.json
dotnet run --project src/api

# Web - http://localhost:4200, with /api proxied to the API
cd src/web && npm install && npm start
```

## Verifying it

```bash
dotnet build                 # solution, warnings as errors
dotnet test                  # service tests + API integration tests
cd src/web && npm run build  # production bundle
cd src/web && npm test       # component tests
```

The .NET suite needs no database: `tests/Api.Tests` boots the real pipeline against the
in-memory provider.

## Configuration

The API validates its options at startup, so a missing secret stops the process rather than
failing on the first request.

The only real secret for local development is the JWT signing key - it must be at least 32
characters, and the API both signs and validates with it:

```bash
dotnet user-secrets --project src/api set "Authentication:Jwt:SigningKey" "<32+ characters>"
```

The secret store is keyed on `UserSecretsId`, which this project renamed to `platform-api`.
Anything set under an earlier id is invisible here, so run that command once even on a machine
that had secrets before. `dotnet build`, `dotnet test` and `dotnet ef migrations add` all work
without it; `dotnet run` does not start.

The LocalDB connection string, issuer and audience are non-secret and set in
`src/api/appsettings.Development.json`, pointing at a `PlatformAI` database. Create the schema
before first run:

```bash
dotnet ef database update --project src/api
```

The web application reads `src/web/public/config.json` at runtime, before bootstrap, so a
single built artifact can be promoted across environments.

`NuGet.config` pins the feeds for restore. The private `CoffeegCommon` feed is listed but
disabled: enable it and authenticate with the Azure Artifacts Credential Provider when you
need a package from it.

## Starting a product from this

1. Fork it, then pick a real name and replace `Platform` throughout - assemblies, namespaces,
   the solution, and `UserSecretsId` in `src/api/Platform.Api.csproj`. Changing that id
   orphans whatever is in the old secret store, so re-set the signing key afterwards.
2. Strip `src/web/` to a shell, or delete it.
3. Add your tables to `AppDbContext`, scaffold a migration, and write your first controller
   following `Docs/ARCHITECTURE.md`'s vertical slice recipe.
4. Record what you change about the structure in `Docs/DECISIONS.md`. D-0008 and D-0009 are
   the two entries that describe this split; D-0001 to D-0007 are the platform decisions
   (stack, auth, Identity) kept from the project it came from - the beverage-domain and
   user-search entries were removed when the libraries were extracted.
