# Requirements coverage

Exercise brief → implementation. One list item per requirement — edit this file, the
page updates; no template changes needed. See src/app/home/requirements.ts for the format:
`- Label — description` per line, optional trailing `::pending` flag, `**bold**` and
`[text](url)` supported in the description.

- Candidate-authored user story — "As a user, I can CRUD my own tasks — visible only to me."
- Database: app table + users table, PK and 2+ fields — SQLite — Tasks + Users, PK/FK, auto-seeded.
- Web API with CRUD endpoints — Basic.API — full CRUD on /api/tasks; [OpenAPI docs via Scalar ↗](https://qa-demo-api-a3dhdwf0aqdbcnck.centralus-01.azurewebsites.net/scalar/v1).
- Second API: create user, login, authorized and anonymous endpoints — Basic STS.API — register, login (JWT), /me; no username enumeration.
- Data access layer — Basic.Data — EF Core + SQLite repositories.
- Independent business logic layer — Basic.Core — zero dependencies, owns validation and rules.
- Unit tests for all components — 38 tests — 32 xUnit plus 6 Vitest UI tests.
- Responsive, user-friendly frontend with full CRUD — Angular 21 — login drawer, guarded routes, JWT interceptor, inline editing.
- README with setup docs, seed data and credentials — Setup steps, endpoint tables, seeded demo account.
- GenAI deliverable — This app is the GenAI deliverable — built with Claude Code. ::pending
- Presentation & code review — walked through live with the panel: user story, design decisions, architecture, and a working demo. ::pending
