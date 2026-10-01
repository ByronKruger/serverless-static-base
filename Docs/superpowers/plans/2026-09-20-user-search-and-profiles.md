# Plan: finding other people, and reading their preferences

> **Partly superseded on `extract-api-platform-libraries`.** The search half of this slice
> survives, in `src/platform/Platform.Common`. The profile half - `GET /api/users/{id}`,
> `UserProfileResponse`, and everything it says about reading another person's preferences -
> was removed with the beverage domain when the library was extracted (D-0021). Kept as
> history; not rewritten.

Status: complete
Date: 2026-09-20 (executed 2026-09-21)
Spec: [2026-09-20-user-search-and-profiles.md](../specs/2026-09-20-user-search-and-profiles.md)
Scope: names at registration, an autocomplete search over username / first name / surname
in the shell header, and a profile page listing the found person's saved preferences.
Out of scope: everything under the spec's "Out of scope" heading - notably editing your own
names after registration, and backfilling the three existing accounts.

## Decisions to record

Written to `Docs/DECISIONS.md` in step 13, newest at the bottom, index table updated.
Four were chosen explicitly in session and are `Accepted`; D-0018 is the agent's judgment
call and stays `Proposed` until a human accepts it.

| ID | Title | Status |
| --- | --- | --- |
| D-0016 | First name and surname are required for new accounts, and nullable in the database | Accepted |
| D-0017 | A preference is readable by any signed-in person and writable only by its owner | Accepted |
| D-0018 | User search is an upper-cased contains match over three columns, unindexed | Proposed |
| D-0019 | The header search lives in `core/user-search/`, not in a feature | Accepted |
| D-0020 | Cross-feature overlap is carried by `shared/`; no feature imports another | Accepted |

D-0017 is the one to read twice: it reverses rule 9 of
`Docs/domain/beverage-preference.md`. That rule lives in a domain doc rather than in a
decision entry, so nothing gets a `Superseded by` line - the domain doc is rewritten in
step 13 and D-0017 is where the reversal is argued.

---

## Steps

Ordered so `dotnet build` and `npm test` stay green at every boundary. Steps 1-7 are
`src/api`, 8-12 are `src/web`, 13 is documentation.

### 1. Names on the user row

- `Entities/AppUser.cs` - `FirstName`, `LastName` as `string?`, with the doc comment
  saying why they are nullable in the database but required by the validator.
- `Data/Configurations/AppUserConfiguration.cs` - `HasMaxLength(64)` on both. No index:
  D-0018 accepts a scan.
- `Dtos/RegisterRequest.cs` - two params after `UserName`. It is a positional record, so
  the compiler finds every construction site. There are **six**, all in tests:
  `AuthControllerTests.cs:69,88,98,117`, `BeverageTypesControllerTests.cs:24`,
  `BeveragePreferencesControllerTests.cs:40`.
- `Dtos/UserResponse.cs` - `string? FirstName`, `string? LastName`. AutoMapper matches
  them by name, so `MapperProfiles/UserProfile.cs` needs no new configuration - confirm by
  running the existing auth tests rather than assuming.
- `Validation/Authentication/RegisterRequestValidator.cs` - `NotEmpty` and
  `MaximumLength(64)` on both, messages phrased like the ones already there. No character
  class: names carry apostrophes, hyphens, spaces and accents, and `UserName`'s
  `^[A-Za-z0-9._@+-]+$` must not be copied onto them.
- `Services/AuthService.cs` - set both on the new `AppUser`.
- Migration: `dotnet ef migrations add UsersHaveNames --project src/api --output-dir Data/Migrations`.
  Two nullable columns, so no data repair and no `Down` caveat - unlike
  `WaterIsAnOptionalSplash`. Confirm the scaffold contains only `AddColumn` pairs before
  applying it.

### 2. The caller's id becomes an int at the port

`ICurrentUser.UserId` is a `string?` that `BeveragePreferenceService.CallerId()` parses.
`UserService` needs the same id for self-exclusion, and duplicating that parse is how the
two drift. The grep says `UserId` has exactly **one** consumer
(`Services/BeveragePreferenceService.cs:102`), so it is replaced rather than joined.

