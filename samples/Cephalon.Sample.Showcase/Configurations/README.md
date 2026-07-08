## Showcase Split Configuration

This sample uses Cephalon's split configuration convention instead of a single `showcase.settings.json`.

Load order:

- `Configurations/**/Add*.json`
- `Configurations/**/{Environment}.json`
- `SHOWCASE_*` environment variables

The Showcase host now resolves the environment from `DOTNET_ENVIRONMENT`, then `ASPNETCORE_ENVIRONMENT`, and falls back to `Development`.

Showcase intentionally keeps its authored settings under grouped `Development.json` files so the sample makes the available config surface obvious at a glance.

Current layout:

- `ConnectionStrings` keeps shared named connections for the sample infrastructure.
- `ConnectionStrings` keeps explicit PostgreSQL names for the sample `write`, `read`, and `history` roles so each logical database can move independently.
- `Engine/*` splits the engine and companion-pack surface into focused folders.
- `Observability` keeps provider-specific logging configuration such as the sample Serilog console profile.
- `OpenApi` keeps documentation route/version settings separate from descriptive metadata.
- `ApiRoutes` keeps public transport prefixes and result-envelope behavior together.
- `ReferenceDocs` keeps hosted reference-doc defaults separate from the runtime engine section.

Database behavior:

- `Engine/Databases/*` is wired migration-first for the showcase sample.
- Provider selection is configuration-driven. `Engine:Databases:*:Provider` decides whether a role uses `PostgreSql`, `InMemory`, or another supported provider.
- The checked-in `Development.json` and `Local.json` profiles point at Docker Desktop-backed PostgreSQL roles and apply EF Core migrations on startup for `write`, `read`, and `history`.
- Tests can override the same keys to isolated `InMemory` roles without changing host code.
- `dotnet ef` is supported through design-time DbContext factories for `ShowcaseWriteDbContext`, `ShowcaseReadDbContext`, and `ShowcaseAuditHistoryDbContext`.
- `Showcase:ReadModelProjection:HostedServiceEnabled` defaults to `true`. Override it to `false` only in tests or diagnostics that need to observe write/read separation before the background read-model catch-up loop can rebuild or process pending projection jobs.
- When the sample is running, `/api/v1/showcase/system/database-topology` gives the rich JSON operator-facing answer for the active role providers, migration targets, write/read row counts, and durable projection-job state resolved from those configuration files, `/api/v1/showcase/system/database-topology/brief` exports the same live answer as a shareable Markdown handoff, and `/api/v1/showcase/system/database-topology/handoff` packages a `README.md`, the brief, a machine-readable `handoff-manifest.json`, and the raw projection into one downloadable zip.

If you want a local-only profile, add `Local.json` beside any existing `Development.json` file, then run the sample with `DOTNET_ENVIRONMENT=Local` or `ASPNETCORE_ENVIRONMENT=Local`.
