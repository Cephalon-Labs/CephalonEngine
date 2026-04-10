# Cephalon Engine Roadmap

Editable roadmap diagram: `docs/cephalon-engine-roadmap.drawio`

Planning baseline in this document reflects the repository state as of `April 10, 2026`.

## Target outcome

Cephalon should become a modular runtime platform that can:

- compose modules, capabilities, and policies deterministically
- run across multiple hosts and transports without rewriting feature code
- generate opinionated app shapes from blueprints instead of ad-hoc setup
- ship as reusable packages, templates, and samples for other teams
- grow later into package loading, workflow, orchestration, and AI-driven runtime scenarios

## Current status

The foundation is no longer hypothetical. The repository already ships:

- host-agnostic module contracts in `Cephalon.Abstractions`
- configuration-driven engine composition in `Cephalon.Engine`
- technology-profile modeling for future-facing workloads through `Engine:Technologies` and `AppProfile.Technologies`
- baseline companion packages for `AgenticWorkloads`, `EventDrivenIntegration`, `KnowledgeRetrieval`, and `EdgeNativeDelivery`
- assembly-based module discovery
- module and capability policy toggles through `Engine:Options`
- manifest v2 with engine version, module metadata, and capability source mapping
- host adapters for ASP.NET Core and generic worker hosts
- transport support for `RestApi`, `JsonRpc`, `Grpc`, `GraphQL`, `ServerSentEvents`, and `WebSocket`
- OpenAPI + Scalar for REST-facing ASP.NET Core hosts
- scaffold plans, scaffold generation, and a working CLI
- a `dotnet new` template-pack baseline for the shipped blueprints
- starter templates and a reference package for module authoring
- an explicit package-assembly loading baseline with package manifest introspection
- a baseline trust and capability-policy surface for package trust and REST boundary enforcement
- sample apps per shipped blueprint
- runtime failure policy baseline with fail-fast, capture-only, best-effort stop, and restart guards
- operational health endpoints, diagnostics surface, and dependency-health contributor baseline for ASP.NET Core hosts
- observability conventions for logs, metrics, tracing, and telemetry export guidance
- a benchmark suite plus baseline guardrail validation for composition, strict trust-policy composition, runtime lifecycle, ASP.NET Core request logging, and scaffolding hot paths
- a GitHub Actions release-validation workflow that runs the repo-native build, test, benchmark, and guardrail flow

That changes the plan materially:

- we do not need another “start the engine” phase
- we do need an “adopt this safely outside the repo” phase
- we should prioritize SDK hardening, templates, samples, and operational polish before advanced platform features
- public-surface hardening and compatibility guidance now sit inside that shipped SDK-adoption baseline rather than as vague follow-up work

## Sprint alignment

The project board now tracks both delivered work and upcoming work through explicit sprint buckets:

- `Foundation Sprint 1`: `ENG-000`, `ENG-001`, `ENG-002`, `ENG-003`, `ENG-004`
- `Foundation Sprint 2`: `ENG-006`, `ENG-007`, `ENG-008`, `ENG-009`
- `Foundation Sprint 3`: `ENG-014`, `ENG-015`, `ENG-025`
- `Adoption Sprint 0`: `ENG-016`, `ENG-017`, `ENG-018`
- `Operational Sprint 0`: `ENG-019`, `ENG-020`, `ENG-021`, `ENG-024`, `ENG-023`
- `Platform Sprint 0`: `ENG-012`
- `Sprint 1`: delivered `ENG-005`, `ENG-026`, and `ENG-027`, and opened the phase 2 operational gap-inventory track
- `Sprint 2`: exporter packaging is now part of the shipped phase-2 baseline, Cassandra contact-point health plus ClickHouse analytics health plus Consul control-plane health plus Elasticsearch cluster health plus HTTP external API plus Kafka broker metadata plus Memcached cache plus MongoDB plus MQTT plus MySQL plus NATS plus Neo4j plus OpenSearch plus Oracle plus Postgres plus RabbitMQ plus Redis/cache plus SQL Server dependency-health packaging anchor the provider-specific follow-through, the shared diagnostics/event-id catalog now anchors the structured diagnostics baseline, and release validation now calls out the health/export convention suite explicitly
- `Sprint 3`: runtime-answers follow-through, the shipped package distribution/provenance and signer-verification follow-through under `ENG-011`, the shipped `ENG-013` execution-graph lifecycle/observability plus hosted-execution convention and agentic orchestration-link follow-through, the shipped `ENG-022` suite-scaffold-shape baseline under `#79`, the shipped built-in `MicroserviceSuite` composition baseline under `#80`, the shipped multi-service suite sample baseline under `#81`, the shipped suite-governance and additive gateway/control-plane guidance baseline under `#82`, the shipped `ENG-029` self-hosted OTLP follow-through slice, the shipped Azure Monitor first-vendor slice, the shipped AWS second-vendor slice, the shipped GCP third-vendor slice, the shipped Oracle Cloud managed-ingestion slice, the shipped DigitalOcean collector/defaults slice, the shipped VMware Tanzu proxy/defaults slice, the shipped Kubernetes collector/defaults slice, the shipped downstream Cloudflare/custom-provider authoring slice under `#120`, the shipped Grafana Cloud OTLP/header slice under `#126`, and the shipped New Relic native OTLP/api-key slice under `#127`
- `Sprint 4`: shipped `ENG-033` cross-platform validation/shell parity plus `ENG-034` first-run adoption and environment-doctor work so install, validation, and first-run guidance now hold together outside the repo
- `Sprint 5`: shipped `ENG-035` external module-package lifecycle prove-out so published packages, trust policy, and runtime introspection are exercised end to end outside the repo-local assembly path
- `Sprint 6`: shipped `ENG-036` containerized local runtime and operations prove-out so Docker Desktop / WSL teams can validate startup, health, and telemetry handoff with a reproducible sample deployment shape
- `Sprint 7`: shipped `ENG-037` generated-app local package-feed bootstrap follow-through so freshly scaffolded apps can restore, build, and take the documented container path from a seeded or repointed package source without repo-only NuGet knowledge
- `Sprint 8`: shipped `ENG-038` generated-app published-output and deployment baseline so scaffolded apps now carry a deterministic folder-publish profile plus a validated smoke path from scaffold -> publish -> run from published output
- `Sprint 9`: shipped `ENG-039` generated-app Linux `systemd` deployment baseline so scaffolded apps now also carry self-hosted service assets plus a validated WSL `systemd-analyze` path for Linux-class deployment packaging
- `Sprint 10`: shipped `ENG-040` generated-app Windows Service deployment baseline so scaffolded apps now also carry self-hosted Windows install assets plus a validated install-preview path against published output without admin-only repo validation
- `Sprint 11`: shipped `ENG-041` generated-app IIS deployment baseline so scaffolded apps now also carry hosted Windows site/app-pool assets plus a validated ANCM `web.config` and install-preview path against published output without requiring a live IIS install
- `Sprint 12`: shipped `ENG-042` generated-app Azure App Service deployment baseline so scaffolded apps now also carry hosted Azure ZIP-deploy assets plus a validated run-from-package and Azure CLI preview path against published output without requiring live Azure credentials
- `Sprint 13`: shipped `ENG-043` generated-app Azure Container Apps deployment baseline so scaffolded apps now also carry hosted Azure source-deploy assets plus a validated Dockerfile build and Azure CLI preview path from the generated app root without requiring live Azure credentials
- `Sprint 14`: shipped `ENG-044` generated-app Kubernetes deployment baseline so scaffolded apps now also carry platform-neutral manifest/apply assets plus a validated Dockerfile build and `kubectl kustomize` preview path from the generated app root without requiring a live cluster
- `Sprint 15`: shipped `ENG-045` generated-app container-image publishing baseline so scaffolded apps now also carry provider-neutral build/tag/push assets plus a validated local-registry smoke path from the generated app root without requiring a cloud-specific registry contract
- `Sprint 16`: open phase 8 with `ENG-046`, `ENG-047`, and `ENG-048` so taxonomy, structured config, and host-agnostic contracts freeze before package or template drift starts
- `Sprint 17`: deliver `ENG-049` and `ENG-050` so the relational-first data and eventing golden path exists before broader provider or security follow-through
- `Sprint 18`: deliver `ENG-051`, `ENG-052`, and `ENG-053` so identity/authorization, multi-tenancy/audit, and CLI/scaffolding/template/sample alignment land on top of the frozen phase-8 contract
- `Sprint 19`: deliver `ENG-055` and `ENG-056` so validation, benchmarks, docs, XML comments, and reference-doc alignment close the phase-8 truthfulness gap before broader expansion claims
- `Sprint 20`: **shipped** `ENG-058 M1` ABT foundation — `IAppBehavior`, `IBehaviorContext`, `BehaviorDispatcher`, `BehaviorExecutionSlot`, `CompatibilityMatrix`, ABT-001–ABT-006 compatibility rules, hosting — 499/499 tests (commit `9d657da`)
- `Sprint 21`: **shipped** `ENG-058 M2` HTTP Transport Pack — 7 HTTP bindings (`rest`, `jsonrpc`, `graphql`, `graphql-sse`, `graphql-ws`, `sse`, `ws`), `LazyTransportBinding` — 516/516 tests (commit `c957966`)
- `Sprint 22`: **shipped** `ENG-058 M3` Messaging Transport Pack — InMemory, RabbitMQ, Kafka bindings; M2 CTS leak fix — 527/527 tests (commit `9183407`)
- `Sprint 23`: **shipped** `ENG-058 M4` Pattern Execution Strategies — 5 strategies (`cqrs`, `event-driven`, `saga-step`, `process-manager`, `direct`), `ISagaStateStore`, `IProcessCheckpointStore`, `FrozenDictionary` registry, `IBehaviorContext.CorrelationId`, `IProcessCompletion` — 575/575 tests (commit `cc2ab0a`)
- `Sprint 24 (M5)`: **shipped** `ENG-058 M5` Source Generator — `BehaviorSourceGenerator` (analyzer + incremental generator), ABT0010–ABT0013, `BehaviorRegistrationHints.g.cs`, 584/584 tests (commit `8455b9a`)
- `Sprint 24 (M6)`: **shipped** `ENG-058 M6` Runtime Integration — `BehaviorRuntimeContributor`, `IBehaviorAdvisory` system, `IBehaviorContext.EventStore`, `BehaviorDiagnostics` 5100-5109, 592/592 tests (commit `62d386c`)
- `Sprint 25`: **shipped** `ENG-054` MongoDB document-store provider — `Cephalon.Data.MongoDB` + `Cephalon.EventSourcing.MongoDB`, MongoDB.Driver 3.4.0 — 599/599 tests (commit `f94dc28`)
- `Sprint 26`: **shipped** `ENG-054` Redis key-value-store provider — `Cephalon.Data.Redis` + `Cephalon.EventSourcing.Redis`, StackExchange.Redis 2.8.16 — 607/607 tests
- `Sprint 27`: **shipped** `ENG-054` Neo4j graph-store provider — `Cephalon.Data.Neo4j` + `Cephalon.EventSourcing.Neo4j`, Neo4j.Driver 6.0.0 — 615/615 tests
- `Sprint 28`: **shipped** `ENG-054` Cassandra wide-column-store provider — `Cephalon.Data.Cassandra` + `Cephalon.EventSourcing.Cassandra`, CassandraCSharpDriver 3.22.0 — 624/624 tests
- `Sprint 29`: **shipped** `ENG-054` ClickHouse analytics-store provider — `Cephalon.Data.ClickHouse` + `Cephalon.EventSourcing.ClickHouse`, ClickHouse.Driver 1.0.2 — 632/632 tests
- `Sprint 30`: **shipped** `ENG-054` Elasticsearch + OpenSearch search-store provider — 4 packages, Elastic.Clients.Elasticsearch 8.17.0 + OpenSearch.Client 1.8.0 — 640/640 tests
- `Sprint 31`: **shipped** `ENG-054` Qdrant vector-store + NATS ledger-store provider — 4 packages, Qdrant.Client 1.17.0 + NATS.Net 2.7.3 — 648/648 tests. ENG-054 Track 1 complete: all 9 non-relational provider families delivered. The same sprint also shipped `ENG-058-T30` behavior-aware REST endpoint helper follow-through: `MapBehaviorRestGroup(...)`, versioned OpenAPI metadata defaults, XML-comment enrichment, and showcase cart route deduplication over the behavior dispatcher, `ENG-058-T31` OpenAPI XML-comment deduplication follow-through so behavior `<summary>` and `<remarks>` render as distinct Scalar/OpenAPI summary and description text, `ENG-058-T32` named OpenAPI document selection via `BehaviorRestEndpointGroup.ApiVersion(major)`, `ENG-058-T33` versioned OpenAPI config canonicalization around `OpenApi:EnabledVersions` plus `OpenApi:DefaultVersion`, `ENG-058-T34` versioned Scalar doc-link behavior, `ENG-058-T35` canonical Scalar links plus `/api/v{major}` REST-route alignment, `ENG-058-T36` configurable docs-route surfaces and module-major REST defaults, `ENG-058-T37` shared `BehaviorApiSurface` canonical routing across the generic non-REST behavior HTTP bindings, `ENG-058-T38` canonical `ApiRoutes:Prefixes` defaults for both built-in and generic HTTP transport surfaces, `ENG-058-T40` public REST documentation separation plus tag-metadata follow-through so module-owned REST groups now own the published REST OpenAPI/Scalar surface, `ENG-058-T41` cleanup that removed behavior-declared REST so `MapEndpoints(...)` plus `MapBehaviorRestGroup(...)` are no longer the main REST authoring story, `ENG-058-T43` module-owned behavior authoring baseline so one module can explicitly own both internal-only and REST-exposed behaviors through `BehaviorModuleBase` and `RestBehaviorModuleBase`, `ENG-058-T44` the single-surface REST behavior module DSL plus `AutoRegister=false` default so REST behavior modules can declare public routes and internal ownership in one pass through `ConfigureRestBehaviors(...)`, `ENG-058-T46` transport-neutral behavior results plus optional REST result-envelope projection so behaviors can return raw `TOut` or `BehaviorResult<T>` in the core contract while ASP.NET Core can still publish `ResultModel<T>` / `ResultModelError` on the REST wire when the host opts into `ApiRoutes:ResultEnvelope:Enabled`, `ENG-058-T47` plural REST error-envelope follow-through so validation and other multi-reason failures now flow through an `errors` collection backed by `BehaviorFault.InnerFaults` while OpenAPI generation keeps the same canonical plural contract, `ENG-058-T48` showcase `AddToCartBehavior` authoring follow-through so the cart sample now demonstrates `BehaviorResult<T>`, multi-fault validation, checkout conflicts, and focused REST-envelope overrides in a concrete end-to-end module example, `ENG-058-T49` concise no-payload `BehaviorResult` factories so common async-return branches can now say `BehaviorResult.Invalid(...)` / `BehaviorResult.NotFound(...)` / `BehaviorResult.Conflict(...)` without repeating `<T>` while docs call out the remaining `Task.FromResult<BehaviorResult<TOut>>(...)` inference edge case explicitly, `ENG-058-T50` configurable documented REST response statuses plus default `500` so hosts can narrow or expand the status list shown in OpenAPI + Scalar through `OpenApi:BehaviorRest:DocumentedStatusCodes` while Cephalon now keeps `500` visible by default and stops forcing `400`/`404` when that list is narrowed, and `ENG-058-T51` concise `Result<T>` / `Result` aliases so new behavior authoring can use `Result<T>` plus `Result.*(...)` while the legacy `BehaviorResult<T>` surface stays available as a compatibility alias and REST/OpenAPI recognizes both families. The transport surface now keeps GraphQL semantics schema-owned while still aligning GraphQL-family behavior endpoints with the same prefix/version policy used by the rest of the generic HTTP transport pack
- `Infrastructure Phase 1`: solution filter files (`core.slnf`, `data.slnf`, `observability.slnf`, `aspnetcore.slnf`) + scaffolding scripts (`New-ProviderPack.ps1`, `New-ObservabilityPack.ps1`)
- `Infrastructure Phase 2`: test assembly split — `Cephalon.Tests` monolith (648 tests) split into `Cephalon.Tests.Support` + `Cephalon.Tests.Composition` (327) + `Cephalon.Tests.Hosting` (200) + `Cephalon.Tests.Tooling` (121) — 648/648 tests
- `Sprint 32`: backlog and roadmap alignment for all completed work through Sprint 31, status closeout for ENG-054/056/057
- `Sprint 33`: EF projection contributor (`IProjectionContributor`), Wolverine dispatch observability (`ActivitySource` + `Meter`), validation script fix for post-test-split environment, ENG-051 closeout — 648/648 tests
- `Sprint 34`: comprehensive engine audit — WebSocket `[LoggerMessage]` logging fix, flaky test fix, architecture inventory/recommendations docs, ENG-049/050/052/053/055 closeout (all phase-8 baseline acceptance met), ENG-059 benchmark expansion planned — 648/648 tests
- `Sprint 35`: **shipped** ENG-059 runtime hot-path benchmark expansion — data layer dispatch, behavior dispatch, authorization evaluation, tenant resolution, event sourcing, outbox staging — 13 new benchmarks across 6 classes, guardrails 10→23, 648/648 tests
- `Sprint 31`: **shipped** ENG-054 provider configuration follow-through — connection-string-native packs now share `ConnectionStringName` plus `ConnectionString` (`Cephalon.Data.MongoDB`, `Cephalon.Data.Redis`), URI-first packs now share `UriName` plus `Uri` (`Cephalon.Data.Elasticsearch`, `Cephalon.Data.OpenSearch`, `Cephalon.Data.Neo4j`, `Cephalon.Data.Nats`), named values resolve from the root `ConnectionStrings` or `Uris` sections as appropriate, packs fail fast when both settings are supplied, and the docs now call out the provider-family contract explicitly while Cassandra/Qdrant remain topology-first
- `Sprint 36`: `ENG-060` established the engine-owned database topology baseline so physical database roles, migration targets, outbox routing, and history stores stop drifting across provider packs
- `Sprint 37`: `ENG-061` is now shipped on top of that topology contract: `Cephalon.Data.EntityFramework` consumes `Engine:Databases` write/read roles directly, publishes role and migration metadata through the runtime surface, and supplies the migration-registration primitives that also let additive companion packs register truthful history-role execution
- `Sprint 38`: `ENG-062` is now shipped: `Cephalon.Audit.EntityFramework` provides the first durable audit-history baseline through `Engine:Audit:History` plus a selected engine-owned database role, defaulting to `History`, and the showcase sample now uses distinct `WriteDb`, `ReadDb`, and `HistoryDb` databases to prove the topology end to end
- `Sprint 31` follow-through: `ENG-063` is now shipped on top of the first durable history baseline so audit history now includes a host-agnostic filtered-page reader contract, engine-owned retention settings, `/engine/audit-history` operator routes, and showcase-facing `/api/v1/showcase/audit/history` endpoints instead of staying write-only
- `Sprint 36–37 (Phase 11)`: planned resilience foundation — circuit breaker, retry/timeout/bulkhead, rate limiting, `onion-architecture` and `anti-corruption-layer` pattern descriptors
- `Sprint 38–39 (Phase 12)`: planned migration and advanced coordination — strangler fig, saga choreography, BFF pattern, feature flags, durable execution foundations
- `Sprint 40–41 (Phase 13)`: planned next-generation patterns — cell-based architecture, data mesh, CDC
- `Later / not scheduled yet`: further cloud/platform integrations beyond the shipped phase 6 baseline, `ENG-054` hybrid-runtime service-mesh and serverless expansion, and future solution-level expansion only when an explicit adoption scenario needs them