- `Interfaces/ICurrentUser.cs` - `string? UserId` becomes `int? UserKey`.
- `Helpers/HttpContextCurrentUser.cs` - does the `int.TryParse` with
  `CultureInfo.InvariantCulture`. This is exactly what `Helpers/` is for: an adapter for a
  platform port.
- `Services/BeveragePreferenceService.cs` - `CallerId()` loses the parse and reads
  `currentUser.UserKey`. Behaviour is unchanged: a missing claim and a malformed one both
  become null, and both already produced `auth.unidentified_caller`.
- `tests/Application.Tests/Fakes/FakeCurrentUser.cs` - constructor takes `int?`;
  `IsAuthenticated => UserKey is not null`.
- `tests/Application.Tests/Services/BeveragePreferenceServiceTests.cs` - **check for a
  test that passes a non-numeric claim.** The fake can no longer express "malformed", so
  if one exists it folds into the null case and its name changes to match. Do not delete
  the assertion; the code path still exists, it just moved behind the port.

`UserService` then does **not** need a `Result<int>` ceremony: it passes
`currentUser.UserKey ?? 0` as the id to exclude, because no user has id 0 and
self-exclusion is a convenience rather than a security boundary - the endpoint is already
default-deny. The `Result<int>` stays only in the service where the caller id *is* the
security boundary.

### 3. Repository port and adapter

- `Interfaces/Repositories/IUserRepository.cs`:
  - `Task<AppUser?> FindByIdAsync(int id, CancellationToken ct)`
  - `Task<IReadOnlyCollection<AppUser>> SearchAsync(string upperCasedTerm, int limit, int excludeUserId, CancellationToken ct)`

  The parameter is named for what it is: the **service** upper-cases, the repository
  compares. Two places doing it would be one place too many.
- `Repositories/IdentityUserRepository.cs` - both over `userManager.Users`, which is the
  `IQueryable<AppUser>` Identity already exposes, so `UserManager` stays confined to this
  class. Needs a `Microsoft.EntityFrameworkCore` using for `ToListAsync`.

  ```
  .AsNoTracking()
  .Where(u => u.Id != excludeUserId && (
      u.NormalizedUserName!.Contains(term) ||
      (u.FirstName != null && u.FirstName.ToUpper().Contains(term)) ||
      (u.LastName  != null && u.LastName.ToUpper().Contains(term))))
  .OrderByDescending(u => u.NormalizedUserName!.StartsWith(term))
  .ThenBy(u => u.UserName)
  .Take(limit)
  ```

  `NormalizedUserName` is Identity's own upper-invariant column and needs no `ToUpper()`.
  **Do not reach for `EF.Functions.Like`** - step 7 explains why it would pass locally and
  throw in the test suite.

### 4. DTOs

- `Dtos/UserSummaryResponse.cs` - `(int Id, string UserName, string? FirstName, string? LastName)`.
  No email, ever. No preference count: the profile's array length is what the header needs.
- `Dtos/UserProfileResponse.cs` - the summary's fields plus `DateTimeOffset CreatedAtUtc`
  and `IReadOnlyCollection<BeveragePreferenceResponse> Preferences`.
- `Dtos/UserSearchRequest.cs` - the `[FromQuery]` model, which
  `Docs/ARCHITECTURE.md` explicitly houses in `Dtos/`. Written with `{ get; init; }`
  properties and `[FromQuery(Name = "q")]` on `Term`, **not** as a positional record:
  `FluentValidationFilter` resolves `IValidator<T>` off `argument.GetType()` for each bound
  action argument, so it must be a bound complex type for the validator to run at all.
- `Dtos/BeveragePreferenceResponse.cs` - the doc comment "as its owner sees it" is now
  false. Correct it; the DTO itself does not change.

### 5. Validator

- `Validation/Users/UserSearchRequestValidator.cs` - `Term` `NotEmpty`,
  `MinimumLength(2)`, `MaximumLength(64)`; `Limit` `InclusiveBetween(1, 25)` when present.
  Registered by assembly scan, so nothing is added to `AddCoffeegValidation`.

