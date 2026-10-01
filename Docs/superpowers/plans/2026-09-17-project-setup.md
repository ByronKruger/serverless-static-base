# Plan: project setup for the Coffeeg API and web application

> **Paths and names in this plan are pre-extraction.** It describes a single `Coffeeg.Api`
> project, which D-0021 split into `src/platform/Platform.Common` plus a host, and
> `Coffeeg.*` became `Platform.*`. The decisions it made still hold; the file paths do not.
> Kept as history; not rewritten.

Status: complete
Date: 2026-09-17, decisions accepted 2026-09-18
Scope: architecture, directory structure and project setup for `src/api` and `src/web`.
Explicitly out of scope: any feature (beverage types, ingredients, preferences, roles).

## Goal

Turn `Docs/ARCHITECTURE.md` from prose into a repository that compiles, tests and runs,
so the first feature is a vertical slice through existing structure rather than a
green-field decision.

## Decisions taken along the way

`Docs/DECISIONS.md` D-0001 to D-0008, all `Accepted`. The log was renumbered from the top
on 2026-09-18: the three original entries were treated as implemented and dropped, so the
entries written during this work now start at D-0001.

## Steps

1. **Resolve the host conflict.** `Docs/ARCHITECTURE.md` locks an ASP.NET Core Web API;
   the then-current decision log named an "ASP.NET Functions Application". Asked the human;
   answer was "Web API now, Functions-ready". Recorded as D-0001; the Functions entry was
   retired. Done.
2. **Backend skeleton.** Solution, `src/api` project, central package management,
   `Directory.Build.props`, repo `NuGet.config`, `.editorconfig`, `.gitignore`. Done.
3. **Cross-cutting backend plumbing.** Result pattern, RFC 9457 error model and global
   exception handler, FluentValidation edge filter, AutoMapper registration, JWT bearer
   with default-deny authorization, CORS from validated options, EF Core context, OpenAPI
   with the bearer scheme, `IClock` / `ICurrentUser` ports and adapters, kebab-case route
   transformer, `ApiControllerBase`, and an anonymous `GET /api/health` so the pipeline has
   a smoke test. Done.
4. **Test harnesses.** `tests/Application.Tests` with fake ports and `Result` tests;
   `tests/Api.Tests` with `WebApplicationFactory` over in-memory configuration and an
   in-memory database. Done - 8 tests pass.
5. **Frontend skeleton.** Angular 22 workspace (standalone, zoneless), ng-zorro-antd,
   Tailwind v4 without preflight, design tokens and the five locked breakpoints, runtime
   `config.json` loaded before bootstrap, functional interceptors, `ApiError` mapping,
   global `ErrorHandler`, app shell with top navigation, three-column content region,
   page header, not-found page, lazy routing. Done - production build passes.
6. **Documentation.** Per-project `AGENTS.md` files, root `README.md`, decision entries.
   Done.
7. **Close the documentation drift.** With D-0002 accepted, `Docs/ARCHITECTURE.md` now
   carries the full `src/api` and `src/web` trees, the real middleware order in place of
   the `(X)` placeholder, and a persistence lock that names `Data/` and the repository
   ports rather than a non-existent Infrastructure layer. Done.

## Verification

```bash
dotnet build                     # Build succeeded, warnings as errors
dotnet test                      # 5 + 3 passed
cd src/web && npm run build      # bundle generated, feature chunks lazy
cd src/web && npm test           # 1 passed
```

## Open questions for the human

1. **Repository name.** `serverless-static-base-2` no longer describes the host. Rename
   the repository, or plan the move to Functions that D-0001 keeps cheap.
2. **AutoMapper licence, only if Coffeeg is ever commercialised.** D-0004 rests on the
   project sitting below the free-use threshold. If that changes, buy a licence or
   supersede D-0004 with the Mapperly option.
3. **CI.** Nothing pins the toolchain outside a developer machine yet. D-0008's follow-up
   is to pin Node 22 and npm 11 in the first pipeline.

## Next step

The first vertical slice. `beverage-types` is the natural first resource: it is the
aggregate every preference hangs off, and it exercises the moderator policy that already
exists in `Authorization/Policies.cs`.
