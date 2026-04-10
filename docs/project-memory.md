# Cephalon Project Memory

Project memory in this document reflects the repository state observed on `April 11, 2026`.

This page is a repo-oriented orientation snapshot. It is meant to help contributors recover context quickly before they change code, docs, planning, or package surfaces.

Cross-references: `README.md`, `docs/README.md`, `docs/architecture.md`, `docs/architecture-inventory.md`, `docs/architecture-recommendations.md`, `docs/engine-roadmap.md`, `docs/engine-backlog.md`, `docs/compatibility.md`

## Identity

Cephalon is not being positioned as a single application shell.

The repository is aiming at a modular .NET engine/framework foundation that is growing into a modular runtime platform. The recurring design center is:

- host-agnostic contracts first
- deterministic module and package composition
- explicit app-model, transport, technology, and policy selection
- runtime introspection as a product surface
- additive companion packs instead of engine-core sprawl
- generated starter output, templates, and samples that stay aligned with the runtime contract

## Current technical baseline

- the repo is pinned to `.NET SDK 10.0.201` through `global.json`
- the shipped project baseline is `net10.0`
- notable packaging exceptions are the template pack and source generator surfaces that stay on `netstandard2.0`
- central package management is enabled through `Directory.Packages.props`
- repo-wide build defaults enable nullable reference types, implicit usings, XML doc generation, and warnings-as-errors
- the default Cephalon package version baseline is currently `0.1.0-preview`

## Repo shape at a glance

Current project counts from the workspace scan:

- `src`: 81 projects
- `tests`: 4 projects
- `samples`: 9 projects
- `templates`: 6 projects
- `playground`: 2 projects
- `benchmarks`: 1 project

These counts are useful as a scale indicator only. They will drift as new companion packs and samples are added.

## What the repository already ships

The repo is well past an early prototype. The following surfaces are already present and should be treated as part of the living product:

- `Cephalon.Abstractions` as the host-agnostic contract layer
- `Cephalon.Engine` as the composition, manifest, runtime, policy, package-loading, trust, and app-model center
- `Cephalon.AspNetCore` and `Cephalon.Worker` as the primary host adapters
- transport adapters for REST, JSON-RPC, gRPC, GraphQL, Server-Sent Events, and WebSocket
- `Cephalon.Scaffolding`, `Cephalon.Cli`, and `Cephalon.TemplatePack` as the adoption and generation surfaces
- `Cephalon.ReferenceDocs` plus the published `docs/reference/` output as the XML-doc publishing path
- `Cephalon.Observability` plus dependency-health, exporter, cloud, and provider companion packs
- `Cephalon.Behaviors` plus HTTP, messaging, pattern, and source-generator companion packages
- `Cephalon.Data` and `Cephalon.EventSourcing` plus relational and non-relational provider families
- samples that model intended blueprint shapes and reference modules that model intended package authoring

## Runtime and architecture anchors

The architecture docs and the codebase align around a few important truths:

- blueprints are not the same thing as design patterns, deployment topology, or transports
- Cephalon models app shape through multiple dimensions rather than one overloaded architecture label
- the built-in app blueprints are `modular-monolith`, `modular-vertical-slice`, and `microservice`
- the built-in suite blueprint is `microservice-suite`
- transports are first-class descriptors and are expected to stay aligned across runtime, scaffolding, CLI parsing, templates, samples, and docs
- technology profiles are additive workload hints, not excuses to explode the blueprint catalog
- the engine owns the runtime contract and generator surfaces should follow it instead of re-encoding their own semantics

Current built-in transport surface:

- `rest-api`
- `json-rpc`
- `grpc`
- `graphql`
- `server-sent-events`
- `websocket`

Current built-in technology profile surface includes:

- `agentic-workloads`
- `event-driven-integration`
- `knowledge-retrieval`
- `realtime-experience`
- `edge-native-delivery`
- `serverless-hosting`
- `identity-access`
- `multi-tenancy`
- `hybrid-cloud-runtime`
- `service-mesh-integration`

## High-value code anchors

When recovering context in code, these are good starting points:

