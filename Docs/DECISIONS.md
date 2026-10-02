# Decisions

Single log for architecturally significant choices. Agents: read this file before changing strcture, stack, or contracts. See `AGENTS.md` and Architecture decisions.

When this file becomes hard to scan (roughly 8-15 accepted entries, or one entry needing its own options appendix), split into `adr/` per the migration notes in `AGENTS.md`. Until then, do not create other decision files.

## Index

| ID | Title | Status | Date |
| --- | --- | --- | --- |
| D-0001 | ASP.NET Core Web API host, composed so a serverless host stays reachable | Accepted | 2026-09-17 |
| D-0002 | SQL Server as the EF Core provider | Accepted | 2026-09-18 |
| D-0003 | AutoMapper 16.x as the mapping library | Accepted | 2026-09-18 |
| D-0004 | xUnit and NSubstitute as the .NET test stack | Accepted | 2026-09-18 |
| D-0005 | Users keyed by an int surrogate; email is the natural key and the sign-in credential | Accepted | 2026-09-18 |
| D-0006 | The API issues its own access tokens | Accepted | 2026-09-18 |
| D-0007 | First name and surname are required for new accounts, and nullable in the database | Accepted | 2026-09-20 |
| D-0008 | Cross-cutting API code lives in one `Platform.Common` library; the product owns controllers and migrations | Accepted | 2026-10-01 |
| D-0009 | Registration is generic over the product's `DbContext` and is told the product's assembly | Proposed | 2026-10-01 |

## Template (copy for each new entry)

D-NNNN - `<short title: problem + chosen option>`

Status: Proposed | Accepted | Deprecated | Superseded by D-NNNN | Rejected
Date: YYYY-MM-DD
Deciders: `<names or "proposed by agent, pending human">`

Context
- <2-4 sentences. What forces the choice. Link issues if any.>

Decision
- In the context of , facing , we chose  to achieve , accepting .

Options considered
 - ...
 - ...

Consequences
- Good: ...
- Bad: ...
- Follow-up: ...

---

## D-0001 - API host: ASP.NET Core Web API, composed so a serverless host stays reachable

Status: Accepted
Date: 2026-09-17
Deciders: ByronKruger

Context
- `Docs/ARCHITECTURE.md` locks the backend to thin MVC controllers, `Program.cs` as the only composition root, `WebApplicationFactory` integration tests, and explicitly rules out Minimal APIs. An earlier entry in this log committed the backend to an "ASP.NET Functions Application" instead. Both could not hold: Azure Functions has no MVC controller pipeline and no `WebApplicationFactory`.
- The repository name (`serverless-static-base-2`) suggests a static-web-app plus functions deployment was once the intent, so the pull towards serverless is real and will come back.

Decision
- In the context of scaffolding the backend, facing a direct conflict between the retired Functions entry and the architecture locks, we chose an ASP.NET Core Web API whose registration and pipeline each sit behind a single hosting-agnostic extension method to achieve a host that satisfies every architecture lock today while keeping a move to a serverless host a change to the host project only, accepting that controllers and the MVC filter pipeline would still have to be rewritten if that move ever happens.

Options considered
- ASP.NET Core Web API with hosting-agnostic composition (chosen).
- ASP.NET Core Web API with composition inlined in `Program.cs`: simpler, but a later serverless move means re-deriving every registration.
- Azure Functions isolated worker: honours the retired entry's title, but contradicts the controller, pipeline and testing locks, which are the more detailed and more recent statement.

Consequences
- Good: every lock in `Docs/ARCHITECTURE.md` is satisfiable as written, and integration tests run the real pipeline.
- Good: `Program.cs` is two statements, so the cost of re-hosting is bounded and visible.
- Bad: the repository name now describes an intent the code does not follow.
- Follow-up: rename the repository, or plan the move to Functions that this entry keeps cheap.

---

## D-0002 - SQL Server as the EF Core provider

Status: Accepted
Date: 2026-09-18
Deciders: ByronKruger

Context
- The architecture document locks code-first EF Core behind an infrastructure boundary but names no database. A provider has to be chosen before the first migration exists, and it is expensive to change after one.

