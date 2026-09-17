# Coffeeg Agent Instructions

This file is the shared source of truth for repo conventions for all agents. Keep it short and use it as a map to the deeper project and domain documentation.

## Required Reading

- Use `Docs/domain/README.md` to select the relevant domain file before making business-rule changes.
- When editing inside a project folder, also read that folder's per-project `AGENTS.md` sibling (in addition to this root file).
- For complex work, follow the Superpowers planning flow and write future specs/plans under `Docs/superpowers/`.

## Repository Map

| Area | Purpose |
| `Docs/domain` | System-of-record business and implementation knowledge |
| `Docs/superpowers` | Current Superpowers specs and implementation plans for agent-driven work |
| `src/api` | Server-side ASP.NET application  |
| `src/web` | Front-end Angular based web-application |

## Architecture Invariants

- Business logic goes into the service layer.
- Persistence and data access go into Data-access layer.
- Controllers stay thin and delegate to service layer methods.

            ## Backend Conventions

            ## Frontend Conventions

## Harness Commands

- Standard command usage for both `src/api` and `src/web` projects. ASP.NET: 'dotnet' based commands, e.g. 'dotnet run', 'dotnet build', EF ('dotnet-ef data migrations add', 'dotnet-ef database update'). Angular/Node: e.g. 'npm run start', 'npm run build'.

## Documentation Rules

- `AGENTS.md` is a table of contents and guardrail file, not an encyclopedia.
- Domain knowledge belongs in `Docs/domain`, indexed by `Docs/domain/README.md`.
- Team skills belong in `Docs/skills`
- Accepted architecture-wide decisions belong in `Docs/ARCHITECTURE.md`
- Architecture decision records live in `Docs/DECISIONS.md` (indexed by the same file).
- When docs and code disagree, trust the code, fix the docs, and mention the drift.

## Planning Rules

- Use an ephemeral plan in the chat for small, local changes.
- Use Superpowers planning for multi-step work, cross-project work, risky domain changes, or changes that need handoff.
- Put new design specs under `Docs/superpowers/specs/` and implementation plans under `Docs/superpowers/plans/`.
- Keep plans updated with decisions, verification commands, and open questions.

## Architecture decisions

- This file (`AGENTS.md`) does not record decisions. It only tells you how to read and write them.
- Architecture decision records live in `Docs/DECISIONS.md`, and is also indexed by the same file (`Docs/DECISION.md`).

### Before you change structure, stack or contracts

1. Read all of `Docs/DECISIONS.md`.
2. Treat every entry with status `Accepted` as a constraint. Do not silently contradict it.
3. If the task would violate an accepted decision, stop. Either
    - Implement inside the accepted decision, or
    - Propose a new entry that supersedes the old one and wait for a human to accept it.
4. If `DECISIONS.md` and the code disagree, the code is ot automatically right. Flag the mismatch; do not "fix" the log to match a drift unless the human asked you to.

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




















































# Agent rules
Greenfield ASP.NET Core Web API. No existing samples yet - follow this file
until Features/ contains a reference use case.

## Locks (do not reinterpret)
- Controllers + [ApiController], not Minimal APIs.Done
- Features/{Feature}/{UseCase}/ : request, validator, handler, response.X
- Handler returns Result<T>. Expected failures are Result, not exceptions.Done
- Success = raw DTO. Errors = Problem Details. No success envelope.
- Mapping: explicit or Mapperly. No AutoMapper.X
- JWT bearer, default deny. Named policies, not ad-hoc role strings.Done
- EF/DbContext only in Infrastructure. Entities are not HTTP contracts.Done
- System.Text.Json, camelCase, UTC. CancellationToken on actions/handlers.

## Layout (create if missing)
src/api src/api/Interfaces src/api/Services src/api/Repositories src/api/Helpers src/api/Extensions
src/web with standard Angular file structure with /components /services

Domain: no Microsoft.AspNetCore.*, no EF.

## Before coding a feature
If horizontal features has no completed use case yet, read ARCHITECTURE.md Locks
and the feature recipe. After the first use case exists, copy it.

## Do not build yet
Second database, Minimal APIs, GraphQL, outbox, cookie auth, Identity UI.
If the task needs one: write an ADR, wait for approval.

            ## Verify
            dotnet test
            New convention -> one line here, one ADR if it is a real choice.

