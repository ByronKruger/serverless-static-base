# Spec: finding other people, and reading their preferences

> **Partly superseded on `extract-api-platform-libraries`.** The search half of this slice
> survives, in `src/platform/Platform.Common`. The profile half - `GET /api/users/{id}`,
> `UserProfileResponse`, and everything it says about reading another person's preferences -
> was removed with the beverage domain when the library was extracted (D-0021). Kept as
> history; not rewritten.

Status: draft, awaiting approval
Date: 2026-09-20
Domain: [user-accounts.md](../../domain/user-accounts.md),
[beverage-preference.md](../../domain/beverage-preference.md)

## The request

> Allow users to search for one another using an auto-complete input search field. Users
> must be able to save their first name and surname upon registration. The search term
> must match their username or first name or surname. Selecting an auto-completed result
> takes you to a detailed view of that user's profile, which lists their beverage
> preferences.

A fifth requirement - that the profile reuse the create-preference page as a read-only
version - was **withdrawn in session** before this spec was written. See
[Reuse](#reuse-what-is-actually-shared).

Confirmed in session on 2026-09-20:

1. First name and surname are **required for new accounts**. The columns stay nullable so
   the three accounts already in the database, and the preferences hanging off them,
   survive the migration.
2. A profile is readable by **any signed-in person**. Anonymous callers get nothing, and
   another person's **email is never exposed** - it is the sign-in credential (D-0009).
3. The profile shows the **same card grid** as the existing My-preferences page.
4. Cards list **only the lines the person saved**, not the whole composition.
5. The overlap between the two features is carried by `shared/`. Neither feature imports
   the other.

## The rule this reverses

`Docs/domain/beverage-preference.md` rule 9 currently reads:

> Preferences are **private**. Somebody else's is reported as `preference.not_found`
> (404), never 403, so ids cannot be probed.

That cannot stand beside a profile page that lists somebody else's preferences. It becomes:

> Preferences are **readable by any signed-in person and writable only by their owner**.

Note what does **not** change. `/api/beverage-preferences` and
`/api/beverage-preferences/{id}` still mean *mine*: they stay scoped to the caller and a
stranger's id still answers `preference.not_found`. Cross-user reading happens through the
new profile endpoint, which names the user in the route. Every
`IBeveragePreferenceRepository` method still takes the user id it queries for - the
profile passes the *subject's* id rather than the caller's, so the invariant that a query
cannot forget the user holds unchanged.

The consequence worth stating plainly: a saved preference is no longer private, and there
is no opt-out. Somebody who saved a drink yesterday under the old rule did so under a
promise this slice breaks.

## Model

Two nullable columns on the existing user row. Nothing else changes.

**AppUser** gains `FirstName` and `LastName`, `nvarchar(64)`, both nullable in the
database and both required by the registration validator. Accounts registered before this
slice keep NULLs: they remain findable by username and their profiles render with the
username alone.

The two classes of account are the cost of not backfilling. `UserSummaryResponse` and
`UserProfileResponse` type both names as optional, and the web client derives a display
name that falls back to the username.

## Matching

The search term matches, case-insensitively, any of **username**, **first name** or
**surname**, as a *contains* match.

`NormalizedUserName` is Identity's own upper-cased column and needs no help. The two new
columns have no normalized twin, so the service upper-cases the term and compares against
`FirstName!.ToUpper()` / `LastName!.ToUpper()`, which EF translates to `UPPER()` in SQL.
That is provider-independent and correct, and it is **not indexable** - a function on the
column means a scan. Fine at this size; named here so it is a known cost rather than a
surprise. The upgrade path, if the user table ever grows, is persisted computed columns
plus an index, mirroring what `EmailIndex` does for email.

| Input | Behaviour |
| --- | --- |
| Term under 2 characters | 400, validator - an autocomplete on one letter returns the whole table |
| Term over 64 characters | 400, validator |
| `limit` | Optional, 1-25, defaults to 10 |
| The caller themselves | Excluded from results - the point is finding *other* people |
| No matches | 200 with an empty array, never 404 |

Ordering is deterministic so the dropdown does not reshuffle between keystrokes: results
whose username starts with the term come first, then the rest, each group alphabetical by
username.

## HTTP surface

Both endpoints are authenticated by the inherited fallback policy on `ApiControllerBase`.
No new named policy: any signed-in person may read any profile, which is a rule about the
resource, not a role.

| Method | Route | Purpose |
| --- | --- | --- |
| GET | `/api/users?q={term}&limit={n}` | Autocomplete matches - id, username, first name, surname |
| GET | `/api/users/{id}` | One profile with the person's preferences embedded |

New resource, so a `UsersController` / `IUserService` / `UserService` trio is created once,
per the vertical-slice recipe. `POST /api/auth/register` gains two required body fields.

`UserProfileResponse` **embeds `BeveragePreferenceResponse`** rather than declaring its
own preference shape. That DTO carries nothing owner-only, and a second read shape for the
same data is how the two drift. Its doc comment, which currently says "as its owner sees
it", is corrected.

`user.not_found` (404) is the only new error code. It has one caller, so it lives as a
private member on `UserService`, matching the precedent `AuthService` sets rather than
`BeverageErrors`, which exists because its codes have two callers each.

## Reuse: what is actually shared

The withdrawn requirement would have made one component serve both an editing and a
read-only purpose. The principle that replaced it: **reuse the stable pieces - tokens,
primitives, slots, behaviour directives - never one class pretending to be several UIs.**
That is also what `Docs/ARCHITECTURE.md` locks under "Reusable components", so the create
page stays a form and nothing else, and no decision entry is needed to reconcile anything.

Read against the existing code, the honest overlap between the create form and a read-only
view is exactly three things, and each moves to where both features can reach it without
either importing the other:

| Piece | Today | Becomes |
| --- | --- | --- |
| `describeAmount(amount, unit)` - the pure function phrasing `"2 tsp"` / `"Yes"` / `"None"` | `features/preferences/data-access/preferences.models.ts` | `shared/utils/amount.ts`, with the minimal `AmountUnit` shape it reads |
| The title-plus-term/value card at `preferences-page.html:28` | Inline markup, one call site | `shared/ui/detail-card/`, domain-free: a `title`, `rows` of `{ term, value }`, and an `empty` message |
| The transport-to-domain mapping instinct | `preferences.models.ts` | Each feature keeps its own mapping. Only the pure function is shared. |

`detail-card` is a primitive, not a preference - hence the name. It takes strings, so
`shared/ui` keeps its no-feature-imports rule literally, and extracting it removes an
existing duplication rather than creating one: the My-preferences page adopts it too.

What is deliberately **not** shared: `PreferencesStore`, `PreferencesApi`, the create
form, and `BeverageSlot` / `SlotOption`. Those exist to build and submit a form. A profile
neither builds nor submits one, and a store that served both would be the overloaded class
this design is avoiding.

## Web

### The header search - `core/user-search/`

The autocomplete lives in the shell, so it is app chrome rather than a feature: one host,
one instance, alive for the life of the app. It gets a `core/` folder mirroring
`core/auth/` - contracts, API service, store and the component itself, all
`providedIn: 'root'`. This is an addition to the `core/` tree in `Docs/ARCHITECTURE.md`,
in the same way `Data/`, `Options/` and `Authorization/` were additions in D-0002, and it
is what keeps `core/layout/shell` from importing feature code.

Behaviour lives in the store, in RxJS, not in the component: `debounceTime(250)`,
`distinctUntilChanged()`, a 2-character floor, and `switchMap` so a slower earlier
response cannot overwrite a newer one. `nz-autocomplete` and `nz-input` are used as they
ship (D-0006). Picking a result navigates to `/users/{id}` and clears the input; a term
with no matches renders one disabled "No matches" row.

On small viewports the input collapses with the nav, which the shell already owns. The
header's layout is the one D-0012 documents: it sits in the same content band as the nav,
and Ant's 64px header line-height is left alone.

### The profile - `features/users/`

A lazy feature behind `authenticatedGuard`, with its store and API service on the route's
providers and its icons registered there, exactly as `features/preferences/` does.

- `/users/:id` renders a page header - display name, `@username`, and the preference count
  derived from the array length - above a grid of `app-detail-card`, one per preference.
- Zero preferences renders `nz-empty`.
- A 404 renders as a feature banner from the page's own template, per the three-error-
  classes rule.
- Route title is the static `'Profile - Coffeeg'`: the person's name is not known until
  after the load, and a dynamic document title is not worth a `Title` injection here.

`features/users/data-access/users.contracts.ts` declares **only the fields the profile
reads** from the endpoint - a narrower view of the same payload, not a copy of the API's
DTO.

**The trap this must not re-ship.** The API omits nulls (`WhenWritingNull`), so an absent
amount arrives as `undefined`, not `null`. That is the bug the last slice shipped and
fixed, and a new feature reading the same payload can reintroduce it. `amount` is typed
`amount?: number | null` and normalised with `?? null`, and the store spec sends the shape
the API actually sends - including an omitted amount - so "no ice" cannot render as "Yes".

### Registration

`firstName` and `lastName` controls on the register page, both required, max 64, with
`autocomplete="given-name"` and `"family-name"`. `RegisterRequest` gains both in
`auth.contracts.ts`. Server-side field messages already resolve case-insensitively through
`fieldErrorsFor`, so `FirstName` finds `firstName` with no change.

`AuthenticatedUser` and `UserResponse` gain both names as optional. Nothing in the shell
uses them yet - the header still shows the username - but the session is the natural place
for them and omitting them would mean a second call to learn your own name.

## Decisions this needs

Written to `Docs/DECISIONS.md` during execution, not before. Four record choices made
explicitly in session and are `Accepted`; one is a judgment call and stays `Proposed`.

| ID | Title | Status |
| --- | --- | --- |
| D-0016 | First name and surname are required for new accounts, and nullable in the database | Accepted |
| D-0017 | A preference is readable by any signed-in person and writable only by its owner | Accepted |
| D-0018 | User search is an upper-cased contains match over three columns, unindexed | Proposed |
| D-0019 | The header search lives in `core/user-search/`, not in a feature | Accepted |
| D-0020 | Cross-feature overlap is carried by `shared/`; no feature imports another | Accepted |

## Tests

Per the lock, every endpoint gets a service unit test and an HTTP test.

- `tests/Application.Tests/Services/UserServiceTests.cs` - matching on each of the three
  columns, case-insensitivity, a NULL-named account found by username, self-exclusion,
  limit clamping and default, ordering (prefix before contains), profile found, profile
  not found, a profile whose preferences are empty.
- `tests/Api.Tests/Controllers/UsersControllerTests.cs` - 401 anonymous on both, 200
  search, 400 on a one-character term, 200 profile with preferences, 404 on an unknown id,
  and **an assertion on the raw JSON that no response carries an email**.
- `FakeUserRepository` gains the two new methods; `AuthServiceTests` and
  `AuthControllerTests` are updated for the two new required fields.
- Web: `shared/utils/amount.spec.ts` (moved from the preferences store spec's coverage),
  `core/user-search/user-search.store.spec.ts` (debounce, the 2-character floor,
  `switchMap` cancellation, clear), `features/users/users.store.spec.ts` (mapping, the
  omitted-amount normalisation, an empty profile).

## Out of scope

- **Editing your own names after registration.** The request says "upon registration", and
  `Docs/domain/user-accounts.md` already lists profile editing as not-in-scope. Existing
  accounts therefore have no way to acquire a name.
- **Backfilling names** onto the three existing accounts, and the non-nullable columns
  that would follow.
- **Searching by email.** Email is the sign-in credential; making it findable would let
  anyone confirm an address is registered, which is the leak
  `auth.invalid_credentials` exists to prevent.
- **An opt-out of being discoverable.** Considered and rejected in session: it would need a
  settings screen in this slice and would leave search returning nothing until people
  opted in.
- **The whole composition on a profile card.** Only saved lines render, so "no ice" and
  "never mentioned ice" look alike - which rule 8 says are the same statement anyway.
- **Paging the search results.** A capped `limit` is the whole of it; an autocomplete that
  pages is a different interaction.
- **A profile route for yourself.** `/users/{your id}` will render, but nothing links to
  it and "My preferences" stays the way you see your own.