Decision
- In the context of standing up persistence, facing an unspecified database, we chose `Microsoft.EntityFrameworkCore.SqlServer` with retry-on-failure and a configured command timeout to achieve the default that matches an Azure-hosted ASP.NET stack, accepting that a later move to PostgreSQL means regenerating migrations.

Options considered
- SQL Server / Azure SQL (chosen).
- PostgreSQL via Npgsql: cheaper to host, but no other signal in the repository points to it.
- Deferring the choice: not possible, because `AddDbContext` needs a provider to compile.

Consequences
- Good: LocalDB works out of the box for local development, and the connection string is the only environment-specific input.
- Bad: provider-specific behaviour (retry strategy, migrations history table) now lives in `Extensions/PersistenceExtensions.cs`.
- Follow-up: none. The first migration can be generated against this provider.

---

## D-0003 - AutoMapper 16.x as the mapping library

Status: Accepted
Date: 2026-09-18
Deciders: ByronKruger

Context
- `Docs/ARCHITECTURE.md` locks mapping to AutoMapper. AutoMapper 14.0.0 is the last MIT-licensed release, and every version below 15.1.1 carries GHSA-rvv3-g6hj-g44x, a high-severity uncontrolled-recursion denial of service. With warnings as errors, NuGet audit fails the build on it.
- AutoMapper 15 and later are published under a licence that is free below a revenue threshold and paid above it. This project is not a commercial product, so it sits under that threshold.

Decision
- In the context of choosing a mapping library version, facing a known high-severity advisory in every freely licensed release, we chose AutoMapper 16.2.0 to achieve a build with no unresolved vulnerability, accepting a licence that would need revisiting if this project were ever commercialised.

Options considered
- AutoMapper 16.2.0 (chosen).
- AutoMapper 14.0.0 with NU1903 suppressed: keeps the MIT licence, ships a known denial of service.
- Riok.Mapperly or hand-written mapping: MIT and allocation-free, but contradicts the AutoMapper lock, so it would need its own entry superseding that lock.

Consequences
- Good: `dotnet restore` passes NuGet audit with warnings as errors.
- Bad: the licence is conditional on the project staying non-commercial, which is a constraint that lives outside the code.
- Follow-up: if this project is ever commercialised, revisit this entry - either buy a licence or supersede it with the Mapperly option, updating the mapping lock in `Docs/ARCHITECTURE.md` at the same time.

---

## D-0004 - xUnit and NSubstitute as the .NET test stack

Status: Accepted
Date: 2026-09-18
Deciders: ByronKruger

Context
- The architecture document requires a handler unit test and an API integration test per endpoint, and names the two projects, but not the frameworks they use.

Decision
- In the context of setting up the test projects, facing an unspecified stack, we chose xUnit 2.9 with NSubstitute for fakes and `Microsoft.AspNetCore.Mvc.Testing` for HTTP-level tests to achieve the default ASP.NET testing toolchain, accepting that assertions are plain `Assert` calls rather than a fluent syntax.

Options considered
- xUnit and NSubstitute (chosen).
- xUnit and Moq: equally common, but Moq's 4.20 telemetry episode makes it a harder default to justify.
- Adding FluentAssertions: version 8 moved to a paid licence for commercial use, which is a cost this repository does not need to take on for readability alone.

Consequences
- Good: no licence questions and no extra concepts in the test projects.
- Bad: assertion failure messages are terser than a fluent library's.
- Follow-up: hand-written fakes live in `tests/Application.Tests/Fakes`; prefer them over a mocking framework when the port is small.

---

## D-0005 - Users keyed by an int surrogate; email is the natural key and the sign-in credential

Status: Accepted
Date: 2026-09-18
Deciders: ByronKruger

Context
- The brief for the first feature slice asked for two things that cannot both be literally true: email as the primary key of users, and `IdentityUser` with the `Id` field overridden to an integer primary key. A table has one primary key, and every Identity relationship table (`AspNetUserRoles`, `AspNetUserClaims`, `AspNetUserLogins`, `AspNetUserTokens`) is generic over that single key column and joins on it.
- Email is also a poor physical key on its own terms: it is mutable, it is personal data, and it would end up duplicated across five tables.
- What the brief was reaching for turned out to be two separate things: how a user row is keyed, and how a person identifies themselves when signing in. Separating them lets both answers be the right one.

