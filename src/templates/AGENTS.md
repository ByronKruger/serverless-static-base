# src/templates - HopeTech.Common.Templates

`dotnet new` **item** templates for an API built on `HopeTech.Common`. Read the root
`AGENTS.md` first; this file covers only what is specific to this project.

## Why this is a second project

The template engine loads a package only if its `PackageType` is `Template`. Put these files
inside `HopeTech.Common` and a consumer would have them on disk and `dotnet new` would still
not see them. A `PackageType=Template` package also carries no `lib/`, so it cannot be the
runtime dependency - hence two packages, one referenced and one installed (D-0011).

## Layout

```
src/templates/
  HopeTech.Common.Templates.csproj   # PackageType=Template, content-only, Compile removed
  items/
    <name>/
      .template.config/template.json
      <the files the template emits, in the folders it emits them into>
```

`items/**` is `Content`, and `<Compile Remove="**\*" />` keeps it out of this project's
compilation. That is deliberate: a template body references namespaces this project does not
have, and is allowed to be a stub. The consequence is that **`dotnet build` proves nothing
about a template**. The only verification is the loop below.

## The verification loop

Nothing in `dotnet build` or `dotnet test` covers a template. Run this after any change to
`items/`:

```bash
TMP=$(mktemp -d)
dotnet pack src/templates/HopeTech.Common.Templates.csproj -o "$TMP/pkg"
dotnet new install "$TMP/pkg/HopeTech.Common.Templates.1.0.0.nupkg"

mkdir "$TMP/probe" && cd "$TMP/probe"
dotnet new web -n Acme.Probe -o .
dotnet add reference <path>/HopeTech.Common/HopeTech.Common.csproj
dotnet restore                       # required - see "The namespace bind" below
dotnet new htc-host --dbContext ProbeDbContext --force
dotnet new htc-resource -n Order
dotnet new htc-entity -n Order --dbContext ProbeDbContext
dotnet build                         # this is the assertion

cd - && dotnet new uninstall HopeTech.Common.Templates
```

Every template in `items/` is expected to produce code that **compiles** when its stated
prerequisites are met. `htc-service` and `htc-controller` name contracts they do not emit, so
their prerequisite is `htc-contracts` for the same resource - that composition is part of
what the loop has to check.

## Three things the engine does that will catch you out

1. **The namespace bind needs a restored project.**
   `{"type": "bind", "binding": "msbuild:RootNamespace"}` is how every template picks up the
   consumer's namespace. It resolves by evaluating the nearest project - and in a project
   that has never been restored it resolves to nothing, so the symbol falls back to its
   `defaultValue` and the generated files come out in the `HopeTech.Api` placeholder
   namespace, silently. Scaffolding into an unrestored project is the single most likely
   cause of a "the template is broken" report.

2. **`-n` on an item template defaults to the output directory name, not `defaultName`.**
   So `sourceName` is only safe when `-n` is the thing the user obviously means - a resource
   name. `htc-host` therefore has **no `sourceName`**: it takes the context name as an
   explicit `--dbContext` parameter with `fileRename`, because a context called after
   whatever folder the user happened to be in is worse than a wrong default.

3. **Overlapping replacement tokens are a trap.** `sourceName: "Widget"` also rewrites
   `Widgets`, which is exactly why the plural comes out right (`Widget` -> `Order` gives
   `WidgetsController` -> `OrdersController`). It also means you cannot add a second symbol
   whose token contains `Widget`. Irregular plurals are the accepted cost: the templates
   pluralise by appending `s`, and `--tableParameter` on `htc-entity` exists so at least the
   table name can be fixed without an edit.

## Adding a template

- One directory under `items/`, named for the short name without the `htc-` prefix.
- `identity` and `groupIdentity` are `HopeTech.Common.Templates.<Thing>`; `shortName` is
  `htc-<thing>`; `tags.type` is `item`.
- Emit files into the folders they belong in (`Controllers/`, `Dtos/`, `Validation/<Resource>/`),
  not at the template root. An item template writes relative to the invocation directory, so
  nesting in the template is what makes the output land correctly from the project root.
- Carry a `rootNamespace` bind symbol and use `HopeTech.Api` as the placeholder namespace, so
  every template behaves the same way.
- No `postActions`. The display-instructions action is reported as unsupported by the .NET 9
  CLI and prints an error-shaped line, and it does not substitute `sourceName` into its text -
  so guidance that a reader needs goes into the generated file as a comment, where it is also
  still there tomorrow.
- Bump `PackageVersion` in the csproj when the emitted code changes, and say what changed in
  `CHANGELOG.md`.
- Run the loop above. A template that has not been scaffolded and built has not been tested.

## The templates, and what each is for

| Short name | Emits | Prerequisite |
| --- | --- | --- |
| `htc-host` | `Program.cs`, the product `DbContext`, `appsettings*.json`, launch profiles, a health endpoint | An empty `Microsoft.NET.Sdk.Web` project; needs `--force` to replace what `dotnet new web` left |
| `htc-resource` | A whole REST noun: controller, service pair, contracts, validator, error codes | - |
| `htc-contracts` | `Create{X}Request`, `{X}Response`, and the request validator | - |
| `htc-service` | `I{X}Service` and `{X}Service`, with error codes as private members | `htc-contracts` for the same resource |
| `htc-controller` | A thin controller on `ApiControllerBase` | `htc-contracts` and a service for the same resource |
| `htc-entity` | EF entity, its `IEntityTypeConfiguration`, repository port and implementation | A `DbContext` deriving from `HopeTechIdentityDbContext` |
| `htc-service-tests` | An xUnit fixture for one service, plus `FakeClock` and `FakeCurrentUser` | An xUnit project referencing the API project |

`htc-resource` is the one-shot path; `htc-contracts` + `htc-service` + `htc-controller` is the
same output assembled a layer at a time, for a resource that already half exists.
