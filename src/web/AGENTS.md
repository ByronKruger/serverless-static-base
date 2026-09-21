# src/web - coffeeg-web

Read the root `AGENTS.md` first. This file only maps the workspace; the rules live in
`Docs/ARCHITECTURE.md` and the choices in `Docs/DECISIONS.md`.

Angular 22, standalone, zoneless, signals. Ant Design via `ng-zorro-antd` (D-0006),
Tailwind v4 for layout only and with preflight deliberately off (D-0007).
Node 22 with npm 11 (D-0008).

## Where things go

```
src/app/
  core/            # true singletons, used once per app
    auth/          # AuthStore (root), AuthApi, AuthStorage, auth interceptor, role names
    config/        # AppConfig contract + the pre-bootstrap loader
    errors/        # global ErrorHandler and the reporting sink token
    http/          # ApiError model, api-error interceptor, tracing interceptor
    icons/         # the one icon set, registered explicitly
    layout/        # app shell, and the placeholder start page
    user-search/   # the header's search box: contracts, models, api, store, component
  shared/ui/       # presentational primitives only - no feature or data-access imports
  shared/utils/    # pure functions
  features/<name>/ # routes, pages, store, data-access; see features/README.md
  app.config.ts    # root providers
  app.routes.ts    # one lazy route per feature
```

## Conventions that are already decided

