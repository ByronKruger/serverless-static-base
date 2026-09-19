# Decisions

Single log for architecturally significant choices. Agents: read this file before changing strcture, stack, or contracts. See `AGENTS.md` and Architecture decisions.

When this file becomes hard to scan (roughly 8-15 accepted entries, or one entry needing its own options appendix), split into `adr/` per the migration notes in `AGENTS.md`. Until then, do not create other decision files.

## Index

| ID | Title | Status | Date |
| --- | --- | --- | --- |
| D-0001 | ASP.NET Core Web API host, composed so a serverless host stays reachable | Accepted | 2026-09-17 |
| D-0002 | One API project with horizontal folders, tests in a sibling `tests/` tree | Accepted | 2026-09-18 |
| D-0003 | SQL Server as the EF Core provider | Accepted | 2026-09-18 |
| D-0004 | AutoMapper 16.x as the mapping library | Accepted | 2026-09-18 |
| D-0005 | xUnit and NSubstitute as the .NET test stack | Accepted | 2026-09-18 |
| D-0006 | ng-zorro-antd as the Ant Design implementation | Accepted | 2026-09-18 |
| D-0007 | Tailwind CSS v4 with preflight disabled | Accepted | 2026-09-18 |
| D-0008 | Node 22 and npm 11 as the frontend toolchain baseline | Accepted | 2026-09-18 |
| D-0009 | Users keyed by an int surrogate; email is the natural key and the sign-in credential | Accepted | 2026-09-18 |
| D-0010 | The API issues its own access tokens | Accepted | 2026-09-18 |
| D-0011 | The access token lives in localStorage | Accepted | 2026-09-18 |
| D-0012 | ng-zorro's stylesheet is imported into a cascade layer below Tailwind's utilities | Proposed | 2026-09-19 |

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
- In the context of scaffolding the backend, facing a direct conflict between the retired Functions entry and the architecture locks, we chose an ASP.NET Core Web API whose registration and pipeline each sit behind a single hosting-agnostic extension method (`AddCoffeegApi`, `UseCoffeegApiPipeline`) to achieve a host that satisfies every architecture lock today while keeping a move to a serverless host a change to the host project only, accepting that controllers and the MVC filter pipeline would still have to be rewritten if that move ever happens.

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

## D-0002 - One API project with horizontal folders, tests in a sibling tests tree

Status: Accepted
Date: 2026-09-18
Deciders: ByronKruger

Context
- `Docs/ARCHITECTURE.md` gives a target folder tree for `src/api` (Interfaces, Services, Repositories, Helpers, Dtos, Results, Exceptions, Entities, Validation, Controllers, MapperProfiles, Extensions) and names two test projects, `tests/Application.Tests` and `tests/Api.Tests`. That tree had no home for the `DbContext`, for options classes, or for authorization policy names, and it trailed off mid-list.

Decision
- In the context of laying out the backend, facing a documented tree with three gaps, we chose a single `Coffeeg.Api` project holding the documented folders plus `Data/` (DbContext and its configurations), `Options/` (validated options and their configurators) and `Authorization/` (policy and role name constants), with the two test projects in a sibling `tests/` tree and package versions centralised in `Directory.Packages.props`, to achieve one obvious destination for every file, accepting that three of those folders are additions to the previously documented tree rather than a reading of it.

Options considered
- Documented tree plus three named folders (chosen).
- Strictly the previously documented tree, with the DbContext in `Repositories/` and options classes in `Helpers/`: no drift, but two folders stop meaning one thing.
- Separate Domain / Application / Infrastructure projects: contradicts the horizontal layering the architecture document locks.

Consequences
- Good: no file lands in a folder by default, and each addition maps to a lock in the architecture document.
- Good: `Docs/ARCHITECTURE.md` now carries the full tree, so it and the code agree.
- Bad: three folders in that tree originate here rather than in the original architecture write-up.
- Follow-up: `Data/Migrations` appears the first time `dotnet ef migrations add` runs.

---

## D-0003 - SQL Server as the EF Core provider

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

## D-0004 - AutoMapper 16.x as the mapping library

Status: Accepted
Date: 2026-09-18
Deciders: ByronKruger

Context
- `Docs/ARCHITECTURE.md` locks mapping to AutoMapper. AutoMapper 14.0.0 is the last MIT-licensed release, and every version below 15.1.1 carries GHSA-rvv3-g6hj-g44x, a high-severity uncontrolled-recursion denial of service. With warnings as errors, NuGet audit fails the build on it.
- AutoMapper 15 and later are published under a licence that is free below a revenue threshold and paid above it. Coffeeg is not a commercial product, so it sits under that threshold.

