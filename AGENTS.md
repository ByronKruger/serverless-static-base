# Platform Agent Instructions

This file is the shared source of truth for repo conventions for all agents. Keep it short and use it as a map to the deeper project and domain documentation.

This repository **is** the library. It is not a product and it holds no product: a .NET 9 class library of the cross-cutting machinery of an ASP.NET Core Web API, nullable enabled, warnings as errors (D-0008, D-0010). There is no reference host and no client application here - an API implementation that consumes the library lives in its own repository. The library names no product noun anywhere, including in its comments.

Two projects, and only two:

- **`HopeTech.Common`** - the library, at the repository root. A runtime dependency: a consumer takes it by `PackageReference`.
- **`src/templates`** - `HopeTech.Common.Templates`, a collection of `dotnet new` **item** templates that scaffold the directory structures and code files an API built on the library keeps re-deriving. Not a runtime dependency; a consumer installs it with `dotnet new install` (D-0011).

Templates cannot live inside the library package. The template engine only loads packages whose `PackageType` is `Template`, so a consumer referencing the library would have the files on disk and `dotnet new` would still not see them. That is why there is a second project rather than a folder.

## Required Reading

- Use `Docs/domain/README.md` to select the relevant domain file before making business-rule changes.
- When editing inside a project folder, also read that folder's per-project `AGENTS.md` sibling (in addition to this root file).
- For complex work, follow the Superpowers planning flow and write future specs/plans under `Docs/superpowers/`.

## Repository Map

| Area | Purpose |
| --- | --- |
| `HopeTech.Common/` | The reusable library: auth, authz, problem details, result pattern, options, pipeline |
| `src/templates/` | `HopeTech.Common.Templates`: `dotnet new` item templates for the library's consumers |
| `tests/HopeTech.Common.Tests/` | The library's own tests. Fake ports only - no ASP.NET, no `DbContext`, no database |
| `Docs/domain` | System-of-record business and implementation knowledge |
| `Docs/superpowers` | Current Superpowers specs and implementation plans for agent-driven work |
| `Docs/ARCHITECTURE.md` | The locks every change must respect |
| `Docs/DECISIONS.md` | Architecture decision records |
| `CHANGELOG.md` | What changed for a consumer, per release |

## Where things go

### `HopeTech.Common/` - the library

| Folder | Holds | Never holds |
| --- | --- | --- |
| `Identity/` | `AppUser`, `AppRole`, their configurations, `HopeTechIdentityDbContext`, `IdentityUserRepository` | A product's entities, or any migration |
| `Abstractions/` | Ports: `IClock`, `ICurrentUser`, `IAccessTokenFactory`, `IUserRepository`, `IAuthService` | Implementations |
| `Authorization/` | `Policies`, `Roles` name constants | Ad-hoc role strings anywhere else |
| `Errors/` | `GlobalExceptionHandler` | Control flow for expected misses |
| `Extensions/` | `AddHopeTechApi<TDbContext>`, `UseHopeTechApiPipeline`, and the `Add*` each composes | Anything a product registers |
| `Http/` | `ApiControllerBase`, `ResultExtensions`, `HttpContextCurrentUser`, `KebabCaseParameterTransformer` | Controllers (D-0008) |
| `Mapping/` | AutoMapper profiles for the library's own contracts | |
| `Options/` | Options classes with validation attributes, and their configurators | |
| `Results/` | `Result`, `Result<T>`, `Error`, `ErrorType` | A product's error-code classes |
| `Services/` | `AuthService` | A product's services |
| `Time/` | `SystemClock` | |
| `Tokens/` | `JwtAccessTokenFactory` | |
| `Validation/` | The library's validators, plus `FluentValidationFilter` | Validators over EF entities |
| `Contracts/` | The request/response types the library owns | A product's DTOs |

Two things the library deliberately does **not** ship, each argued in D-0008:

- **Controllers.** A library can ship them through an `ApplicationPart`, and this one does
  not: route templates, `[EndpointName]` values and `[ProducesResponseType]` sets are a
  product's public HTTP contract, and a library owning them makes that contract a function
  of a dependency version. `ApiControllerBase` is the part worth sharing.
- **Migrations.** Two consumers have different tables and cannot share a
  `__EFMigrationsHistory`. The library ships the entity types and their
  `IEntityTypeConfiguration<T>`s for database tables (entities) that cover cross-cutting
  concerns; the product scaffolds the migration.

TODO: remove (becomes self-explanatory)
A third thing it no longer ships: **a reference host**. What a product writes - `Program.cs`,
its `DbContext`, its controllers - is now a template under `src/templates/items/`, not a
project in this repository (D-0010). The host shape is still version-controlled; it is just
version-controlled as something a consumer can run rather than something this solution builds.

### `src/templates/` - the item templates

One directory per template under `src/templates/items/`, each with its own
`.template.config/template.json`. See `src/templates/AGENTS.md` before adding or editing one.
Template bodies are **not compiled by this repository** - the project excludes them from
`Compile` on purpose - so a change to one is only verified by installing the pack and
scaffolding into a throwaway project. `src/templates/AGENTS.md` has the loop.

## Architecture Invariants

The library names no product noun anywhere, including in its comments. The library never
pollutes its domain with that of a specific consumer product API. Nothing in this repository
consumes the library except `tests/HopeTech.Common.Tests`, and those tests exercise the
library's own types - they are not an example API.

## Harness Commands

