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
- `ConnectionStrings` keeps explicit PostgreSQL names for the sample `write` and `history` roles, while the current `read` role intentionally shares the write database until the sample grows a projector-driven read store.
- `Engine/*` splits the engine and companion-pack surface into focused folders.
- `Observability` keeps provider-specific logging configuration such as the sample Serilog console profile.
- `OpenApi` keeps documentation route/version settings separate from descriptive metadata.
- `ApiRoutes` keeps public transport prefixes and result-envelope behavior together.
- `ReferenceDocs` keeps hosted reference-doc defaults separate from the runtime engine section.

Database behavior:

- `Engine/Databases/*` is wired migration-first for the showcase sample.
- Non-Docker runs still fall back to isolated in-memory databases per role so the sample remains zero-setup.
- Docker/PostgreSQL runs apply EF Core migrations on startup for `write` and `history`.
- `dotnet ef` is supported through design-time DbContext factories for `ShowcaseWriteDbContext` and `ShowcaseAuditHistoryDbContext`.

If you want a local-only profile, add `Local.json` beside any existing `Development.json` file, then run the sample with `DOTNET_ENVIRONMENT=Local` or `ASPNETCORE_ENVIRONMENT=Local`.