- `src/Cephalon.Abstractions/Modules/IModule.cs`
- `src/Cephalon.Abstractions/Modules/ModuleDescriptor.cs`
- `src/Cephalon.Engine/Composition/EngineServiceCollectionExtensions.cs`
- `src/Cephalon.Engine/Runtime/IRuntime.cs`
- `src/Cephalon.Engine/Runtime/RuntimeOperationalStory.cs`
- `src/Cephalon.AspNetCore/Hosting/EngineWebApplicationBuilderExtensions.cs`
- `src/Cephalon.AspNetCore/Hosting/EngineWebApplicationExtensions.cs`
- `src/Cephalon.Worker/Hosting/WorkerHostApplicationBuilderExtensions.cs`
- `src/Cephalon.Cli/CliApplication.cs`
- `src/Cephalon.Behaviors/Services/BehaviorDispatcher.cs`
- `src/Cephalon.Abstractions/Behaviors/IBehaviorContext.cs`

These files anchor the public or cross-cutting runtime contract more reliably than any one sample or playground host.

## Introspection is part of the product

Runtime introspection is not secondary documentation. It is an explicit operator-facing surface.

Important routes and concepts that repeatedly appear across docs and code:

- `/engine`
- `/engine/manifest`
- `/engine/snapshot`
- `/engine/runtime-story`
- `/engine/modules`
- `/engine/packages`
- `/engine/diagnostics`
- `/engine/technologies`
- `/engine/technology-catalog`
- `/engine/technology-surfaces`
- `/engine/capabilities`
- `/engine/dependencies`
- `/health`
- `/health/live`
- `/health/ready`

If a change impacts lifecycle, packages, diagnostics, technologies, executions, or health semantics, it likely also impacts one or more of these routes and the docs that describe them.

## Adoption surfaces

The repository now treats adoption as a first-class concern, not as a later packaging task.

The main adoption paths are:

- `Cephalon.Cli` as a packaged `.NET tool` with the `cephalon` command
- `Cephalon.TemplatePack` as the `dotnet new` install surface
- `Cephalon.Scaffolding` as the shared rendering layer behind generated output
- `samples/` as adoption-quality blueprint examples
- `playground/` as freeform experimentation rather than the official starter baseline

The generated app baseline already includes publishing and deployment assets for:

- published output smoke
- Windows Service
- IIS
- Azure App Service
- container image publishing
- Azure Container Apps
- Kubernetes
- Linux `systemd`

## Documentation model

The repository uses two documentation layers on purpose:

- hand-authored Markdown under `README.md` and `docs/` is the primary human-facing product and adoption documentation
- XML comments on public contracts are the API explanation layer used by IntelliSense and generated reference-doc publishing

This distinction matters. Hand-authored docs should explain ownership, usage, architecture, and adoption. XML comments should explain supported public API behavior precisely enough for reference generation.

## Planning memory

The roadmap and backlog indicate that Cephalon has already shipped large parts of its foundation, adoption hardening, operational baseline, package-loading baseline, execution/orchestration baseline, and solution-level platform baseline.

The near-term planning center has shifted away from "start the engine" and toward:

- hardening and truthfulness across shipped surfaces
- resilience and migration patterns
- keeping docs, templates, CLI behavior, scaffolding, package metadata, and runtime contracts aligned

Recent roadmap memory worth keeping in mind:

- Sprint 35 shipped benchmark expansion for hot paths
- Phase 11 is planned around resilience foundations such as circuit breaker, retry, timeout, bulkhead, rate limiting, and taxonomy additions such as Onion Architecture and Anti-Corruption Layer
- Phase 12 is planned around migration and advanced coordination, including strangler fig, saga choreography, BFF, feature flags, and durable execution foundations

## Collaboration agreements

These are explicit working agreements from the current collaboration and should be extended as new standing decisions are made.