## Planning principles

- prefer stabilizing the shipped surface over inventing new layers too early
- keep the engine configuration-driven and host-agnostic by default
- keep logical data selection separate from physical database topology so one codebase can move between layouts without rewriting hosts
- keep future-facing technology choices additive through explicit technology profiles instead of blueprint explosion
- treat scaffolding, CLI, and benchmark coverage as part of the engine product, not side tools
- make every new runtime feature observable, testable, and benchmarkable
- prove one relational-first golden path before widening provider-family or hybrid-runtime claims
- keep orchestration additive and delay distributed runners until package loading, lifecycle, and policy are strong enough

## Phase 0: Foundation shipped

Status: substantially complete

What is already in place:

- app model and blueprint contracts
- technology-profile contract for future-facing workload guidance
- module discovery and dependency ordering
- lifecycle baseline with runtime status
- manifest v2 and runtime introspection endpoints
- ASP.NET Core and worker host adapters
- transport adapter split
- scaffold generation and CLI baseline
- observability baseline
- benchmark baseline

What still belongs to foundation hardening:

- richer runtime failure and restart policies beyond the shipped baseline
- more actionable diagnostics for module/package authors

## Phase 1: SDK hardening and external adoption

Status: substantially complete

Goal: turn the current repo from “good internal foundation” into something other teams can adopt predictably.

Deliverables:

- package/version compatibility guidance
- CLI polish for real developer workflows
- generated output that stays aligned across `Cephalon.Scaffolding`, `Cephalon.Cli`, `Cephalon.TemplatePack`, and the repository package catalog
- GraphQL transport delivery that keeps the runtime catalog, scaffold output, tests, and component docs aligned with the adapter split
- DocFX-ready XML comments across the supported published assembly set, with tests kept outside that publishing boundary unless promoted intentionally
- technology profiles that stay aligned across runtime introspection, scaffolding, CLI, and template defaults
- companion packages that turn selected technology profiles into reusable runtime primitives without bloating the engine core
- module-authoring starters and reference packages that stay aligned with runtime contracts
- operational polish on top of the shipped failure-policy and health/telemetry baselines

Exit criteria:

- a new team can create a Cephalon app without copying code out of this repo manually
- a new module can be authored from a supported starter path
- generated apps, docs, package references, and install surfaces stay aligned with the shipped engine conventions

Current note:

- the supported phase-1 adoption baseline is now shipped across public-surface hardening, GraphQL transport delivery, compatibility guidance, DocFX-ready XML comments, the explicit test-harness visibility policy that keeps shared helpers internal while leaving only framework-required xUnit classes and a narrow reflective transport-contract exception public in `tests/Cephalon.Tests`, the release package-artifact baseline that defines the intended shipped NuGet/template surface explicitly, the shipped checksum/provenance manifest follow-through under `ENG-032`, and a dedicated `.NET tool` install surface for `Cephalon.Cli`

## Phase 2: Operational hardening

Status: substantially complete

