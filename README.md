# HopeTech.Common

The code every ASP.NET Core Web API re-derives, as a dependency instead of a copy:
authentication, authorization, the error model, the result pattern, validated options, and
the request pipeline.

This repository is the library. It holds no product and no example application - what a
product writes for itself is an item template you run, not a project you read (D-0010).

| Package | What it is | How you take it |
| --- | --- | --- |
| `HopeTech.Common` | The library. A runtime dependency. | `dotnet add package HopeTech.Common` |
| `HopeTech.Common.Templates` | `dotnet new` item templates. Not a runtime dependency. | `dotnet new install HopeTech.Common.Templates` |

## What arrives as a dependency, already wired and tested

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

## What you write

`Program.cs` (two statements), a `DbContext`, its migrations, and your controllers. The
templates scaffold all four.

## Quick start

```bash
# 1. A controllers-based host project, restored - the templates read RootNamespace from it.
mkdir Acme.Orders && cd Acme.Orders
dotnet new web -n Acme.Orders -o .
dotnet add package HopeTech.Common
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet restore

# 2. The composition root, the product DbContext, settings, and a health endpoint.
#    --force, because `dotnet new web` already wrote Program.cs and appsettings.json.
dotnet new install HopeTech.Common.Templates
dotnet new htc-host --dbContext OrdersDbContext --audience acme-orders --database AcmeOrders --force

# 3. The signing key. The API both signs and validates with it, so it is a real secret,
#    and options validation stops the process when it is missing or under 32 characters.
dotnet user-secrets init
dotnet user-secrets set "Authentication:Jwt:SigningKey" "<32+ characters>"

# 4. Your schema. The library ships the Identity entity types; you own the migration.
dotnet ef migrations add InitialIdentity --output-dir Data/Migrations
dotnet ef database update

dotnet run
```

`GET /api/health` answers anonymously; `POST /api/auth/register` and `POST /api/auth/login`
are yours to add - the library ships `IAuthService` and the contracts, and you ship the
controller, because route templates and `[ProducesResponseType]` sets are your public HTTP
contract and not a function of a dependency version (D-0008).

Your `Program.cs` ends up as two statements:

```csharp
builder.Services.AddHopeTechApi<OrdersDbContext>(builder.Configuration, typeof(Program).Assembly);
...
app.UseHopeTechApiPipeline();
```

> **Pass your own assembly.** Validator, mapper-profile and entity-configuration discovery
> each scan one assembly at a time. Omit `typeof(Program).Assembly` and you get an API that
> starts, answers every route, and never validates a request - with nothing failing at
> startup to say so (D-0009).

## Adding a resource

```bash
dotnet new htc-resource -n Order      # controller, service pair, contracts, validator, error codes
dotnet new htc-entity   -n Order      # EF entity, its configuration, repository port and implementation
```

Then the three things a template cannot do for you, each named in a comment in the generated
code: add `public DbSet<Order> Orders => Set<Order>();` to your context, register the two
pairs in `Program.cs`, and scaffold a migration.

| Template | Emits |
| --- | --- |
| `htc-host` | `Program.cs`, the product `DbContext`, `appsettings*.json`, launch profiles, a health endpoint |
| `htc-resource` | A whole REST noun: controller, service pair, contracts, validator, error codes |
| `htc-contracts` | `Create{X}Request`, `{X}Response`, and the request validator |
| `htc-service` | `I{X}Service` and `{X}Service`, error codes as private members |
| `htc-controller` | A thin controller on `ApiControllerBase` |
| `htc-entity` | EF entity, `IEntityTypeConfiguration`, repository port and implementation |
| `htc-service-tests` | An xUnit fixture for one service, plus `FakeClock` and `FakeCurrentUser` |

`dotnet new <short-name> --help` lists every parameter. Two worth knowing:

- Templates read your namespace from the project's `RootNamespace`, which resolves **only in
  a restored project**. Scaffold into an unrestored one and the files come out in the
  `HopeTech.Api` placeholder namespace instead.
- Plurals are formed by appending `s`, so `-n Company` gives `CompanysController`. Rename it,
  and pass `--tableParameter Companies` to `htc-entity`.

## Configuration

Every section is bound, validated by data annotations, and `ValidateOnStart` - a missing
secret stops the process rather than failing on the first request.

```json
{
  "Authentication": {
    "Jwt": {
      "Issuer": "https://localhost:7011",
      "Audience": "acme-orders",
      "SigningKey": "<32+ characters - user secrets or a key vault, never appsettings>",
      "AccessTokenLifetimeMinutes": 480,
      "RoleClaimType": "roles",
      "UserIdClaimType": "sub"
    }
  },
  "Cors": {
    "AllowedOrigins": [ "http://localhost:4200" ]
  },
  "Database": {
    "ConnectionString": "Server=(localdb)\\MSSQLLocalDB;Database=AcmeOrders;Trusted_Connection=True;TrustServerCertificate=True",
    "CommandTimeoutSeconds": 30,
    "EnableSensitiveDataLogging": false
  }
}
```

`htc-host` writes the non-secret half of this for you, and leaves `SigningKey` out of
`appsettings.json` on purpose.

## Prerequisites

| Tool | Version | Notes |
| --- | --- | --- |
| .NET SDK | 9.0.3xx | `net9.0` is pinned in `Directory.Build.props` |
| SQL Server | LocalDB or any instance | Needed to run a consumer; not needed to build or test the library |

## Working on the library

```
HopeTech.Common/            The library                      -> HopeTech.Common/AGENTS.md
src/templates/              The item templates               -> src/templates/AGENTS.md
tests/HopeTech.Common.Tests/  The library's own tests, fake ports only
Docs/ARCHITECTURE.md        The locks every change must respect
Docs/DECISIONS.md           Why the stack looks like this
Docs/domain/                Business rules, indexed by its README
AGENTS.md                   Conventions for agents and humans
CHANGELOG.md                What changed for a consumer
```

```bash
dotnet build                          # solution, warnings as errors
dotnet test                           # the library's tests - no database needed
dotnet pack -c Release -o artifacts   # both packages
```

A template change is **not** covered by `dotnet build`: template bodies are content, excluded
from compilation on purpose. `src/templates/AGENTS.md` has the pack-install-scaffold-build
loop that does verify one.

`NuGet.config` pins the feeds for restore. The private feed listed there is disabled by
default: enable it and authenticate with the Azure Artifacts Credential Provider when you need
a package from it.