- when the team settles on an approach, plan, recurring workflow, or repeated command pattern, record it in project memory so it does not rely on thread-local recall alone
- when deeper or version-sensitive external research is needed, especially around `.NET`, `.NET 10`, libraries, frameworks, support policy, or official guidance, use internet research instead of relying only on prior model knowledge
- when delegating research work to sub-agents, prefer primary and official sources first, then synthesize the result back into repo context for Cephalon-specific decisions
- for meaningful work, prefer using sub-agents in complementary roles when that improves the outcome, for example business/product framing, architecture, design patterns, documentation, planning cards, quality review, testing, benchmarking, or other task-shaped specialties
- let sub-agents collaborate as a working group rather than as isolated note takers; they can cross-check one another's reasoning, surface tradeoffs, and help review the same change from different perspectives before the final decision lands
- sub-agents may use internet research when it helps them validate framework guidance, technical options, benchmarks, security practices, documentation, or other external references that matter to the decision
- repo-local memory is the reliable cross-thread source of truth; agreements that matter beyond the current thread should be written down here or in another repo-owned document
- Cephalon is still in an active POC and invention phase, so decisions should not stay trapped by the current implementation alone; if a materially better approach exists for engine quality, architecture, performance, security, developer ergonomics, or future-proofing, it is acceptable to change direction deliberately
- when rethinking an area during this POC phase, optimize for making Cephalon a stronger long-term engine: reduce common developer pain, keep the platform broad enough for varied project types, and prefer durable engine primitives over short-term local convenience
- Cephalon does not yet have legacy production consumers that must be preserved at all costs; when a compatibility shim conflicts with a cleaner long-term engine contract during the POC phase, prefer removing the shim and shipping the better design deliberately

Current standing examples from this collaboration:

- keep important collaboration memory in `docs/project-memory.md`
- treat internet-backed research as the default follow-through for sub-agent learning tasks when accuracy or freshness matters
- when a coding task is complete and the change is validated, stage only the intended files, create a commit, keep `master` updated as the integration branch, push the finished work to GitHub, and leave project docs aligned with the shipped behavior
- when a code change affects documented behavior, inspect the related `docs/*` graph and update every impacted hand-authored document that references that surface instead of patching only one nearby page
- GitHub Project cards should contain enough narrative detail for a reader to understand the work without extra thread context, and they should populate the standard fields `Assignee`, `Label`, `Type`, `Project`, `Estimate`, `Iteration`, `Test`, `Benchmark`, `Milestone`, and `Relationship` when cross-card references exist
- when a commit materially advances a GitHub Project card, add a card comment that records what changed and references the commit id so the card keeps a readable implementation history
- repository-facing written artifacts should be in English, including hand-authored documentation, commit messages, and planning or tracking content that becomes part of the project record
- use sub-agents proactively for review, consultation, and parallel investigation when a task benefits from multiple perspectives instead of treating delegation as a last resort
- keep future-facing engine quality in view during POC work, including architecture strength, design-pattern fit, performance, security, support for broad project shapes, and overall developer experience
- when a host exposes both module-owned REST helpers and generic behavior HTTP routes, treat the module-owned REST groups as the public REST/OpenAPI/Scalar surface and keep the generic behavior routes focused on non-REST adapter transports
- public REST is module-owned only: do not declare `http.rest` in `[BehaviorAllowedTransports(...)]` or `ConfigureTopology(...)`; author REST through `RestBehaviorModuleBase.ConfigureRestBehaviors(...)` by default and reserve low-level `MapBehaviorRestGroup(...)` work for advanced/manual cases
- `Engine:Behaviors` is no longer a per-behavior REST topology contract; keep it focused on behavior discovery and auto-registration
- when a module explicitly owns behaviors, prefer `BehaviorModuleBase` for process-only modules and `RestBehaviorModuleBase` when the same module also exposes some of those behaviors over REST; keep raw `IRestModule` implementations for REST modules that do not dispatch through behaviors
- prefer `BehaviorModuleBase` when a module explicitly owns behaviors but does not expose a public REST surface
- prefer `RestBehaviorModuleBase` when the same module owns behaviors and exposes some of them over REST
- prefer the single-surface REST DSL on `RestBehaviorModuleBase`: public `behaviors.Group(...).MapGet/MapPost/...` routes imply ownership automatically, while `behaviors.Internal<TBehavior>()` is the explicit path for internal-only or custom/manual-route behaviors in the same module
- keep one bounded context in one module when possible; do not split modules only to separate internal behaviors from REST-exposed behaviors
- `Engine:Behaviors:AutoRegister` is now an opt-in fallback rather than the default behavior-ownership path; prefer explicit module ownership and only turn scanning back on when a host intentionally wants convention-based discovery
- keep behavior return contracts transport-neutral by default: prefer raw `TOut` for simple success paths, prefer `Result<T>` for expected non-success branches, keep `BehaviorResult<T>` as a compatibility alias during the transition, and treat `ResultModel<T>` / `ResultModelError` as an optional REST wire-format policy rather than the universal engine contract
- standardize provider-pack settings by family instead of forcing one property shape on every pack: use `ConnectionStringName` plus `ConnectionString` for connection-string-native providers, use `UriName` plus `Uri` for URI-first providers, resolve named entries from the root `ConnectionStrings` or `Uris` sections as appropriate, fail fast when both named and inline settings are configured together, and keep topology-first providers such as Cassandra and Qdrant explicit
- preferred long-term data direction: keep `Engine:Data` as the logical app-model and capability-selection layer, use the shipped engine-owned `Engine:Databases` baseline for physical roles, migrations, and topology introspection, do not make mandatory `DbContext` base classes the primary engine contract, and keep durable audit history as additive provider-pack follow-through instead of overloading the narrow `Cephalon.Audit` baseline
- the current database-topology role-reference contract is intentionally narrow: `Write` and `Read` stay concrete root targets, while `Outbox` and `History` can explicitly reuse `write` through `UseRole` and still layer local schema/runtime overrides; broader role graphs are a later follow-through only if runtime and migration truth stay explicit
- the current database-topology baseline now has three complementary operator answers: `/engine/databases` is the raw engine-owned topology projection, `/engine/database-roles` plus `snapshot.DatabaseRoles` are the resolved runtime catalog for requested versus resolved roles, `UseRole`, co-location, consumers, audit-history metadata, and provider-contributed live role health, and `/engine/database-migrations` plus `snapshot.DatabaseMigrations` are the resolved runtime catalog for logical migration targets, their current status, and provider-added command guidance
- the current durable audit-history baseline is `Cephalon.Audit.EntityFramework`, driven by `Engine:Audit:History` plus a selected engine-owned database role that defaults to `history`, with `Engine:Audit:History:Export`, `Engine:Audit:History:Retention`, `IAuditHistoryReader`, `IAuditHistoryExporter`, `/engine/audit-history`, `/engine/audit-history/export`, and showcase-facing `/api/v1/showcase/audit/history` plus `/api/v1/showcase/audit/history/export` routes now proving read, bounded NDJSON export, and retention follow-through while the showcase sample keeps configured `WriteDb` / `ReadDb` / `HistoryDb` roots for Docker-backed runs plus an explicit `Outbox -> write` dependent role reference
- the showcase sample now always wires `Cephalon.Data.EntityFramework` plus `Cephalon.Audit.EntityFramework`; Docker mode keeps the configured PostgreSQL root roles, while non-Docker local/test runs rewrite `Write`, `Read`, and `History` to unique in-memory targets so `/engine/database-roles`, `/engine/database-migrations`, and showcase-facing audit-history routes stay truthful without external infrastructure
- `Cephalon.Data.EntityFramework` now contributes live role-probe metadata through the engine-owned database-role catalog, including connectivity outcome plus pending-migration diagnostics for registered `DbContext` roles, while dependent targets such as `outbox -> write` can inherit resolved-role runtime truth without losing their logical role identity
- the engine-owned migration catalog now carries typed provider-added command templates through `DatabaseMigrationCommandDescriptor`; `Cephalon.Data.EntityFramework` uses that surface to publish bundle/script/update guidance per logical role while staying honest that true bundle/script generation or execution orchestration is still a later slice
- durable audit-history export is intentionally NDJSON-only for the first shipped slice, configuration-gated through `Engine:Audit:History:Export`, and bounded by `MaxEntries`; replay UX, richer export formats, and delivery automation remain separate follow-through work
- the current eventing operator baseline now splits configured dispatch ownership from live dispatch state explicitly: `IEventDispatchRuntimeDescriptorCatalog` plus `/engine/event-dispatch-runtimes` answer what managed dispatch runtimes are active and what outbox/runtime ids they own, while `IEventDispatchRuntimeCatalog` plus `/engine/event-dispatches` answer the latest reported dispatch outcome, retry intent, timestamps, and totals per outbox path; both sets also flow into `/engine/snapshot` as `EventDispatchRuntimes` and `EventDispatchStates`
- the current engine-owned outbox answer is no longer a static staged-descriptor only: `OutboxDescriptor.DispatchPolicy` plus `IOutboxDispatchPolicyCatalog` now carry effective ownership per outbox as `disabled`, `consumer-managed`, or runtime-managed, `/engine/outboxes` is the canonical operator surface for that truth, and eventing/Wolverine runtime surfaces are expected to stay aligned with the same ownership answer instead of re-deriving it ad hoc
- the showcase sample now wires `Cephalon.Eventing.Wolverine` directly so the official `wolverine-managed` dispatch path stays visible through `/engine/event-dispatch-runtimes`, `/engine/event-dispatches`, and the runtime snapshot without custom sample-only operator code
- treat `EventDispatchRuntimeDescriptor.Summary` as the canonical per-runtime aggregate operator answer; keep `/engine/event-dispatches` as the per-outbox detail surface, and prefer pack-specific runtime surfaces such as Wolverine to consume the canonical summary instead of re-aggregating dispatch state independently
- when a provider pack truthfully stages durable outbox messages and can persist dispatch outcomes, register `IEventDispatchStore` alongside `IOutbox` so consumer-managed and adapter-managed dispatch can reuse the same provider contract without host-specific glue
- current provider-native dispatch-store coverage now includes Entity Framework, MongoDB, Redis, Elasticsearch, OpenSearch, Neo4j, Qdrant, NATS, and Cassandra; Cassandra now uses a deterministic message-sharded pending-dispatch index so wide-column workloads can answer due-event queries truthfully without pretending to be a globally ordered queue, while ClickHouse now stays explicit as a staging-only outbox with `DispatchPolicy.PolicyId = unsupported` until a truthful mutable pending-dispatch design exists
- phase 11 resilience work now has both a contract-first baseline and a first transport-native follow-through: `Engine:Resilience`, `AppProfile.Resilience`, and `/engine/resilience` remain the public source of truth for requested `Retry`, `Timeout`, `CircuitBreaker`, `Bulkhead`, and `RateLimiting` intent, while ASP.NET Core now projects effective public-HTTP enforcement through `IRateLimitingRuntimeCatalog`, `/engine/rate-limiting`, and `snapshot.RateLimitingPolicies` with operator/docs route exclusions; broader behavior-pipeline resilience remains a later slice
- the built-in pattern taxonomy now includes `onion-architecture` and `anti-corruption-layer`; future architecture guidance should treat those as shipped runtime-app-model descriptors rather than pending recommendations only