### 6. Service and controller

- `Interfaces/Services/IUserService.cs` - `SearchAsync`, `GetProfileAsync`.
- `Services/UserService.cs` - depends on `IUserRepository`, `IBeveragePreferenceRepository`,
  `ICurrentUser` and `IMapper`. A users service reading the preferences port is normal
  under horizontal layering; a users service calling `IBeveragePreferenceService` would
  not be, so do not.
  - `SearchAsync` - upper-case the term with `ToUpperInvariant()`, default the limit to 10,
    call the port, map.
  - `GetProfileAsync` - `FindByIdAsync`, null gives `user.not_found` (404), otherwise
    `ListForUserAsync(id, ct)` - **the existing method, unchanged**, passed the subject's
    id instead of the caller's.
  - `user.not_found` lives as a private member here, following `AuthService`'s precedent.
    `BeverageErrors` exists because its codes have two callers each; this one has one.
- `MapperProfiles/UserProfile.cs` - add `AppUser -> UserSummaryResponse`.
  `UserProfileResponse` is **composed in the service** from two separately mapped parts
  rather than through a `context.Items` key like `RolesKey`: the preferences are entities
  needing their own map, and nesting `context.Mapper.Map<...>` inside a member mapping is
  harder to read than one line in the service. Note the departure in the profile's doc
  comment so the next reader does not think it was an oversight.
  This map now needs `BeveragePreferenceProfile` **and** `BeverageTypeProfile` present -
  three profiles together in any unit test that exercises it.
- `Controllers/UsersController.cs` - two actions on `ApiControllerBase`, so both are
  authenticated by the inherited fallback and neither takes a policy.
  `[EndpointName("SearchUsers")]` and `[EndpointName("GetUserProfile")]`;
  `[ProducesResponseType]` for 200 / 400 on search and 200 / 404 on profile. The kebab-case
  transformer turns `Users` into `users`, and `{id:int}` cannot collide with `?q=`.
- `Extensions/ServiceCollectionExtensions.cs` - one line in
  `AddCoffeegApplicationServices`.

### 7. API tests

- `tests/Application.Tests/Fakes/FakeUserRepository.cs`:
  - `Seed` gains optional `firstName` / `lastName` **and must set
    `NormalizedUserName = userName.ToUpperInvariant()`** - it does not today, so a fake
    search over that column would match nothing and the tests would pass for the wrong
    reason.
  - `SearchAsync` mirrors step 3's predicate, ordering and limit with
    `StringComparison.Ordinal` on already-upper-cased values.
- `tests/Application.Tests/Services/UserServiceTests.cs` - a match on each of the three
  columns; case-insensitivity; an account with NULL names still found by username;
  self-exclusion; the default limit; a clamped limit; ordering puts a prefix match before a
  mid-string match; profile found with its preferences; profile not found returns
  `user.not_found`; a profile with no preferences returns an empty collection, not null.
- `tests/Api.Tests/Controllers/UsersControllerTests.cs` - 401 anonymous on both routes;
  200 search; 400 on a one-character term; 400 on `limit=0`; 200 profile with preferences;
  404 on an unknown id; and **a raw-JSON assertion that no response body contains `@` or
  the word `email`**, which is the only way to prove the leak cannot regress past a DTO
  change.
- Update the six `RegisterRequest` construction sites and any registration assertions.

**What these tests do not prove.** `CoffeegApiFactory` swaps the database for
`UseInMemoryDatabase`, and the service tests use a fake. So the matching in step 3 is
verified twice in-process and **never against SQL Server**. Two consequences:

- `EF.Functions.Like` would throw on the in-memory provider. `ToUpper().Contains()` runs
  on both - in-process for the tests, translated to `UPPER(...) LIKE '%...%'` on SQL
  Server - which is why D-0018 chose it.
- Ordinal in-process comparison and SQL Server's collation can disagree on accented
  characters. The LocalDB run in Verification is the only place that is exercised, so it
  is not optional.

### 8. The shared pieces

Done in one step so `npm test` never sees a half-moved function.