Goal: make Cephalon safe to operate in real environments.

Deliverables:

- deeper readiness and liveness semantics beyond the shipped baseline
- richer runtime failure, stop, and restart policies beyond the shipped baseline
- richer structured diagnostics and event IDs across packages
- `ILogger` provider integration such as Serilog when hosts need richer sinks, enrichers, or log-routing behavior without inventing a new logging abstraction
- ASP.NET Core request/response logging with bounded body capture and trace/log correlation over the shared `ILogger` pipeline
- clearer operational answers to “what loaded, what started, what failed, and why?”
- benchmark-driven performance guardrails for hot engine paths

Current inventory:

- `docs/operational-hardening-gap-inventory.md` now records the shipped baseline versus the remaining phase-2 gaps so follow-through work stays grounded in the code that already exists
- that inventory now includes shipped `Cephalon.Observability.OpenTelemetry` and `Cephalon.Observability.Serilog` companion packages plus shipped `Cephalon.Observability.CassandraDependencies`, `Cephalon.Observability.ClickHouseDependencies`, `Cephalon.Observability.ConsulDependencies`, `Cephalon.Observability.ElasticsearchDependencies`, `Cephalon.Observability.HttpDependencies`, `Cephalon.Observability.KafkaDependencies`, `Cephalon.Observability.MemcachedDependencies`, `Cephalon.Observability.MongoDbDependencies`, `Cephalon.Observability.MqttDependencies`, `Cephalon.Observability.MySqlDependencies`, `Cephalon.Observability.NatsDependencies`, `Cephalon.Observability.Neo4jDependencies`, `Cephalon.Observability.OpenSearchDependencies`, `Cephalon.Observability.OracleDependencies`, `Cephalon.Observability.PostgresDependencies`, `Cephalon.Observability.RabbitMqDependencies`, `Cephalon.Observability.RedisDependencies`, and `Cephalon.Observability.SqlServerDependencies` companion packages, together with a published runtime diagnostics catalog, runtime-story surface, configurable failure-policy warmup/drain/backoff semantics, opt-in ASP.NET Core request/response body logging with request/trace correlation plus default sensitive-value redaction, explicit release-validation guidance for health/export conventions, and refreshed benchmark guardrails that separate prepared composition/lifecycle hot paths plus strict trust-policy composition plus bounded, correlated, and concurrent ASP.NET Core request-logging paths from benchmark harness setup
- the remaining cloud-vendor tracing/export follow-through has been re-scoped into phase 6 cloud and platform integrations because the expanded self-hosted plus AWS plus Azure plus GCP plus Huawei Cloud plus Alibaba Cloud plus Oracle Cloud plus DigitalOcean plus Red Hat OpenShift plus VMware Tanzu plus Kubernetes target list, together with the downstream Cloudflare/custom-provider guidance path and the now-explicit Grafana Cloud OTLP/header follow-through target, is broader than the shipped phase-2 operational baseline

Exit criteria:

- operators can diagnose engine startup and module failures quickly
- host health semantics are predictable across ASP.NET Core and worker hosts
- performance regressions in composition/runtime/scaffolding are caught intentionally

## Phase 3: Extensibility and package loading

Status: substantially complete

Goal: let Cephalon load and validate independently shipped module packages.

Current baseline already in place:

- explicit package assembly paths can be declared through `Engine:Discovery:Packages`
- package manifests can be declared through `Engine:Discovery:Packages`
- package directories can be scanned through `Engine:Discovery:PackageDirectories`
- package metadata can be governed through `Engine:PackagePolicy`
- package manifests can declare package-to-package dependencies with version bounds
- package-loaded modules flow through the same runtime/module contracts
- package load results are exposed through `/engine/packages` and manifest v2 metadata
- package trust and capability policy are exposed through `Engine:Trust` and `/engine/trust-policy`
- package publisher and signer provenance can be declared and evaluated through package manifests and trust allow-lists
- detached package signatures can be cryptographically verified against trusted public keys or trusted signing certificate chains
- package manifests can declare external distribution metadata and provenance metadata that stay visible through `/engine/packages`
- module-author guidance now covers release-channel, package URI, source revision, build URI, and provenance statement hints for externally distributed packages

Exit criteria:

- Cephalon can load distributable module packages without relying on one monolithic app assembly
- package errors fail fast with actionable messages

## Phase 4: Execution and orchestration model

Status: substantially complete

Goal: expand from a composition engine into a richer execution platform.

Current baseline already in place:

- active modules can now contribute operator-facing execution graphs through `IExecutionGraphContributor`
- active modules can now contribute operator-facing hosted executions through `IHostedExecutionContributor`
- execution graphs are surfaced through `IExecutionRuntimeCatalog`, `/engine/execution-graphs`, and `/engine/snapshot`
- hosted executions are surfaced through `IHostedExecutionRuntimeCatalog`, `/engine/hosted-executions`, and `/engine/snapshot`
- execution-graph lifecycle state is now surfaced through `/engine/runtime-story` and `/engine/snapshot`, including load, activate, and deactivate transitions
- hosted-execution lifecycle state is now surfaced through `/engine/runtime-story` and `/engine/snapshot`, including load, activate, and deactivate transitions
- execution-graph and hosted-execution lifecycle transitions now publish through the shared diagnostics catalog plus `cephalon.execution-graphs.transitions` and `cephalon.hosted-executions.transitions`
- agentic tools can now link back to capability keys, execution graphs, and hosted executions through the existing `Cephalon.Agentics` contract instead of inventing a parallel orchestration registry
- `/engine/technology-surfaces` and `/engine/snapshot` now project those linked AI/orchestration entries with live runtime-story state
- graph descriptors stay additive to the existing module/capability model through module ids and capability-key references
- hosted-execution descriptors stay additive to the existing module and Generic Host model instead of introducing a separate engine-owned runner
- invalid graph ids, entry nodes, edges, module references, and capability references now fail during build instead of leaking broken runtime metadata
- invalid hosted-execution ids, source-module references, and cross-module execution-graph references now fail during build instead of leaking broken runtime metadata
- invalid agent-tool references to unknown capability keys, execution graphs, or hosted executions now fail when the agentic tool catalog is resolved instead of leaking broken orchestration metadata

Exit criteria:

- orchestration features build on the same runtime model instead of bypassing it
- long-running engine behavior is observable and policy-driven

## Phase 5: Solution-level platform

Status: substantially complete

Goal: support higher-level solution shapes, not only individual Cephalon apps.

Current baseline already in place:

- `SuiteScaffoldPlan` and `SuiteScaffoldService` now define a separate suite-level scaffold contract for shared projects, shared folders, and per-service slots
- `ScaffoldScopes.Suite` now marks suite-owned shared assets explicitly instead of overloading the current single-app scaffold scopes
- the first suite-shape validation baseline now fails when service-slot dependencies or shared-folder ownership point at undeclared suite identities
- `SuiteBlueprint` and `BuiltInSuiteBlueprints` now define a built-in `MicroserviceSuite` blueprint that composes repeatable service slots from the existing `Microservice` app blueprint
- suite shared-foundation defaults now reuse the shipped `Microservice` foundation template and package hints instead of defining a second disconnected service-level project shape
- `samples/Cephalon.Sample.MicroserviceSuite` now demonstrates a shared foundation project plus separate catalog and orders services on top of the existing `Microservice` host wiring
- `shared/Cephalon.Sample.MicroserviceSuite.Governance` now demonstrates a shared governance package that keeps optional gateway/control-plane guidance additive to the suite sample instead of folding it into the engine or suite contract

Deliverables:

- `MicroserviceSuite` blueprint composed from the existing app-level `Microservice` scaffold contract is now shipped
- solution-level samples for multiple Cephalon services are now shipped
- shared governance/convention packages are now shipped in the suite sample baseline
- optional gateway or control-plane guidance is now documented as an additive sample-level layer rather than a required suite-contract feature

Exit criteria:

- Cephalon can describe and scaffold not only one service, but an intentional suite of services
- the suite model still reuses the same engine, blueprint, and package contracts

## Phase 6: Cloud and platform integrations

Status: later

Goal: add deployment-targeted companion integrations without pushing vendor assumptions into the engine core.

Current baseline already in place:

- cloud-neutral OTLP exporter wiring through `Cephalon.Observability.OpenTelemetry`
- the shared `Microsoft.Extensions.Logging.ILogger` pipeline plus `Cephalon.Observability.Serilog`
- correlated ASP.NET Core request/response logging through `Engine:Observability:HttpLogging`
- host-agnostic runtime, diagnostics, health, and validation surfaces that later cloud-targeted companions can build on
- self-hosted collector and runtime defaults plus Azure Monitor, AWS, GCP, Huawei Cloud, Alibaba Cloud, Oracle Cloud, Red Hat OpenShift, DigitalOcean, VMware Tanzu, and Kubernetes are now shipped as the first slices on top of the cloud-neutral OTLP baseline, `#120` has now shipped downstream Cloudflare/custom-provider authoring guidance because current Cloudflare docs center Worker-native telemetry export to third-party OTLP destinations rather than a generic external-host sink, `#126` has now shipped Grafana Cloud OTLP endpoint wiring plus access-policy-backed auth headers, and `#127` has now shipped New Relic native OTLP/api-key guidance as the latest explicit vendor-specific child

Deliverables:

- self-hosted observability companion follow-through for OTLP-collector-managed deployments and host-managed runtime defaults is now shipped
- Azure Monitor companion follow-through as the first explicit cloud-specific slice on top of the shared OpenTelemetry baseline is now shipped
- AWS companion follow-through as the second explicit cloud-specific slice on top of the shared OpenTelemetry baseline is now shipped
- GCP companion follow-through is now shipped as the third explicit cloud-specific slice on top of the shared OpenTelemetry baseline
- Huawei Cloud companion follow-through is now shipped as the fourth explicit cloud-specific slice on top of the shared OpenTelemetry baseline
- Alibaba Cloud companion follow-through is now shipped as the fifth explicit cloud-specific slice on top of the shared OpenTelemetry baseline
- Oracle Cloud companion follow-through is now shipped as the latest cloud-specific slice on top of the shared OpenTelemetry baseline, centered on Oracle Cloud APM traces/metrics ingestion plus hosted Oracle defaults instead of folding Oracle-specific data-upload and data-key rules back into the generic OTLP package
- Red Hat OpenShift companion follow-through is now shipped as the latest platform-first slice on top of the shared OpenTelemetry baseline
- DigitalOcean companion follow-through is now shipped as the latest collector-first slice on top of the shared OpenTelemetry baseline, centered on runtime defaults and collector handoff instead of an over-claimed managed OTLP exporter path
- VMware Tanzu companion follow-through is now shipped as the latest proxy-first slice on top of the shared OpenTelemetry baseline, centered on Wavefront proxy handoff and hosted Tanzu defaults instead of a generic vendor-direct OTLP exporter claim
- Kubernetes companion follow-through is now shipped as the latest platform-neutral collector-first slice on top of the shared OpenTelemetry baseline, centered on in-cluster collector wiring and generic cluster resource defaults instead of a vendor-specific managed exporter claim
- downstream Cloudflare and custom-provider companion authoring guidance is now shipped under `#120`, keeping the remaining Cloudflare follow-through honest about the current Worker-native export model instead of promising a generic first-party host-side sink
- Grafana Cloud companion follow-through is now shipped as the latest explicit OTLP endpoint/auth-header slice on top of the shared OpenTelemetry baseline, centered on direct Grafana Cloud endpoint wiring plus access-policy-backed auth headers while keeping the collector-first path available
- New Relic companion follow-through is now shipped as the latest explicit native OTLP endpoint/api-key slice on top of the shared OpenTelemetry baseline, centered on region-aware endpoint defaults plus required `api-key` header guidance while keeping the collector-first path available
- exporter wiring, auth, resource-attribute conventions, and hosted-runtime defaults that stay inside companion packages instead of `Cephalon.Engine`
- documentation, validation, and planning guidance that make the supported targets, deployment assumptions, and downstream companion-package authoring path explicit
- a clear package split whenever different clouds or platforms need distinct companion packs instead of one overloaded abstraction

