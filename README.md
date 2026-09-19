# Coffeeg

Personal beverage preferences: each user saves what they want in a drink, one preference
per beverage type. See `Docs/domain/` for the business rules.

Implemented so far: the project setup (architecture, folder structure, composition roots,
test harnesses) and the first feature slice, **user registration and sign-in** - accounts
on ASP.NET Identity, a self-issued JWT, and `/auth/register` and `/auth/login` pages with
the signed-in username and a log-out button in the navigation header.

## Layout

```
src/api/                 ASP.NET Core Web API (.NET 9)          -> src/api/AGENTS.md
src/web/                 Angular 22 SPA                         -> src/web/AGENTS.md
tests/Application.Tests/ Service tests, fake ports only
tests/Api.Tests/         HTTP tests over the real pipeline
Docs/ARCHITECTURE.md     The locks every change must respect
Docs/DECISIONS.md        Why the stack looks like this
Docs/domain/             Business rules, indexed by its README
AGENTS.md                Conventions for agents and humans
```

## Prerequisites

| Tool | Version | Notes |
| --- | --- | --- |
| .NET SDK | 9.0.3xx | `net9.0` is pinned in `Directory.Build.props` |
| Node.js | 22.22.3+ | Angular 22 refuses to run below this |
| npm | 11+ | npm 10 cannot resolve this workspace's peer graph (D-0008) |
| SQL Server | LocalDB or any instance | Only needed once persistence is used |

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

## Configuration

The API validates its options at startup, so a missing secret stops the process rather
than failing on the first request.

The only real secret for local development is the JWT signing key - it must be at least
32 characters, and the API both signs and validates with it:

```bash
dotnet user-secrets --project src/api set "Authentication:Jwt:SigningKey" "<32+ characters>"
```

The LocalDB connection string, issuer and audience are non-secret and already set in
`src/api/appsettings.Development.json`, pointing at a `CoffeegAI` database. Create the
schema before first run:

```bash
dotnet ef database update --project src/api
```

The web application reads `src/web/public/config.json` at runtime, before bootstrap, so a
single built artifact can be promoted across environments.

`NuGet.config` pins the feeds for restore. The private `CoffeegCommon` feed is listed but
disabled: enable it and authenticate with the Azure Artifacts Credential Provider when you
need a package from it.