- `shared/utils/amount.ts` - `AmountUnit { symbol: string | null; isYesNo: boolean }` and
  `describeAmount(amount, unit)`, moved verbatim from `preferences.models.ts`.
- `shared/utils/amount.spec.ts` - yes / no both ways, `None`, a symbol, a missing symbol.
- `features/preferences/data-access/preferences.models.ts` - delete the local
  `describeAmount` and import it. `Unit` keeps `name`, `min` and `max` and structurally
  satisfies `AmountUnit`, so call sites do not change.
- `shared/ui/detail-card/detail-card.ts` + `.html` - `title = input.required<string>()`,
  `rows = input.required<readonly DetailRow[]>()`, `empty = input<string>()`, where
  `DetailRow { term: string; value: string }`. Strings only, so `shared/ui` keeps its
  no-feature-imports rule literally. It is named for what it is - a primitive, not a
  preference.
- `features/preferences/pages/preferences-page/` - adopt `app-detail-card`, which deletes
  the `<dl>` at `preferences-page.html:28`. Map to rows in a **`computed`** on the page,
  not a method called from the template, so the mapping runs once per change rather than
  once per render.

### 9. `core/user-search/`

Mirrors `core/auth/`: contracts, models, API, store and the component, all
`providedIn: 'root'`, because there is one instance for the life of the app.

- `user-search.contracts.ts` - `UserSummaryResponse`, with `firstName?` / `lastName?`
  optional because the API omits nulls.
- `user-search.models.ts` - `PersonSummary { id, userName, displayName }` and
  `toPersonSummary`, where `displayName` falls back to the username when both names are
  absent. This is the one place the two classes of account from D-0016 are reconciled.
- `user-search.api.ts` - `search(term, limit)` with `HttpParams` for `q` and `limit`.
- `user-search.store.ts` - a private `term` signal, `toObservable(term)` piped through
  `debounceTime(250)`, `distinctUntilChanged()`, a 2-character floor, and `switchMap`, back
  to a signal with `toSignal`. **`catchError` goes inside the `switchMap`**, returning an
  empty list and setting an error signal: a failure that escapes to the outer pipeline
  completes it, and the search box then stops working silently for the rest of the session.
- `user-search.ts` + `.html` - `nz-input` inside `nz-autocomplete`, used as they ship.
  Picking a result navigates to `/users/{id}` and clears the term; a term with matches
  pending shows Ant's loading state; a term with none renders one disabled "No matches"
  row.
- `user-search.store.spec.ts` - debounce collapses keystrokes, the 2-character floor
  suppresses the call entirely, `switchMap` discards a slow earlier response, an error
  leaves the store usable for the next term, `clear()` empties results.
- `core/icons/icons.provider.ts` - add `SearchOutline`. It belongs here, not on a feature
  route, because the header is core chrome.

### 10. Shell

- `core/layout/shell/shell.html` - `<app-user-search />` in the existing content band,
  rendered only when `isAuthenticated()`. It must carry the **same** collapse treatment as
  the nav (`hidden md:flex` plus `[class.!flex]="navOpen()"`), or it stays visible when the
  nav is folded on a narrow viewport.
- `core/layout/shell/shell.ts` - import `UserSearch`.

Two things D-0012 already learned the hard way: do not put a `leading-*` utility on
`nz-header`, because Ant's horizontal menu sizes itself from the header's 64px
line-height; and Ant's autocomplete dropdown is light-themed against the dark header. The
second is accepted, not fixed - `Docs/ARCHITECTURE.md` forbids overriding the component
library, and D-0012's whole point is that a utility now beats Ant on any property they
both set.

### 11. `features/users/`

- `users.routes.ts` - lazy, `authenticatedGuard`, `providers: [UsersStore, UsersApi,
  provideNzIcons([...])]`, route title the static `'Profile - Coffeeg'`.
- `data-access/users.contracts.ts` - **only the fields the profile reads**, a narrower view
  of the same payload rather than a copy of the API's DTO. `amount` is typed
  `amount?: number | null`.
- `data-access/users.models.ts` - `PersonProfile` and `SavedPreference`, mapping through
  `describeAmount` and normalising `amount ?? null`.