Exit criteria:

- self-hosted collector and runtime defaults can be enabled on top of the shipped OTLP baseline without modifying `Cephalon.Engine` or `Cephalon.Abstractions`
- supported cloud and platform integrations can be enabled without modifying `Cephalon.Engine` or `Cephalon.Abstractions`
- downstream developer-authored provider packages can reuse the shared telemetry contract without modifying `Cephalon.Engine` or `Cephalon.Abstractions`
- the shared `ILogger` pipeline and cloud-neutral OTLP baseline remain intact
- docs, validation flows, and planning metadata make the supported targets explicit

## Phase 7: External adoption and operator readiness

Status: substantially complete

Goal: let external teams install, validate, package, and run Cephalon outside this repository without repo-only tribal knowledge.

Current baseline already in place:

- `Cephalon.Cli` already ships a working `new` command plus hosted-reference-doc workflows
- `Cephalon.TemplatePack` already ships installable blueprint and module starters
- sample apps and a reference module package already prove the main blueprint and package-authoring shapes inside the repo
- package manifests, trust policy, package policy, and `/engine/packages` already expose the runtime package-loading contract
- runtime status, health, diagnostics, runtime-story, and telemetry-export surfaces already give operators a truthful runtime answer once a host is running
- release validation, package publishing, and template/tool install smoke coverage already existed before phase 7, even though they started from a Windows-first baseline

Deliverables:

- cross-platform script, shell, and CI parity for the repo-native validation, packaging, and install surfaces
- a first-run adoption path with an environment-doctor or equivalent self-check flow in `Cephalon.Cli`
- an end-to-end external module-package lifecycle prove-out that exercises publish, trust, load, and runtime introspection outside the repo-local assembly path
- a containerized local runtime/operations sample path that proves health, config-loading, and telemetry/export handoff under Docker Desktop or WSL-friendly environments without pushing Docker-specific behavior into the engine core
- generated-app bootstrap assets and package-source guidance that let a freshly scaffolded app restore, build, and run from a seeded local feed or a replaced external source without rediscovering Cephalon's package assumptions
- a generated-app published-output baseline that proves scaffolded hosts can be folder-published, started from published artifacts, and inspected through the shipped runtime, health, and docs surfaces
- a generated-app Windows Service deployment baseline that proves scaffolded hosts carry an installable self-hosted Windows service-manager shape after publish without inventing platform-specific packaging from scratch
- a generated-app IIS deployment baseline that proves scaffolded hosts carry a hosted Windows site/app-pool shape after publish without inventing platform-specific ASP.NET Core Module packaging from scratch
- a generated-app Azure App Service deployment baseline that proves scaffolded hosts carry a hosted Azure ZIP-deploy shape after publish without inventing cloud-specific packaging from scratch
- a generated-app container-image publishing baseline that proves scaffolded hosts carry a provider-neutral build/tag/push image shape from the generated Dockerfile and app root without inventing a registry workflow from scratch
- a generated-app Azure Container Apps deployment baseline that proves scaffolded hosts carry a hosted Azure source-deploy shape from the generated Dockerfile and app root without inventing cloud-specific container-deploy packaging from scratch
- a generated-app Kubernetes deployment baseline that proves scaffolded hosts carry a platform-neutral manifest/apply shape from the generated Dockerfile and app root without inventing a second cluster-deploy packaging workflow from scratch
- a generated-app Linux `systemd` deployment baseline that proves scaffolded hosts carry an installable self-hosted service-manager shape after publish without inventing platform-specific packaging from scratch

Current status as of `April 5, 2026`:

- `ENG-033` is implemented: repo-native validation, package publishing, and reference-doc flows now run through `pwsh`-friendly scripts with Windows and Ubuntu CI legs
- `ENG-034` is implemented: `Cephalon.Cli` now ships `cephalon doctor`, and the repo now has a dedicated getting-started path plus aligned help/readme guidance
- `ENG-035` is implemented: published module `.nupkg` artifacts can now be staged through `cephalon package stage`, loaded from out-of-tree package directories, and verified through trust/policy/runtime-introspection coverage
- `ENG-036` is implemented: the modular monolith sample now ships a Dockerfile, compose stack, collector config, optional package-directory override, container-runtime docs, and an optional smoke script that verifies Docker Desktop / WSL-friendly runtime startup plus `/health/*` and `/engine/*` replay
- `ENG-037` is implemented: `cephalon new` and the shipped `dotnet new` app starters now emit `NuGet.config` plus a local package-feed placeholder, the shared prerelease package-version baseline is aligned again, and a real generated app has been verified through scaffold -> package publish -> build -> Docker compose smoke
- `ENG-038` is implemented: scaffolded hosts and shipped `dotnet new` app starters now emit `Properties/PublishProfiles/CephalonFolder.pubxml`, publish into deterministic `artifacts/publish/<ProjectName>/` output, and are verified through a real scaffold -> package publish -> folder publish -> run published output smoke path
- `ENG-039` is implemented: scaffolded hosts and shipped `dotnet new` app starters now emit Linux `systemd` deployment assets under `deploy/linux/systemd/`, and those generated units are verified through a real scaffold -> package publish -> folder publish -> WSL `systemd-analyze verify` smoke path
- `ENG-040` is implemented: scaffolded hosts and shipped `dotnet new` app starters now emit Windows Service deployment assets under `deploy/windows-service/`, and those generated install/remove scripts are verified through a real scaffold -> package publish -> folder publish -> install-preview smoke path against published output
- `ENG-041` is implemented: scaffolded hosts and shipped `dotnet new` app starters now emit IIS deployment assets under `deploy/iis/`, and those generated install/remove scripts plus the SDK-generated ANCM `web.config` are verified through a real scaffold -> package publish -> folder publish -> install-preview smoke path against published output
- `ENG-042` is implemented: scaffolded hosts and shipped `dotnet new` app starters now emit Azure App Service deployment assets under `deploy/azure-app-service/`, and those generated ZIP packaging and deploy-preview scripts are verified through a real scaffold -> package publish -> folder publish -> run-from-package smoke path against the current Azure CLI contract
- `ENG-043` is implemented: scaffolded hosts and shipped `dotnet new` app starters now emit Azure Container Apps deployment assets under `deploy/azure-container-apps/`, and those generated source-deploy scripts are verified through a real scaffold -> package publish -> local Docker build -> deploy-preview smoke path against the current Azure CLI contract
- `ENG-044` is implemented: scaffolded hosts and shipped `dotnet new` app starters now emit Kubernetes deployment assets under `deploy/kubernetes/`, and those generated manifest/apply scripts are verified through a real scaffold -> package publish -> local Docker build -> `kubectl kustomize` preview smoke path against the generated app root
- `ENG-045` is implemented: scaffolded hosts and shipped `dotnet new` app starters now emit container-image publishing assets under `deploy/container-image/`, and those generated build/tag/push scripts are verified through a real scaffold -> package publish -> local Docker build -> local-registry push smoke path against the generated app root
- the planned phase-7 baseline plus the generated-app bootstrap, published-output, container-image publishing, Windows/Linux self-hosted deployment follow-through, hosted Windows IIS path, hosted Azure App Service plus Azure Container Apps paths, and the platform-neutral Kubernetes path are now in place, so the next adoption work can stay scenario-driven instead of filling a known install/run/deploy gap

Exit criteria:

- a team can install the CLI or template pack, scaffold an app or module, and validate its environment on Windows or Linux-class shells without custom repo knowledge
- the repo-native validation and packaging flow no longer depends on Windows-only shell assumptions
- an out-of-tree Cephalon package can be published, trusted, loaded, and inspected through the shipped runtime surfaces
- at least one adoption-quality sample host can be run through a documented containerized path that preserves the current `/engine/*`, `/health/*`, and telemetry behaviors
- a freshly scaffolded Cephalon app can restore, build, and take the documented container path after seeding or repointing the supported `cephalon` package source
- a freshly scaffolded Cephalon app can publish to a deterministic folder profile and run from published output with the expected runtime, health, and docs surfaces
- a freshly scaffolded Cephalon app can carry a documented Windows Service baseline with generated install assets and a verified install-preview path against published output
- a freshly scaffolded Cephalon app can carry a documented IIS site/app-pool baseline with generated install assets, the expected ANCM `web.config`, and a verified install-preview path against published output
- a freshly scaffolded Cephalon app can carry a documented Azure App Service ZIP-deploy baseline with generated packaging assets, `WEBSITE_RUN_FROM_PACKAGE` guidance, and a verified Azure CLI preview path against published output
- a freshly scaffolded Cephalon app can carry a documented container-image publishing baseline with generated build/tag/push assets, provider-neutral registry guidance, and a verified local-registry smoke path against the generated app root
- a freshly scaffolded Cephalon app can carry a documented Azure Container Apps source-deploy baseline with generated Docker assets, `az containerapp up --source` guidance, and a verified Azure CLI preview path against the generated app root
- a freshly scaffolded Cephalon app can carry a documented Kubernetes deployment baseline with generated manifest/apply assets, `kubectl kustomize` guidance, and a verified preview path against the generated app root
- a freshly scaffolded Cephalon app can carry a documented Linux `systemd` service baseline with generated install assets and a verified self-hosted service-manager path

## Phase 8: Configurable application architecture and runtime primitives

Status: in progress

Goal: let consumer apps keep one Cephalon codebase while switching architecture, data, messaging, identity, tenancy, and audit choices through configuration and additive companion packs instead of host rewrites or blueprint explosion.

Product principle for this phase: Cephalon should lower ceremony for consumer apps by absorbing repetitive plumbing, declarations, and host wiring so teams write less framework code and focus more of their codebase on business logic.

Current baseline already in place:

- configuration-driven `Blueprint`, `Patterns`, `Technologies`, and `Transports` through the `Engine` section
- built-in blueprints for `ModularMonolith`, `ModularVerticalSlice`, and `Microservice`
- shipped pattern and technology catalogs plus additive technology companion-package wiring
- scaffold plans that already imply `Application`/`Domain`/`Infrastructure` plus `Commands`/`Queries`/`Policies`/`Strategies` starter shapes
- runtime snapshot, runtime-story, diagnostics, technology-surface, and package-introspection endpoints that later packs can extend without inventing a second control plane
- an observability companion-package ecosystem that already proves provider-specific follow-through can stay outside `Cephalon.Engine` and `Cephalon.Abstractions`
- `ENG-046` landed locally: phase-8 pattern and technology ids now exist with alias-aware resolution in the shipped catalogs
- `ENG-047` landed locally: structured `Engine:Data`, `Engine:Identity`, `Engine:Tenancy`, `Engine:Audit`, and `Engine:Messaging` settings now flow into the resolved app profile with phase-8 prerequisite validation
- `ENG-048` landed locally: `Cephalon.Abstractions` now carries host-agnostic data, authorization, tenancy, audit, and id contracts with XML comments, package-surface coverage, and reference-doc validation, and `Cephalon.Engine` now exposes merged projection, inbox, outbox, and authorization-policy catalogs through `/engine/projections`, `/engine/inboxes`, `/engine/outboxes`, `/engine/authorization-policies`, and `/engine/snapshot`
- `ENG-049` is in progress locally: `Cephalon.Data` now provides runtime-neutral `IReadStore` / `IWriteStore` dispatching backed by command/query handlers, `Cephalon.Data.EntityFramework` now registers single-context or split read/write Entity Framework Core `DbContext` roles plus opt-in Entity Framework-backed inbox and outbox baselines and optional `Sfid.EntityFramework` conventions, `Cephalon.Engine` now surfaces additive inbox/outbox catalogs through `IInboxCatalog`, `IOutboxCatalog`, `/engine/inboxes`, `/engine/outboxes`, and `/engine/snapshot`, the Entity Framework pack now contributes staged-only `outbox-producers` entries plus application-managed `inbox-stores` entries under `event-driven-integration` when that technology is active, the Entity Framework outbox now also exposes an adapter-neutral `IEventDispatchStore` with durable `dispatch_attempt_count` / `dispatched_at_utc` / `next_attempt_at_utc` follow-through for later first-class adapters, and `Cephalon.Ids.Sfid` wraps the official `Sfid.Net` generator behind `IIdGenerator` plus the official generator interface with `Engine:Data:Ids:Sfid` topology support while richer projection persistence/runtime surfaces remain open
- `ENG-050` is now in progress locally: `Cephalon.Eventing` exposes public `EventPublication`, `IEventPublisher`, `EventDispatchItem`, and `IEventDispatchStore` contracts, registers an outbox-backed staged publication path only when a real `IOutbox` exists, surfaces that truth through `eventing.publish` and the `event-publishers` runtime surface, now also exposes declared subscription descriptors through `eventing.subscriptions` and the `event-subscriptions` runtime surface, can report when an application-managed inbox store is available, can link declared subscriptions to hosted-execution descriptors plus execution graphs and runtime-story state when modules publish that metadata, now also carries application-managed subscription runtime-state/reporting plus stable diagnostics conventions for those outcomes, now also carries application-managed outbox-dispatch runtime-state/reporting through `IEventDispatchRuntimeReporter` / `IEventDispatchRuntimeCatalog` with `reported.*` metadata on the new `event-dispatches` surface, now projects configured dispatch-runtime descriptor metadata through those outbox-facing entries, now has a runtime-neutral bridge for later adapter-owned dispatch loops without claiming that the pack itself already owns broker dispatch, retries, or handler execution, and now ships an official `Cephalon.Eventing.Wolverine` adapter slice that can run as a thin host-wiring baseline with `dispatchBridge = consumer-managed` or as an opt-in `wolverine-managed` durable staged-dispatch loop on top of `IEventDispatchStore` while its adapter surface aggregates latest outcome and retry/runtime totals for operator views and its pack-specific diagnostics convention exposes stable `4300-4303` loop event ids through `/engine/diagnostics`
- `ENG-051` is now in progress locally: `Cephalon.Identity` now exists as the first host-agnostic identity companion pack with config-driven `IdentityRuntimeOptions`, declarative `IdentityPolicyMetadataKeys`, a default metadata-driven `IAuthorizationEvaluator`, a truthful `identity-authorization` technology surface under `identity-access`, and stable `4400-4401` diagnostics-catalog entries for allow/deny outcomes, and `Cephalon.Identity.AspNetCore` now exists as the follow-through host adapter with config-driven `Engine:Identity:AspNetCore` options plus a REST-only `RequireCephalonAuthorization(...)` helper that maps `ClaimsPrincipal`, route values, and request metadata into the shared Cephalon authorization contracts without polluting the core
- `ENG-051` follow-through now also honors ASP.NET Core challenge/forbid behavior when the host or endpoint metadata already declares authentication schemes, including the low-ceremony `WithCephalonAuthenticationSchemes(...)` endpoint helper, which keeps scheme ownership with the consumer host while letting Cephalon return ecosystem-native `401` and `403` outcomes instead of always forcing problem-details fallbacks
- `ENG-051` follow-through now also respects `AllowAnonymous` endpoint metadata inside protected route groups so consumer apps can keep standard ASP.NET Core public-route semantics without forking Cephalon authorization wiring for the rest of the group
- `ENG-051` follow-through now also has direct request-factory coverage for custom claim-type mapping, subject-id fallback behavior, and optional claim/route/query/header projection flags so the low-ceremony ASP.NET Core adapter keeps its config-driven authorization-request shaping truthful beyond the happy-path hosting tests
- `ENG-051` follow-through now also honors `EnableDefaultEvaluator` and `EnableRuntimeSurface` truthfully, so the host-agnostic pack can opt out of the built-in evaluator without turning protected endpoints into missing-service failures and can suppress the `identity-authorization` runtime surface entirely when a consumer wants that pack-level telemetry quiet
- `ENG-051` follow-through now also covers controller/action boundaries through a public `[RequireCephalonAuthorization]` attribute and projects an `identity-aspnetcore` runtime surface so `/engine/technology-surfaces` can report how many ASP.NET Core endpoints are protected, which Cephalon policy ids are active, and where `AllowAnonymous` overrides still exist across minimal APIs and MVC-style controllers
- `ENG-051` follow-through now also bridges authenticated ASP.NET Core principal data into the ambient audit actor contract when `Cephalon.Audit` is active and the host has not registered a custom accessor, which keeps the low-ceremony actor path in the host adapter instead of leaking ASP.NET Core concerns into the host-agnostic audit pack
- `ENG-052` is now in progress locally: `Cephalon.MultiTenancy` now exists as the first host-agnostic tenancy companion pack with config-driven `MultiTenancyRuntimeOptions`, `AddMultiTenancy(...)`, a built-in configuration-driven `ITenantResolver`, an ambient `ITenantContextAccessor`, a truthful `tenant-resolution` surface under `multi-tenancy`, stable `4500-4502` diagnostics-catalog entries, and explicit miss semantics that keep tenant-id, tenant-key, and host-name mismatches from silently defaulting into another tenant while still allowing hosts to disable the built-in resolver cleanly
- `ENG-052` is also now proving the narrow audit slice locally: `Cephalon.Audit` exists as the first host-agnostic audit companion pack with `AddAudit(...)`, `AuditRuntimeOptions`, `AuditMetadataKeys`, ambient actor access, a default recorder, stable `4600-4601` diagnostics-catalog entries, and a dedicated `IAuditStoreCatalog` surfaced through `/engine/audit-stores` and `/engine/snapshot`
- `ENG-052` follow-through now also honors `AuditRuntimeOptions.EnableInMemoryWriter` / `Engine:Audit:EnableInMemoryWriter` end to end across service-collection, ASP.NET Core, and Worker host paths, and the runtime audit-store catalog now stays aligned with that choice instead of pretending the memory-backed store is active when it is not
- `ENG-052` follow-through now also preserves additive consumer audit-store contributions when `AddAudit()` is active, so `/engine/audit-stores` and `/engine/snapshot` keep showing custom/runtime-provided stores even when the built-in `audit-default` store is disabled or absent
- `ENG-052` follow-through now also keeps audit actor resolution low ceremony in ASP.NET Core hosts by consuming the authenticated principal through the identity adapter when available, while runtime surfaces still answer truthfully whether that bridge is active, merely available, or absent and custom audit actor accessors remain authoritative
- `ENG-053` is now in progress locally: `Cephalon.Scaffolding` and `Cephalon.Cli` now emit canonical phase-8 ids plus structured `Engine:Data`, `Engine:Identity`, `Engine:Tenancy`, `Engine:Audit`, and `Engine:Messaging` sections, generated hosts now centralize the common low-ceremony phase-8 pack wiring, generated tests now start with architecture smoke checks plus per-feature behavior specifications, `Cephalon.TemplatePack` starter apps now carry a narrow `Sfid` plus `Audit` baseline with the same canonical config shape, and starter sample hosts now mirror that same baseline with hosting coverage that locks their `/engine/app-model` answers
- `ENG-055` is now in progress locally: `scripts/validate-phase8-conventions.ps1` now gives phase 8 a named validation replay across settings/profile truth, runtime catalogs and surfaces, relational data plus `Sfid`, eventing plus Wolverine, identity/tenancy/audit, ASP.NET Core adapter follow-through, starter generation, package-surface truth, reference-doc generation, and adoption-doc alignment, `scripts/validate-release.ps1` now runs that focused suite by default, `Cephalon.Benchmarks` now carries explicit phase-8 composition, runtime-lifecycle, and scaffolding baselines with refreshed guardrail thresholds from the current BenchmarkDotNet output, and the repo-native release-validation path is green again after compatibility-truth and discovery-scope fixes in the reference-module and test harness assets
- messaging adapter decision for phase 8 is now explicit: `Cephalon.Eventing.Wolverine` is the current first-class adapter path, `MassTransit` is the tracked-later adapter candidate after that first-class path is validated, and `MediatR`, `LiteBus`, `NServiceBus`, and `SlimMessageBus` remain consumer-owned coexistence choices unless a later bridge or adapter package is deliberately shipped

Milestone outline:

- `M1 Taxonomy and contracts`: freeze blueprint vs pattern vs technology vs companion-pack semantics; add structured `Engine:Data`, `Engine:Identity`, `Engine:Tenancy`, `Engine:Audit`, and `Engine:Messaging` sections; add host-agnostic contracts and runtime surfaces
- `M2 Data and messaging foundation`: ship a relational-first golden path through `Cephalon.Data`, `Cephalon.Data.EntityFramework`, CQRS read/write split, projections, outbox, event runtime follow-through, optional Wolverine integration, and `Cephalon.Ids.Sfid`
- `M2` should use the official `Sfid.Net` and `Sfid.EntityFramework` packages for the id baseline instead of inventing a custom Cephalon-specific Snowflake implementation
- `M3 Identity, tenancy, and audit baseline`: ship optional `RBAC`, `ABAC`, and policy-based authorization, multi-tenant runtime contracts plus tenant-aware audit/history surfaces, and keep ASP.NET Core-specific wiring in host adapters
- `M4 CLI, scaffolding, templates, and samples`: align `Cephalon.Cli`, `Cephalon.Scaffolding`, `Cephalon.TemplatePack`, and adoption-quality samples with the same phase-8 config and package contract
- `M5 Provider-family and hybrid-runtime follow-through`: expand beyond the relational-first baseline into explicit non-relational provider families plus additive `HybridCloudRuntime`, `ServiceMeshIntegration`, and `ServerlessHosting` follow-through only after the core contract is proven

Planned workstreams:

- `WS1 Engine core`: taxonomy, structured settings, host-agnostic contracts, validation, runtime surfaces, and XML-commented public descriptors
- `WS2 Companion packs`: data, eventing, identity, multi-tenancy, audit, Sfid, and optional third-party adapters that stay config-driven and observability-aware
- `WS3 CLI and scaffolding`: generation, package hints, template defaults, and golden-path samples that mirror the same runtime semantics
- `Validation lane`: cross-cutting review of benchmarks, test coverage, XML comments, runtime introspection, docs/reference-doc alignment, and over-claim risk before milestone closeout

