# Plan: first feature slice - registration and authentication

Status: complete
Date: 2026-09-18
Scope: registration and password sign-in, end to end. Database renamed to `CoffeegAI`.
Sign-in is by email (D-0009); the username is a display name.
Out of scope: email verification, password reset, email sign-in, token revocation,
route guards (nothing is protected yet).

## Decisions taken

`Docs/DECISIONS.md` D-0009 (Proposed), D-0010 and D-0011 (Accepted).

## Steps

1. **Database.** Confirmed write access to LocalDB (create table, insert, select, drop as
   `BYRON-COMPUTER\byron`), created `CoffeegAI`, pointed the connection string at it. Done.
2. **Identity model.** `AppUser : IdentityUser<int>`, `AppRole : IdentityRole<int>`,
   `CoffeegDbContext : IdentityDbContext<AppUser, AppRole, int>`, unique `EmailIndex`,
   roles seeded by the migration. Migration `InitialIdentity` generated and applied. Done.
3. **Token issuance.** `JwtOptions` reworked from OIDC discovery to issuer/audience/signing
   key/lifetime; `JwtAccessTokenFactory` signs HS256 tokens; the bearer handler validates
   against the same key. Done.
4. **Application layer.** `IUserRepository` port over Identity, `IAuthService` /
   `AuthService`, `RegisterRequest` / `LoginRequest` / `AuthenticationResponse` /
   `UserResponse`, FluentValidation validators, AutoMapper profile, `AuthController` with
   two anonymous endpoints. Done.
5. **Tests.** 10 service tests on fake ports; 8 HTTP tests through the real pipeline. Done.
6. **Frontend.** `AuthStore` with `localStorage` persistence, `AuthApi`, `AuthStorage`,
   `/auth/login` and `/auth/register` pages, header showing the username with a log-out
   button, 6 store tests. Done.
7. **Documentation.** Decision entries, both project `AGENTS.md` files, `README.md`, and a
   new `Docs/domain/user-accounts.md` indexed in the domain README. Done.

## Things that had to change along the way

- **`InvariantGlobalization` had to be turned off.** `Microsoft.Data.SqlClient` resolves
  collations through culture data and throws `en-us is an invalid culture identifier`
  under invariant mode. It and SQL Server (D-0003) cannot both be had; the property is now
  `false` with a comment saying why.
- **Generated migrations fail the repo style rules.** `Data/Migrations/.editorconfig` marks
  them as generated code, otherwise `TreatWarningsAsErrors` breaks the build on IDE0161
  every time a migration is scaffolded.
- **Error responses were being served as `application/json`.** `[Produces("application/json")]`
  on `ApiControllerBase` is a result filter that overwrites `ContentTypes` on every
  `ObjectResult`, including problem responses - a silent violation of the RFC 9457 lock
  that existed since the scaffold. The attribute is gone and the media type is now forced
  centrally in `ResultExtensions.AsProblemJson`, covered by tests on both the service
  failure path and the edge validation filter.
- **The test factory gave every scope its own empty database.** `AddDbContext` registers
  its options as scoped, so `UseInMemoryDatabase($"...{Guid.NewGuid()}")` built a new
  database name per scope and the seeded roles were never visible to a request. The name
  is now fixed per factory.

## Verification

```bash
dotnet build                     # succeeded, warnings as errors
dotnet test                      # 15 + 11 passed
cd src/web && npm run build      # succeeded, auth pages in their own lazy chunks
cd src/web && npm test           # 7 passed
```

End to end against LocalDB: registered a user (200 with token), restarted the API and
signed in as that user (200), duplicate email rejected (409 `application/problem+json`,
`code: auth.email_taken`), wrong password rejected (401). The row in `AspNetUsers` has an
int `Id`, the email, the username, an 84-character password hash, and a `User` row in
`AspNetUserRoles`.

## Resolved after the first pass

- **D-0009 accepted**, with a change: the int `Id` stays the primary key and email stays
  the enforced natural key, and **sign-in moved from username to email**. `LoginRequest`
  now carries `Email`, `AuthService.LoginAsync` looks the user up with
  `FindByEmailAsync`, and the login page asks for an email address. The username is still
  collected at registration, still uniquely indexed, and still what the header shows - it
  is simply not a credential. No migration change was needed: the schema already had the
  unique email index this relies on.

## Open questions for the human

1. **Nothing is protected yet.** There are no authenticated routes, so there is no route
   guard and no 401-triggered sign-out. Both are worth adding with the first protected
   feature.
2. **Email is trusted as typed.** There is no verification step, so an account can be
   created against an address the person does not control. That matters more now that
   email is the credential.

## Next step

`beverage-types`, the aggregate every preference hangs off, which exercises the moderator
policy already sitting in `Authorization/Policies.cs`.