```bash
dotnet build                 # the solution, warnings as errors
dotnet test                  # the library's tests
dotnet pack -c Release -o artifacts   # both packages
```

Verifying a template change needs its own loop, because nothing in `dotnet build` covers it:

```bash
dotnet pack src/templates/HopeTech.Common.Templates.csproj -o <tmp>
dotnet new install <tmp>/HopeTech.Common.Templates.1.0.0.nupkg
# scaffold into a throwaway restored web project, then build it
dotnet new uninstall HopeTech.Common.Templates
```

## Documentation Rules

- This file (`AGENTS.md`) is a table of contents and guardrail file, not an encyclopedia.
- Domain knowledge belongs in `Docs/domain`, indexed by `Docs/domain/README.md`.
- Accepted architecture-wide decisions belong in `Docs/ARCHITECTURE.md`.
- Architecture decision records live in `Docs/DECISIONS.md` (indexed by the same file).
- When docs and code disagree, trust the code, fix the docs, and mention the drift.
- Whenever a change is of the nature of cross-cutting concerns for ASP.NET APIs - it changes
  the consumer contract, adds new functionality, and so on - record it in the consumer guide
  (`README.md`) and in `CHANGELOG.md`.
- Alter configuration (the `Authentication` / `Cors` / `Database` sections), public surface
  registrations, item templates, and the quick-start content in `README.md` whenever new
  cross-cutting functionality is added or changes in a way a consumer would want to know
  about. A change to the public surface that is not reflected in an item template is an
  incomplete change.

## Planning Rules

- Use an ephemeral plan in the chat for small, local changes.
- Use Superpowers planning for multi-step work, cross-project work, risky domain changes, or changes that need handoff.
- Put new design specs under `Docs/superpowers/specs/` and implementation plans under `Docs/superpowers/plans/`.
- Keep plans updated with decisions, verification commands, and open questions.

## Superpowers stop condition

- After requirements/grill, write a spec. After spec approval, write a plan. Stop. Do not invoke executing-plans, subagent-driven-development, bootstrap skills, or application code until the human has read that plan file and replied with an explicit go (e.g. "execute the plan" plus an execution method). Existence of `Docs/superpowers/plans/*.md` is not authorization.

## Architecture decisions

- This file (`AGENTS.md`) does not record decisions. It only tells you how to read and write them.
- Architecture decision records live in `Docs/DECISIONS.md`, and are indexed by the same file.

### Before you change structure, stack or contracts

1. Read all of `Docs/DECISIONS.md`.
2. Treat every entry with status `Accepted` as a constraint. Do not silently contradict it.
3. If the task would violate an accepted decision, stop. Either
    - Implement inside the accepted decision, or
    - Propose a new entry that supersedes the old one and wait for a human to accept it.
4. If `DECISIONS.md` and the code disagree, the code is automatically right. Flag the mismatch; do not "fix" the log to match a drift unless the human asked you to.

### When you must write a decision

Write a new entry in `DECISIONS.md` when the work does any of the following:
- Chooses or changes a framework, language, runtime, or major library (API, ORM, auth, messaging, hosting, etc.).
- Sets a cross-cutting convention: folder layout, API shape, error model, auth scheme, naming, layering, mapping style.
- Changes what the library hands a consumer, or what a consumer must write for itself.
- Accepts a durable trade-off (consistency vs availability, code-first vs schema-first, sync vs async).
- Rejects a plausible alternative that a future agent would otherwise re-propose.

Do **not** write an entry for: typo fixes, local refactors that stay inside an accepted decision, adding a feature that follows existing patterns, or one-off implementation details.

If you are unsure whether it is a decision, write the entry as `Proposed` and ask the human. Do not invent an `Accepted` decision.

### How to append an entry

- Never rewrite history. Do not edit the Decision, Context or Consequence of an `Accepted` entry except to add a `Superseded by D-NNNN` line. Identifiers inside a historical entry stay as they were written, even after a rename; the entry that performed the rename carries the mapping.
- New work that replaces an old choice: add a new numbered entry, set the old entry to `Superseded by D-NNNN`, set the new entry to `Accepted` only if the human confirmed it; otherwise leave it `Proposed`.
- Assign next free `D-NNNN`. Do not reuse ids.
- Newest entries go at the **bottom**.
- Keep the index table at the top of `DECISIONS.md` in sync (id, title, status, date).
- One decision per entry. Split if you are smuggling two or more choices into one record.
- Fill every required heading in the template already in `DECISIONS.md`. Empty "N/A" sections are worse than omitting an optional heading; omit only what the template marks optional.
- Prefer a short Y-statement in the Decision section: `In the context of <situation>, facing <concern>, we chose <option> to achieve <goal>, accepting <trade-off>.`

### Status values

Use only: `Proposed`, `Accepted`, `Deprecated`, `Superseded by D-NNNN`, `Rejected`.

Agents may create `Proposed` entries. Agents may not mark an entry `Accepted` unless the user explicitly accepted it in this session or the entry is a mechanical split of an already-accepted decision during a later migration to `adr/`.

### What not to do

- Do not duplicate decision text into `AGENTS.md`, `README.md`, or code comments as the source of truth.
- Do not create `adr/`, `docs/adr`, or extra decision files while this phase is active.
- Do not log session scratch, task status, or "we will do X next" here. That is not a decision.

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
a single caller. A code with two callers earns a named static class in the owning project -
in a consumer product, that is its own `Results/` folder, which is what the `htc-resource`
item template scaffolds.