Decision
- In the context of modelling users and signing them in, facing a brief that named both email and an int `Id` as the primary key, we chose `AppUser : IdentityUser<int>` with the int surrogate as the primary key, email as an enforced natural key - `RequireUniqueEmail` in Identity plus a unique `EmailIndex` on `NormalizedEmail` - and **email plus password as the sign-in credential**, to achieve a stable machine key for relationships and the identifier people actually remember for authentication, accepting that email is not the primary key column and that changing an address later means changing a credential.

Options considered
- Int surrogate key, unique email index, sign in with email (chosen).
- Email as the literal primary key (`IdentityUser<string>` with `Id` set to the email): matches one half of the brief, contradicts the other, and propagates a mutable personal-data string into every Identity join table.
- Composite key of id and email: satisfies neither requirement and breaks Identity's stores outright.
- Sign in with the username: what the first cut implemented. Rejected on user experience - a person reliably remembers the address they registered with, and less reliably the username they picked.

Consequences
- Good: two accounts cannot share an email, which was the substance of the request, and that same uniqueness is what makes email safe to authenticate against.
- Good: usernames stay separately unique through Identity's own `UserNameIndex`, matching "a secondary uniqueness element, not part of the key". The username is a display name - it is what the navigation header shows.
- Bad: code that reasons about "the user's key" has to know which key it means - `Id` for relationships, email for identity and sign-in.
- Bad: a future "change my email" feature changes a credential, not just a contact detail, so it will need the same care as a password change (re-authentication, and ideally verification of the new address).
- Follow-up: email verification is not implemented, so an address is trusted as typed. Worth doing before email carries anything more than sign-in.

---

## D-0006 - The API issues its own access tokens

Status: Accepted
Date: 2026-09-18
Deciders: ByronKruger (requested username and password authentication in session, 2026-09-18)

Context
- The API was scaffolded as a pure JWT bearer resource server: it validated tokens against an external authority's OIDC metadata and never issued any. Username-and-password authentication has no external authority to defer to, so somebody has to mint the token.

Decision
- In the context of adding password authentication, facing an API that could validate tokens but not create them, we chose to have the API sign and validate its own tokens with one symmetric HMAC-SHA256 key from configuration to achieve a self-contained login with no identity provider to stand up, accepting that the signing key is now a deployment secret and that the API is a single point of compromise for token issuance.

Options considered
- Self-issued symmetric-key JWTs (chosen).
- Self-issued asymmetric (RSA) JWTs: lets other services validate without the signing key, which is worth doing the moment a second service exists. Unnecessary while the API is both issuer and only audience.
- An external identity provider (Entra ID, Auth0, Keycloak): the right answer at scale, but a large dependency for a first slice, and it does not fit a brief that asks for local username and password accounts.

Consequences
- Good: `JwtOptions` carries the issuer, audience, key and lifetime, all validated at startup, so a missing or short key stops the process.
- Bad: the signing key must be distributed to every instance, and rotating it invalidates every live token at once.
- Bad: tokens cannot be revoked before expiry. Sign-out is client-side only; a denylist keyed on the `jti` claim is the way in when that is needed.
- Follow-up: move to asymmetric keys before a second service has to validate these tokens.

---

## D-0007 - First name and surname are required for new accounts, and nullable in the database

Status: Accepted
Date: 2026-09-20
Deciders: ByronKruger (chose in session, 2026-09-20)

Context
- User search has to match a person's real name, not only the handle they picked, so
  `AppUser` needs a first name and a surname. Three accounts already exist, two of them
  carrying saved records, and a non-nullable column cannot be added to a
  populated table without inventing values for those rows.
- "Required" and "not null" are separable: one is a rule about new registrations, the
  other is a rule about every row that has ever existed.

Decision
- In the context of collecting names at registration, facing a populated table that
  cannot take a non-nullable column, we chose to make both names **required by the
  registration validator** and **nullable in the schema** to achieve a guarantee that
  every account created from now on carries both, without inventing data for the accounts
  that predate them, accepting that the database now holds two classes of account and
  every reader of a name has to handle its absence.

Options considered
- Required at the edge, nullable in the schema (chosen).
- Required everywhere, with a backfill: one class of account and a simpler model, but the
  backfill can only invent placeholder names or delete real accounts with real
  records hanging off them.
