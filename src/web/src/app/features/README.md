# Features

One folder per feature. A feature owns its routes, pages, store, and data-access,
and nothing outside it imports from within it.

```
features/<name>/
  <name>.routes.ts        # lazy route config; feature providers go here
  pages/                  # routed components
  data-access/            # HTTP services and the transport types they map from
  <name>.store.ts         # signal store, provided on the route
```

Rules that matter here (see `Docs/ARCHITECTURE.md`):

- Every feature route is lazy-loaded from `app.routes.ts`.
- Feature stores are provided on the route's `providers`, never `providedIn: 'root'`,
  so they are destroyed with the feature.
- Components talk to the store or facade - never to `HttpClient` directly.
- Transport types stop at `data-access/`; the rest of the feature speaks domain models.