Deliverables:

- a canonical mapping for `Hexagonal`, `Layered`, `CleanArchitecture`, `DDD`, `CQRS`, `Outbox`, and `EventSourcing` as patterns instead of new blueprints
- a canonical mapping for `IdentityAccess`, `MultiTenancy`, `HybridCloudRuntime`, `ServiceMeshIntegration`, and `ServerlessHosting` as additive technologies instead of engine-core rewrites
- host-agnostic contracts for commands, queries, read/write stores, projections, outbox/inbox, authorization subjects/resources/policies, tenant context/resolution, audit entries, and id generation
- a relational-first, Entity Framework-centered baseline that proves CQRS read/write split, projections, outbox handoff, and `Sfid` ids before broader provider expansion
- event-driven runtime follow-through that upgrades the shipped event-channel surface into truthful declared-subscription plus staged-publishing, then fuller publisher/subscriber/runtime-answer semantics without breaking the current technology-pack split
- optional identity/authorization, multi-tenancy, and audit companion packages that a consumer can turn on, turn off, or override through configuration
- CLI/scaffolding/template/sample alignment with the same config sections, package hints, and scaffold plans
- observability, diagnostics, and runtime-snapshot follow-through for every active phase-8 pack
- benchmark, validation, docs, and reference-doc follow-through that keeps the shipped capability claims truthful as phase-8 packages land
- a lower-ceremony consumer experience where common infrastructure, architecture, and runtime choices move into config, companion packs, and scaffold conventions instead of repeated host/bootstrap code

Exit criteria:

- a consumer app can keep one Cephalon codebase and change supported architecture, data, security, and runtime choices through configuration plus companion-pack selection rather than a host rewrite
- `Cephalon.Abstractions` stays host-agnostic and public contracts remain XML-commented enough for supported reference-doc publishing
- the first golden path works end to end for relational Entity Framework plus CQRS plus outbox plus event-driven integration plus configurable identity/authorization plus multi-tenancy plus audit plus `Sfid`
- CLI generation, scaffold output, templates, samples, and runtime introspection tell the same story for the phase-8 baseline
- benchmark guardrails, docs, and public XML comments stay aligned with each shipped phase-8 contract instead of lagging implementation
- non-relational provider breadth plus hybrid-cloud, service-mesh, and serverless follow-through remain explicit later slices until the golden path proves the contract
- consumer apps can keep framework ceremony low by declaring architecture/runtime choices once through configuration and package selection while concentrating hand-written code on business logic, domain rules, and use-case behavior

Current planning note as of `April 8, 2026`:

- `ENG-046`, `ENG-047`, and `ENG-048` should freeze the phase-8 taxonomy, settings, and contracts before workstream-specific implementation names drift
- `ENG-049` and `ENG-050` are now actively proving the relational-first data and eventing baseline before broader provider expansion; the next truth gate is moving from application-managed publication/subscription reporting into a truthful first-class adapter path without over-claiming pack-owned dispatch behavior
- `ENG-051` and `ENG-052` should layer identity/authorization plus multi-tenancy/audit on top of the frozen data/messaging contract
- `ENG-051` is now proving the host-agnostic evaluator/runtime-surface baseline plus the first ASP.NET Core adapter slice, and the next follow-through should deepen scheme/challenge alignment plus broader host-integration coverage without polluting the core
- `ENG-052` is now proving the configuration-driven tenant-resolution/runtime-surface baseline plus the first narrow `Cephalon.Audit` recording slice, and the next follow-through should deepen audit storage and adapter options instead of overloading the tenancy pack with membership, domain-onboarding, or data-isolation claims too early
- `ENG-053` is now proving the TDD/BDD-friendly starter-test convention on top of the frozen ids/config/package baseline, and the next follow-through should focus on any remaining sample/template docs parity plus starter polish instead of re-deciding the starter semantics
- `ENG-055` is now establishing the named validation replay, refreshed benchmark guardrails, and docs/runtime-truth guard for the shipped phase-8 baseline, while `ENG-056` should continue the broader docs, XML-comment, and reference-doc closeout before phase 8 claims broad readiness
- `ENG-056` is now done locally: the checked-in `docs/reference/` bundle has been regenerated for the current phase-8 assembly set, the tooling test lane now guards bundle drift by comparing the checked-in output against the current `Cephalon.ReferenceDocs` generator after normalizing volatile timestamps, and the top-level adoption docs plus blueprint sample READMEs now describe the shipped phase-8 starter baseline truthfully
- `ENG-054` Track 1 (non-relational provider families) is now complete: all 9 provider families (MongoDB, Redis, Neo4j, Cassandra, ClickHouse, Elasticsearch, OpenSearch, Qdrant, NATS) shipped across Sprints 25–31 with 18 companion packages (9 data + 9 event-sourcing), 648/648 tests green; hybrid-runtime, service-mesh, and serverless expansion remain `later` until explicit adoption cases
- `ENG-057` event-sourcing follow-through is now complete: `Cephalon.EventSourcing` core contracts plus 10 provider implementations (EntityFramework + 9 non-relational) are shipped with `IBehaviorContext.EventStore` wiring through ENG-058 M6

## Phase 9: Adaptive Behavior Topology

Status: done

Goal: introduce a unified application-behavior model that composes domain operations across transports, patterns, and execution strategies without requiring per-transport or per-pattern rewrites.

Delivered:

- `ENG-058 M1` ABT foundation: `IAppBehavior`, `IBehaviorContext`, `BehaviorDispatcher`, `BehaviorExecutionSlot`, `CompatibilityMatrix`, 6 compatibility rules (ABT-001 through ABT-006), hosting integration — 499/499 tests (Sprint 20)
- `ENG-058 M2` HTTP Transport Pack: 7 HTTP bindings (`rest`, `jsonrpc`, `graphql`, `graphql-sse`, `graphql-ws`, `sse`, `ws`), `LazyTransportBinding` — 516/516 tests (Sprint 21)
- `ENG-058 M3` Messaging Transport Pack: InMemory, RabbitMQ, Kafka bindings; M2 CTS leak fix — 527/527 tests (Sprint 22)
- `ENG-058 M4` Pattern Execution Strategies: 5 strategies (`cqrs`, `event-driven`, `saga-step`, `process-manager`, `direct`), `ISagaStateStore`, `IProcessCheckpointStore`, `FrozenDictionary` registry, `IBehaviorContext.CorrelationId`, `IProcessCompletion` — 575/575 tests (Sprint 23)
- `ENG-058 M5` Source Generator: `BehaviorSourceGenerator` (`IIncrementalGenerator` + `DiagnosticAnalyzer`), ABT0010–ABT0013 diagnostics, `BehaviorRegistrationHints.g.cs` — 584/584 tests (Sprint 24)
- `ENG-058 M6` Runtime Integration: `BehaviorRuntimeContributor`, `IBehaviorAdvisory` system, `IBehaviorContext.EventStore` wiring, `BehaviorDiagnostics` EventId 5100-5109 — 592/592 tests (Sprint 24)
- `ENG-058-T30` behavior-aware REST endpoint helper follow-through: `MapBehaviorRestGroup(...)`, `BehaviorRestEndpointGroup.MapBehaviorGet/Post/Put/Patch/Delete(...)`, module-major versioned operation-name defaults, XML-comment-backed OpenAPI enrichment, `DefaultBehaviorContext` event-store DI wiring for HTTP execution, and showcase cart route deduplication — composition HTTP tests 13/13 plus showcase hosting tests 33/33 (Sprint 31)
- `ENG-058-T31` behavior REST OpenAPI summary/description split follow-through: behavior XML `<summary>` now maps to the OpenAPI operation summary while XML `<remarks>` maps to the OpenAPI operation description, avoiding duplicate Scalar text and locking the generated cart endpoint contract through showcase hosting coverage — composition HTTP tests 13/13 plus showcase hosting tests 34/34 (Sprint 31)
- `ENG-058-T32` behavior REST OpenAPI document-version follow-through: `BehaviorRestEndpointGroup.ApiVersion(...)` now assigns behavior-shaped REST endpoints to named OpenAPI documents, operation-name versioning now falls back to the module major only when no explicit API version is supplied, `OpenApi:Documents` plus optional `OpenApi:DefaultDocument` now drive ASP.NET Core OpenAPI/Scalar document registration, showcase cart routing now opts into `v1`, and hosting coverage now locks `/openapi/v2.json` plus `/scalar/v2` behavior — composition HTTP tests 14/14 plus hosting tests 235/235 (Sprint 31)
- `ENG-058-T33` behavior REST OpenAPI versioned-config canonicalization: `OpenApi:EnabledVersions` plus `OpenApi:DefaultVersion` is now the canonical host config for versioned API documents, legacy `OpenApi:Documents` and `OpenApi:DefaultDocument` remain available for backward compatibility or custom named docs, the global `OpenApi:Version` info override now only applies to single-document hosts so multi-document metadata stays truthful, and the showcase sample plus authoring docs now demonstrate the numeric version contract — composition HTTP tests 14/14 plus hosting tests 235/235 (Sprint 31)
- `ENG-058-T34` Scalar multi-document doc-link follow-through: `/scalar` now redirects to the configured default versioned document while `/scalar/v1`, `/scalar/v2`, and similar paths remain available as pinned doc links; hosting coverage and authoring docs now lock that behavior — composition HTTP tests 14/14 plus targeted hosting tests 2/2 (Sprint 31)
- `ENG-058-T35` canonical REST/doc version alignment: `.ApiVersion(major)` now prefixes behavior-owned REST routes with `/v{major}` so host-mounted `/api` surfaces line up with the selected OpenAPI document, the showcase sample now exposes versioned REST routes under `/api/v1/...`, Scalar document selection rewrites back to canonical `/scalar/v1` and `/scalar/v2` links, and hosting/composition coverage locks the aligned contract — composition HTTP tests 14/14 plus targeted hosting tests 3/3 plus showcase hosting tests 34/34 (Sprint 31)
- `ENG-058-T36` configurable docs-route surfaces and module-major REST defaults: `OpenApi:RoutePattern` now controls the OpenAPI JSON route, `OpenApi:Scalar:RoutePrefix` now controls the Scalar UI base path, `ApiRoutes:Prefixes:Rest` now controls the built-in REST host prefix, and `MapBehaviorRestGroup(...)` now defaults its document/route version from the owning module descriptor major version while keeping `.ApiVersion(...)` as an explicit override. That round established the version-aligned REST/OpenAPI/Scalar contract before the broader transport-prefix unification landed — composition HTTP tests 14/14 plus hosting tests 4/4 plus showcase hosting tests 34/34 (Sprint 31)
- `ENG-058-T37` shared `BehaviorApiSurface` canonical routing for generic behavior HTTP bindings: `BehaviorTopologyDescriptor` now carries a transport-agnostic `ApiSurface`, `BehaviorApiSurfaceDescriptor.CreateDefault(...)` derives group/operation paths from the behavior id, `WithApiSurface(...)` plus the source generator keep fluent and compile-time topology aligned, and the JSON-RPC, GraphQL, GraphQL-SSE, GraphQL-WS, SSE, and WebSocket bindings now project canonical versioned routes from that shared surface. The earlier `/behaviors/{id}` compatibility aliases introduced in that round were later removed by `ENG-058-T39` so the canonical versioned routes remain the only generic behavior HTTP surface — behavior API-surface tests 4/4 plus composition HTTP tests 14/14 (Sprint 31)
- `ENG-058-T38` canonical `ApiRoutes:Prefixes` defaults across built-in and generic HTTP transport surfaces: the canonical host config now defaults to `Rest=/api`, `GraphQL=/graphql`, `JsonRpc=/json-rpc`, `Grpc=/grpc`, `Ws=/ws`, `Sse=/sse`, `GraphQLWs=/graphql-ws`, and `GraphQLSse=/graphql-sse`; built-in transport mappers now read the same prefix contract as the generic behavior bindings; the showcase sample now consumes route prefixes from generated client config instead of hard-coded transport paths; and hosting/composition coverage now locks both default and override behavior — behavior API-surface + HTTP binding tests 22/22 plus targeted hosting tests 3/3 plus showcase hosting tests 34/34 (Sprint 31)
- `ENG-058-T39` remove generic `/behaviors/{id}` aliases from the behavior HTTP surface: canonical versioned routes are now the only generated behavior HTTP endpoints; `BehaviorApiSurfaceRouteResolver` no longer appends behavior-id compatibility aliases; the retired behavior-specific prefix/config aliases have been removed so the generic bindings read the same canonical `ApiRoutes:Prefixes:*` contract as the built-in host mappers; and XML comments, component docs, authoring guidance, and HTTP binding coverage now treat the canonical versioned routes as the single public contract — composition HTTP tests 24/24 (Sprint 31)
- `ENG-058-T40` public REST documentation separation plus tag-metadata follow-through: module-owned REST endpoints now own the published REST OpenAPI + Scalar surface, `MapBehaviorRestGroup(...)` can override tag names and descriptions explicitly, module XML `<summary>` plus `<remarks>` can now flow into top-level OpenAPI tag descriptions, and hosting/tooling coverage locks the public REST-vs-generic-adapter distinction — targeted hosting tests 3/3 plus package-surface tests 51/51 (Sprint 31)
- `ENG-058-T41` module-owned REST cleanup and removal of behavior-declared REST: `http.rest` is no longer a valid behavior allowlist or topology transport, `ViaHttpRest(...)` and the generic REST binding/config surface were removed, `Engine:Behaviors` now stays focused on auto-registration instead of per-behavior REST overrides, `ApiRoutes:Prefixes:Rest = ""` still mounts versioned module-owned REST routes at the root while `null` still falls back to `/api`, and docs/sourcegen/reference output now treat `MapEndpoints(...)` plus `MapBehaviorRestGroup(...)` as the only REST authoring path — behavior baseline + behavior source-generator + hosting + tooling coverage (Sprint 31)
- `ENG-058-T42` attribute-only behavior baseline synthesis plus transport alias normalization: the runtime now synthesizes topology from `[BehaviorAllowedPatterns]` plus `[BehaviorAllowedTransports]` when exactly one pattern is declared and no explicit topology exists, ambiguous multi-pattern declarations fail fast with a clearer authoring error, `http.grpc` is accepted as an allowlist alias for canonical `grpc`, and composition/component-doc coverage now lock the attribute-only registration behavior for non-REST transports (Sprint 31)
- `ENG-058-T43` module-owned behavior authoring base classes and ownership validation: `IBehaviorModuleBuilder`, `IBehaviorOwnerModule`, and `OwnedBehaviorRegistration` now make explicit module-owned behaviors a first-class engine contract, `BehaviorModuleBase` and `RestBehaviorModuleBase` now give authors a cleaner module API than implementing multiple interfaces directly, engine composition now validates duplicate ownership and preserves owned topology when auto-registration is enabled, REST helper mapping now rejects another module's owned behavior, and docs/reference output now publish the new ownership-first authoring path — behavior baseline tests 47/47 plus hosting tests 3/3 plus tooling tests 72/72 (Sprint 31)
- `ENG-058-T43` explicit module-owned behavior authoring model: `IBehaviorOwnerModule`, `IBehaviorModuleBuilder`, and `OwnedBehaviorRegistration` now make module-owned behavior registration explicit; `BehaviorModuleBase` and `RestBehaviorModuleBase` now give developers a low-ceremony authoring path for process-only and REST-exposed behavior modules; engine composition now validates duplicate ownership across modules; and behavior-backed REST mapping now rejects modules that try to expose behaviors owned by another module — behavior baseline tests 46/46 plus REST OpenAPI hosting tests 3/3 plus package-surface tests 51/51 (Sprint 31)
- `ENG-058-T43` module-owned behavior authoring baseline: `IBehaviorOwnerModule`, `IBehaviorModuleBuilder`, and `OwnedBehaviorRegistration` now make module-owned behavior declarations explicit, `BehaviorModuleBase` and `RestBehaviorModuleBase` give authors a higher-level authoring path, engine build now rejects duplicate owners, and REST helper mapping now rejects cross-module behavior exposure when ownership is explicit (Sprint 31)
- `ENG-058-T44` single-surface REST behavior-module DSL and opt-in auto-registration fallback: `RestBehaviorModuleBase` now centers authoring on `ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)` so public REST routes and internal-only behavior ownership can live in one module method, `IRestBehaviorModuleBuilder` plus `IRestBehaviorEndpointGroupBuilder` now make `Group(...).MapGet/MapPost/...` imply ownership automatically while `Internal<TBehavior>()` covers internal-only or custom/manual-route behaviors, `MapAdditionalEndpoints(...)` remains the advanced escape hatch for raw Minimal API work, `Engine:Behaviors:AutoRegister` now defaults to `false` so module ownership is the primary path and assembly scanning is an opt-in fallback, and showcase/reference-doc/component coverage now all align with that authoring model — behavior owner + baseline tests 48/48 plus REST OpenAPI + showcase hosting tests 37/37 plus package-surface tests 51/51 (Sprint 31)
- `ENG-058-T46` transport-neutral behavior results plus optional REST result envelopes: `Cephalon.Abstractions` now ships `BehaviorResult<T>`, `IBehaviorResult`, `BehaviorResultStatus`, and `BehaviorFaultSeverity` so behaviors can return structured expected outcomes without forcing HTTP envelopes into the core contract, explicit module-owned behaviors now fall back to `direct` topology when no extra topology metadata is supplied, ASP.NET Core now exposes `ResultModel<T>`, `ResultModelError`, and `ResultModelErrorDetail` as the optional REST wire envelope behind `ApiRoutes:ResultEnvelope:Enabled`, REST failures now use an `errors` collection so validation and other multi-reason outcomes can return more than one structured item, module-owned REST can wrap both raw `TOut` and `BehaviorResult<T>` results without forcing transport-specific envelopes back into the behavior contract, and module-owned REST OpenAPI now publishes wrapped success schemas without duplicating error properties on `2xx` responses — behavior result + baseline tests 61/61 plus REST OpenAPI hosting tests 2/2 plus package-surface tests 51/51 (Sprint 31)
- `ENG-058-T47` plural REST error envelopes and multi-fault projection follow-through: the optional REST `ResultModelError` contract now uses an `errors` collection instead of a singular `error` object, `BehaviorRestResponseMapper` now projects `BehaviorFault.InnerFaults` into multiple structured REST errors for validation and other multi-reason failures, `ResultModelDocumentTransformer` now treats `errors` as the canonical REST envelope property without carrying a legacy singular fallback, and hand-authored docs plus REST OpenAPI hosting coverage now lock the plural wire contract — REST OpenAPI hosting tests 5/5 plus package-surface tests 51/51 (Sprint 31)
- `ENG-058-T48` showcase `AddToCartBehavior` authoring example for `BehaviorResult<T>`: the cart sample now demonstrates transport-neutral expected outcomes directly in `AddToCartBehavior`, including multi-fault validation through `BehaviorFault.InnerFaults`, a conflict result when the cart is already checked out, showcase-host overrides that opt into the REST result envelope for focused tests, and module-authoring/component docs that now point to the cart sample as the concrete reference for `BehaviorResult<T>` plus REST `errors` projection — showcase hosting tests 36/36 (Sprint 31)
- `ENG-058-T49` concise no-payload `BehaviorResult` factories: `BehaviorResultDescriptor` plus implicit conversion into `BehaviorResult<T>` now let common async-return branches use `BehaviorResult.Invalid(...)`, `BehaviorResult.NotFound(...)`, `BehaviorResult.Conflict(...)`, `BehaviorResult.Forbidden(...)`, `BehaviorResult.Unauthorized(...)`, and `BehaviorResult.NoContent(...)` without restating the payload type; sample/component docs now demonstrate the shorter authoring path, and the authoring guidance now spells out the one remaining `Task.FromResult<BehaviorResult<TOut>>(...)` inference edge case explicitly — behavior result tests 3/3 plus package-surface tests 51/51 plus clean-worktree REST OpenAPI hosting verification (Sprint 31)
- `ENG-058-T51` concise `Result<T>` / `Result` aliases for transport-neutral outcomes: `Cephalon.Abstractions` now prefers `Result<T>` plus `Result.*(...)` for new behavior authoring while keeping `BehaviorResult<T>` / `BehaviorResult` as compatibility aliases, ASP.NET Core REST/OpenAPI detection now recognizes both result families, and the component/module-authoring docs now point teams to the shorter authoring shape by default while still documenting the legacy alias path (Sprint 31)
- `ENG-058-T50` configurable documented REST response statuses plus default `500`: `OpenApi:BehaviorRest:DocumentedStatusCodes` now controls which HTTP status codes Cephalon's behavior-owned REST helpers publish into OpenAPI + Scalar, the default documented status set now includes `500`, route-group defaults no longer force `400`/`404` when the host narrows the list, and hosting coverage now locks both the default `500` visibility and a custom filtered status-code list — REST OpenAPI hosting tests 6/6 (Sprint 31)

Exit criteria:

- behaviors compose across multiple transports and patterns through a single dispatch model
- transport bindings stay additive through companion packages
- pattern execution strategies are selectable per behavior through configuration
- source generator catches mismatches at build time rather than runtime

## Phase 10: Non-relational provider baseline

Status: done

Goal: prove the companion-pack data provider pattern across all major non-relational store categories without changing `Cephalon.Engine` or `Cephalon.Abstractions`.

Delivered:

- 9 non-relational provider families shipped across Sprints 25–31:
  - MongoDB (document-store) — Sprint 25, 599/599 tests
  - Redis (key-value-store) — Sprint 26, 607/607 tests
  - Neo4j (graph-store) — Sprint 27, 615/615 tests
  - Cassandra (wide-column-store) — Sprint 28, 624/624 tests
  - ClickHouse (analytics-store) — Sprint 29, 632/632 tests
  - Elasticsearch (search-store) — Sprint 30, 640/640 tests
  - OpenSearch (search-store) — Sprint 30
  - Qdrant (vector-store) — Sprint 31, 648/648 tests
  - NATS (ledger-store) — Sprint 31
- each provider family delivers both `Cephalon.Data.{Provider}` and `Cephalon.EventSourcing.{Provider}` companion packages (18 packages total)
- full component-guide documentation for all 18 packages
- test assembly split into 4 focused assemblies (Infrastructure Phase 2): `Cephalon.Tests.Composition` (327), `Cephalon.Tests.Hosting` (200), `Cephalon.Tests.Tooling` (121)

Exit criteria:

- every major store category (document, key-value, graph, wide-column, analytics, search, vector, ledger) has a Cephalon-supported `IOutbox`/`IInbox`/`IEventStore` implementation
- no changes to `Cephalon.Engine` or `Cephalon.Abstractions` were required
- all companion packs follow the same module/registration/capability pattern established by `Cephalon.Data.EntityFramework`

## Cross-cutting follow-through: Database topology and durable audit history

Status: in progress

Goal: separate physical database role topology, migration targeting, and durable audit-history routing from the logical `Engine:Data` app-model slice so one Cephalon codebase can move between single-database, split read/write, dedicated outbox, and dedicated history layouts through configuration and additive companion packs.

Target: Sprint 31–33

Planned deliverables:

- `ENG-060` engine-owned `Engine:Databases` topology contract with named roles such as `Write`, `Read`, and `History`
- runtime introspection for active database roles and topology answers through a dedicated catalog plus `/engine/snapshot`
- `ENG-061` role-aware relational follow-through so `Cephalon.Data.EntityFramework` consumes database-role topology instead of inventing a separate physical-layout model
- migration targeting that references named roles, keeps startup apply explicit, and treats bundle/script-based deployment as the production path
- `ENG-062` durable audit-history follow-through that keeps `Cephalon.Audit` narrow while letting a first provider-backed store target a named database role
- `ENG-063` durable audit-history reader, retention, and operator-surface follow-through on top of the first provider-backed store

Current truth:

- the initial `ENG-060` baseline is now shipped: `Engine:Databases` projects into `EngineSettings`, `AppProfile.Databases`, `/engine/databases`, `/engine/app-model`, and `/engine/snapshot`
- `ENG-061` is now shipped: `Cephalon.Data.EntityFramework` consumes the engine-owned `write` and optional `read` roles directly, exposes role and migration metadata through the runtime surface, and can execute startup schema apply for those registered `DbContext` roles through a generic-host hosted service
- `ENG-062` is now shipped: `Cephalon.Audit.EntityFramework` consumes `Engine:Audit:History` plus the selected engine-owned database role named by `Engine:Audit:History:DatabaseRole`, publishes durable audit-store metadata, and proves the topology in the showcase sample with distinct write/read/history databases
- `ENG-063` is now shipped: durable audit history now also includes `Engine:Audit:History:Retention`, a host-agnostic `IAuditHistoryReader`, `/engine/audit-history`, and showcase-facing audit-history endpoints over the same durable store
- the remaining phase-10 work is now broader provider consumption, role references, richer topology/runtime metadata, dedicated outbox execution, bundle/script orchestration guidance, and replay/export follow-through for durable history

Exit criteria:

- a consumer app can keep one Cephalon codebase and move between shared-db and split-db layouts through config and additive pack wiring
- database topology, migration targeting, and durable audit-history state are all introspectable instead of hidden in host startup code
- provider packs stay additive because the engine owns the role and migration contract instead of one pack becoming the de facto source of truth
- optional convenience `DbContext` base classes, if they appear later, remain thin DX helpers rather than the primary engine contract

## Recommended implementation order

Updated priority order as of `April 7, 2026`:

1. start phase 7 with `ENG-033` cross-platform validation and shell parity so the shipped build, test, publish, and install flows stop assuming Windows-specific shell behavior
2. follow immediately with `ENG-034` first-run adoption and environment-doctor work so external teams have one clear install, validation, and runtime-smoke path
3. `ENG-035` is now complete, so Cephalon's package-manifest, trust, provenance, and runtime-introspection story is exercised outside this repository through the staged external package flow
4. `ENG-036` is now complete, so Docker Desktop / WSL users have a reproducible modular monolith sample deployment that preserves the shipped runtime, health, and OTLP collector path
5. `ENG-037` is now complete, so newly scaffolded apps carry the documented package-source bootstrap needed to restore, build, and follow the container path without repo-only package-feed knowledge
6. `ENG-038` is now complete, so newly scaffolded apps also carry a deterministic folder-publish profile and a validated published-output smoke path that runs outside the repo tree once the supported package source is seeded or repointed
7. `ENG-039` is now complete, so newly scaffolded apps also carry Linux `systemd` install assets and a validated WSL verification path that closes the self-hosted service-manager gap after publish
8. `ENG-040` is now complete, so newly scaffolded apps also carry Windows Service install assets and a validated install-preview path that closes the self-hosted Windows service-manager gap after publish
9. `ENG-041` is now complete, so newly scaffolded apps also carry IIS install assets and a validated ANCM/published-output preview path that closes the hosted Windows deployment gap after publish
10. `ENG-042` is now complete, so newly scaffolded apps also carry Azure App Service ZIP-deploy assets and a validated run-from-package preview path that closes the hosted Azure deployment gap after publish
11. `ENG-043` is now complete, so newly scaffolded apps also carry Azure Container Apps source-deploy assets and a validated Dockerfile plus Azure CLI preview path that closes the hosted Azure container deployment gap from the generated app root
12. `ENG-044` is now complete, so newly scaffolded apps also carry Kubernetes manifest/apply assets and a validated Dockerfile plus `kubectl kustomize` preview path that closes the platform-neutral cluster deployment gap from the generated app root
13. `ENG-045` is now complete, so newly scaffolded apps also carry provider-neutral container-image build/tag/push assets and a validated local-registry smoke path that closes the remaining image-publication gap between local Dockerfile validation and hosted container deployment targets
14. keep phase 6 in `later / Todo` until another explicit cloud or platform target becomes adoption-driven beyond the shipped self-hosted, Azure Monitor, AWS, GCP, Huawei Cloud, Alibaba Cloud, Oracle Cloud, Red Hat OpenShift, DigitalOcean, VMware Tanzu, Kubernetes, Cloudflare/custom-provider guidance, Grafana Cloud, and New Relic baseline
15. future solution-level expansion only when an explicit adoption scenario needs it
16. open phase 8 with `ENG-046`, `ENG-047`, and `ENG-048` so ids, structured config sections, and host-agnostic contracts freeze before package implementations or template defaults drift
17. follow with `ENG-049` and `ENG-050` so Cephalon proves a relational Entity Framework plus CQRS plus outbox plus eventing golden path before it claims broader provider breadth
18. then deliver `ENG-051`, `ENG-052`, and `ENG-053` so identity/authorization, multi-tenancy/audit, and CLI/scaffolding/template/sample follow-through land on the same frozen phase-8 contract
19. then deliver `ENG-055` and `ENG-056` so benchmarks, validation, docs, XML comments, and reference-doc alignment prove the phase-8 claims before the repo widens the public story
20. `ENG-054` Track 1 (non-relational providers) and `ENG-057` (event-sourcing follow-through) are now complete; keep hybrid-cloud, service-mesh, and serverless expansion as explicit later slices until an adopter needs them beyond the proven golden path
21. open phase 11 with resilience foundation (circuit breaker, retry/timeout/bulkhead, rate limiting) plus `onion-architecture` and `anti-corruption-layer` pattern descriptors so production microservice deployments have configuration-driven fault tolerance
22. follow with phase 12 for migration and advanced coordination (strangler fig, saga choreography, BFF pattern, feature flags, durable execution) so enterprise adoption and distributed coordination stories are complete
23. then phase 13 for next-generation patterns (cell-based architecture, data mesh, CDC) when explicit adoption scenarios justify the investment

## Phase 11: Resilience Foundation

Status: planned

Goal: add production-critical resilience infrastructure so consumer microservices can handle cascading failures, transient faults, and traffic spikes through configuration-driven policies.

Target: Sprint 36–37

Planned deliverables:

- `onion-architecture` pattern descriptor in `BuiltInPatterns.cs` for taxonomy completeness alongside Clean and Hexagonal
- `anti-corruption-layer` pattern descriptor in `BuiltInPatterns.cs` for explicit DDD integration boundary support
- circuit breaker abstraction (`ICircuitBreaker` with open/half-open/closed state machine) integrated into the behavior pipeline
- retry with exponential backoff and jitter, timeout enforcement, and bulkhead isolation policies through `Microsoft.Extensions.Resilience` (Polly v8)
- rate limiting middleware integration through `Microsoft.AspNetCore.RateLimiting` wired into the ASP.NET Core host adapter
- `Engine:Resilience` configuration section covering circuit-breaker, retry, timeout, bulkhead, and rate-limit policies
- capabilities: `resilience.circuit-breaker`, `resilience.retry`, `resilience.timeout`, `resilience.bulkhead`, `resilience.rate-limiting`

Exit criteria:

- a consumer app can configure per-behavior resilience policies through `Engine:Resilience` without writing custom middleware
- health probes and circuit breakers compose together to prevent cascading failures
- rate limiting can be configured per behavior or per transport

## Phase 12: Migration and Advanced Coordination

Status: planned

Goal: expand the distributed coordination story with choreography-based sagas, incremental migration support, progressive delivery, and durable execution foundations.

Target: Sprint 38–39

Planned deliverables:

- `strangler-fig` pattern descriptor and `IStranglerFigRouter` for incremental migration from legacy systems
- `backend-for-frontend` pattern descriptor for explicit per-client transport binding configuration
- saga choreography execution strategy (`ChoreographySagaExecutionStrategy`) for event-reaction-based coordination alongside the existing orchestration-based saga
- feature flags abstraction (`IFeatureToggle`) with per-behavior, per-module, and per-tenant evaluation
- durable execution foundations (`IDurableExecution<TState>`) on top of the existing process-manager and event-sourcing contracts

Exit criteria:

- a consumer app can migrate incrementally from a legacy system using the strangler fig router
- sagas can coordinate through events (choreography) in addition to state (orchestration)
- feature flags can gate behavior availability per tenant
- durable execution workflows survive process restarts through replay semantics

## Phase 13: Next-Generation Patterns

Status: planned

Goal: add differentiation-grade patterns that position CephalonEngine for the next generation of distributed application architecture.

Target: Sprint 40–41

Planned deliverables:

- cell-based architecture technology descriptor and `ICellBoundary` abstraction for blast-radius isolation and cell-to-cell routing
- data mesh `IDataProduct<T>` abstraction where modules own queryable data products surfaced through the runtime catalog
- change data capture `ICdcCapture` abstraction for automated database-change publication through the outbox without explicit staging

Exit criteria:

- modules can declare cell boundaries with explicit blast-radius isolation
- modules can expose queryable data products through the runtime catalog
- database changes can be captured and published through the outbox without explicit staging

## Decision guardrails

- keep host-specific APIs out of `Cephalon.Abstractions`
- keep blueprint, transport, and policy selection configuration-driven by default
- keep scaffolding, CLI, and package catalogs aligned with runtime contracts
- prefer benchmark coverage before optimizing or refactoring hot paths blindly
- do not start distributed orchestration before package loading and runtime policy are stable
- prefer one official adapter path per infrastructure category until the runtime-neutral contract is proven; for phase 8 messaging that path is `Wolverine`
- keep `MassTransit` in the tracked-later candidate set until the runtime-neutral eventing contract and the `Wolverine` first-class path are proven strongly enough to justify a second official adapter
- allow consumer-owned infrastructure choices such as `MediatR`, `LiteBus`, `NServiceBus`, and `SlimMessageBus`, but do not claim first-class runtime truth for them without an explicit Cephalon bridge or adapter
- keep one durable-messaging owner per flow; do not mix Cephalon-managed and third-party durable messaging semantics on the same path