- `users.store.ts`, `pages/profile-page/profile-page.ts` + `.html` - page header with
  display name, `@username` and the count from the array length, above a grid of
  `app-detail-card`; `nz-empty` for nobody's-saved-anything; the `ApiError` banner from the
  page's own template for a 404.
- `users.store.spec.ts` - maps a profile; **an omitted `amount` renders as `No`, not
  `Yes`**; an empty preference list; a profile whose names are absent falls back to the
  username.
- `app.routes.ts` - one lazy `users` route, before the `**` catch-all.

The omitted-amount test is not optional. It is the bug the last slice shipped and fixed in
`features/preferences`, and this is a second reader of the same payload, so the same
mistake is available again.

### 12. Registration

- `core/auth/auth.contracts.ts` - `RegisterRequest` gains both; the response user gains
  both as optional.
- `core/auth/auth.session.ts` - `AuthenticatedUser` gains both and `toSession` maps them.
- `features/auth/pages/register-page/` - two required controls, max 64, with
  `autocomplete="given-name"` and `"family-name"`, and the same `fieldErrors()` template
  shape the other three fields use. `fieldErrorsFor` already matches `FirstName` to
  `firstName` case-insensitively, so no change there.
- `core/auth/auth.store.spec.ts` - update whatever asserts the registration payload.

### 13. Documentation

- `Docs/DECISIONS.md` - the five entries and five index rows.
- `Docs/domain/beverage-preference.md` - rewrite rule 9, and say in "Who owns what" that a
  preference is now readable by anyone signed in. Bump `lastVerified`.
- `Docs/domain/user-accounts.md` - names at registration and the rules around them; a
  "Finding other people" section for the match rules, the 2-character floor and the
  email-is-never-searchable line; move "changing a name after registration" into
  "Not in scope yet". Bump `lastVerified`. No new domain file, so
  `Docs/domain/README.md` changes only if a code reference does.
- `Docs/ARCHITECTURE.md` - `user-search/` in the `core/` tree.
- `src/api/AGENTS.md` - a "Users, search and profiles" section: the three-column match, the
  scan, the no-email rule, and that `ListForUserAsync` is now called with a subject id.
- `src/web/AGENTS.md` - `core/user-search/`, `features/users/`, and the `shared/`
  pieces with the note that no feature imports another.
- This plan - status to `complete`, plus a "Things that had to change along the way"
  section written from what actually happened.

---

## Traps found while planning

Each of these is already handled in a step above; collected here so none is lost.

1. **The HTTP tests run on the in-memory provider**, so no automated test exercises the
   real SQL. Hence `ToUpper().Contains()` rather than `EF.Functions.Like`, and hence the
   mandatory LocalDB run.
2. **`FakeUserRepository.Seed` does not set `NormalizedUserName`.** Left alone, a fake
   search over that column matches nothing and the suite passes for the wrong reason.
3. **`RegisterRequest` is a positional record** with six construction sites - all in tests,
   all caught by the compiler.
4. **`FluentValidationFilter` keys off the bound argument's runtime type**, so the query
   model has to be a bound complex type or the 400 never happens.
5. **`UserProfileResponse` embeds a DTO from another profile**, so any unit test touching
   that map needs all three AutoMapper profiles.
6. **The API omits nulls**, so `amount` arrives `undefined`. Second reader, same trap.
7. **An error escaping the search pipeline completes it**, killing the search box for the
   session. `catchError` belongs inside the `switchMap`.
8. **A running `Coffeeg.Api` locks its own binaries** and no MSBuild flag works around it -
   recorded at the end of the previous plan. Stop it before building.

## Things that had to change along the way

Seven, all found by the steps above rather than guessed at in advance.

- **A `params` overload silently granted no roles.** Giving `FakeUserRepository.Seed` a
  names-carrying overload with the names *before* `params string[] roles` made
  `Seed(email, user, pwd, Roles.User, Roles.Moderator)` bind the two roles to the two
  names: C# prefers a non-params match over a params expansion, so the account got no
  roles and one auth test failed. Names now go through a separately named `SeedPerson`,
  and the remark on `Seed` says why an overload is not an option.