## Working assumptions for contributors

Unless the code clearly proves otherwise, contributors should assume:

- host adapters should stay thin
- reusable behavior belongs in engine services, modules, or companion packs rather than in app hosts
- deterministic ordering and explicit registration beat ambient discovery magic
- any public runtime, package, or diagnostics surface may also have docs, sample, scaffold, template, and validation-script follow-through
- hand-authored docs are part of the product and should be kept truthful when shipped behavior changes
- documents under `docs/*` often cross-reference one another, so code changes may require multi-file doc updates across hubs, component pages, operations guides, compatibility guidance, roadmap/backlog notes, and related adoption docs
- GitHub Project tracking should stay readable on its own, with complete card descriptions and the expected planning fields filled in so another contributor can understand scope, validation, timing, and dependencies quickly
- GitHub Project history should remain traceable from the card itself, including comment-level references to relevant commit ids as work lands
- the durable written project record should stay in English so docs, commits, cards, and related planning artifacts read consistently across the repository
- XML comments on public contracts are not optional polish; they are part of the supported documentation surface

## Current next-look areas

If you need to resume deeper analysis later, the most likely next focus areas are:

- `Cephalon.Engine` runtime, package, trust, and introspection internals
- `Cephalon.Behaviors` and the ABT pipeline, strategies, and transport bindings
- `Cephalon.Data` plus provider families and event-sourcing follow-through
- `Cephalon.Observability` plus dependency-health and exporter/provider conventions
- CLI, scaffolding, template, and sample alignment whenever app-model or package contracts change

## Local workspace notes

This snapshot was created from the current local workspace, not from a clean published release artifact.

Observed local notes during the scan:

- the git worktree contains local untracked directories such as `.build/`, `.claude/`, `.dotnet/`, `.dotnet-cli/`, `.dotnet-appdata/`, and `.nuget-appdata/`
- direct `git status` required a safe-directory override because the repository ownership on disk differs from the current user context

Those notes may be local-environment artifacts rather than repository-intended state.
