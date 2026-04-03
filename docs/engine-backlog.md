# Cephalon Engine Backlog

Backlog status in this document reflects the repository state as of `April 3, 2026`.

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

Phase 1 SDK hardening, phase 2 operational hardening, phase 3 extensibility/package loading, and phase 4 execution/orchestration are now substantially complete on their shipped baselines. Phase 5 solution-level platform work is now also substantially complete on its shipped baseline, with the suite-scaffold contract baseline under `#79`, built-in `MicroserviceSuite` composition baseline under `#80`, multi-service suite sample baseline under `#81`, and shared governance plus additive gateway/control-plane guidance baseline under `#82`, while phase 6 keeps the shipped self-hosted OTLP slice, Azure Monitor first-vendor slice, AWS second-vendor slice, GCP third-vendor slice, Huawei Cloud fourth-vendor slice, Alibaba Cloud fifth-vendor slice, Red Hat OpenShift platform-first slice, DigitalOcean collector/defaults slice under `#114`, VMware Tanzu proxy/defaults slice under `#118`, and downstream Cloudflare/custom-provider authoring guidance slice under `#120`.

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

### ENG-028 Repo-wide XML-comment hygiene for test harnesses

Status: done
Estimate: 6

Delivered:

- `tests/Cephalon.Tests` now keeps shared test-harness types internal where safe while leaving only framework-required xUnit classes and a small reflective transport-contract exception public
- tooling coverage now locks that policy by asserting the test assembly exports only xUnit test classes plus the explicit allow-listed transport contract exception and that XML-document generation stays disabled for the test project
- reference-doc and compatibility guidance now state the test-harness policy explicitly so the supported DocFX/reference-doc boundary stays limited to shipped packages and intentionally promoted samples

## Current operational focus

Phase 2 operational hardening is now substantially complete:

- keep the completed gap inventory, shipped OpenTelemetry companion package, shipped Serilog companion package, and published diagnostics catalog reflected accurately in docs and project tracking
- keep the shipped Cassandra, ClickHouse, Consul, Elasticsearch, HTTP, Kafka, Memcached, MongoDB, MQTT, MySQL, NATS, Neo4j, OpenSearch, Oracle, Postgres, RabbitMQ, Redis, and SQL Server dependency-health companion baseline reflected accurately in docs and project tracking, with any additional provider packs treated as future adoption-driven expansion work
- keep the shipped ASP.NET Core request/response logging, bounded body capture, trace/log correlation, runtime-story, and failure-policy warmup/drain/restart-backoff surfaces reflected accurately in docs and project tracking
- keep the shipped operational release-validation guidance for health and telemetry-export conventions reflected accurately in docs and project tracking
- keep self-hosted plus cloud-vendor tracing/export follow-through tracked under `ENG-029` instead of leaving it as an implied phase-2 blocker
- keep `docs/operational-hardening-gap-inventory.md` current as the source of truth for what phase-2 gaps were closed versus what moved into later phases

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
- default sensitive-value redaction across query-string, JSON, form, and header-style plain-text HTTP logging payloads

Follow-up later:

- adoption-driven provider-specific dependency health packs beyond the shipped Cassandra, ClickHouse, Consul, Elasticsearch, HTTP, Kafka, Memcached, MongoDB, MQTT, MySQL, NATS, Neo4j, OpenSearch, Oracle, Postgres, RabbitMQ, Redis, and SQL Server baseline

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
- ASP.NET Core request logging now has a shipped guardrail scenario that covers correlated request/response body capture over the public host surface
- the benchmark catalog now also covers strict trust-policy composition plus bounded-truncation and concurrent ASP.NET Core request-logging paths

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
- detached-signature verification against trusted public keys and trusted signing certificate chains
- manifest/runtime introspection through package metadata and `/engine/packages`
- integration coverage using `Cephalon.ReferenceModule.Operations` as a real package-loaded module

Follow-up later:

- external distribution hooks and broader provenance attestations beyond the current signature-verification baseline
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

Status: done
Estimate: 5

Why:

- Cephalon becomes a platform when modules are distributable independently

Delivered:

- package discovery inputs through `Engine:Discovery:Packages`, `Engine:Discovery:PackageDirectories`, and the package builder APIs
- explicit package load failures for missing manifests, duplicate registrations, integrity mismatches, and dependency-registration gaps
- manifest-declared compatibility, target-framework, version, and package-dependency validation through `cephalon.package.json`
- package policy, detached-signature verification through trusted public keys or trusted signing certificate chains, publisher/signer provenance, and trust hooks surfaced through `Engine:PackagePolicy`, `Engine:Trust`, and `/engine/packages`
- external package distribution metadata and provenance metadata surfaced through `distribution`, `provenance`, and `/engine/packages`
- authoring guidance for externally distributed packages, including release-channel, package URI, source revision, build URI, and provenance statement hints

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

Status: done
Estimate: 5

Why:

- this is the bridge from framework to execution platform

Delivered:

- a first execution-graph contract through `IExecutionGraphContributor`, `ExecutionGraphDescriptor`, and `IExecutionRuntimeCatalog`
- a first hosted/background execution contract through `IHostedExecutionContributor`, `HostedExecutionDescriptor`, and `IHostedExecutionRuntimeCatalog`
- additive runtime introspection through `/engine/execution-graphs` and `/engine/snapshot`
- additive hosted/background introspection through `/engine/hosted-executions` and `/engine/snapshot`
- build-time validation for graph ids, nodes, edges, referenced modules, and referenced capability keys
- build-time validation for hosted-execution ids, source modules, and referenced execution graphs
- execution-graph lifecycle state through `/engine/runtime-story` and `/engine/snapshot`, including operator-visible load, activate, and deactivate transitions
- hosted/background execution lifecycle state through `/engine/runtime-story` and `/engine/snapshot`, including operator-visible load, activate, and deactivate transitions
- runtime diagnostics coverage for execution-graph and hosted/background lifecycle transitions through `cephalon.execution-graphs.transitions`, `cephalon.hosted-executions.transitions`, and the `Cephalon.Engine` event-id catalog
- agentic tool descriptors can now link back to capability keys, execution graphs, and hosted executions through the existing `Cephalon.Agentics` contract
- `/engine/technology-surfaces` and `/engine/snapshot` now project those linked AI/orchestration surfaces with live runtime-story state
- invalid agent-tool references to unknown capability keys, execution graphs, or hosted executions now fail when the agentic tool catalog is resolved
- module-author guidance for publishing workflow and execution-graph descriptors without bypassing the existing module/capability model
- descriptive hosted/background execution conventions that stay on top of the existing module and Generic Host model instead of introducing a separate engine-owned runner abstraction

### ENG-022 `MicroserviceSuite` blueprint

Status: done
Estimate: 2

Why:

- the current shipped blueprints focus on individual apps or services, not coordinated suites

Delivered:

- public suite-scaffold contracts through `SuiteScaffoldPlan`, `SuiteScaffoldService`, and `ScaffoldScopes.Suite`
- explicit separation between shared suite projects/folders and per-service slots so future multi-service blueprints do not overload the current single-app `ScaffoldPlan`
- validation that suite services can only depend on declared shared projects or other declared service slots
- a built-in `MicroserviceSuite` blueprint through `SuiteBlueprint` and `BuiltInSuiteBlueprints`
- suite shared-foundation and repeatable service-slot defaults composed from the shipped `Microservice` scaffold contract instead of redefining host, contracts, and module project shapes a second time
- a reference `Cephalon.Sample.MicroserviceSuite` sample with a shared foundation project plus separate catalog and orders microservice hosts
- smoke coverage proving both suite services boot, stay on the shipped `Microservice` blueprint, and surface shared suite conventions through their runtime endpoints
- a shared `Cephalon.Sample.MicroserviceSuite.Governance` package that keeps suite-level governance guidance outside the engine core and outside any one service host
- additive gateway and control-plane guidance that stays sample-level and consumes the existing `/engine/*` runtime surfaces instead of inventing a new engine-owned coordinator

## Current cloud and platform integration work

### ENG-029 Cloud-targeted observability companion integrations

Status: later
Estimate: 184

Why:

- the self-hosted OTLP collector/runtime-default slice plus the Azure Monitor, AWS, GCP, Huawei Cloud, Alibaba Cloud, Red Hat OpenShift, DigitalOcean, and VMware Tanzu slices are now shipped, and `#120` has now shipped downstream Cloudflare and custom-provider authoring guidance instead of a misleading first-party Cloudflare exporter package
- current Cloudflare Workers observability docs center Worker-native traces and logs plus exporting OpenTelemetry-compliant traces and logs from Workers to third-party OTLP destinations, with metrics export still unsupported, so a generic Cephalon host-side Cloudflare sink would over-claim the current platform story
- this work should stay in companion packages, preserve the shared `ILogger` pipeline plus the cloud-neutral OTLP baseline, and leave room for downstream developer-authored provider packages

Acceptance:

- keep the shipped self-hosted deployment defaults explicit and reusable instead of burying them inside vendor-specific companion packs
- keep the shipped Azure Monitor, AWS, GCP, Huawei Cloud, Alibaba Cloud, Red Hat OpenShift, and DigitalOcean slices explicit on top of the shared OpenTelemetry baseline
- keep the shipped Red Hat OpenShift and DigitalOcean companion follow-through explicit instead of rolling them back into one ambiguous remaining-platform scope
- keep the shipped DigitalOcean collector-first follow-through explicit instead of over-claiming a managed DigitalOcean OTLP exporter surface that the current platform docs do not promise
- keep the shipped VMware Tanzu proxy-first follow-through explicit on top of the shared OTLP baseline instead of reopening Cloudflare or pretending the current Tanzu docs describe one generic vendor-direct OTLP exporter path
- keep the shipped `#120` scope centered on downstream Cloudflare and custom-provider companion authoring guidance until Cloudflare documents a host-side ingestion story that fits Cephalon's .NET runtime model
- keep vendor/platform-specific exporter wiring, auth, resource attributes, and hosted defaults outside `Cephalon.Engine` and `Cephalon.Abstractions`
- keep the shared `ILogger` pipeline and existing `Cephalon.Observability.OpenTelemetry` baseline intact
- add docs, validation, and planning sync for the supported targets plus the downstream companion-package authoring path, including Cloudflare-oriented guidance that stays honest about the current Worker-native export model
- avoid shipping a first-party `Cephalon.Observability.Cloudflare` package unless Cloudflare later exposes a documented generic OTLP ingestion story for external hosts
- avoid starting implementation on any new vendor/platform target without first narrowing it explicitly

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
- ENG-029 Huawei Cloud observability exporter wiring and hosted Huawei Cloud defaults on top of the OTLP baseline
- ENG-029 Alibaba Cloud observability exporter wiring and hosted Alibaba Cloud defaults on top of the OTLP baseline
- ENG-029 Red Hat OpenShift collector wiring and hosted OpenShift defaults on top of the OTLP baseline
- ENG-029 DigitalOcean collector wiring and hosted DigitalOcean defaults on top of the OTLP baseline
- ENG-029 VMware Tanzu proxy handoff and hosted Tanzu defaults on top of the OTLP baseline

### Sprint 2

- operational hardening follow-through after the shipped health, telemetry, and CI baselines
- shipped OpenTelemetry companion packaging plus Cassandra contact-point health plus ClickHouse analytics health plus Consul control-plane health plus Elasticsearch cluster health plus HTTP external API, Kafka broker metadata, Memcached cache, MongoDB document database, MQTT broker, MySQL database, NATS broker, Neo4j graph database, OpenSearch cluster health, Oracle database, Postgres database, RabbitMQ broker, Redis/cache, and SQL Server dependency-health companions, together with the shared diagnostics/event-id catalog for active packages, opt-in ASP.NET Core request/response logging with trace correlation, and explicit release-validation guidance for health/export conventions

### Sprint 3

- shipped first execution-graph contract baseline plus hosted-execution follow-through under `ENG-013`
- shipped package distribution and provenance follow-through beyond the original package-loading baseline
- shipped repo-wide XML-comment hygiene for test harnesses through explicit xUnit visibility rules plus tooling-backed guards under `ENG-028`
- ENG-029 self-hosted OTLP collector/runtime-default follow-through plus the shipped Azure Monitor, AWS, GCP, Huawei Cloud, Alibaba Cloud, Red Hat OpenShift, DigitalOcean, VMware Tanzu, and Cloudflare/downstream provider authoring guidance slices
