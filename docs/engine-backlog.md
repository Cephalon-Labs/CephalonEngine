# Cephalon Engine Backlog

Backlog status in this document reflects the repository state as of `April 1, 2026`.

## Completed foundation work

### ENG-000 App model and blueprint contract

Status: done

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

Delivered:

- assembly-based module discovery
- opt-in discovery filters
- duplicate module-id and module-type validation
- deterministic ordering after discovery

### ENG-002 Lifecycle hooks baseline

Status: done

Delivered:

- initialize/start/stop lifecycle hooks
- dependency-ordered execution and reverse-order shutdown
- runtime status tracking
- lifecycle test coverage

### ENG-003 Engine options and policy baseline

Status: done

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

Delivered:

- manifest schema version
- engine version
- module version, metadata, dependencies, and tags
- capability source-module mapping

Follow-up later:

- compatibility strategy for future manifest revisions

### ENG-006 Worker adapter baseline

Status: done

Delivered:

- generic-host worker adapter
- worker playground
- shared lifecycle behavior between HTTP and worker hosts

### ENG-007 ASP.NET Core contribution model baseline

Status: done

Delivered:

- host mapping conventions for engine endpoints
- protocol-separated transport contribution model
- OpenAPI + Scalar for REST surfaces
- adapter split for `JsonRpc` and `Grpc`

### ENG-008 Observability baseline

Status: done

Delivered:

- engine logs, metrics, and tracing conventions
- startup summaries
- tests around diagnostics behavior

Follow-up later:

- richer export-ready telemetry
- production operations guidance

### ENG-009 Blueprint-aware scaffolding and CLI baseline

Status: done

Delivered:

- blueprint-aware scaffold plans
- concrete scaffold generation
- CLI generation workflow
- package-catalog alignment for generated test infrastructure dependencies

### ENG-014 Protocol adapter packages baseline

Status: done

Delivered:

- transport catalog
- host-aware transport gating
- runnable `RestApi`, `JsonRpc`, `Grpc`, `ServerSentEvents`, and `WebSocket` paths
- gRPC unary and streaming coverage

### ENG-015 Benchmark suite baseline

Status: done

Delivered:

- BenchmarkDotNet project in the solution
- composition benchmarks
- runtime lifecycle benchmarks
- scaffolding benchmarks

### ENG-025 Technology companion packages baseline

Status: done

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

## Current priority work

### ENG-005 Engine API and package surface hardening

Status: next

Why:

- the public surface is becoming real product surface, not just repo-internal code

Acceptance:

- review extension points across abstractions, engine, adapters, scaffolding, and CLI
- reduce incidental public API where possible
- improve XML docs and package-facing guidance
- make compatibility expectations explicit

### ENG-016 Blueprint sample suite

Status: done

Delivered:

- `samples/` now exists alongside `playground/`
- sample projects for `ModularMonolith`, `ModularVerticalSlice`, and `Microservice`
- the sample suite is wired into the main solution
- smoke hosting tests verify each sample boots and exposes its expected app model and endpoint shape

### ENG-017 `dotnet new` / template-pack support

Status: done

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

Delivered:

- `/health`, `/health/live`, and `/health/ready` with runtime-backed JSON responses
- `RuntimeHealthEvaluator` shared across ASP.NET Core, worker hosts, and observability
- `IDependencyHealthContributor` baseline for host-agnostic dependency health reporting
- `/engine/dependencies` for dependency-level runtime introspection
- `/engine/diagnostics` with meter, activity source, counter names, and live health reports
- runtime failure and restart counters for telemetry baselines
- `Engine:Observability:Telemetry` config contract plus startup log guidance

Follow-up later:

- dedicated exporter packages or OpenTelemetry companion integration
- richer provider-specific dependency health packs beyond the baseline contributor contract
- release-validation guidance for health and export conventions

### ENG-021 Benchmark guardrails in validation flow

Status: done

Delivered:

- repository guardrail catalog for the shipped benchmark scenarios
- CSV reader and validator in `Cephalon.Benchmarks`
- CLI validation command for the latest BenchmarkDotNet reports
- test coverage for benchmark report parsing and guardrail evaluation
- benchmark docs updated with the validation flow

Follow-up later:

- expand guardrails as new hot paths become important
- revisit baseline thresholds when benchmark scenarios evolve materially

### ENG-024 Explicit package assembly loading baseline

Status: done

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

Why:

- Cephalon becomes a platform when modules are distributable independently

Acceptance:

- define package discovery inputs
- verify compatibility and dependency requirements
- make load failures explicit and diagnosable
- support policy and trust hooks

### ENG-012 Capability permissions and trust policy

Status: done

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

Why:

- this is the bridge from framework to execution platform

Acceptance:

- define a first execution graph or workflow contract
- integrate it with lifecycle and observability
- keep the design additive to the existing module model

### ENG-022 `MicroserviceSuite` blueprint

Status: later

Why:

- the current shipped blueprints focus on individual apps or services, not coordinated suites

Acceptance:

- define suite-level scaffold shape
- keep suite blueprints composed from existing app-level contracts
- add reference samples for multi-service Cephalon solutions

## Recommended next 3 sprints

### Sprint 1

- ENG-005 Engine API and package surface hardening
- operational hardening follow-through after the shipped health, telemetry, and CI baselines

### Sprint 2

- ENG-011 Package and plugin loading
- exporter packaging and dependency-specific health follow-through

### Sprint 3

- ENG-013 Workflow and orchestration primitives
- package distribution and trust follow-through beyond the current baseline
