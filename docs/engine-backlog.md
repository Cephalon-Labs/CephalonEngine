# Cephalon Engine Backlog

Backlog status in this document reflects the repository state as of `April 2, 2026`.

## Completed foundation work

### ENG-000 App model and blueprint contract

Status: done
Estimate: 5

Delivered:

- blueprint/app-model contracts
- pattern and transport selection
- technology-profile selection for future-facing workloads
- scaffold plans attached to the app profile
- configuration-driven blueprint selection

Follow-up later:

- expand the built-in technology catalog only when a workload needs distinct validation, guidance, or scaffold conventions
- avoid turning every new technology trend into a new blueprint unless it changes project shape materially

### ENG-001 Module discovery from assemblies

Status: done
Estimate: 8

Delivered:

- assembly-based module discovery
- opt-in discovery filters
- duplicate module-id and module-type validation
- deterministic ordering after discovery

### ENG-002 Lifecycle hooks baseline

Status: done
Estimate: 5

Delivered:

- initialize/start/stop lifecycle hooks
- dependency-ordered execution and reverse-order shutdown
- runtime status tracking
- lifecycle test coverage

### ENG-003 Engine options and policy baseline

Status: done
Estimate: 3

Delivered:

- `EngineOptions`
- module and capability toggles
- configuration-driven policy inputs
- runtime options introspection

Follow-up later:

- failure/restart policy
- richer startup policy and feature gating

### ENG-004 Manifest v2

Status: done
Estimate: 5

Delivered:

- manifest schema version
- engine version
- module version, metadata, dependencies, and tags
- capability source-module mapping

Follow-up later:

- compatibility strategy for future manifest revisions

### ENG-006 Worker adapter baseline

Status: done
Estimate: 5

Delivered:

- generic-host worker adapter
- worker playground
- shared lifecycle behavior between HTTP and worker hosts

### ENG-007 ASP.NET Core contribution model baseline

Status: done
Estimate: 8

Delivered:

- host mapping conventions for engine endpoints
- protocol-separated transport contribution model
- OpenAPI + Scalar for REST surfaces
- adapter split for companion transport packages such as `JsonRpc` and `Grpc`

### ENG-008 Observability baseline

Status: done
Estimate: 5

Delivered:

- engine logs, metrics, and tracing conventions
- startup summaries
- tests around diagnostics behavior

Follow-up later:

- richer export-ready telemetry
- production operations guidance

### ENG-009 Blueprint-aware scaffolding and CLI baseline

Status: done
Estimate: 8

Delivered:

- blueprint-aware scaffold plans
- concrete scaffold generation
- CLI generation workflow
- package-catalog alignment for generated test infrastructure dependencies

### ENG-014 Protocol adapter packages baseline

Status: done
Estimate: 8

Delivered:

- transport catalog
- host-aware transport gating
- runnable `RestApi`, `JsonRpc`, `Grpc`, `ServerSentEvents`, and `WebSocket` paths
- gRPC unary and streaming coverage

### ENG-015 Benchmark suite baseline

Status: done
Estimate: 5

Delivered:

- BenchmarkDotNet project in the solution
- composition benchmarks
- runtime lifecycle benchmarks
- scaffolding benchmarks

### ENG-025 Technology companion packages baseline

Status: done
Estimate: 13

Delivered:

- `Cephalon.Agentics` companion package for `AgenticWorkloads`
- `Cephalon.Eventing` companion package for `EventDrivenIntegration`
- `Cephalon.Retrieval` companion package for `KnowledgeRetrieval`
- `Cephalon.Edge` companion package for `EdgeNativeDelivery`
- `ITechnologyServiceContributor` and `ITechnologyCapabilityContributor` activation pattern
- scaffold/package hints aligned with built-in technology profiles
- playground and test coverage for technology-aware runtime services and capabilities

Follow-up later:

- add additional packs such as eventing, edge, or orchestration only when they need shared runtime primitives
- define publishing/versioning guidance for technology packs outside the repository

## SDK hardening follow-through

Phase 1 SDK hardening is now substantially complete. Current execution focus has moved to phase 2 operational hardening follow-through on the roadmap and project board.

### ENG-005 Engine API and package surface hardening