Decision
- In the context of choosing a mapping library version, facing a known high-severity advisory in every freely licensed release, we chose AutoMapper 16.2.0 to achieve a build with no unresolved vulnerability, accepting a licence that would need revisiting if Coffeeg were ever commercialised.

Options considered
- AutoMapper 16.2.0 (chosen).
- AutoMapper 14.0.0 with NU1903 suppressed: keeps the MIT licence, ships a known denial of service.
- Riok.Mapperly or hand-written mapping: MIT and allocation-free, but contradicts the AutoMapper lock, so it would need its own entry superseding that lock.

Consequences
- Good: `dotnet restore` passes NuGet audit with warnings as errors.
- Bad: the licence is conditional on the project staying non-commercial, which is a constraint that lives outside the code.
- Follow-up: if Coffeeg is ever commercialised, revisit this entry - either buy a licence or supersede it with the Mapperly option, updating the mapping lock in `Docs/ARCHITECTURE.md` at the same time.

---

## D-0005 - xUnit and NSubstitute as the .NET test stack

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

## D-0006 - ng-zorro-antd as the Ant Design implementation

Status: Accepted
Date: 2026-09-18
Deciders: ByronKruger

Context
- `Docs/ARCHITECTURE.md` locks the component library to the Ant Design language, with out-of-the-box components and no overrides, but names no package.

Decision
- In the context of choosing the Angular component library, facing a language-level lock with no package named, we chose `ng-zorro-antd` 22.1 to achieve the Ant Design language in first-party Angular components that track the Angular major version, accepting that the application is tied to that project's release cadence.

Options considered
- ng-zorro-antd (chosen): MIT, versioned in lockstep with Angular majors.
- Wrapping the React Ant Design components: not viable in a zoneless Angular application.
- A different design language such as PrimeNG or Angular Material: contradicts the lock.

Consequences
- Good: components, icons and i18n come from one source, which satisfies the "one icon set" lock.
- Bad: an Angular major upgrade waits on an ng-zorro release.
- Follow-up: icons are registered explicitly in `core/icons/icons.provider.ts`, and features register their own rather than pulling in the full set.

---

## D-0007 - Tailwind CSS v4 with preflight disabled

Status: Accepted
Date: 2026-09-18
Deciders: ByronKruger

Context
- The architecture document uses Tailwind for layout, spacing, type and responsive behaviour, and separately forbids overriding the component library. Tailwind's preflight is a global element reset; loaded normally it flattens the element styles Ant Design's components rely on, which is exactly the override that is forbidden.

Decision
- In the context of running Tailwind alongside Ant Design, facing a reset that would override the component library, we chose to import only Tailwind's theme and utilities layers, never `tailwindcss/preflight.css`, and to declare the five locked breakpoints and the design tokens in `@theme`, to achieve utility classes that compose with Ant Design instead of fighting it, accepting that no element reset ships at all and any base-element styling must be written by hand in `styles.css`.

Options considered
- Theme plus utilities, no preflight (chosen).
- Full Tailwind with per-component overrides to repair Ant Design: forbidden by the lock.
- Tailwind only inside feature styles: utilities are global by nature, so this does not work.

Consequences
- Good: `@theme` tokens are both Tailwind utilities and plain CSS variables, so the design-tokens lock and the Tailwind lock are satisfied by one definition.
- Bad: browser default margins on headings and lists are whatever Ant Design leaves behind rather than a normalised baseline, so templates must not assume a reset.
- Follow-up: the breakpoints in `src/styles.css` are the single source of truth: 320, 481, 769, 1025, 1201.

---

## D-0008 - Node 22 and npm 11 as the frontend toolchain baseline

Status: Accepted
Date: 2026-09-18
Deciders: ByronKruger

Context
- Angular 22's CLI declares `node: ^22.22.3 || ^24.15.0 || >=26.0.0`. The npm 10.9.8 that ships with Node 22 fails to resolve this workspace's peer graph with `Cannot read properties of null (reading 'edgesOut')`. npm 11 resolves it.

Decision
- In the context of pinning the frontend toolchain, facing an engine floor and a broken bundled package manager, we chose Node 22.23.2 with npm 11, recorded in `src/web/package.json` as `packageManager`, to achieve an install that reproduces on a clean machine, accepting that developers must run a Node version manager rather than whatever Node happens to be on their PATH.