- Optional everywhere: cheapest, but then search by surname silently misses whoever
  declined, and the feature's usefulness depends on a field nobody has to fill in.

Consequences
- Good: the migration is two `AddColumn` statements, no data repair, and a reversible
  `Down` - unlike `WaterIsAnOptionalSplash`, which had to rewrite rows.
- Good: the accounts that predate this stay searchable by username, which is always
  present and unique.
- Bad: `UserSummaryResponse.FirstName` and `.LastName` are nullable in the contract
  forever, and the web client needs a display-name fallback. That fallback lives in
  exactly two places - `toPersonSummary` and `toPersonProfile`.
- Bad: nobody can acquire a name retroactively, because editing your own names after
  registration is not built. The three existing accounts will read as username-only until
  it is.
- Follow-up: a profile-editing slice. It is the only way an existing account ever gets a
  name, and it is also what makes "required" true of the whole table rather than of new
  rows only.

---

## D-0008 - Cross-cutting API code lives in one Platform.Common library; the product owns controllers and migrations

Status: Accepted
Date: 2026-10-01
Deciders: ByronKruger

Context
- This repository is being turned into the base for future MVPs: a reusable library of the
  code every API server-side project re-derives - authentication, authorization, the error
  model, the result pattern - with each product depending on it rather than copying it.
- The earlier single-project layout put all of that in one API project with
  horizontal folders, which was right while there was one product. It means product code and
  reusable machinery share every folder: `Services/` held `AuthService` next to a
  product service, `Results/` held `Result` next to a product's error types.
- That earlier layout explicitly rejected "separate Domain / Application / Infrastructure
  projects". That rejection is about a **vertical** split of one product's layers, each layer
  a project. The seam here is **horizontal** and different: code that names a product noun on
  one side, code that names none on the other.
- The branch doing this extraction (`extract-api-platform-libraries`) is fork-only. It is
  never merged, so on `master` this entry has no subject and the single-project layout still
  describes the code.

Decision
- In the context of turning this repository into a base for future MVPs, facing a single
  project in which product code and reusable machinery share every folder, we chose to split
  it along one horizontal seam - a `Platform.Common` class library for everything that names
  no product noun, and a host project holding `Program.cs`, the product `DbContext`, its
  migrations and its controllers - to achieve a dependency a new product can take instead of
  re-deriving auth, the error model and the result pattern, accepting that the library
  carries the full dependency set (ASP.NET Core, EF Core, SQL Server, Identity, AutoMapper,
  FluentValidation) for every consumer, and that the single-project layout no longer holds.

The horizontal folders that layout protects are **kept**, inside the library: `Results/`,
`Services/`, `Validation/` and the rest each still mean one thing. Three were renamed where
the old name only made sense inside a single project - `Interfaces/` became `Abstractions/`,
`Dtos/` became `Contracts/`, `MapperProfiles/` became `Mapping/` - and `Helpers/`, which was
a bag, became `Http/`, `Time/` and `Tokens/`.

Options considered
- One `Platform.Common` library plus a reference host (chosen).
- Three libraries split by dependency weight (`Core` with no ASP.NET, `AspNetCore`,
  `Identity`): a worker or console MVP could then take `Result` and the ports without pulling
  in EF Core and Identity. Rejected as more structure than one person maintaining one base
  needs today; it stays available, because the folder boundaries inside the single library
  are exactly where those seams would fall.
- Keep one project and let each MVP copy it: no dependency to version, but every fix to the
  error model or token issuance then exists in as many copies as there are products, which is
  the problem this work exists to end.
- Ship controllers from the library through an `ApplicationPart`: register and login would
  work out of the box. Rejected - route templates, `[EndpointName]` values and
  `[ProducesResponseType]` sets are a product's public HTTP contract, and a library owning
  them makes that contract a function of a dependency version. Overriding one would mean
  inheriting from it or shadowing its route, both of which read worse than the eight lines a
  host writes instead. `ApiControllerBase` is the part worth sharing, and it shares fine as a
  base class.
- Ship migrations from the library: rejected. Migrations are a product's schema history. Two
  consumers have different tables and cannot share a `__EFMigrationsHistory`. The library
  ships the entity types and their `IEntityTypeConfiguration<T>`s; the product scaffolds the
  migration.