- **Validation-error keys are not consistently cased.** `ModelStateDictionary` is
  case-insensitive, so when `FluentValidationFilter` adds a failure under the property
  name it lands on whatever key the binder already recorded if the two differ only by
  case. `Term` binds from `q`, so `"Term"` survives; `Limit` binds from `limit`, so the
  binder's lower-case key wins. The tests match case-insensitively and say why; the web
  client's `fieldErrorsFor` already did.
- **The search store swallowed unexpected errors.** Written to set an `ApiError` signal
  and otherwise set null, it discarded anything that was not one. A page would rethrow and
  let `GlobalErrorHandler` take it, but a throw inside the pipeline completes it, so the
  store now hands non-`ApiError` failures to the injected `ErrorHandler` directly. Its
  spec registers `apiErrorInterceptor`, without which the test exercised a configuration
  the app does not have.
- **`HttpTestingController` proved the cancellation better than the assertion did.** The
  superseded-response test tried to flush the stale request and got "Cannot flush a
  cancelled request" - `switchMap` had already unsubscribed. Asserting
  `first.cancelled` is the stronger statement.
- **A required input cannot be read in a constructor (NG8118).** The profile page loaded in
  its constructor, which the compiler rejects because the router binds inputs afterwards.
  Fixing it exposed a second bug that was never going to be caught by a test: the router
  reuses the component when you go from one profile straight to another, so a one-shot
  load would have left the previous person on screen. It now loads from
  `toObservable(this.id)`.
- **A route parameter is a string.** `id` needed `numberAttribute`, or the profile URL
  would have been built from `"23"` while the type claimed `number`.
- **The end-to-end script's 401s were the harness, not the code.** Every authenticated call
  over `http://localhost:5237` returned 401, including endpoints from the previous slice.
  `UseHttpsRedirection` answers HTTP with a 307, and .NET drops the `Authorization` header
  when following a redirect to a different scheme - so the token never arrived. Over
  `https://localhost:7011` everything passed first time. Worth remembering: this failure
  mode looks exactly like a rejected token.

## Verification

```powershell
dotnet build                                  # succeeded, warnings as errors
dotnet test                                   # 70 + 51 passed (was 50 + 34)
dotnet ef database update --project src/api   # UsersHaveNames applied to LocalDB
cd src/web; npm run build                     # succeeded, profile-page its own 3.05 kB chunk
cd src/web; npm test                          # 38 passed (was 16)
```

The migration scaffolded as exactly two `AddColumn` statements with no data repair and a
reversible `Down` - checked before applying, unlike `WaterIsAnOptionalSplash`, which had
to rewrite rows.

### End to end against LocalDB, over HTTPS

29 checks, all passing. The ones worth recording:

- An **accented surname** (`Kowalskí`) is matched by an upper-cased term, which is the case
  a collation assumption gets wrong and the case neither test layer can reach.
- Each of the three columns matches, mid-string as well as prefixed, in either case.
- `byron` - a pre-existing account with **NULL names** - is still found by username.
- The caller is absent from their own results; a one-character term, an empty term,
  `limit=0` and `limit=99` are each 400; a term matching nobody is `200 []`.
- A username prefix match sorts above a mid-string match.
- A profile returns the other person's two preferences with their names; **neither route's
  raw body contains an email**; an unknown id is 404 `user.not_found`; both routes are 401
  anonymous.
- **D-0017 did not leak.** Somebody else's preference by its own id is still 404
  `preference.not_found` on `/api/beverage-preferences/{id}`, and `GET
  /api/beverage-preferences` still returns only the caller's.
- The whiskey saved without water comes back as
  `{"ingredientId":9,"ingredientName":"Water","unit":{…}}` - **no `amount` key at all**,
  which is the shape `users.store.spec.ts` pins as "No".

Through the Angular dev-server proxy on :4200, sign-in, search and profile all return the
payloads the client contracts expect.

### Found in use, after the plan was marked complete

