# Cephalon Project Memory

Project memory in this document reflects the repository state observed on `April 14, 2026`.

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
- Phase 12 is planned around migration and advanced coordination, including strangler fig, saga choreography, BFF, feature flags, and durable execution foundations; the descriptor-only taxonomy slice is now shipped through `strangler-fig` and `backend-for-frontend`, and the first strangler-fig runtime-contract slice is now also shipped through `IStranglerFigRouteContributor`, `IStranglerFigRuntimeCatalog`, `IStranglerFigRouter`, `/engine/strangler-fig`, and `snapshot.StranglerFigRoutes`, while migration-progress/configuration, host-level cutover, and BFF client-binding follow-through remain later work

## Collaboration agreements

These are explicit working agreements from the current collaboration and should be extended as new standing decisions are made.

- when the team settles on an approach, plan, recurring workflow, or repeated command pattern, record it in project memory so it does not rely on thread-local recall alone
- when deeper or version-sensitive external research is needed, especially around `.NET`, `.NET 10`, libraries, frameworks, support policy, or official guidance, use internet research instead of relying only on prior model knowledge
- when delegating research work to sub-agents, prefer primary and official sources first, then synthesize the result back into repo context for Cephalon-specific decisions
- for meaningful work, prefer using sub-agents in complementary roles when that improves the outcome, for example business/product framing, architecture, design patterns, documentation, planning cards, quality review, testing, benchmarking, or other task-shaped specialties
- let sub-agents collaborate as a working group rather than as isolated note takers; they can cross-check one another's reasoning, surface tradeoffs, and help review the same change from different perspectives before the final decision lands
- sub-agents may use internet research when it helps them validate framework guidance, technical options, benchmarks, security practices, documentation, or other external references that matter to the decision
- repo-local memory is the reliable cross-thread source of truth; agreements that matter beyond the current thread should be written down here or in another repo-owned document
- keep generic hosting tests deterministic by isolating them from transitive sample `Configurations` content, and keep sample-host tests such as the showcase harness pointing at their real project content roots explicitly instead of relying on whatever config files happen to be copied into a test output folder
- Cephalon is still in an active POC and invention phase, so decisions should not stay trapped by the current implementation alone; if a materially better approach exists for engine quality, architecture, performance, security, developer ergonomics, or future-proofing, it is acceptable to change direction deliberately
- when rethinking an area during this POC phase, optimize for making Cephalon a stronger long-term engine: reduce common developer pain, keep the platform broad enough for varied project types, and prefer durable engine primitives over short-term local convenience
- Cephalon does not yet have legacy production consumers that must be preserved at all costs; when a compatibility shim conflicts with a cleaner long-term engine contract during the POC phase, prefer removing the shim and shipping the better design deliberately

Current standing examples from this collaboration:

- keep important collaboration memory in `docs/project-memory.md`
- keep the durable planning workflow explicit through `docs/planning-governance.md`; meaningful work must stay aligned across repo docs, GitHub Project tracking, sprint placement, validation expectations, and commit-reference history
- treat internet-backed research as the default follow-through for sub-agent learning tasks when accuracy or freshness matters
- when a coding task is complete and the change is validated, stage only the intended files, create a commit, keep `master` updated as the integration branch, push the finished work to GitHub, and leave project docs aligned with the shipped behavior
- when a code change affects documented behavior, inspect the related `docs/*` graph and update every impacted hand-authored document that references that surface instead of patching only one nearby page
- when database-topology operator guidance becomes a stable reusable runtime answer, move it into an engine-owned route and snapshot contract first, then let the showcase sample consume that answer and add only sample-specific adaptation
- GitHub Project cards should contain enough narrative detail for a reader to understand the work without extra thread context, and they should populate the standard fields `Assignee`, `Label`, `Type`, `Project`, `Estimate`, `Iteration`, `Test`, `Benchmark`, `Milestone`, and `Relationship` when cross-card references exist
- when a commit materially advances a GitHub Project card, add a card comment that records what changed and references the commit id so the card keeps a readable implementation history
- roadmap, backlog, sprint placement, plan details, and GitHub Project cards must stay synchronized; if scope, estimate, validation, milestone, or relationships change, update both the docs and the card instead of leaving one side stale
- repository-facing written artifacts should be in English, including hand-authored documentation, commit messages, and planning or tracking content that becomes part of the project record
- use sub-agents proactively for review, consultation, and parallel investigation when a task benefits from multiple perspectives instead of treating delegation as a last resort
- keep future-facing engine quality in view during POC work, including architecture strength, design-pattern fit, performance, security, support for broad project shapes, and overall developer experience
- when a host exposes both module-owned REST helpers and generic behavior HTTP routes, treat the module-owned REST groups as the public REST/OpenAPI/Scalar surface and keep the generic behavior routes focused on non-REST adapter transports
- `OpenApi:EnabledVersions` and legacy `OpenApi:Documents` govern only the published OpenAPI + Scalar document set; generic behavior HTTP adapter routes resolve their version/document segment from `ApiRoutes:DefaultBehaviorDocumentName` or, when unset, the raw configured `OpenApi:DefaultVersion`
- module-owned REST version segments default to the owning module major version; tests or harnesses that target a module-owned public REST surface should derive that `/api/v{major}` segment from module metadata instead of hardcoding `v1`, and showcase transport projections should keep `restOperations` separate from generic behavior `transportIds`
- public REST is module-owned only: do not declare `http.rest` in `[BehaviorAllowedTransports(...)]` or `ConfigureTopology(...)`; author REST through `RestBehaviorModuleBase.ConfigureRestBehaviors(...)` by default and reserve low-level `MapBehaviorRestGroup(...)` work for advanced/manual cases
- if Cephalon adds lower-ceremony REST authoring later, keep it as metadata or projection-driven shorthand that still materializes into module-owned public REST; do not go back to direct behavior-owned REST activation
- if a higher-precedence module-owned REST mapping exists for a behavior, suppress lower-precedence implicit, generated, or convention REST projections for that same behavior by default instead of running them side by side
- the shipped generated-module REST shorthand stays explicit and module-owned through `IRestBehaviorEndpointGroupBuilder.MapGeneratedProfiles()` and `MapGeneratedProfiles(string)`: it prefers source-generated `GetRestProfiles()` plus `GetRestProfileBehaviorTypes()` hints, falls back only to a bounded scan of the explicit owning module assembly when generated type hints are unavailable, and still never publishes public REST from `[AppBehavior]` alone
- if Cephalon adds low-code REST shorthand later, `[AppBehavior]` plus auto-registration alone must still not publish a public REST boundary; shorthand must remain an explicit REST opt-in
- the shipped behavior-authored REST metadata bridge is `BehaviorRestProfileAttribute` plus optional repeated `BehaviorRestBindingAttribute` declarations in `Cephalon.Behaviors.Http`: together they declare a candidate REST method, relative pattern, optional API major version, and explicit route/query/header/body input-binding descriptors for future module-owned generated projections, but they remain metadata-only, do not publish public REST by themselves, and do not override host OpenAPI document publication policy
- the shipped explicit consumption path for that metadata is `IRestBehaviorEndpointGroupBuilder.MapProfile<TBehavior>()`: it stays module-owned, prefers source-generated `GetRestProfiles()` hints, falls back only to the explicitly targeted behavior type when generated hints are unavailable, seeds a group API version only when `.ApiVersion(...)` is not set explicitly, fails fast when profiled behaviors in the same group declare conflicting candidate API versions, and now preserves explicit binding descriptors through the same normalized projection pipeline
- the current `RestBehaviorModuleBase` DSL now compiles into a normalized internal REST projection contract before ASP.NET Core materialization, and the shipped runtime follow-through now exposes resolved public REST truth through `IRestEndpointRuntimeCatalog`, `/engine/rest-endpoints`, `/engine/rest-endpoints/{restEndpointId}`, and `snapshot.RestEndpoints`
- the shipped profile-consumption shorthand keeps runtime publication on the same `sourceKind = module-dsl` path, while `/engine/rest-endpoints` now distinguishes shorthand-authored endpoints through `metadata.authoringStyle = behavior-module-profile` and exposes explicit profile-driven binding plans through the typed `RestEndpointRuntimeDescriptor.BindingDescriptors` surface plus the matching `snapshot.RestEndpoints` data instead of treating `metadata.bindingDescriptors` as the canonical answer
- the shipped generated module-owned shorthand also keeps runtime publication on the same `sourceKind = module-dsl` path, while `/engine/rest-endpoints` now distinguishes generated shorthand through `metadata.authoringStyle = behavior-module-generated`
- the shipped REST precedence-visibility baseline now also exposes `IRestEndpointCandidateRuntimeCatalog`, `/engine/rest-endpoint-candidates`, `/engine/rest-endpoint-candidates/{candidateId}`, and `snapshot.RestEndpointCandidates`; within the normalized behavior-projection path the effective precedence is `explicit module DSL > MapProfile<TBehavior>() > MapGeneratedProfiles(...)`, and the candidate catalog preserves published-versus-suppressed status, precedence rank, winning candidate id, and suppression reason as runtime truth
- the shipped REST governance baseline now also exposes `IRestEndpointSuppressionRuntimeCatalog`, `/engine/rest-endpoint-suppressions`, `/engine/rest-endpoint-suppressions/{suppressionId}`, and `snapshot.RestEndpointSuppressions`, plus `IRestEndpointOverrideRuntimeCatalog`, `/engine/rest-endpoint-overrides`, `/engine/rest-endpoint-overrides/{overrideId}`, and `snapshot.RestEndpointOverrides`; `RestApi:Suppressions` and `RestApi:Overrides` are host-level ASP.NET Core publication-governance surfaces for descriptor-backed module-owned shorthand candidates, not engine-core route-authoring contracts
- the shipped `RestApi:Suppressions` baseline remains suppression-only and shorthand-only by design: it can hide `MapProfile<TBehavior>()` and `MapGeneratedProfiles(...)` candidates before precedence resolution, it can refine `Behaviors`/`Modules` targeting further with optional `ApiVersionMajors`, `Methods`, `RelativePatterns`, and `RouteGroupPrefixes`, those selector refiners match the original shorthand candidate shape before override actions are applied, and it still does not rewrite route shape, HTTP method, input binding, or OpenAPI document membership or override explicit module DSL/manual module-owned REST endpoints
- the shipped `RestApi:Overrides` baseline is also shorthand-only by design and now supports the same optional target refiners plus `ApiVersionMajor`, `Method`, constrained relative `Pattern`, and constrained explicit `Bindings`: it can retarget `MapProfile<TBehavior>()` and `MapGeneratedProfiles(...)` candidates to another effective API major version, HTTP method, relative route pattern, and/or explicit binding plan, records the applied rule through `RestEndpointCandidateRuntimeDescriptor.AppliedOverrideId`, matches its selector refiners against the original shorthand candidate shape before override actions are applied, drives ASP.NET Core materialization through the same effective projection shape reported by the runtime catalogs, keeps the `/api/v{major}` route segment and OpenAPI document name aligned when version changes, keeps mapped route truth aligned when pattern changes, allows placeholder-preserving rewrites by default, now also allows placeholder renames when the effective explicit route-binding plan covers the renamed placeholder set exactly, now also allows placeholder removals when the original projection already exposes full explicit route-binding coverage and the effective explicit binding plan keeps every affected original route-bound property explicitly bound, and now also allows placeholder additions when the effective explicit route-binding plan covers the full final placeholder set and every newly route-bound property was either already explicitly bound in the original projection or, for `POST`/`PUT`/`PATCH`, already part of the original deterministic remaining-body fallback surface; it still rejects broader implicit-property promotion outside that constrained body-fallback path, replaces shorthand explicit binding descriptors when `Bindings` are supplied while still letting unbound route placeholders and remaining request-body fields fill object properties deterministically, fails fast when the effective method-plus-binding plan is invalid or when a placeholder rename, removal, or addition would rely on inference, lose explicit binding coverage, or promote a non-body-fallback implicit property, and still does not override explicit module DSL, manual module-owned REST endpoints, or shorthand groups that already declare `.ApiVersion(...)` explicitly for version selection
- the shipped suppression-rule baseline now fails fast when a rule omits both `Behaviors` and `Modules`, so typo-shaped config cannot silently turn into a global shorthand kill switch
- when more than one REST suppression rule matches the same candidate, the shipped resolver now picks the most specific rule deterministically by populated target dimensions first (`Behaviors`, `Modules`, `ApiVersionMajors`, `Methods`, `RelativePatterns`, `RouteGroupPrefixes`), then by preferring behavior-targeted rules over module-only rules, then by narrower authoring-style scope, then by fewer total selector values, then by stable rule id ordering
- when more than one REST override rule matches the same candidate, the shipped resolver uses that same expanded specificity model before falling back to stable rule id ordering
- when REST governance suppresses a shorthand candidate, runtime truth should point at the governing rule id rather than pretending another candidate won; `RestEndpointCandidateRuntimeDescriptor.SuppressedBySuppressionId` is now the canonical distinction from precedence-based `SuppressedByCandidateId`
- when REST governance retargets a shorthand candidate through version, method, or pattern override, runtime truth should point at the governing rule id rather than hiding the decision inside the final endpoint shape; `RestEndpointCandidateRuntimeDescriptor.AppliedOverrideId` is now the canonical trace for that override
- the shipped profile-binding baseline treats explicit route/query/header/body bindings as overrides rather than as an exclusive binder mode, still lets unbound route placeholders and request bodies fill remaining object properties deterministically, and fails fast when a JSON body tries to overwrite a property reserved by an explicit non-body binding
- the current profile-binding hardening baseline now rejects invalid binding metadata at build time through `ABT0019` through `ABT0025`, removes enum-ordinal assumptions from source-generated `GetRestProfiles()` output, and re-checks route-placeholder truth during `BehaviorRestProfileResolver` normalization so `MapProfile<TBehavior>()` stays fail-fast even when it falls back to direct attribute metadata instead of generated hints
- the current public REST baseline now fails fast when two resolved public endpoints collide on the same `HTTP method + route pattern`; future shorthand or convention publication must compose through the same projection, runtime-catalog, and collision-validation pipeline instead of bypassing it
- the next endpoint-strategy follow-through after the shipped suppression-plus-version-plus-method-plus-pattern-plus-binding-plus-placeholder-rename-plus-placeholder-removal-plus-placeholder-addition-plus-implicit-body-fallback-promotion governance baseline is a broader config-override model only if runtime truth remains explicit and introspectable; any future implicit-property-promotion route model beyond that constrained body-fallback path, or any broader binding-rewrite model, should extend the same candidate, suppression, and override catalogs instead of bypassing them, and any future additional shorthand sources should land only when that runtime truth stays visible
- explicit manual module-owned REST now joins that same engine-owned runtime catalog and duplicate-route guard baseline; freeform host routes outside `IRestModule` remain out of scope unless Cephalon deliberately adopts them later
- `Engine:Behaviors` is no longer a per-behavior REST topology contract; keep it focused on behavior discovery and auto-registration
- when a module explicitly owns behaviors, prefer `BehaviorModuleBase` for process-only modules and `RestBehaviorModuleBase` when the same module also exposes some of those behaviors over REST; keep raw `IRestModule` implementations for REST modules that do not dispatch through behaviors
- prefer `BehaviorModuleBase` when a module explicitly owns behaviors but does not expose a public REST surface
- prefer `RestBehaviorModuleBase` when the same module owns behaviors and exposes some of them over REST
- prefer the single-surface REST DSL on `RestBehaviorModuleBase`: public `behaviors.Group(...).MapGet/MapPost/...` routes imply ownership automatically, while `behaviors.Internal<TBehavior>()` is the explicit path for internal-only or custom/manual-route behaviors in the same module
- keep one bounded context in one module when possible; do not split modules only to separate internal behaviors from REST-exposed behaviors
- `Engine:Behaviors:AutoRegister` is now an opt-in fallback rather than the default behavior-ownership path; prefer explicit module ownership and only turn scanning back on when a host intentionally wants convention-based discovery
- keep behavior return contracts transport-neutral by default: prefer raw `TOut` for simple success paths, prefer `Result<T>` for expected non-success branches, keep `BehaviorResult<T>` as a compatibility alias during the transition, and treat `ResultModel<T>` / `ResultModelError` as an optional REST wire-format policy rather than the universal engine contract
- prioritize core engine quality, contract clarity, performance, security, and maintainability before pushing deeper agentic, AI, or broader multi-platform runtime follow-through; future-tech expansion should build on a solid engine baseline instead of driving premature core-contract churn
- standardize provider-pack settings by family instead of forcing one property shape on every pack: use `ConnectionStringName` plus `ConnectionString` for connection-string-native providers, use `UriName` plus `Uri` for URI-first providers, resolve named entries from the root `ConnectionStrings` or `Uris` sections as appropriate, fail fast when both named and inline settings are configured together, and keep topology-first providers such as Cassandra and Qdrant explicit
- preferred long-term data direction: keep `Engine:Data` as the logical app-model and capability-selection layer, use the shipped engine-owned `Engine:Databases` baseline for physical roles, migrations, and topology introspection, do not make mandatory `DbContext` base classes the primary engine contract, and keep durable audit history as additive provider-pack follow-through instead of overloading the narrow `Cephalon.Audit` baseline
- the current database-topology role-reference contract is intentionally narrow: `Write` and `Read` stay concrete root targets, while `Outbox` and `History` can explicitly reuse `write` through `UseRole` and still layer local schema/runtime overrides; broader role graphs are a later follow-through only if runtime and migration truth stay explicit
- the current database-topology baseline now has five complementary operator answers: `/engine/databases` is the raw engine-owned topology projection, `/engine/database-roles` plus `snapshot.DatabaseRoles` are the resolved runtime catalog for requested versus resolved roles, `UseRole`, co-location, consumers, audit-history metadata, and provider-contributed live role health, `/engine/database-migrations` plus `snapshot.DatabaseMigrations` are the resolved runtime catalog for logical migration targets, their current status, typed resolved-role runtime state, and provider-added command guidance, `/engine/database-migration-playbook` plus `snapshot.DatabaseMigrationPlaybook` are the canonical engine-owned ordered migration answer with generated counts, ordered steps, physical-target execution groups, grouped command sets, combined command-batch templates, startup-apply truth, resolved-role identity, and selected production-versus-manual command guidance, and `/engine/database-topology` plus `snapshot.DatabaseTopology` are the canonical engine-owned posture summary, advisory, and ordered action-plan answer over role health, migration status, and production-guidance completeness
- the current durable audit-history baseline is `Cephalon.Audit.EntityFramework`, driven by `Engine:Audit:History` plus a selected engine-owned database role that defaults to `history`, with `Engine:Audit:History:Export`, `Engine:Audit:History:Retention`, `IAuditHistoryReader`, `IAuditHistoryExporter`, `/engine/audit-history`, `/engine/audit-history/export`, and showcase-facing `/api/v1/showcase/audit/history` plus `/api/v1/showcase/audit/history/export` routes now proving read, bounded NDJSON export, and retention follow-through while the showcase sample keeps configured `WriteDb` / `ReadDb` / `HistoryDb` roots for Docker-backed runs plus an explicit `Outbox -> write` dependent role reference
- the showcase database-topology operator projection now layers six complementary sample-level answers on top of the engine routes: a readiness summary, an ordered operator action plan, derived operator insights, an ordered migration playbook, a Markdown operator brief export at `/api/v1/showcase/system/database-topology/brief`, and a downloadable self-describing handoff package at `/api/v1/showcase/system/database-topology/handoff` that now includes a package `README.md`, a machine-readable `handoff-manifest.json`, the Markdown brief, and the raw projection payload; the engine-owned `/engine/database-topology` posture is now the baseline for readiness, engine-level advisories, and the engine portion of the ordered action plan, the engine-owned `/engine/database-migration-playbook` route is now the baseline for ordered migration guidance plus physical-target execution-group truth, grouped command sets, and combined command batches, and sample-only follow-through stays limited to read-model drift, backlog, retry pressure, disabled-sync truth, and repo-root command adaptation instead of re-deriving core role/migration posture locally, while the projection still preserves engine-owned action categories plus source role or migration ids instead of collapsing those stable fields away
- the showcase sample now always wires `Cephalon.Data.EntityFramework` plus `Cephalon.Audit.EntityFramework`; Docker mode keeps the configured PostgreSQL root roles, while non-Docker local/test runs rewrite `Write`, `Read`, and `History` to unique in-memory targets so `/engine/database-roles`, `/engine/database-migrations`, and showcase-facing audit-history routes stay truthful without external infrastructure
- `Cephalon.Data.EntityFramework` now contributes live role-probe metadata through the engine-owned database-role catalog, including connectivity outcome plus pending-migration diagnostics for registered `DbContext` roles, while dependent targets such as `outbox -> write` can inherit resolved-role runtime truth without losing their logical role identity
- the engine-owned database runtime contract now also includes `RoleProbeFreshnessSeconds`; `Cephalon.Data.EntityFramework` uses the merged shared-plus-role runtime selection to cache probe results per logical role, treats `0` as the explicit no-cache answer, defaults to a 30-second freshness window when none is configured, invalidates cached probes when migration runtime state changes, and now projects stable cache/freshness/source timing through the typed `DatabaseRoleProbeDescriptor` contract on `DatabaseRoleRuntimeDescriptor.Probe` plus `DatabaseRoleDescriptor.Probe`, while keeping `probeSource`, `probeFreshnessOrigin`, `probeFreshUntilUtc`, and `probeAgeSeconds` metadata only for additive provider details and compatibility. The showcase database-topology projection now consumes the typed probe surface directly instead of parsing those stable keys locally
- the engine-owned migration catalog now carries typed provider-added command templates through `DatabaseMigrationCommandDescriptor`; `Cephalon.Data.EntityFramework` uses that surface to publish bundle/script/update guidance per logical role, `DatabaseMigrationDescriptor` now also carries typed `RecommendedExecutionOrder` hints plus typed resolved-role runtime fields so operator playbooks and projections do not hardcode `write` / `read` / `history` locally or parse only metadata dictionaries for stable engine-owned answers, and the command descriptor now also carries typed operator metadata such as `ToolId`, `ExecutionCategory`, and `WorkingDirectoryHint` so sample or host projections do not have to infer those reusable fields only from metadata dictionaries, while staying honest that true bundle/script generation or execution orchestration is still a later slice
- the engine-owned migration runtime now also includes `DatabaseMigrationOperationalPlaybook`, `DatabaseMigrationOperationalStep`, `DatabaseMigrationOperationalExecutionGroup`, `DatabaseMigrationOperationalExecutionGroupCommand`, `DatabaseMigrationOperationalExecutionGroupCommandBatch`, and `IDatabaseMigrationOperationalPlaybookProvider`; `Cephalon.Engine` now publishes `/engine/database-migration-playbook` plus `snapshot.DatabaseMigrationPlaybook` as the canonical ordered migration answer over the lower-level migration catalog, selecting one production-recommended and one manual/direct command path per target when available, grouping logical targets into physical-target execution batches when possible, surfacing those grouped command paths directly, and now also publishing deterministic combined command-batch templates for the selected production and manual paths, while the showcase sample consumes that engine-owned playbook directly and limits its own logic to repo-root runnable command adaptation plus sample-only read-model follow-through
- the engine-owned database role plus migration-playbook contract now also surfaces stable physical-target identity and shared-target coordination truth; `/engine/database-roles` plus `snapshot.DatabaseRoles` now answer `PhysicalTargetId`, `PhysicalTargetDisplayName`, and physical co-location for logical roles that share one database, `/engine/database-migration-playbook` plus `snapshot.DatabaseMigrationPlaybook` now answer coordination counts, physical-target execution groups, grouped command sets, combined command batches, and per-step coordinated migration ids and hints, and `/engine/database-topology` now adds shared-physical-target migration-coordination advisories plus action-plan entries when pending or failed logical migration work spans one physical database, while the showcase sample consumes those answers directly in `/showcase`, the Markdown brief, and the handoff package instead of inferring shared-database risk locally
- durable audit-history export is intentionally NDJSON-only for the first shipped slice, configuration-gated through `Engine:Audit:History:Export`, and bounded by `MaxEntries`; replay UX, richer export formats, and delivery automation remain separate follow-through work
- the current eventing operator baseline now splits configured dispatch ownership from live dispatch state explicitly: `IEventDispatchRuntimeDescriptorCatalog` plus `/engine/event-dispatch-runtimes` answer what managed dispatch runtimes are active and what outbox/runtime ids they own, while `IEventDispatchRuntimeCatalog` plus `/engine/event-dispatches` answer the latest reported dispatch outcome, retry intent, timestamps, and totals per outbox path; both sets also flow into `/engine/snapshot` as `EventDispatchRuntimes` and `EventDispatchStates`
- the current engine-owned outbox answer is no longer a static staged-descriptor only: `OutboxDescriptor.DispatchPolicy` plus `IOutboxDispatchPolicyCatalog` now carry effective ownership per outbox as `disabled`, `consumer-managed`, or runtime-managed, `/engine/outboxes` is the canonical operator surface for that truth, and eventing/Wolverine runtime surfaces are expected to stay aligned with the same ownership answer instead of re-deriving it ad hoc
- the showcase sample now wires `Cephalon.Eventing.Wolverine` directly so the official `wolverine-managed` dispatch path stays visible through `/engine/event-dispatch-runtimes`, `/engine/event-dispatches`, and the runtime snapshot without custom sample-only operator code
- treat `EventDispatchRuntimeDescriptor.Summary` as the canonical per-runtime aggregate operator answer; keep `/engine/event-dispatches` as the per-outbox detail surface, and prefer pack-specific runtime surfaces such as Wolverine to consume the canonical summary instead of re-aggregating dispatch state independently
- when a provider pack truthfully stages durable outbox messages and can persist dispatch outcomes, register `IEventDispatchStore` alongside `IOutbox` so consumer-managed and adapter-managed dispatch can reuse the same provider contract without host-specific glue
- current provider-native dispatch-store coverage now includes Entity Framework, MongoDB, Redis, Elasticsearch, OpenSearch, Neo4j, Qdrant, NATS, and Cassandra; Cassandra now uses a deterministic message-sharded pending-dispatch index so wide-column workloads can answer due-event queries truthfully without pretending to be a globally ordered queue, while ClickHouse now stays explicit as a staging-only outbox with `DispatchPolicy.PolicyId = unsupported` until a truthful mutable pending-dispatch design exists
- phase 11 resilience work now has the contract-first baseline plus shipped follow-through slices: `Engine:Resilience`, `AppProfile.Resilience`, and `/engine/resilience` remain the public source of truth for requested `Retry`, `Timeout`, `CircuitBreaker`, `Bulkhead`, and `RateLimiting` intent, ASP.NET Core projects effective public-HTTP enforcement through `IRateLimitingRuntimeCatalog`, `/engine/rate-limiting`, and `snapshot.RateLimitingPolicies` with operator/docs route exclusions plus endpoint-scoped override metadata from `Engine:Resilience:RateLimiting:Overrides`, and that same host runtime now keeps long-lived stream-versus-connection truth visible there through `transportKind`, `transportSemantics`, `enforcementMoment`, and `longLivedTransportIds` while the built-in GraphQL adapter maps explicit `/graphql`, `/graphql/schema`, `/graphql-sse`, and `/graphql-ws` surfaces under the shared prefix contract. The shared GraphQL transport authoring layer now also exposes `ConfigureGraphQLMutation(...)` and `ConfigureGraphQLSubscription(...)`, so modules can contribute mutation and subscription roots without dropping to raw request-executor hooks, while the built-in hosting coverage now proves configured schema, SSE, and WebSocket routes through real SDL downloads, `text/event-stream` payloads, and `graphql-transport-ws` frames instead of path-only reachability. `Cephalon.Behaviors` now projects effective behavior-execution retry, timeout, circuit-breaker, and bulkhead enforcement through `IBehaviorResilienceRuntimeCatalog`, `/engine/behavior-resilience`, and `snapshot.BehaviorResiliencePolicies`. That behavior-execution layer now also resolves additive `Engine:Resilience:BehaviorExecution:Overrides` entries with `behavior+transport > behavior > transport > default` precedence, keeps explicit disable overrides visible in the runtime catalog, lets the REST helper layer translate the still-effective timeout or open-circuit answers into `503` plus bulkhead answers into `429` with per-route OpenAPI metadata, and now carries explicit behavior-authored idempotency through `BehaviorIdempotencyAttribute` so `Resolve(behaviorId, transportId)` can surface retry eligibility as `eligible`, `ineligible`, or `unknown` while the shared pipeline only replays explicitly idempotent transient failures with effective retry backoff/jitter settings; broader non-route or non-ASP.NET Core transport-native semantics remain later slices
- the built-in pattern taxonomy now includes `onion-architecture`, `anti-corruption-layer`, `strangler-fig`, and `backend-for-frontend`; future architecture guidance should treat those as shipped runtime-app-model descriptors rather than pending recommendations only, while remembering that strangler-router and client-binding runtime follow-through are still separate work
- the current phase 12 strangler-fig baseline follows the same contributor/registry/catalog pattern as other engine surfaces: modules or hosts contribute `StranglerFigRouteDescriptor` entries, the engine composes them into `IStranglerFigRuntimeCatalog` and `snapshot.StranglerFigRoutes`, and ASP.NET Core exposes `/engine/strangler-fig` plus `/engine/strangler-fig/resolve` before any host-specific proxy behavior is added

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
- meaningful work should appear in both repo-owned planning/docs surfaces and GitHub Project tracking; if one side is missing, treat the planning record as incomplete
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