Status: done
Estimate: 4

Delivered:

- public-surface audit across abstractions, engine, adapters, scaffolding, tooling, and companion packages
- regression tests locking the intended exported surface for the CLI, reference-doc tooling, host adapters, worker adapter, scaffolding package, and companion packs
- package-facing guidance tightened so compatibility expectations are explicit across package manifests, scaffold output, template starters, and CLI flows
- the public surface now behaves like a supported product contract instead of repo-internal plumbing

### ENG-026 GraphQL transport adapter

Status: done
Estimate: 5

Delivered:

- dedicated `Cephalon.AspNetCore.GraphQL` adapter package built on Hot Chocolate
- GraphQL transport selection aligned across runtime introspection, scaffolding, and host registration
- working `/graphql` endpoint with module-driven schema contributions on ASP.NET Core
- integration coverage and component docs for GraphQL hosting guidance

### ENG-027 DocFX XML-comment readiness beyond shipped packages

Status: done
Estimate: 8

Delivered:

- XML comments added across benchmark, sample, and reference-module public APIs that belong in the supported published docs set
- supported DocFX/reference-doc boundary documented explicitly for shipped packages, samples, benchmarks, and reference modules
- `tests/Cephalon.Tests` excluded from generated docs scope so test-only fixtures do not blur supported documentation input

Follow-up later:

- keep repo-wide XML-comment hygiene for test harnesses as a separate explicit choice instead of silently expanding published docs scope

### ENG-028 Repo-wide XML-comment hygiene for test harnesses

Status: later
Estimate: 6

Why:

- a fully repo-wide CS1591-clean build would still require a separate decision on whether public test fixtures should become internal, documented, or excluded by convention

Acceptance:

- decide whether public xUnit fixtures and shared test helpers should stay public or become internal where safe
- if repo-wide XML-comment enforcement beyond the published docs set becomes a goal, make the test-harness policy explicit and tooling-backed
- avoid letting test-only visibility choices blur the supported DocFX/reference-doc publishing boundary

## Current operational focus

The active planning wave now moves to phase 2 operational hardening:

- keep the completed gap inventory, shipped OpenTelemetry companion package, and published diagnostics catalog reflected accurately in docs and project tracking
- keep the shipped Elasticsearch, HTTP, Kafka, MongoDB, MQTT, MySQL, NATS, Postgres, RabbitMQ, Redis, and SQL Server dependency-health companions reflected accurately in docs and project tracking while broader provider coverage stays explicit
- keep the shipped Serilog provider companion package reflected accurately in docs and project tracking while cloud tracing/export integration remains an explicit later follow-through item
- keep the shipped ASP.NET Core request/response logging, bounded body capture, and trace/log correlation surfaces reflected accurately in docs and project tracking
- keep the shipped runtime-story surface plus the shipped failure-policy warmup, drain, and restart-backoff semantics reflected accurately in docs and project tracking
- keep the shipped operational release-validation guidance for health and telemetry-export conventions reflected accurately in docs and project tracking
- keep `docs/operational-hardening-gap-inventory.md` current as the source of truth for what phase-2 gaps are still genuinely open

### ENG-016 Blueprint sample suite

Status: done
Estimate: 8

Delivered:

- `samples/` now exists alongside `playground/`
- sample projects for `ModularMonolith`, `ModularVerticalSlice`, and `Microservice`
- the sample suite is wired into the main solution
- smoke hosting tests verify each sample boots and exposes its expected app model and endpoint shape

### ENG-017 `dotnet new` / template-pack support

Status: done
Estimate: 8

Delivered:

- `Cephalon.TemplatePack` project in the main solution
- installable `dotnet new` entries for `ModularMonolith`, `ModularVerticalSlice`, and `Microservice`
- package metadata and readme for the template pack
- pack validation and `dotnet new` smoke coverage in tests
- documentation for local pack/install/create flow

Follow-up later:

- richer template parameterization
- tighter convergence between template content and `Cephalon.Scaffolding`
- upgrade/version guidance once the package distribution story is formalized

### ENG-018 Module SDK and authoring path

Status: done
Estimate: 8

Delivered:

- `cephalon-module` and `cephalon-rest-module` starters in `Cephalon.TemplatePack`
- a reference module package at `samples/Cephalon.ReferenceModule.Operations`
- integration coverage for discovery, lifecycle, capability registration, localization, and REST contribution
- `docs/module-authoring.md` for the recommended package workflow

Follow-up later:

- richer module-template parameterization
- additional transport-specific authoring starters beyond REST
- packaging/version guidance for publishing reference modules externally

## Near-term hardening work

### ENG-019 Runtime failure and restart policy

Status: done
Estimate: 13

Delivered:

- `Engine:FailurePolicy` configuration with startup and stop behaviors
- runtime failure context in `RuntimeStatusSnapshot`
- runtime restart guards and explicit `RestartAsync(...)`
- host introspection through `/engine/failure-policy` and `/engine/status`
- test coverage for fail-fast startup, capture-only startup, best-effort stop, and host startup behavior

Follow-up later:

- richer retry/backoff policies
- failure-event integration with observability/export pipelines
- more operator-facing automation over restart workflows

### ENG-020 Operational health and telemetry exports

Status: done
Estimate: 13

Delivered:

- `/health`, `/health/live`, and `/health/ready` with runtime-backed JSON responses
- `RuntimeHealthEvaluator` shared across ASP.NET Core, worker hosts, and observability
- `IDependencyHealthContributor` baseline for host-agnostic dependency health reporting
- `/engine/dependencies` for dependency-level runtime introspection
- `/engine/diagnostics` with meter, activity source, counter names, and live health reports
- runtime failure and restart counters for telemetry baselines
- `Engine:Observability:Telemetry` config contract plus startup log guidance
- `Engine:Observability:HttpLogging` plus opt-in ASP.NET Core request/response logging with bounded body capture and request/trace correlation

Follow-up later:

- provider-specific dependency health packs on top of the shipped OpenTelemetry companion package

### ENG-021 Benchmark guardrails in validation flow

Status: done
Estimate: 5

Delivered:

- repository guardrail catalog for the shipped benchmark scenarios
- CSV reader and validator in `Cephalon.Benchmarks`
- CLI validation command for the latest BenchmarkDotNet reports
- test coverage for benchmark report parsing and guardrail evaluation
- benchmark docs updated with the validation flow
- composition and runtime benchmarks now prepare configured builders, runtimes, and service providers outside the measured loop so guardrails track `Build()` and lifecycle transition costs directly
- composition baseline thresholds refreshed to match the prepared-scenario hot path shipped in the release-validation flow

Follow-up later:

- expand guardrails as new hot paths become important
- revisit baseline thresholds when benchmark scenarios evolve materially

### ENG-024 Explicit package assembly loading baseline

Status: done
Estimate: 13

Delivered:

- `Engine:Discovery:Packages` configuration contract for explicit module assembly paths
- `EngineBuilder.AddPackageAssembly(...)` and package-reference builder support
- assembly-path package loading with a dedicated load context and explicit failure diagnostics
- package-manifest loading through `Engine:Discovery:Packages:ManifestPath` and `EngineBuilder.AddPackageManifest(...)`
- package-directory discovery through `Engine:Discovery:PackageDirectories` and `EngineBuilder.AddPackageDirectory(...)`
- package compatibility and integrity metadata through `cephalon.package.json`
- `Engine:PackagePolicy` baseline for requiring manifest-driven package loads and stricter package metadata
- publisher and signer provenance metadata plus trust allow-lists for package governance
- detached-signature verification against trusted public keys
- manifest/runtime introspection through package metadata and `/engine/packages`
- integration coverage using `Cephalon.ReferenceModule.Operations` as a real package-loaded module

Follow-up later:

- deeper multi-signer or certificate-chain verification and external distribution hooks beyond the current detached-signature baseline
- versioned package distribution guidance outside the repository

### ENG-023 GitHub Actions release-validation baseline

Status: done
Estimate: 5

Delivered:

- `.github/workflows/release-validation.yml` for `push`, `pull_request`, and manual runs
- GitHub Actions execution on `windows-latest` using the SDK pinned in `global.json`
- CI wired to the repo-native `scripts/validate-release.ps1` flow instead of duplicating build/test/benchmark logic in YAML
- benchmark result artifact upload for CI inspection

