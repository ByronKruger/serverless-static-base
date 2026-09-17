# Decisions

Single log for architecturally significant choices. Agents: read this file before changing strcture, stack, or contracts. See `AGENTS.md` and Architecture decisions.

When this file becomes hard to scan (roughly 8-15 accepted entries, or one entry needing its own options appendix), split into `adr/` per the migration notes in `AGENTS.md`. Until then, do not create other decision files.

## Index

| ID | Title | Status | Date |
| --- | --- | --- | --- |
| D-0001 | Record decisions in this file | Accepted | 2026-09-16 |
| D-0002 | Angular 22 SPA for the frontend | Accepted | 2026-09-16 |
| D-0003 | ASP.NET Functions Application (API) for the backend | Accepted | 2026-09-16 |

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