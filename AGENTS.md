# Platform Agent Instructions

This file is the shared source of truth for repo conventions for all agents. Keep it short and use it as a map to the deeper project and domain documentation.

This repository is a **base for new API projects**, not a product. A .NET 9 class library, nullable enabled, warnings as errors. It holds the cross-cutting
machinery of an ASP.NET Core Web API and nothing else (D-0008). The cross-cutting concerns live in `src/platform/Platform.Common` and `src/templates` Company.COmmon.Templates is a project that is a collection of dotnet item templates for commonly occuring work pieces in ASP.NET API development. 
The library names no product noun anywhere, including in its comments.

## Required Reading

- Use `Docs/domain/README.md` to select the relevant domain file before making business-rule changes.
- When editing inside a project folder, also read that folder's per-project `AGENTS.md` sibling (in addition to this root file).
- For complex work, follow the Superpowers planning flow and write future specs/plans under `Docs/superpowers/`.

## Repository Map

| Area | Purpose |
| `Docs/domain` | System-of-record business and implementation knowledge |
| `Docs/superpowers` | Current Superpowers specs and implementation plans for agent-driven work |
| `src/platform/Platform.Common` | The reusable library: auth, authz, problem details, result pattern, options, pipeline |
| `src/platform/Platform.Template` | Reference ASP.NET host over the library, and the collection of dotnet item templates for a product |

## Where things go

| Folder | Holds | Never holds |
| --- | --- | --- |
| `Identity/` | `AppUser`, `AppRole`, their configurations, `PlatformIdentityDbContext`, `IdentityUserRepository` | A product's entities, or any migration |
| `Abstractions/` | Ports: `IClock`, `ICurrentUser`, `IAccessTokenFactory`, `IUserRepository`, `IAuthService` | Implementations |
| `Authorization/` | `Policies`, `Roles` name constants | Ad-hoc role strings anywhere else |
| `Errors/` | `GlobalExceptionHandler` | Control flow for expected misses |
| `Extensions/` | `AddPlatformApi<TDbContext>`, `UsePlatformApiPipeline`, and the `Add*` each composes | Anything a product registers |
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
  `IEntityTypeConfiguration<T>`s for database table (entities) that cover cross-cutting concerns; the product scaffolds the migration.


## Architecture Invariants

The library names no product noun anywhere, including in its comments. The library never pollutes it's domain with that of a specific consumer product API.

## Harness Commands

- Standard command usage for both `src/api`. ASP.NET: 'dotnet' based commands, e.g. 'dotnet run', 'dotnet build', EF ('dotnet-ef data migrations add', 'dotnet-ef database update').

## Documentation Rules

- This file (`AGENTS.md`) is a table of contents and guardrail file, not an encyclopedia.
- Domain knowledge belongs in `Docs/domain`, indexed by `Docs/domain/README.md`.
- Accepted architecture-wide decisions belong in `Docs/ARCHITECTURE.md`
- Architecture decision records live in `Docs/DECISIONS.md` (indexed by the same file).
- When docs and code disagree, trust the code, fix the docs, and mention the drift.
- Whenever a change that is of the nature of cross-cutting concerns for ASP.NET APIs of the following nature: changes the consumer contract, adds new functionality, etc. Add this change/addition to the consumer guide and `README.md` and `CHANGELOG.md` respectively.
- Alter configuration ("CompanyCommon" json object), public surface registrations, item templates and example usage and quick start guide content whenever new cross-cutting concern functionality is added or changes in a way that would be useful for consumers to know about the changes in the Library Readme.

## Planning Rules

- Use an ephemeral plan in the chat for small, local changes.
- Use Superpowers planning for multi-step work, cross-project work, risky domain changes, or changes that need handoff.
- Put new design specs under `Docs/superpowers/specs/` and implementation plans under `Docs/superpowers/plans/`.
- Keep plans updated with decisions, verification commands, and open questions.

## Superpowers stop condition

- After requirements/grill, write a spec. After spec approval, write a plan. Stop. Do not invoke executing-plans, subagent-driven-development, bootstrap skills, or application code until the human has read that plan file and replied with an explicit go (e.g. “execute the plan” plus an execution method). Existence of `Docs/superpowers/plans/*.md` is not authorization.

## Architecture decisions

- This file (`AGENTS.md`) does not record decisions. It only tells you how to read and write them.
- Architecture decision records live in `Docs/DECISIONS.md`, and is also indexed by the same file (`Docs/DECISION.md`).

### Before you change structure, stack or contracts

1. Read all of `Docs/DECISIONS.md`.
2. Treat every entry with status `Accepted` as a constraint. Do not silently contradict it.
3. If the task would violate an accepted decision, stop. Either
    - Implement inside the accepted decision, or
    - Propose a new entry that supersedes the old one and wait for a human to accept it.
4. If `DECISIONS.md` and the code disagree, the code is automatically right. Flag the mismatch; do not "fix" the log to match a drift unless the human asked you to.

### When you must write a decision

Write a new entry in `DECISIONS.md` when the work does any of the following:
- Chooses or changes a framework, language, runtime, or major library (UI, API, ORM, auth, messaging, hosting, etc.).
- Sets a cross-cutting convention: folder layout, API shape, error model, auth scheme, naming, layering, mapping style.
- Binds the frontend and API together (DTO ownership, versioning, CORS, BFF vs direct calls, OpenAPI generation).
- Accepts a durable trade-off (consistency vs availability, code-first vs schema-first, SSR vs SPA, sync vs async).
- Rejects a plausible alternative that a future agent would otherwise re-propose.

Do **not** write an entry for: typo fixes, local refactors that stay inside an accepted decision, adding a feature that follows existing patterns, or one-off implementation details.

If you are unsure whether it is a decision, write the entry as `Proposed` and ask the human. Do not invent an `Accepted` decision.

### How to append an entry

- Never rewrite history. Do not edit the Decision, Context or Consequence of an `Accepted` entry except to add a `Superseded by D-NNNN` line.
- New work that replaces an old choice: add a new numbered entry, set the old entry to `Superseded by D-NNNN`, set the new entry to `Accepted` only if the human confirmed it; otherwise leave it `Proposed`.
- Assign next free `D-NNNN`. Do not reuse ids.
- Newest entries go at the **bottom**.
- Keep the index table at the top of `DECISIONS.md` in async (id, title, status, date).
- One decision per entry. Split if you are smuggling two or more choices into one record.
- Fill every required heading in the template already in `DECISIONS.md`. Empty "N/A" sections are worse than omitting an optional heading; omit only what the template marks optional.
- Prefer a short Y-statement in the Decision section: `In the context of <situation>, facing <concern>, we chose <option> to achieve <goal>, accepting <trade-off>.`

### Status values

Use only: `Proposed`, `Accepted`, `Deprecated`, `Superseded by D-NNNN`, `Rejected`.

Agents may create `Proposed` entries. Agents may not mark an entry `Accepted` unless the user explicitly accepted it in this session or the entry is a mechanical split of an already-accepted decision during a later migration to `adr/`

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
a single caller. A code with two callers earns a named static
class in the owning project - in a product, that is `src/api/Results/`.