Options considered
- Node 22 with npm 11 (chosen).
- Node 22 with npm 10 and `--legacy-peer-deps`: installs, but resolves a different tree than a working npm would.
- Node 24: also supported by Angular 22, and a reasonable move once CI is on it.

Consequences
- Good: `npm ci` from the committed lockfile is reproducible.
- Bad: npm 11 blocks dependency lifecycle scripts by default, so a package that genuinely needs one must be approved explicitly.
- Follow-up: pin the same pair in CI before the first pipeline runs.

---

## D-0009 - Users keyed by an int surrogate; email is the natural key and the sign-in credential

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

## D-0010 - The API issues its own access tokens

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

## D-0011 - The access token lives in localStorage

Status: Accepted
Date: 2026-09-18
Deciders: ByronKruger (specified in session, 2026-09-18)

Context
- The SPA needs the access token on every API call and needs the signed-in state to survive a page reload. The two realistic stores are `localStorage`, readable by any script on the origin, and an HttpOnly cookie, which script cannot read but which requires CSRF defences and same-site coordination between the API and the client.

Decision
- In the context of persisting the session in the browser, facing a choice between script-readable storage and an HttpOnly cookie, we chose `localStorage` to achieve a client the SPA fully controls with no cookie or CSRF machinery, accepting that any successful XSS on the origin can read the token and use it until it expires.

Options considered
- `localStorage` (chosen, and explicitly asked for).
- HttpOnly, SameSite cookie: immune to token theft by script, but needs CSRF protection and a cookie-issuing endpoint.
- In-memory only: safest, but signs the user out on every reload.

Consequences
- Good: the interceptor reads the token synchronously, and reload behaviour is trivial.
- Bad: XSS becomes account takeover for the life of the token, which is why the lifetime is bounded and why the app must never render untrusted HTML.
- Bad: the token is visible to anything else running on the origin, including a compromised dependency.
- Follow-up: revisit alongside token revocation (D-0010) before handling anything more sensitive than drink preferences.

---

## D-0012 - ng-zorro's stylesheet is imported into a cascade layer below Tailwind's utilities

Status: Proposed
Date: 2026-09-19
Deciders: proposed by agent, pending human

Context
- D-0007 imports Tailwind's theme and utilities into named cascade layers, while `ng-zorro-antd.min.css` was listed in `angular.json` and therefore shipped unlayered. Unlayered CSS outranks every layered rule regardless of specificity, so ng-zorro silently won every property it declares and the Tailwind utilities the templates place on Ant components were inert. The shell header showed the result: `nz-header { display: block }` beat `flex`, so the brand, the nav and the auth buttons each took their own line; `.ant-layout-header { padding: 0 50px }` beat `px-4 md:px-6`; `.ant-btn { display: inline-block }` beat `md:hidden`, so the mobile nav toggle was visible on desktop.

Decision
- In the context of running Tailwind utilities on top of Ant Design components, facing an unlayered component library that outranks every utility, we chose to import `ng-zorro-antd.min.css` from `src/styles.css` into a named `ng-zorro` layer ordered below `components` and `utilities` to achieve the layering D-0007 already assumes, accepting that a Tailwind utility now beats Ant on any property they both set, so the "no overrides" rule in `Docs/ARCHITECTURE.md` becomes a review convention rather than something the cascade enforces.

Options considered
- Import ng-zorro into a layer below `utilities` (chosen): one line, and every existing template starts behaving the way it reads.
- Per-component stylesheet overrides: an emulated-encapsulation selector is unlayered and outranks ng-zorro by specificity, but it repairs one component at a time and leaves the same trap for the next one.
- `!important` on the affected utilities: wins, but spreads through every template that styles an Ant component.

Consequences
- Good: `layout, spacing, type and responsive behaviour via Tailwind` (`Docs/ARCHITECTURE.md`) is now true of Ant components too, not only of plain elements.
- Good: the layer order is declared in one place, `src/styles.css`, next to the breakpoints and tokens.
- Bad: ng-zorro's 560 kB stylesheet is now inlined into `styles.css` by Tailwind's import bundler rather than emitted as its own file, so it is one bundle and one cache entry instead of two.
- Bad: an accidental utility can now change an Ant component's look without anyone noticing, which is exactly what D-0007 wanted to prevent.
- Follow-up: Ant's own invariants must be respected rather than overridden. `.ant-layout-header .ant-menu { line-height: inherit }` means the horizontal menu sizes itself from the header's 64px line-height, so a `leading-*` utility on `nz-header` collapses the menu items; the shell sets `h-16` and leaves the line-height alone.
