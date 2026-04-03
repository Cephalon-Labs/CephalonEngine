# Cephalon Engine Roadmap

Editable roadmap diagram: `docs/cephalon-engine-roadmap.drawio`

Planning baseline in this document reflects the repository state as of `April 3, 2026`.

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
- `Sprint 3`: runtime-answers follow-through, the shipped package distribution/provenance and signer-verification follow-through under `ENG-011`, `ENG-013` planning readiness, the shipped `ENG-029` self-hosted OTLP follow-through slice, the shipped Azure Monitor first-vendor slice, the shipped AWS second-vendor slice, the shipped GCP third-vendor slice, the shipped DigitalOcean collector/defaults slice, the shipped VMware Tanzu proxy/defaults slice, and the shipped downstream Cloudflare/custom-provider authoring slice under `#120`
- `Later / not scheduled yet`: `ENG-022` and future solution-level expansion work

## Planning principles

- prefer stabilizing the shipped surface over inventing new layers too early
- keep the engine configuration-driven and host-agnostic by default
- keep future-facing technology choices additive through explicit technology profiles instead of blueprint explosion
- treat scaffolding, CLI, and benchmark coverage as part of the engine product, not side tools
- make every new runtime feature observable, testable, and benchmarkable
- delay distributed orchestration until package loading, lifecycle, and policy are stronger

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

- the supported phase-1 adoption baseline is now shipped across public-surface hardening, GraphQL transport delivery, compatibility guidance, and DocFX-ready XML comments
- `ENG-028` remains an intentional later hygiene item instead of a blocker for phase-1 exit

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
- the remaining cloud-vendor tracing/export follow-through has been re-scoped into phase 6 cloud and platform integrations because the expanded self-hosted plus AWS plus Azure plus GCP plus Huawei Cloud plus Alibaba Cloud plus DigitalOcean plus Red Hat OpenShift plus VMware Tanzu target list, together with the downstream Cloudflare/custom-provider guidance path, is broader than the shipped phase-2 operational baseline

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

Status: intentionally deferred until phases 1 to 3 are stronger

Goal: expand from a composition engine into a richer execution platform.

Deliverables:

- engine-level background/hosted execution conventions
- internal engine events for activation and runtime transitions
- workflow or execution-graph primitives
- richer multi-module coordination patterns
- AI/orchestration integration points built on existing contracts

Exit criteria:

- orchestration features build on the same runtime model instead of bypassing it
- long-running engine behavior is observable and policy-driven

## Phase 5: Solution-level platform

Status: future

Goal: support higher-level solution shapes, not only individual Cephalon apps.

Deliverables:

- `MicroserviceSuite` blueprint
- solution-level samples for multiple Cephalon services
- shared governance/convention packages
- optional gateway or control-plane guidance later

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
- self-hosted collector and runtime defaults plus Azure Monitor, AWS, GCP, Huawei Cloud, Alibaba Cloud, Red Hat OpenShift, DigitalOcean, and VMware Tanzu are now shipped as the first slices on top of the cloud-neutral OTLP baseline, and `#120` has now shipped downstream Cloudflare/custom-provider authoring guidance because current Cloudflare docs center Worker-native telemetry export to third-party OTLP destinations rather than a generic external-host sink

Deliverables:

- self-hosted observability companion follow-through for OTLP-collector-managed deployments and host-managed runtime defaults is now shipped
- Azure Monitor companion follow-through as the first explicit cloud-specific slice on top of the shared OpenTelemetry baseline is now shipped
- AWS companion follow-through as the second explicit cloud-specific slice on top of the shared OpenTelemetry baseline is now shipped
- GCP companion follow-through is now shipped as the third explicit cloud-specific slice on top of the shared OpenTelemetry baseline
- Huawei Cloud companion follow-through is now shipped as the fourth explicit cloud-specific slice on top of the shared OpenTelemetry baseline
- Alibaba Cloud companion follow-through is now shipped as the fifth explicit cloud-specific slice on top of the shared OpenTelemetry baseline
- Red Hat OpenShift companion follow-through is now shipped as the latest platform-first slice on top of the shared OpenTelemetry baseline
- DigitalOcean companion follow-through is now shipped as the latest collector-first slice on top of the shared OpenTelemetry baseline, centered on runtime defaults and collector handoff instead of an over-claimed managed OTLP exporter path
- VMware Tanzu companion follow-through is now shipped as the latest proxy-first slice on top of the shared OpenTelemetry baseline, centered on Wavefront proxy handoff and hosted Tanzu defaults instead of a generic vendor-direct OTLP exporter claim
- downstream Cloudflare and custom-provider companion authoring guidance is now shipped under `#120`, keeping the remaining Cloudflare follow-through honest about the current Worker-native export model instead of promising a generic first-party host-side sink
- exporter wiring, auth, resource-attribute conventions, and hosted-runtime defaults that stay inside companion packages instead of `Cephalon.Engine`
- documentation, validation, and planning guidance that make the supported targets, deployment assumptions, and downstream companion-package authoring path explicit
- a clear package split whenever different clouds or platforms need distinct companion packs instead of one overloaded abstraction

Exit criteria:

- self-hosted collector and runtime defaults can be enabled on top of the shipped OTLP baseline without modifying `Cephalon.Engine` or `Cephalon.Abstractions`
- supported cloud and platform integrations can be enabled without modifying `Cephalon.Engine` or `Cephalon.Abstractions`
- downstream developer-authored provider packages can reuse the shared telemetry contract without modifying `Cephalon.Engine` or `Cephalon.Abstractions`
- the shared `ILogger` pipeline and cloud-neutral OTLP baseline remain intact
- docs, validation flows, and planning metadata make the supported targets explicit

## Recommended implementation order

Updated priority order as of `April 3, 2026`:

1. workflow and orchestration primitives
2. multi-service suite blueprints
3. cloud and platform integrations, with self-hosted plus Azure Monitor plus AWS plus GCP plus Huawei Cloud plus Alibaba Cloud plus Red Hat OpenShift plus DigitalOcean plus VMware Tanzu shipped, and the downstream Cloudflare/custom-provider guidance slice shipped under `#120` while future first-party additions stay explicit and adoption-driven
4. broader release automation and package-publishing polish

## Decision guardrails

- keep host-specific APIs out of `Cephalon.Abstractions`
- keep blueprint, transport, and policy selection configuration-driven by default
- keep scaffolding, CLI, and package catalogs aligned with runtime contracts
- prefer benchmark coverage before optimizing or refactoring hot paths blindly
- do not start distributed orchestration before package loading and runtime policy are stable