- **Components**: no `.component` suffix (matches the CLI's current grammar). Do not mix.
  One component per file, templates and styles in sibling files.
- **Change detection**: zoneless makes rendering signal-driven, so components do not
  annotate a strategy. Keep class state in signals; inputs are `input()` /
  `input.required()` / `model()`, outputs are `output()`.
- **DI**: `inject()` everywhere. `providedIn: 'root'` only for true singletons. Feature
  services and stores go on the route's `providers` so they die with the feature.
- **State**: a store exposes readonly signals and methods; the writable signal stays
  private; derive with `computed`. Components talk to a store or facade, never to
  `HttpClient`.
- **HTTP**: interceptors are functional and registered once in `app.config.ts`, in order -
  tracing, auth, error mapping. None of them rewrites a success body, and none of them
  toasts: whether a failure deserves a toast is a per-call decision.
- **Errors**: three classes, three UIs. Form validation renders on the field; a mapped
  `ApiError` renders as a feature banner from the feature's own template; anything else
  reaches `GlobalErrorHandler` and goes to the reporting sink.
- **Contracts**: transport types stop at a feature's `data-access/`. The rest of the UI
  speaks domain models.
- **Styling**: Tailwind utilities for layout, spacing, type and responsive behaviour.
  Ant Design components are used as they ship - no overrides. Design tokens are the
  `@theme` variables in `src/styles.css`; breakpoints are 320, 481, 769, 1025, 1201.
  Container queries for reusable cards, viewport media for the shell.
- **Routing**: every feature route is lazy-loaded. The shell owns navigation collapse;
  pages compose the page header and a content region.

## Authentication

`/auth/login` and `/auth/register` are the two pages; both are lazy-loaded from
`features/auth/`. People sign in with **email and password** (D-0009) - the username is
collected at registration and shown in the header, but it is not a credential.

The session - user plus token plus expiry - is held by `AuthStore` in
`core/auth/` and persisted to `localStorage` (D-0011), which is what keeps a reload
signed in. `AuthStorage` guards every access: storage throws outright in a private
window, and the app must still start as signed out rather than fail.

The navigation header reads `AuthStore.userName()` and `isAuthenticated()`; the log-out
button calls `signOut()`, which clears both the signal and storage. Sign-out is local
only - the token stays technically valid until it expires (D-0010).

Pages never call `AuthApi` or `HttpClient` directly; they call the store and subscribe.
Server-side field messages are read with `fieldErrorsFor()`, which matches the API's
`UserName` against the control's `userName` case-insensitively.

## Beverage preferences

`features/preferences/` is the second feature and the template to copy: lazy route, store
and API service on the route's `providers`, transport types confined to `data-access/`,
and its own icons registered in `preferences.routes.ts` rather than in `core/icons`.

The create form is **built from the API response**, not from anything written here. No
drink, ingredient or unit name appears in the TypeScript or the template. A moderator
adding a beverage type changes this page without anyone editing it - that is the point.

`data-access/preferences.models.ts` is the one place worth reading first. Its mapping is
not a rename: the API describes a *catalogue* (an ingredient, which may have variants),
and the form needs *slots* (a thing to decide about, with one or more options). A simple
ingredient becomes a slot with exactly one option, so the template never branches on
"simple or complex" - it just renders a select when `requiresChoice` is true.

Two traps the tests pin down:

- **The unit belongs to the option, not the slot.** Two variants of one ingredient may be
  measured differently, so `unitFor()` reads the chosen option and `chooseOption()` resets
  the amount when the choice changes.
- **The API omits nulls** (`WhenWritingNull`), so an absent amount arrives as `undefined`,
  not `null`. `BeveragePreferenceIngredientResponse.amount` is typed `amount?: number |
  null` and normalised with `?? null` on the way in. Skip that and "no ice" renders as
  "Yes".

A `Boolean` unit is a switch, and it maps to `1` for yes and `null` for no in exactly one
place - `toAmount()` in the create page.

`core/auth/authenticated.guard.ts` keeps signed-out people off these routes and passes a
`returnUrl` the login page honours. It is a convenience, never the boundary: the API is
default-deny and is what actually enforces access.

## Finding people, and their profiles

Two homes, on purpose. The **search box is app chrome**, so it lives in
`core/user-search/` mirroring `core/auth/` - one instance, mounted by the shell, alive for
the life of the app (D-0019). The **profile is a feature**, so `features/users/` owns it
and nothing else, lazily, with its store on the route.

`UserSearchStore` keeps the orchestration in RxJS: a 250 ms debounce, a two-character
floor matching the API's own, and `switchMap` so a slow earlier response cannot overwrite
a newer one. The term arrives through a `Subject` rather than a signal read by
`toObservable`, so the pipeline has no effect to flush and its spec needs nothing but fake
timers.

Four traps the specs pin down:

- **`(selectionChange)` belongs on `nz-autocomplete`, never on `nz-auto-option`.** The two
  outputs share a name and are not interchangeable. The option's fires whenever ng-zorro
  changes that option's selected flag - including the `deselect()` it runs across *every*
  option each time the list changes (`resetActiveItem`, reached from both `updateStatus`
  and the options-changed subscription). Bound there, the dropdown dismissed itself and
  navigated to a person nobody clicked, the moment results arrived. The component's output
  sits behind ng-zorro's own `filter(e => e.isUserInput)` and is the only trustworthy
  signal. `user-search.spec.ts` renders the real component and fails if that binding moves
  back.
- **The search input deliberately has no `ngModel`.** The trigger directive reads the typed
  text off the DOM element and its `onChange` is a no-op until a control registers, so a
  model buys nothing - and costs something: on a pick the trigger writes the chosen
  option's `nzValue` back through that model, which would start a fresh search for the id
  just navigated to. Plain `[value]` plus `(input)` avoids it. `nzValue` carries the id and
  `nzLabel` the display name, because `getLabel()` falls back to `String(nzValue)`.
- **`catchError` goes inside the `switchMap`.** An error allowed to reach the outer
  pipeline completes it, and the search box is then dead for the rest of the session with
  nothing on screen to say so.
- **An unexpected error is handed to `ErrorHandler`, not thrown.** A page rethrows and lets
  `GlobalErrorHandler` take it; a long-lived stream cannot, so it reaches the same sink
  directly. The profile page does the same, for the same reason.
- **The profile loads from `toObservable(this.id)`, not in the constructor.** A required
  input has no value during construction - reading it there is a compile error (NG8118) -
  and the router reuses the component when you go from one profile straight to another, so
  a one-shot load would leave the previous person on screen. The id also needs
  `numberAttribute`: a route parameter arrives as a string.

## What the two features share, and what they do not

`features/users/` and `features/preferences/` **do not import each other** (D-0020). The
overlap is carried by `shared/`:

- `shared/utils/amount.ts` - `describeAmount`, the one definition of how an amount reads
  (`"2 tsp"`, `"Yes"`, `"None"`), over a two-field `AmountUnit` both features' unit models
  structurally satisfy.
- `shared/ui/detail-card/` - a `title` plus `{ term, value }` rows. Domain-free and named
  for what it is, so `shared/ui` keeps its no-feature-imports rule literally. Both the
  My-preferences page and the profile render it; extracting it deleted the `<dl>` that was
  inline in the first.

Each feature keeps its own transport contracts and its own mapping. `users.contracts.ts`
declares only the fields the profile reads - a narrow view of the same payload, not a copy
of the API's DTO.

Not shared, deliberately: `PreferencesStore`, `PreferencesApi`, the create form, and
`BeverageSlot` / `SlotOption`. Those exist to build and submit a form, which a profile
neither builds nor submits. Map card rows in a **`computed`**, not a method called from the
template.

**The omitted-amount trap applies here too.** The API omits nulls, so `amount` arrives as
`undefined`; `users.models.ts` normalises with `?? null` and `users.store.spec.ts` sends
the shape the API really sends. Skip it and "no water" renders as "Yes" - the bug the
preferences slice shipped, now available to a second reader of the same payload.

## Commands

```bash
npm start          # dev server on :4200, /api proxied to https://localhost:7011
npm run build      # production build into dist/
npm test           # vitest via @angular/build:unit-test
```

Runtime settings live in `public/config.json` and are fetched before bootstrap, so the
same build artifact is promoted between environments. Read them by injecting `APP_CONFIG`;
never read `window` or an `environment.ts`.
