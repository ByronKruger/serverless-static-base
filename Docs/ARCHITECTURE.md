## Purpose 

- HTTP API, not a UI host
- Angular based front-end web-app as client 

## Locks (do not reinterpret)

### ASP.NET server-side application

- Host: ASP.NET Core Web API, C#, nullable enabled.
- Presentation: thin MVC controllers ([ApiController]). Async actions. Last parameter is CancellationToken. No business logic, no DbContext, no EF entities in controllers. Not Minimal APIs.
- Features are horizontal 3-tier layering 
- Application: use-case / addition to horizontal layer. Return `Result<T>` for expected outcomes.
- HTTP success: raw DTO / resource. No { success, data } envelope.
- HTTP errors: RFC 9457 Problem Details (application/problem+json).
- Validation, DTOs, mapping, serialization: use AutoMapper. Request validation at the edge (FluentValidation preferred for anything beyond attributes; DataAnnotations acceptable for simple required/length). System.Text.Json only. Enums as strings unless a binary protocol is introduced. CreateXRequest / UpdateXRequest / XResponse. Do not reuse one XDto for all verbs.
- AuthN: JWT bearer resource server. Default deny (authenticated required unless endpoint is explicitly anonymous).
- AuthZ: policy-based permissions at the endpoint; resource/ownership rules in the application layer. Named policies, not ad-hoc role strings. Every new controller authorized by default.
- JSON: System.Text.Json, camelCase, UTC timestamps as ISO-8601.
- DI composition root: Program.cs only.
- Persistence: behind Infrastructure. Code-first EF Core; entities never become API contracts.
- Tests required for every new endpoint: handler unit test + API integration test.
- Request pipeline (middleware order). (X)
- Result pattern and exceptions. Expected failure is data. Unexpected failure is an exception. Do not throw NotFoundException as control flow if the handler already returns Result. Do not catch Exception in handlers and return 200.
- DI, configuration, options. Container is the composition root. Options fail fast at startup. `IOptions<T>` with validation attributes. Missing secrets = process does not start.
- Testing: `tests/Application.Tests`: `{Resource}Service` in isolation. Fake ports only (`IOrderStore`, `IClock`, `ICurrentUser`). No ASP.NET, no `DbContext`, no database. `tests/Api.Tests`: `WebApplicationFactory`. Hit HTTP. Assert status, response DTO shape, Problem Details (`type`, `title`, `status`, `traceId`). Name tests by layer: `Services/{Resource}ServiceTests`, `{Resource}ControllerTests`.Do not new up controllers with a mocked service as the API test. That skips the pipeline (auth, Problem Details, binding). Do not spin Kestrel + EF to test a service method. If you must, the service is not isolated.

### Angular web-application

- Framework dialect: Angular 22+, standalone, zoneless, OnPush implicit. Use @if / @for / @switch. @for always has track. Use inject() for DI. Keep component state in signals.
- Workspace: Single application structured by feature. feature owns its routes, pages, store, and API. core/ contains only true singletons used once per app. shared/ui has no features or data-access imports.
- State management: expose readonly signals and methods. Keep writeableSignal private. Derive with computed. Provide feature stores at the route (`providers` on the route config) so they die with feature. Keep async orchestration in RxJS. Components talk to a store or facade, not to `HttpClient` plus sibling components' signals.
- Reactivity and change detection: Standalone + signal inputs/outputs + implicit OnPush + zoneless. Templates are signal-driven. Component class state is signals. Child inputs are input(), input.required(), model(). Outputs are output()
- HTTP : HttpClient via a feature facade / data-access service. provideHttpClient once in app.config.ts.
- Interceptors: Functional only: auth, tracing, error shape. No success-body rewrite.
- Forms: Reactive forms.
- Styling: Tailwind for layout / spacing / type / responsive + design tokens (CSS variables). 
- Component library: Ant Design language. Use out of the box components. No overrides.
- Responsive and layout: mobile-first utilities. App shell owns navigation collapse. Pages compose shell + page header + content region. Container queries on reusable cards; viewport media on the shell. Breakpoints: 320px, 481px, 769px, 1025px, 1201px. Navigation is top. Layout is 3-column for larger screens and collapse to single column for small screens.
- Config: Runtime `config.json` (or equivalent) loaded before bootstrap.
- Icons: One set, matching the component library.
- Reusable components: Classify every UI piece. Do not stretch one component across classes. Never add mode: 'create' | 'edit' | 'view' | 'approver' | 'mobile' that changes template structure. That is several components or content projection. Prefer content projection and directives over boolean soup.
- Errors: 3 error classes, 3 UIs. Validation: form field/form-level messages; Domain/Http mapped `ApiError`: feature template `@if (res.error())` as feature banner; unexpected exception: global `ErrorHandler` -> reporting sink. Map HTTP failures to ApiError at the data-access / interceptor-error layer, not in each page. Decide per call whether a toast is appropriate. Global interceptor toasts are forbidden as the only policy.
- API contract: types at the data-access edge. UI speaks domain models, not transport shapes.
- Dependency Injection: inject() everywhere. providedIn: 'root' only for true singletons (AuthStore, config, HTTP helpers). Feature services and stores go on route providers. Injection tokens for config and for adapters (HTTP vs mock).
- Naming and files: current Angular CLI file grammar for new files. One component per file.  lock suffix policy in the CLI schematic (component suffix or not) and do not mix.
- Performance: lazy-load every feature route. 

