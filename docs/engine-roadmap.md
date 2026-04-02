# Cephalon Engine Roadmap

Editable roadmap diagram: `docs/cephalon-engine-roadmap.drawio`

Planning baseline in this document reflects the repository state as of `April 2, 2026`.

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
- a benchmark suite plus baseline guardrail validation for composition, runtime lifecycle, and scaffolding hot paths
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
- `Sprint 2`: exporter packaging is now part of the shipped phase-2 baseline, HTTP external API plus Postgres, RabbitMQ, and Redis/cache dependency-health packaging anchor the provider-specific follow-through, and the shared diagnostics/event-id catalog now anchors the structured diagnostics baseline
- `Sprint 3`: runtime-answers follow-through, package distribution and trust follow-through, and `ENG-013` planning readiness
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

Status: current focus

Goal: make Cephalon safe to operate in real environments.

Deliverables:

- deeper readiness and liveness semantics beyond the shipped baseline
- richer runtime failure, stop, and restart policies beyond the shipped baseline
- richer structured diagnostics and event IDs across packages
- dedicated exporter packages or OpenTelemetry integration layers beyond the shipped guidance baseline
- clearer operational answers to “what loaded, what started, what failed, and why?”
- benchmark-driven performance guardrails for hot engine paths

Current inventory:

- `docs/operational-hardening-gap-inventory.md` now records the shipped baseline versus the remaining phase-2 gaps so follow-through work stays grounded in the code that already exists
- that inventory now includes a shipped `Cephalon.Observability.OpenTelemetry` companion package plus shipped `Cephalon.Observability.HttpDependencies`, `Cephalon.Observability.PostgresDependencies`, `Cephalon.Observability.RabbitMqDependencies`, and `Cephalon.Observability.RedisDependencies` companion packages, together with a published runtime diagnostics catalog and runtime-story surface for the active engine and companion packages, which narrows the open phase-2 work to broader dependency-health coverage, deeper health semantics, and release-validation guidance

Exit criteria:

- operators can diagnose engine startup and module failures quickly
- host health semantics are predictable across ASP.NET Core and worker hosts
- performance regressions in composition/runtime/scaffolding are caught intentionally

## Phase 3: Extensibility and package loading

Status: later, but on the critical path to becoming a platform

Goal: let Cephalon load and validate independently shipped module packages.

Current baseline already in place:

- explicit package assembly paths can be declared through `Engine:Discovery:Packages`
- package manifests can be declared through `Engine:Discovery:Packages`
- package directories can be scanned through `Engine:Discovery:PackageDirectories`
- package metadata can be governed through `Engine:PackagePolicy`
- package-loaded modules flow through the same runtime/module contracts
- package load results are exposed through `/engine/packages` and manifest v2 metadata
- package trust and capability policy are exposed through `Engine:Trust` and `/engine/trust-policy`
- package publisher and signer provenance can be declared and evaluated through package manifests and trust allow-lists
- detached package signatures can be cryptographically verified against trusted public keys

Remaining work in this phase is the broader platform story around external distribution, multi-signer/certificate-chain verification, and richer package provenance beyond the shipped compatibility, integrity, detached-signature, publisher/signer metadata, checksum, and package-policy baseline.

Deliverables:

- package/plugin discovery model
- compatibility checks for module packages
- explicit load failure diagnostics
- version and dependency validation rules
- trust and policy hooks for loaded packages

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

## Recommended implementation order

Updated priority order as of `April 2, 2026`:

1. operational hardening follow-through: broader dependency-health packs, readiness/liveness semantics, and release-validation guidance now that the exporter path is shipped, HTTP plus Postgres plus RabbitMQ plus Redis coverage have landed, and both the structured diagnostics catalog and runtime-story surface are in place
2. package/plugin loading
3. package distribution, provenance, and richer trust follow-through beyond the current baseline
4. workflow and orchestration primitives
5. multi-service suite blueprints
6. broader release automation and package-publishing polish

## Decision guardrails

- keep host-specific APIs out of `Cephalon.Abstractions`
- keep blueprint, transport, and policy selection configuration-driven by default
- keep scaffolding, CLI, and package catalogs aligned with runtime contracts
- prefer benchmark coverage before optimizing or refactoring hot paths blindly
- do not start distributed orchestration before package loading and runtime policy are stable
