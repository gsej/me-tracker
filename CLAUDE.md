# CLAUDE.md

Guidance for working in this repo. Architecture and deployment are documented in
`README.md` — this file focuses on how to build/test/run and the conventions and
gotchas that aren't obvious from the code.

## What this is

A real, working weight-tracking app that is deployed and used by several users —
treat it as production software, not a toy. It's currently being polished because
the owner wants to base a **new** app on the same structure, so clean, reusable
patterns (test scaffolding, clear layering) are valued — but being a template is
a nice-to-have, not the repo's purpose. Don't sacrifice the working app for
template-ness.

## Layout

- `code/dotnet/` — ASP.NET Core Web API (`Api/`) + xUnit tests (`Tests/`), `me-tracker.sln`.
- `code/angular/` — Angular PWA frontend.
- `code/tests/rest/` — `.http` files for hitting the API by hand.
- `code/tests/jest/` — Jest setup (frontend).
- `data/create_tables.sql` — reference schema (tables are also auto-created by the repositories).

## Toolchain (current)

- **.NET 10** (`net10.0`) — both `Api` and `Tests`.
- **Angular 22**, **TypeScript ~5.9**. Uses the `@angular/build` application builder.
- **Node**: Angular 22 requires Node ≥ 22.22.3 (or ≥24.15.0 / ≥26). CI uses `22.x`.
- **`@angular/build` must track the Angular major** — after `ng update` it can get
  pinned to a newer major than core (e.g. 22 while on 19); fix it to match.

## Common commands

Backend (from `code/dotnet/`):
```bash
dotnet build
dotnet test                     # xUnit; 52 tests
dotnet run --project Api        # local API on http://localhost:5200
```

Frontend (from `code/angular/`):
```bash
npm start                       # ng serve on http://localhost:4200
npm run build                   # output to dist/me-tracker/browser
npx ng test --watch=false --browsers=ChromeHeadless
```

Local dev: the API's `appsettings.json` uses a local SQLite file and an API key
of `apikey` (user `ApiUser`). The Angular app reads `apiUrl` at runtime from
`settings.json`, which points at `localhost:5200` in source control.

## Testing conventions

- **xUnit + AwesomeAssertions** (a FluentAssertions fork — do **not** add
  FluentAssertions) + **NSubstitute** for mocks.
- **Integration tests** use `Tests/Integration/ApiTestFactory.cs` — a
  `WebApplicationFactory<Program>` that runs the API in-process against a
  throwaway temp SQLite file, with test API keys for users `alice`/`bob`. Reuse
  it for any endpoint/auth/per-user-scoping test. `Program` is a non-static
  class specifically so it can be the factory's entry point.
- **Repository tests** (`Tests/DataAccess/`) run against a temp SQLite file per
  test class.
- Angular: the default CLI scaffold specs were removed. There are currently **no**
  Angular specs, so `ng test` errors with `TS18003` until a real spec is added.
  CI only builds the frontend; it does not run `ng test`.

## Domain / API notes

- **API-key auth** via the `X-API-Key` header (`ApiKeyAuthFilter`); keys come from
  the `ApiKeys` config value. Every request is scoped to the key's `UserId`.
- **Per-user isolation** is a real invariant — repositories filter by `userId`;
  one user must never read another's data. Keep it covered by tests.
- **Weights are soft-deleted** (a `Deleted` flag); normal reads exclude them, the
  backup endpoint reads everything.
- **A weight entry carries an optional free-text `Comment`** — nullable, capped at
  200 chars (via `[MaxLength]` on the request records; `[ApiController]` turns that
  into an automatic `400`), with empty/whitespace normalised to `NULL` in the
  repository. Set it on create, or edit it later via `PUT /api/weight/{id}`.
- **Schema changes use a guarded `ALTER TABLE`, not a migration framework** —
  `WeightRepository.EnsureTablesExist` adds any missing column idempotently
  (checking `PRAGMA table_info` first), so a deployed database picks up new columns
  on next start. This is how `Comment` was added; follow the same pattern.
- **Dates are stored/returned as UTC** regardless of server timezone (there's a
  regression test guarding this) — preserve that when touching the data layer.
- SQLite persistence stores dates as ISO `"O"` and decimals with `InvariantCulture`.

## Conventions

- The API is polished as a skeleton; the `TODO` file at the repo root tracks
  ongoing work and known issues.