## Target folder tree

### ASP.NET server-application

src/api src/api/Interfaces (with nesting for /Repositories, /Services, etc.) src/api/Services src/api/Repositories src/api/Helpers src/api/Dtos src/api/Results src/api/Exceptions src/api/Entities src/api/Validation src/api/Controllers src/api/MapperProfiles src/api/Extensions src/api/

### Angular Web-app

src/app/
  core/                 # auth, interceptors, error handler, config - root singletons
  shared/ui/            # presentational primitives and patterns only
  shared/utils/         # pure functions, no feature imports
  `features/<name>/`      # routes, pages, feature store, data-access
  app.routes.ts
  app.config.ts

## Vertical slice feature recipe (agents start here)

### ASP.NET server-application

When adding a capability under horizontal layering: edit the existing resource types across layers. Do not create a Feature/{UseCase}/ folder.

- Identify the resource. Resource = the REST noun (`beverages-types`, `users`). If `OrdersController` / `IOrderService` already exist, extend them. If its the first endpoint for the noun, create the controller + service pair once.
- HTTP contract (application contracts). Add or extend request/response types in `src/api/Dtos/` and `src/api/Results`. Put contract types that are purely transport attributes into `/Dtos` / `/Results` as well (`[FromQuery]` models that never leave presentation). Prefer contracts so handlers/services stay ASP.NET-free.
- Validation. Validator besides the request type, or in `src/api/Validation/{Resource}`. One validator per request type. Do not validate on the EF entity.
- Domain (only if an invariant appears). Domain errors are typed and become `Result` failures.
- Application service method. Add `Task<Result<T>> {Verb}Async(...)` to `I{Resource}Service` and `{Resource}Service`. One public method per use case. Private helpers stay inside that service. Method body - authorize resource rules -> load via port -> mutate/query -> map to DTO -> `Result`. Pass `CancellationToken` through every await.
- Controller action. Add an action to `{Resource}Controller`. Body: bind request DTO -> `await _service.{Verb}Async(...)` -> `Result` to `ActionResult`. Stay under ~ 15 lines. No `try/catch` for domain outcomes. Status codes: create `201` + `Location`; missing `404`; validation `400` Problem Details; conflict `409`; authz `403`; anonymous `401`.
- Authz. Endpoint policy on the action. Ownership/resource rules inside the service method, not the controller.
- OpenApi. Document status codes on the new action. OperationId stays stable.

Do not:
- Create `Features/{UseCase}/` or a class per verb (`CreateOrderService`).
- Put SQL/EF in controllers or Domain; return entities; throw for expected misses; success envelopes; `Common/Helpers`.
- Create `IOrderManagementService` because `IOrderService` "felt full".
- Put `DbContext`, SQL, or EF entities in the controller or Domain.
- Return entities or `IQueryable`.
- Reuse one `OrderDto` for create, update, and get.
- Throw for expected misses if the method already returns `Result`.
- Add a success envelope.
- Add `Common/Helpers` for logic that belongs on `{Resource}Service`.