Consequences
- Good: a new product writes `Program.cs`, a `DbContext`, and controllers. Registration,
  sign-in, token issuance and validation, RBAC, problem details, the result pattern, the
  validation filter and the request pipeline all arrive as a dependency.
- Good: the seam is testable as a question - "would this file make sense in an API that has
  never heard of this product's domain?" - so where a new file goes is not a judgment call.
- Bad: the library needs ASP.NET Core and takes EF Core, SQL Server, Identity, AutoMapper and
  FluentValidation with it. A consumer that wants only `Result` pays for all of it.
- Bad: a class library does not get the Web SDK's implicit global usings, so
  `Platform.Common.csproj` restates them as `<Using Include="..."/>` items. Without that
  block, moved files stop compiling for a reason that has nothing to do with their code.
- Bad: this entry is scoped to a branch that is never merged. On `master`, the single-project
  layout is still the truth. **If a merge to `master` is ever proposed, re-decide this entry
  first** - `master` is a product, and for a product that layout was the right answer.
- Follow-up: whether the library is consumed by `ProjectReference` or packed as a NuGet
  package, and with what versioning policy, is undecided and belongs to the forked
  repository.

---

## D-0009 - Registration is generic over the product's DbContext and is told the product's assembly

Status: Proposed
Date: 2026-10-01
Deciders: ByronKruger

Context
- Before the split, the registration entry point worked because it sat in the same assembly as the product's
  `DbContext`, its validators and its mapper profiles, and reached all three through
  `typeof(ServiceCollectionExtensions).Assembly`.
- Split by D-0008, three same-assembly scans break, and all three break **silently**:
  `AddValidatorsFromAssembly`, AutoMapper's `AddMaps`, and `ApplyConfigurationsFromAssembly`
  would each find only the library's own types. The API would start, every route would
  answer, and requests would simply never be validated.
- `AddIdentityCore(...).AddEntityFrameworkStores<T>()` and `AddDbContext<T>` both need a
  concrete context type, which a library cannot name.

Decision
- In the context of moving the composition root into a library, facing three same-assembly
  scans that would silently find nothing once split, we chose
  `AddPlatformApi<TDbContext>(configuration, params Assembly[] featureAssemblies)` with
  `TDbContext : PlatformIdentityDbContext`, plus a second `ApplyConfigurationsFromAssembly`
  call inside the library's own context, to achieve a composition root that still reads as
  two statements in every product, accepting that a host which forgets to pass its assembly
  gets an API whose requests are never validated and whose failure is invisible at startup.

The assembly list is de-duplicated and always includes the library's own, so passing
`typeof(Program).Assembly` is additive and passing the library's assembly twice is harmless.

Options considered
- A generic entry point taking the product's assemblies (chosen).
- Scan `AppDomain.CurrentDomain.GetAssemblies()`: nothing to pass and nothing to forget, but
  it registers validators and profiles from every loaded assembly including test and
  third-party ones, and what is loaded at the moment of the call is not deterministic.
- Require the product to call `AddValidatorsFromAssembly` and `AddAutoMapper` itself: honest
  about the dependency, but the composition root stops being two statements and every product
  re-derives three lines it can get wrong in a way nothing detects.
- `[assembly: PlatformFeatureAssembly]` discovered by attribute: removes the parameter, adds
  a marker nobody remembers to apply - the same forgettable step, further from the call site.

Consequences
- Good: `Program.cs` stays two statements, and the one thing a product must pass sits in the
  signature where a reader will see it.
- Good: the `PlatformIdentityDbContext` constraint makes the wrong context type a compile
  error rather than a startup one.
- Bad: forgetting `typeof(Program).Assembly` yields an unvalidated API with no symptom at
  startup. The XML doc on the parameter says so in those words; nothing enforces it.
- Bad: two `ApplyConfigurationsFromAssembly` calls - the base class's and the product's - is
  a thing a reader has to be told, because the method's name suggests it is recursive and it
  is not.
- Good, and worth knowing: `dotnet ef migrations add` prints `No instantiatable types
  implementing IEntityTypeConfiguration were found while scanning assembly 'Platform.Api'`
  for a host that has added no tables yet. That warning is the product's own scan working,
  not a fault.
