# HopeTech.Common.Templates

`dotnet new` item templates for an ASP.NET Core Web API built on
[`HopeTech.Common`](https://www.nuget.org/packages/HopeTech.Common). Not a runtime
dependency - install it, do not reference it.

```bash
dotnet new install HopeTech.Common.Templates
```

| Short name | Emits | Prerequisite |
| --- | --- | --- |
| `htc-host` | `Program.cs`, the product `DbContext`, `appsettings*.json`, launch profiles, register/sign-in and health endpoints | An empty `Microsoft.NET.Sdk.Web` project; pass `--force` to replace what `dotnet new web` wrote |
| `htc-resource` | A whole REST noun: controller, service pair, contracts, validator, error codes | - |
| `htc-contracts` | `Create{X}Request`, `{X}Response`, and the request validator | - |
| `htc-service` | `I{X}Service` and `{X}Service`, error codes as private members | `htc-contracts` for the same resource |
| `htc-controller` | A thin controller on `ApiControllerBase` | `htc-contracts` and a service for the same resource |
| `htc-entity` | EF entity, its `IEntityTypeConfiguration`, repository port and implementation | A `DbContext` deriving from `HopeTechIdentityDbContext` |
| `htc-service-tests` | An xUnit fixture for one service, plus `FakeClock` and `FakeCurrentUser` | An xUnit project referencing the API project |

```bash
mkdir Acme.Orders && cd Acme.Orders
dotnet new web -n Acme.Orders -o .
dotnet add package HopeTech.Common
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet restore

dotnet new htc-host --dbContext OrdersDbContext --audience acme-orders --database AcmeOrders --force
dotnet new htc-resource -n Order
dotnet new htc-entity   -n Order --dbContext OrdersDbContext
```

`dotnet new <short-name> --help` lists every parameter.

## Two things to know

- **Restore first.** Templates read your namespace from the project's `RootNamespace` through
  an MSBuild bind, which resolves only in a project that has been restored. Scaffold into an
  unrestored project and the files come out in the `HopeTech.Api` placeholder namespace
  instead, with nothing to tell you.
- **Plurals are naive.** The resource name is pluralised by appending `s`, so `-n Company`
  gives `CompanysController`. Rename it, and pass `--tableParameter Companies` to
  `htc-entity`.

`htc-resource` is the one-shot path. `htc-contracts` + `htc-service` + `htc-controller` is the
same output assembled a layer at a time, for a resource that already half exists.