**The search box navigated on its own.** Reported on first real use: the dropdown
dismissed itself the moment results arrived and landed on a profile nobody had clicked.
This is exactly the class of bug the unverified browser check would have caught, and no
existing test could - it lived entirely in *which* of ng-zorro's two identically named
`selectionChange` outputs was bound.

`nz-auto-option.selectionChange` fires whenever ng-zorro changes an option's selected
flag, not only on a pick. `resetActiveItem()` calls `clearSelectedOptions(null, true)`,
which calls `deselect()` on every option, and `deselect()` emits unconditionally with
`isUserInput: false`. Two paths reach it - `updateStatus()` on every input event, and the
options-changed subscription when results land - so the handler ran once per rendered
option, per keystroke. `nz-autocomplete.selectionChange` is emitted behind ng-zorro's own
`filter(e => e.isUserInput)`, and is the correct output.

Fixing it also removed the `ngModel` binding. The trigger reads the typed text off the DOM
element and its `onChange` is a no-op until a control registers, so the model was doing no
work - but on a pick the trigger writes the chosen `nzValue` back through it, which would
have started a fresh search for the id just navigated to. `nzValue` now carries the id and
`nzLabel` the display name.

`user-search.spec.ts` renders the real component with the real autocomplete. Its three
navigation guards were each confirmed to **fail against the old wiring and pass against
the new** - including draining ng-zorro's `delay(0)`, without which one of them passed
against the bug and proved nothing.

### Not verified

**The browser UI was not exercised.** The Chrome extension was not connected, so nothing
was clicked. The interaction half of that gap has since been closed twice over - by the
bug above being reported, and by `user-search.spec.ts`, which now renders the real
component and the real autocomplete and covers typing, results arriving, picking a row,
and the disabled no-matches row.

What remains unverified is **layout and responsive behaviour**: the search input's
placement in the header band, and its collapse below 769px. Nothing tests those, and
nobody has looked at them.

`npm test` goes through **PowerShell, not the Bash tool** - Bash breaks the vitest worker
in this repo.

Then end to end against LocalDB over HTTP, because the in-memory provider cannot stand in
for the parts that matter:

- Register an account with a first name and surname; confirm the row has both and the
  response omits neither.
- Register with a blank first name - 400, field-keyed on `FirstName`.
- Find that person by a fragment of the **username**, of the **first name**, and of the
  **surname**; each with the wrong case; each with an accented character if one is seeded.
- Confirm one of the three pre-existing NULL-named accounts is still found by username and
  that its profile renders with the username alone.
- Confirm the caller never appears in their own results.
- `q=a` is 400; `q=` is 400; `limit=0` is 400; `limit=99` is 400; a term matching nobody is
  200 with `[]`.
- Confirm a prefix match sorts above a mid-string match.
- `GET /api/users/{id}` returns the preferences, and **no response on either route contains
  an email**.
- `GET /api/users/{unknown}` is 404 `user.not_found`; both routes are 401 anonymous.
- `GET /api/beverage-preferences/{id}` for somebody else's preference is **still** 404
  `preference.not_found` - D-0017 must not have leaked into the owner-scoped routes.
- In the browser: type in the header, pick a result, land on the profile; a whiskey saved
  with no water reads **"No"**, not "Yes"; the search box still works after a failed
  request; the input folds into the collapsed nav below 769px.

## Open questions for the human

- **The browser UI has not been looked at.** See "Not verified" above. This is the one
  piece of the plan that did not happen.
- **D-0018 is the only `Proposed` entry.** The match is an unindexed scan. Fine at three
  accounts; say if you would rather pay for persisted computed columns and an index now,
  in which case it changes step 1 and step 3.
- **Nobody can acquire a name retroactively.** The three existing accounts, and yours, will
  show as username-only forever until a profile-editing slice exists. That follows from the
  nullable-columns choice and is the most likely thing to annoy you on first run.
- **A saved preference stops being private the moment this ships**, with no opt-out. D-0017
  argues it; it is worth reading before the go.
- **Self-exclusion uses `?? 0`** rather than a `Result<int>` failure. Deliberate - the
  endpoint is default-deny, so a missing caller id cannot happen, and excluding nobody
  leaks nothing. Say if you would rather it failed loudly.