Follow-up later:

- split faster PR validation from deeper release/publish workflows if the repo needs it
- add package-publishing and release-version automation when distribution is formalized

## Platform expansion work

### ENG-011 Package and plugin loading

Status: later
Estimate: 19

Why:

- Cephalon becomes a platform when modules are distributable independently

Acceptance:

- define package discovery inputs
- verify compatibility and dependency requirements
- make load failures explicit and diagnosable
- support policy and trust hooks

### ENG-012 Capability permissions and trust policy

Status: done
Estimate: 8

Delivered:

- `Engine:Trust` configuration contract with package trust, assembly trust, and per-capability access rules
- capability decisions surfaced through `CapabilityPolicyEvaluator` and `/engine/trust-policy`
- package manifests and module manifests now carry trust status
- REST request-time enforcement through `RequireCapability(...)`
- test coverage for trusted package loading, denied capabilities, and HTTP boundary enforcement

Follow-up later:

- richer scopes beyond per-capability allow, trusted-only, and deny
- trust hooks beyond explicit assembly-path packages
- policy and trust integration for future non-REST runtime boundaries

### ENG-013 Workflow and orchestration primitives

Status: later
Estimate: 19

Why:

- this is the bridge from framework to execution platform

Acceptance:

- define a first execution graph or workflow contract
- integrate it with lifecycle and observability
- keep the design additive to the existing module model

### ENG-022 `MicroserviceSuite` blueprint

Status: later
Estimate: 10

Why:

- the current shipped blueprints focus on individual apps or services, not coordinated suites

Acceptance:

- define suite-level scaffold shape
- keep suite blueprints composed from existing app-level contracts
- add reference samples for multi-service Cephalon solutions

## Sprint history and next 3 sprints

Historical sprint buckets below are retrospective planning groups used to backfill iteration and estimate metadata for delivered work.

### Foundation Sprint 1

- ENG-000 App model and blueprint contract
- ENG-001 Module discovery from assemblies
- ENG-002 Lifecycle hooks baseline
- ENG-003 Engine options and policy baseline
- ENG-004 Manifest v2

### Foundation Sprint 2

- ENG-006 Worker adapter baseline
- ENG-007 ASP.NET Core contribution model baseline
- ENG-008 Observability baseline
- ENG-009 Blueprint-aware scaffolding and CLI baseline

### Foundation Sprint 3

- ENG-014 Protocol adapter packages baseline
- ENG-015 Benchmark suite baseline
- ENG-025 Technology companion packages baseline

### Adoption Sprint 0

- ENG-016 Blueprint sample suite
- ENG-017 `dotnet new` / template-pack support
- ENG-018 Module SDK and authoring path

### Operational Sprint 0

- ENG-019 Runtime failure and restart policy
- ENG-020 Operational health and telemetry exports
- ENG-021 Benchmark guardrails in validation flow
- ENG-024 Explicit package assembly loading baseline
- ENG-023 GitHub Actions release-validation baseline

### Platform Sprint 0

- ENG-012 Capability permissions and trust policy

### Sprint 1

- ENG-005 Engine API and package surface hardening
- ENG-026 GraphQL transport adapter
- ENG-027 DocFX XML-comment readiness beyond shipped packages

### Sprint 2

- ENG-011 Package and plugin loading
- operational hardening follow-through after the shipped health, telemetry, and CI baselines
- shipped OpenTelemetry companion packaging plus Elasticsearch cluster health plus HTTP external API, Kafka broker metadata, MongoDB document database, MQTT broker, MySQL database, NATS broker, Postgres database, RabbitMQ broker, Redis/cache, and SQL Server dependency-health companions, together with the shared diagnostics/event-id catalog for active packages, opt-in ASP.NET Core request/response logging with trace correlation, and explicit release-validation guidance for health/export conventions

### Sprint 3

- ENG-013 Workflow and orchestration primitives
- package distribution and trust follow-through beyond the current baseline

### Later / not scheduled yet

- ENG-022 `MicroserviceSuite` blueprint
- ENG-028 repo-wide XML-comment hygiene for test harnesses
