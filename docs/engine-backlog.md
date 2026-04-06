# Cephalon Engine Backlog

Backlog status in this document reflects the repository state as of `April 5, 2026`.

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

Phase 1 SDK hardening, phase 2 operational hardening, phase 3 extensibility/package loading, and phase 4 execution/orchestration are now substantially complete on their shipped baselines. Phase 5 solution-level platform work is now also substantially complete on its shipped baseline, with the suite-scaffold contract baseline under `#79`, built-in `MicroserviceSuite` composition baseline under `#80`, multi-service suite sample baseline under `#81`, and shared governance plus additive gateway/control-plane guidance baseline under `#82`, while phase 6 keeps the shipped self-hosted OTLP slice, Azure Monitor first-vendor slice, AWS second-vendor slice, GCP third-vendor slice, Huawei Cloud fourth-vendor slice, Alibaba Cloud fifth-vendor slice, Red Hat OpenShift platform-first slice, DigitalOcean collector/defaults slice under `#114`, VMware Tanzu proxy/defaults slice under `#118`, downstream Cloudflare/custom-provider authoring guidance slice under `#120`, platform-neutral Kubernetes collector/defaults slice under `#124`, Grafana Cloud OTLP/header slice under `#126`, and New Relic native OTLP/api-key slice under `#127`, with any additional provider packs staying adoption-driven follow-through work.

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

### ENG-030 Release package artifact baseline

Status: done
Estimate: 8

Why:

- the repo now validates build, test, benchmarks, and reference docs, but it still lacks an explicit release-pack baseline for the NuGet and template artifacts we actually intend to ship
- `dotnet pack` currently drifts across benchmarks, playgrounds, sample-only libraries, and CLI/tooling surfaces without a repo-owned definition of the intended package boundary

Delivered:

- the intended release packable surface is now explicit: shipped `src/Cephalon.*` packages plus the reference module and template pack, with benchmarks, playgrounds, sample-only libraries, and the unfinished CLI install surface excluded by default
- shared NuGet metadata and a repo-owned package readme baseline now flow through `Directory.Build.props` so shipped packages publish with consistent authorship, repository, license, and readme metadata
- `scripts/publish-package-artifacts.ps1` now publishes deterministic release artifacts under `artifacts/packages-release` and emits a manifest of the packaged project set
- `scripts/validate-release.ps1`, the release-validation workflow, docs, and tooling coverage now all validate the same package-artifact baseline instead of leaving packaging drift implicit

### ENG-031 CLI tool packaging baseline

Status: done
Estimate: 8

Why:

- `ENG-030` made the intended release artifact set explicit, but it also left `Cephalon.Cli` out on purpose until the repository ships a truthful dedicated install surface instead of a generic library-style nupkg
- the docs currently center `dotnet run --project src/Cephalon.Cli -- ...`, which is fine for repo contributors but not yet the external adoption path we want to validate and publish as a supported CLI install story

Delivered:

- `Cephalon.Cli` now ships as an explicit `.NET tool` package with the stable `cephalon` command name and a package-specific readme instead of staying outside the release package boundary
- the release package-artifact flow now includes the CLI tool package alongside the shipped library and template artifacts
- tooling coverage now validates the packed tool metadata, package contents, and a local install/execute smoke path from the produced `Cephalon.Cli` artifact
- package-publishing docs, compatibility guidance, and repository usage examples now document the supported CLI install path explicitly instead of centering `dotnet run --project` as the only adoption story

### ENG-032 Release package provenance manifest baseline

Status: done
Estimate: 5

Delivered:

- the published release package-artifact manifest now carries top-level `SourceRepository` and `SourceRevision` fields so downstream automation can tie package sets back to the repository revision that produced them
- each packed project now publishes `PackageKind` plus per-file `Path`, `FileName`, `SizeBytes`, and `Sha256` metadata instead of leaving checksum verification to ad-hoc file inspection
- the release flow now emits `package-artifacts.sha256` alongside `package-artifacts-manifest.json` so operators can verify package files without parsing JSON
- package-publishing guidance, compatibility notes, and tooling coverage were updated so the shipped release-artifact contract stays explicit and truthful

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

Status: done
Estimate: 268

Why:

- the self-hosted OTLP collector/runtime-default slice plus the Azure Monitor, AWS, GCP, Huawei Cloud, Alibaba Cloud, Oracle Cloud, Red Hat OpenShift, DigitalOcean, VMware Tanzu, and platform-neutral Kubernetes slices are now shipped, `#120` has now shipped downstream Cloudflare and custom-provider authoring guidance instead of a misleading first-party Cloudflare exporter package, and `#126` has now shipped the Grafana Cloud first-party follow-through target instead of reopening the remaining provider matrix as one vague task
- current New Relic native OTLP docs exposed region-specific OTLP endpoints, the required `api-key` header, and an OTLP/HTTP recommendation that fit a provider-specific companion package without moving vendor assumptions back into the engine core, and that explicit follow-through is now shipped under `#127`
- current Cloudflare Workers observability docs center Worker-native traces and logs plus exporting OpenTelemetry-compliant traces and logs from Workers to third-party OTLP destinations, with metrics export still unsupported, so a generic Cephalon host-side Cloudflare sink would over-claim the current platform story
- this work should stay in companion packages, preserve the shared `ILogger` pipeline plus the cloud-neutral OTLP baseline, and leave room for downstream developer-authored provider packages

Acceptance:

- keep the shipped self-hosted deployment defaults explicit and reusable instead of burying them inside vendor-specific companion packs
- keep the shipped Azure Monitor, AWS, GCP, Huawei Cloud, Alibaba Cloud, Oracle Cloud, Red Hat OpenShift, DigitalOcean, and Kubernetes slices explicit on top of the shared OpenTelemetry baseline
- keep the shipped Red Hat OpenShift and DigitalOcean companion follow-through explicit instead of rolling them back into one ambiguous remaining-platform scope
- keep the shipped DigitalOcean collector-first follow-through explicit instead of over-claiming a managed DigitalOcean OTLP exporter surface that the current platform docs do not promise
- keep the shipped Kubernetes collector-first follow-through explicit on top of the shared OTLP baseline instead of folding generic cluster defaults back into OpenShift, DigitalOcean, or downstream custom-provider guidance
- keep the shipped Oracle Cloud managed traces/metrics follow-through explicit on top of the shared OTLP baseline instead of folding Oracle Cloud APM-specific data-upload and data-key rules back into Huawei Cloud, Alibaba Cloud, or downstream custom-provider guidance
- keep the shipped VMware Tanzu proxy-first follow-through explicit on top of the shared OTLP baseline instead of reopening Cloudflare or pretending the current Tanzu docs describe one generic vendor-direct OTLP exporter path
- keep the shipped `#120` scope centered on downstream Cloudflare and custom-provider companion authoring guidance until Cloudflare documents a host-side ingestion story that fits Cephalon's .NET runtime model
- keep the shipped `#126` scope centered on Grafana Cloud OTLP endpoint wiring plus access-policy-backed auth headers on top of the shared OTLP baseline instead of reopening a generic remaining-provider task
- keep the shipped `#127` scope centered on New Relic native OTLP endpoint wiring plus `api-key`-backed headers and region-aware defaults on top of the shared OTLP baseline instead of reopening a generic remaining-provider task
- keep vendor/platform-specific exporter wiring, auth, resource attributes, and hosted defaults outside `Cephalon.Engine` and `Cephalon.Abstractions`
- keep the shared `ILogger` pipeline and existing `Cephalon.Observability.OpenTelemetry` baseline intact
- add docs, validation, and planning sync for the supported targets plus the downstream companion-package authoring path, including shipped Grafana Cloud OTLP/header guidance, shipped New Relic OTLP/api-key guidance, and Cloudflare-oriented guidance that stays honest about the current Worker-native export model
- avoid shipping a first-party `Cephalon.Observability.Cloudflare` package unless Cloudflare later exposes a documented generic OTLP ingestion story for external hosts
- keep any provider beyond the shipped New Relic slice as a later explicit child item instead of reopening the remaining provider matrix as one task

## Next adoption and operator readiness work

### ENG-033 Cross-platform validation and shell parity baseline

Status: done
Estimate: 13
Completed: April 4, 2026

Completed work:

- updated `scripts/validate-release.ps1` plus the package-publishing, reference-doc publishing, and operational-validation helper scripts so nested PowerShell invocations run through the active host with `pwsh`-friendly parameter binding instead of Windows-only assumptions
- updated `.github/workflows/release-validation.yml` to run the same repo-native validation flow on both Windows and Ubuntu without duplicating build/test/publish logic in workflow YAML
- extended tooling coverage around package publishing, reference-doc publishing, and release validation so the supported shell path stays locked to the same repo-native scripts
- aligned release-validation, package-publishing, and adoption docs with the supported Windows, WSL, and Linux-class shell path

Why:

- the current repo-native validation and publishing flow still invokes Windows PowerShell directly and GitHub Actions currently proves it only on `windows-latest`, even though Cephalon's host/runtime model is meant to stay host-agnostic
- local install smoke coverage for the CLI tool and template pack already exists, but the shipped automation still does not prove those same paths on Linux-class shells or WSL-friendly environments
- hardening the shell and CI path now is higher leverage than adding more engine surface because it stabilizes every shipped package, sample, and tool

Acceptance:

- repo-native PowerShell scripts use a cross-platform invocation strategy that works under PowerShell 7 / `pwsh`
- release validation runs on both Windows and Linux without duplicating build/test/publish logic in workflow YAML
- CLI tool install/help and template install/generate smoke coverage prove a Linux-compatible path in addition to the current Windows path
- docs call out the supported local validation path for Windows, WSL, and Linux-class shells

### ENG-034 First-run adoption and environment doctor path

Status: done
Estimate: 8
Completed: April 4, 2026

Why:

- the repo now has a strong quick start plus package/template/tool install coverage, but new adopters still do not have one first-class answer for "is my environment set up correctly?"
- `Cephalon.Cli` currently focuses on generation and docs publishing, which leaves install, upgrade, and runtime-smoke validation spread across multiple docs and package readmes
- a first-run doctor path will reduce support friction and make the framework easier to adopt outside the repo

Acceptance:

- `Cephalon.Cli` ships a first-class doctor or equivalent environment-verification command that checks the expected SDK/tool/template/runtime prerequisites
- docs add a dedicated getting-started/adoption guide that walks from install to generated-app run and `/engine/*` inspection
- CLI help text, README guidance, and package readmes stay aligned with the same first-run path
- automated coverage proves the doctor/verification flow and its documented happy path

Completed work:

- shipped `cephalon doctor` in `Cephalon.Cli` with required SDK/runtime checks plus advisory template-pack detection
- added `docs/getting-started.md` as the install, verification, scaffold, run, and `/engine/*` inspection path
- aligned CLI help text, `README.md`, `src/Cephalon.Cli/PACKAGE.md`, and `templates/Cephalon.TemplatePack/PACKAGE.md` with the doctor-first adoption flow
- added CLI and documentation coverage for the doctor happy path, required-failure path, and doc/readme alignment

### ENG-035 External module package lifecycle prove-out

Status: done
Estimate: 13
Completed: April 4, 2026

Why:

- the engine can already load manifest-driven packages with trust and provenance metadata, but the repo still proves most of that story through repo-local references and docs rather than an operator-facing end-to-end workflow
- a practical framework needs a repeatable "author package, publish artifact, trust it, load it, inspect it" baseline outside the repo-local assembly path
- this work keeps package loading explicit and introspectable while making the external distribution story concrete enough for real adopters

Acceptance:

- the reference module or an equivalent sample package can be published as an external artifact and loaded into a host from an out-of-tree package location
- docs show the full package author -> publish -> trust -> load -> inspect flow using `cephalon.package.json`, `Engine:PackagePolicy`, and `Engine:Trust`
- automated coverage proves external package load paths, trust/policy outcomes, and runtime introspection outside the repo-local assembly-only scenario
- package-publishing guidance stays aligned with the prove-out flow

Completed work:

- shipped `cephalon package stage` in `Cephalon.Cli` so published module `.nupkg` artifacts can be materialized into loadable package directories outside the repo-local assembly path
- added end-to-end CLI and ASP.NET Core host coverage that packs `Cephalon.ReferenceModule.Operations`, stages it into an out-of-tree package directory, loads it through `Engine:Discovery:PackageDirectories`, and verifies trust/policy/runtime-introspection surfaces
- added `docs/external-package-lifecycle.md` as the operator-facing publish -> stage -> trust -> load -> inspect walkthrough
- aligned `docs/package-publishing.md`, `docs/module-authoring.md`, `samples/Cephalon.ReferenceModule.Operations/README.md`, CLI docs, and package readme guidance with the same external package staging flow

### ENG-036 Containerized local runtime and operations baseline

Status: done
Estimate: 13
Completed: April 4, 2026

Completed work:

- made `Cephalon.Sample.ModularMonolith` container-friendly by letting late command-line or environment configuration override the sample JSON baseline and by wiring `Cephalon.Observability.OpenTelemetry`
- added a Dockerfile, `compose.yaml`, collector config, optional package-directory compose override, sample README, and plugin placeholder so the modular monolith sample can run under Docker Desktop / WSL with the same `/engine/*` and `/health/*` surfaces
- added `docs/container-runtime.md`, aligned the root/docs/operations guidance with the same container path, and shipped the optional `scripts/validate-container-runtime.ps1` smoke entry point without making Docker a release-validation dependency
- added hosting and tooling coverage for the container assets/configuration path and verified the sample through the shipped Docker compose smoke flow

Why:

- the repo ships runnable samples and strong `/engine/*` introspection surfaces, but it still lacks a reproducible Docker Desktop / WSL-friendly path for validating a host with production-like deployment boundaries
- operators and adopters need a concrete local deployment shape for health checks, telemetry configuration, package mounts, and environment-driven settings before Cephalon feels practical beyond source builds
- this work can prove the existing engine/runtime surface through sample-level assets without pushing Docker-specific behavior into `Cephalon.Engine`

Acceptance:

- at least one adoption-quality sample host ships with a Dockerfile and container run guidance that preserve current config-loading and `/engine/*` surfaces
- a Docker Compose or equivalent local orchestration path proves app startup, health endpoints, and at least one telemetry/export handoff or collector path
- docs explain how to run the sample under Docker Desktop / WSL and where to inspect runtime, health, and package surfaces
- validation guidance includes a containerized smoke path without making Docker a required engine dependency

### ENG-037 Generated app local package-feed bootstrap baseline

Status: done
Estimate: 8
Completed: April 4, 2026

Why:

- generated apps from `cephalon new` and the shipped `dotnet new` starters can expose the right runtime shape, but a fresh adopter still needs a truthful first-run answer for where Cephalon packages come from before restore, build, or container startup will work outside this repo
- the shared package version baseline had drifted far enough that `dotnet pack` could default repo artifacts to `1.0.0` while scaffolds, templates, and docs still expected the preview package line, which makes adoption guidance look correct while restore/install behavior disagrees
- phase 7 should close the gap between "I can scaffold an app" and "I can actually build and run that generated app" without requiring repo-only NuGet knowledge

Acceptance:

- generated apps from both `cephalon new` and the app-focused `dotnet new` starters emit `NuGet.config` plus a documented local package-feed placeholder by default
- shared package-version defaults stay aligned with the preview package line used by scaffolds, templates, docs, and the CLI tool install path
- docs explain how to seed the generated-app local feed or replace the `cephalon` package source before first build and first container run
- automated coverage plus a real smoke flow prove scaffold -> package publish -> restore/build -> Docker compose startup and the expected `/engine/*`, `/health/*`, and docs surfaces

Completed work:

- aligned shared `Cephalon.*` package-version defaults in `Directory.Build.props` back to `0.1.0-preview` so packed artifacts, templates, scaffolds, and install docs point at the same prerelease baseline
- updated `Cephalon.Scaffolding` and the shipped app templates to emit `NuGet.config`, `.cephalon/packages/README.md`, and README guidance that explain the seeded-local-feed bootstrap path
- removed the redundant ASP.NET Core framework reference from generated web hosts so the generated app build stays warning-free on the supported path
- aligned `README.md`, `docs/getting-started.md`, `docs/container-runtime.md`, `docs/package-publishing.md`, `docs/components/scaffolding.md`, CLI package guidance, and template-pack guidance with the prerelease install plus local-feed bootstrap flow
- added scaffolding, CLI, template-pack, package-publishing, and documentation coverage for the generated-app bootstrap contract, then verified a real generated app through scaffold -> publish-package-artifacts into `./.cephalon/packages` -> `dotnet build` -> `docker compose up --build` with `/engine/snapshot`, `/health/ready`, and `/scalar`

### ENG-038 Generated app published-output and deployment baseline

Status: done
Estimate: 8
Completed: April 4, 2026

Why:

- generated apps can now bootstrap packages and take the documented container path, but adopters still need a truthful deployment-like answer for publishing and starting a Cephalon host from emitted artifacts instead of a source-tree build
- without a shipped publish profile and a smoke path that exercises published output, teams still have to rediscover host project paths, output conventions, and runtime probes before they can trust the generated app beyond local source builds
- phase 7 should close the gap between "I can build and container-run a generated app" and "I can publish and run the generated output" without repo-only MSBuild or host-startup knowledge

Acceptance:

- generated hosts from both `cephalon new` and the shipped app-focused `dotnet new` starters emit a deterministic folder publish profile by default
- docs explain how to publish and run a generated app from published output, including the supported seeded or repointed `cephalon` package-source expectation
- automated coverage plus an optional validation script prove scaffold -> package publish -> folder publish -> run published output and the expected `/engine/*`, `/health/*`, and docs surfaces
- published-output guidance stays aligned across scaffolding, CLI, template-pack, and operations docs

Completed work:

- updated `Cephalon.Scaffolding` and the shipped app templates to emit `Properties/PublishProfiles/CephalonFolder.pubxml` that publish into deterministic `artifacts/publish/<ProjectName>/` output without forcing an app host executable
- added `docs/generated-app-publishing.md` and aligned `README.md`, `docs/getting-started.md`, `docs/container-runtime.md`, `docs/operations.md`, `docs/components/scaffolding.md`, `docs/components/cli.md`, CLI package guidance, and template-pack guidance with the published-output path
- added `scripts/validate-generated-app-publish.ps1` so the repo can scaffold a temporary app, seed local packages, publish the host, start the published DLL, and validate `/health/ready`, `/engine`, `/engine/snapshot`, and `/scalar`
- extended scaffolding, CLI, template-pack, and documentation coverage for the generated publish contract, then verified a real smoke path through scaffold -> publish-package-artifacts into `./.cephalon/packages` -> `dotnet publish -p:PublishProfile=CephalonFolder` -> run published output

### ENG-039 Generated app Linux systemd deployment baseline

Status: done
Estimate: 8
Completed: April 4, 2026

Why:

- generated apps can now bootstrap packages, publish deterministically, and take the documented container path, but adopters still need a truthful self-hosted Linux answer for turning that published output into a long-running service on a VM or bare-metal host
- without shipped service assets and a verification path, teams still have to rediscover `/opt/*` layout, environment-file location, and `systemctl` install steps before they can trust the generated app outside local shells or container tooling
- phase 7 should close the gap between "I can publish the app" and "I can package the published output into a Linux service-manager shape" without pushing Linux-service behavior into `Cephalon.Engine`

Acceptance:

- generated hosts from both `cephalon new` and the shipped app-focused `dotnet new` starters emit Linux `systemd` deployment assets by default
- docs explain how to install published output plus the generated unit/environment files on a Linux target and how to validate the unit before enabling it
- automated coverage plus an optional validation script prove scaffold -> package publish -> folder publish -> Linux `systemd` unit verification under WSL or Linux-class shells
- Linux self-hosted guidance stays aligned across scaffolding, CLI, template-pack, getting-started, operations, and generated-app publishing docs

Completed work:

- updated `Cephalon.Scaffolding` and the shipped app templates to emit `deploy/linux/systemd/README.md`, `deploy/linux/systemd/<App>.service`, and `deploy/linux/systemd/<App>.env` alongside the existing publish/container assets
- added `docs/linux-systemd-deployment.md` and aligned `README.md`, `docs/getting-started.md`, `docs/generated-app-publishing.md`, `docs/container-runtime.md`, `docs/operations.md`, `docs/components/scaffolding.md`, `docs/components/cli.md`, CLI package guidance, and template-pack guidance with the Linux self-hosted path
- added `scripts/validate-generated-app-systemd.ps1` so the repo can scaffold a temporary app, seed local packages, publish the host, rewrite the generated unit to WSL-visible publish paths, and run `systemd-analyze verify`
- extended scaffolding, CLI, template-pack, and documentation coverage for the generated Linux deployment contract, then verified a real smoke path through scaffold -> publish-package-artifacts into `./.cephalon/packages` -> `dotnet publish -p:PublishProfile=CephalonFolder` -> WSL `systemd-analyze verify`

### ENG-040 Generated app Windows Service deployment baseline

Status: done
Estimate: 8
Completed: April 4, 2026

Why:

- generated apps can now bootstrap packages, publish deterministically, and take both the documented container path and Linux self-hosted path, but Windows-first teams still need a truthful self-hosted answer for turning that published output into a long-running service without rediscovering `sc.exe` arguments and content-root handling
- without shipped install assets plus Windows Service-aware host wiring, teams still have to rediscover `C:\Services\*` layout, `--contentRoot` handling, and service-recovery commands before they can trust the generated app outside local shells or container tooling
- phase 7 should close the gap between "I can publish the app" and "I can package the published output into a Windows service-manager shape" without pushing Windows-only behavior into `Cephalon.Engine`

Acceptance:

- generated hosts from both `cephalon new` and the shipped app-focused `dotnet new` starters emit Windows Service deployment assets by default
- generated hosts are wired for Windows Service lifetime and content-root handling without changing the blueprint or engine contracts
- docs explain how to preview, install, verify, and remove the generated Windows Service assets on a Windows target
- automated coverage plus an optional validation script prove scaffold -> package publish -> folder publish -> Windows Service install preview against published output
- Windows self-hosted guidance stays aligned across scaffolding, CLI, template-pack, getting-started, operations, and generated-app publishing docs

Completed work:

- updated `Cephalon.Scaffolding` and the shipped app templates to emit `deploy/windows-service/README.md`, `deploy/windows-service/install-service.ps1`, and `deploy/windows-service/remove-service.ps1` alongside the existing publish/container/Linux assets
- updated generated ASP.NET Core host wiring plus the shipped app templates to use `Microsoft.Extensions.Hosting.WindowsServices`, `WindowsServiceHelpers.IsWindowsService()`, and `builder.Host.UseWindowsService()` so service lifetime and content-root behavior stay aligned under SCM startup
- added `docs/windows-service-deployment.md` and aligned `README.md`, `docs/README.md`, `docs/getting-started.md`, `docs/generated-app-publishing.md`, `docs/operations.md`, `docs/components/scaffolding.md`, `docs/components/cli.md`, CLI package guidance, and template-pack guidance with the Windows self-hosted path
- added `scripts/validate-generated-app-windows-service.ps1` so the repo can scaffold a temporary app, seed local packages, publish the host, verify the generated Windows Service host wiring, and replay the shipped install/remove scripts in preview mode against published output
- extended scaffolding, CLI, template-pack, and documentation coverage for the generated Windows deployment contract, then verified a real smoke path through scaffold -> publish-package-artifacts into `./.cephalon/packages` -> `dotnet publish -p:PublishProfile=CephalonFolder` -> Windows Service install preview

### ENG-041 Generated app IIS deployment baseline

Status: done
Estimate: 8
Completed: April 4, 2026

Why:

- generated apps can now bootstrap packages, publish deterministically, and take both the documented self-hosted Windows and Linux paths, but Windows-hosted teams still need a truthful IIS answer for turning that published output into a site plus app-pool deployment without rediscovering the ASP.NET Core Module contract
- without shipped IIS assets plus guidance around the SDK-generated `web.config`, teams still have to rediscover `C:\inetpub\sites\*` layout, `appcmd.exe` flow, hosting-bundle prerequisites, and ANCM expectations before they can trust the generated app on a hosted Windows baseline
- phase 7 should close the gap between "I can publish the app" and "I can package the published output into an IIS site/app-pool shape" without pushing IIS-specific behavior into `Cephalon.Engine`

Acceptance:

- generated hosts from both `cephalon new` and the shipped app-focused `dotnet new` starters emit IIS deployment assets by default
- generated published output keeps the expected ASP.NET Core Module `web.config` so IIS can launch the host through `dotnet`
- docs explain how to preview, install, verify, and remove the generated IIS assets on a Windows target
- automated coverage plus an optional validation script prove scaffold -> package publish -> folder publish -> IIS install preview against published output
- IIS hosted guidance stays aligned across scaffolding, CLI, template-pack, getting-started, operations, and generated-app publishing docs

Completed work:

- updated `Cephalon.Scaffolding` and the shipped app templates to emit `deploy/iis/README.md`, `deploy/iis/install-site.ps1`, and `deploy/iis/remove-site.ps1` alongside the existing publish, container, Windows Service, and Linux assets
- aligned generated app and template README guidance with the hosted Windows IIS path built on top of the SDK-generated ASP.NET Core Module `web.config`
- added `docs/iis-deployment.md` and aligned `README.md`, `docs/README.md`, `docs/getting-started.md`, `docs/generated-app-publishing.md`, `docs/operations.md`, `docs/components/scaffolding.md`, `docs/components/cli.md`, CLI package guidance, and template-pack guidance with the IIS hosted path
- added `scripts/validate-generated-app-iis.ps1` so the repo can scaffold a temporary app, seed local packages, publish the host, verify the generated `web.config`, and replay the shipped IIS install/remove scripts in preview mode against published output
- extended scaffolding, CLI, template-pack, and documentation coverage for the generated IIS deployment contract, then verified a real smoke path through scaffold -> publish-package-artifacts into `./.cephalon/packages` -> `dotnet publish -p:PublishProfile=CephalonFolder` -> IIS install preview

### ENG-042 Generated app Azure App Service deployment baseline

Status: done
Estimate: 8
Completed: April 4, 2026

Why:

- generated apps can now bootstrap packages, publish deterministically, and take self-hosted Windows/Linux plus hosted IIS paths, but cloud-hosted teams still need a truthful Azure App Service answer for turning that published output into a deployable ZIP artifact without rediscovering the run-from-package contract
- without shipped Azure App Service assets plus guidance around `WEBSITE_RUN_FROM_PACKAGE=1`, `az webapp deploy`, and the deterministic ZIP package path, teams still have to rediscover packaging and deploy-preview behavior before they can trust the generated app on a hosted Azure baseline
- phase 7 should close the gap between "I can publish the app" and "I can package the published output into an Azure App Service ZIP-deploy shape" without pushing Azure-specific behavior into `Cephalon.Engine`

Acceptance:

- generated hosts from both `cephalon new` and the shipped app-focused `dotnet new` starters emit Azure App Service deployment assets by default
- generated deployment assets package published output into a deterministic ZIP artifact and preserve the expected SDK-generated `web.config`
- docs explain how to preview, package, and deploy the generated Azure assets with the current Azure CLI contract
- automated coverage plus an optional validation script prove scaffold -> package publish -> folder publish -> Azure App Service ZIP packaging and preview against published output
- Azure App Service guidance stays aligned across scaffolding, CLI, template-pack, getting-started, operations, and generated-app publishing docs

Completed work:

- updated `Cephalon.Scaffolding` and the shipped app templates to emit `deploy/azure-app-service/README.md` and `deploy/azure-app-service/deploy-zip.ps1` alongside the existing publish, container, Windows Service, IIS, and Linux assets
- aligned generated app and template README guidance with the hosted Azure App Service ZIP-deploy path built on top of the deterministic `CephalonFolder.pubxml` publish output
- added `docs/azure-app-service-deployment.md` and aligned `README.md`, `docs/README.md`, `docs/getting-started.md`, `docs/generated-app-publishing.md`, `docs/operations.md`, `docs/components/scaffolding.md`, `docs/components/cli.md`, CLI package guidance, and template-pack guidance with the Azure App Service path
- added `scripts/validate-generated-app-app-service.ps1` so the repo can scaffold a temporary app, seed local packages, publish the host, package the generated ZIP artifact, and replay the shipped Azure deploy script in preview mode against the current Azure CLI contract
- extended scaffolding, CLI, template-pack, and documentation coverage for the generated Azure App Service deployment contract, then verified a real smoke path through scaffold -> publish-package-artifacts into `./.cephalon/packages` -> `dotnet publish -p:PublishProfile=CephalonFolder` -> Azure App Service ZIP packaging and deploy preview

### ENG-043 Generated app Azure Container Apps deployment baseline

Status: done
Estimate: 8
Completed: April 4, 2026

Why:

- generated apps can now bootstrap packages, publish deterministically, and take self-hosted Windows/Linux plus hosted IIS and Azure App Service paths, but cloud-hosted teams using Azure's container-native platform still need a truthful Container Apps answer from the shipped Dockerfile and app root without inventing another deployment script from scratch
- without shipped Azure Container Apps assets plus guidance around `az containerapp up --source`, ingress, target port, and baseline environment variables, teams still have to rediscover source-root deployment behavior before they can trust the generated app on a hosted Azure container baseline
- phase 7 follow-through should close the gap between "I can validate the generated Dockerfile locally" and "I can deploy the generated app to Azure Container Apps" without pushing Azure-specific behavior into `Cephalon.Engine`

Acceptance:

- generated hosts from both `cephalon new` and the shipped app-focused `dotnet new` starters emit Azure Container Apps deployment assets by default
- generated deployment assets validate the source root and preview the current Azure CLI contract from the shipped Dockerfile/app root shape
- docs explain how to preview and deploy the generated Azure Container Apps assets with `az containerapp up --source`
- automated coverage plus an optional validation script prove scaffold -> package publish -> local Docker build -> Azure Container Apps deploy preview against the generated app root
- Azure Container Apps guidance stays aligned across scaffolding, CLI, template-pack, getting-started, operations, and generated-app publishing docs

Completed work:

- updated `Cephalon.Scaffolding` and the shipped app templates to emit `deploy/azure-container-apps/README.md` and `deploy/azure-container-apps/deploy-up.ps1` alongside the existing publish, container, Windows Service, IIS, Azure App Service, and Linux assets
- aligned generated app and template README guidance with the hosted Azure Container Apps source-deploy path built on top of the shipped Dockerfile and `NuGet.config` bootstrap
- added `docs/azure-container-apps-deployment.md` and aligned `README.md`, `docs/README.md`, `docs/getting-started.md`, `docs/generated-app-publishing.md`, `docs/operations.md`, `docs/components/scaffolding.md`, `docs/components/cli.md`, CLI package guidance, and template-pack guidance with the Azure Container Apps path
- added `scripts/validate-generated-app-container-apps.ps1` so the repo can scaffold a temporary app, seed local packages, validate the generated Dockerfile locally, and replay the shipped Azure deploy script in preview mode against the current Azure CLI contract
- extended scaffolding, CLI, template-pack, and documentation coverage for the generated Azure Container Apps deployment contract, then verified a real smoke path through scaffold -> publish-package-artifacts into `./.cephalon/packages` -> local Docker build -> Azure Container Apps deploy preview

### ENG-044 Generated app Kubernetes deployment baseline

Status: done
Estimate: 8
Completed: April 4, 2026

Why:

- generated apps can now bootstrap packages, publish deterministically, and take self-hosted Windows/Linux plus hosted IIS, Azure App Service, and Azure Container Apps paths, but teams deploying onto generic or self-managed Kubernetes clusters still need a truthful manifest/apply answer from the shipped Dockerfile and app root without inventing a second deploy workflow
- without shipped Kubernetes assets plus guidance around `kubectl kustomize`, namespace shape, service exposure, and baseline health probes, teams still have to rediscover cluster-ready manifest conventions before they can trust the generated app on a platform-neutral Kubernetes baseline
- adoption follow-through should close the gap between "I can validate the generated Dockerfile locally" and "I can render/apply the generated app onto Kubernetes" without pushing cluster-specific behavior into `Cephalon.Engine`

Acceptance:

- generated hosts from both `cephalon new` and the shipped app-focused `dotnet new` starters emit Kubernetes deployment assets by default
- generated deployment assets render the manifest set locally and preview the current `kubectl kustomize` contract from the shipped Dockerfile/app root shape
- docs explain how to preview and apply the generated Kubernetes assets, including namespace, service, and health-probe expectations
- automated coverage plus an optional validation script prove scaffold -> package publish -> local Docker build -> Kubernetes manifest preview against the generated app root
- Kubernetes guidance stays aligned across scaffolding, CLI, template-pack, getting-started, operations, and generated-app publishing docs

Completed work:

- updated `Cephalon.Scaffolding` and the shipped app templates to emit `deploy/kubernetes/README.md`, `deploy/kubernetes/apply.ps1`, `deploy/kubernetes/kustomization.yaml`, `deploy/kubernetes/namespace.yaml`, `deploy/kubernetes/deployment.yaml`, and `deploy/kubernetes/service.yaml` alongside the existing publish, container, Windows Service, IIS, Azure App Service, Azure Container Apps, and Linux assets
- aligned generated app and template README guidance with the Kubernetes manifest/apply path built on top of the shipped Dockerfile and `NuGet.config` bootstrap
- added `docs/kubernetes-deployment.md` and aligned `README.md`, `docs/README.md`, `docs/getting-started.md`, `docs/generated-app-publishing.md`, `docs/operations.md`, `docs/components/scaffolding.md`, `docs/components/cli.md`, CLI package guidance, and template-pack guidance with the Kubernetes path
- added `scripts/validate-generated-app-kubernetes.ps1` so the repo can scaffold a temporary app, seed local packages, validate the generated Dockerfile locally, and replay the shipped Kubernetes apply script in preview mode against the current `kubectl kustomize` contract
- extended scaffolding, CLI, template-pack, and documentation coverage for the generated Kubernetes deployment contract, then verified a real smoke path through scaffold -> publish-package-artifacts into `./.cephalon/packages` -> local Docker build -> Kubernetes manifest preview

### ENG-045 Generated app container-image publishing baseline

Status: done
Estimate: 8
Completed: April 4, 2026

Why:

- generated apps can now bootstrap packages, publish deterministically, and take self-hosted Windows/Linux plus hosted IIS, Azure App Service, Azure Container Apps, and Kubernetes paths, but teams still need a truthful provider-neutral image build/tag/push answer from the shipped Dockerfile and app root without inventing a registry workflow from scratch
- without shipped container-image assets plus guidance around `docker build`, `docker push`, additional tags, and registry auth expectations, teams still have to rediscover how Cephalon's generated Dockerfile should become a pullable image before they can trust the hosted container baselines
- adoption follow-through should close the gap between "I can validate the generated Dockerfile locally" and "I can publish a pullable image for Kubernetes or another hosted container platform" without pushing registry-specific behavior into `Cephalon.Engine`

Acceptance:

- generated hosts from both `cephalon new` and the shipped app-focused `dotnet new` starters emit container-image publishing assets by default
- generated publish assets preview the current `docker build` and `docker push` contract from the shipped Dockerfile/app root shape and can build one or more image tags without hidden repo assumptions
- docs explain how to preview, build, and push the generated container-image assets, including reuse with the hosted container deployment baselines
- automated coverage plus an optional validation script prove scaffold -> package publish -> local Docker build -> local-registry push against the generated app root
- container-image guidance stays aligned across scaffolding, CLI, template-pack, getting-started, operations, generated-app publishing, and the hosted container deployment docs

Completed work:

- updated `Cephalon.Scaffolding` and the shipped app templates to emit `deploy/container-image/README.md` and `deploy/container-image/publish-image.ps1` alongside the existing publish, container, Windows Service, IIS, Azure App Service, Azure Container Apps, Kubernetes, and Linux assets
- aligned generated app and template README guidance with the provider-neutral container image build/tag/push path built on top of the shipped Dockerfile and `NuGet.config` bootstrap
- added `docs/container-image-publishing.md` and aligned `README.md`, `docs/README.md`, `docs/getting-started.md`, `docs/generated-app-publishing.md`, `docs/operations.md`, `docs/components/scaffolding.md`, `docs/components/cli.md`, CLI package guidance, and template-pack guidance with the container-image path
- added `scripts/validate-generated-app-container-image.ps1` so the repo can scaffold a temporary app, seed local packages, preview the shipped build/push contract, build the generated image, and prove push through a local Docker registry backed by `registry:2`
- extended scaffolding, CLI, template-pack, and documentation coverage for the generated container-image publishing contract, then verified a real smoke path through scaffold -> publish-package-artifacts into `./.cephalon/packages` -> generated `publish-image.ps1` -> local registry push

## Next configurable application-platform work

This feature wave is split into three non-overlapping workstreams so core contracts can freeze before package and generation follow-through:

- `WS1 Engine core`: `ENG-046`, `ENG-047`, and `ENG-048`
- `WS2 Companion packs`: `ENG-049`, `ENG-050`, `ENG-051`, and `ENG-052`
- `WS3 CLI and scaffolding`: `ENG-053`
- `Validation lane`: `ENG-055` and `ENG-056`
- `ENG-054` and `ENG-057` stay later until the phase-8 golden path proves the contract

Cross-cutting success principle for this wave:

- every shipped slice should reduce ceremony for consumer apps by moving repeatable infrastructure, runtime wiring, and declarations into Cephalon configuration, companion packs, and scaffold conventions
- every shipped slice should reduce boilerplate without hiding operational truth, so teams can focus their hand-written code on business logic rather than framework plumbing

### ENG-046 App-model taxonomy and catalog expansion baseline

Status: done
Estimate: 5
Completed: April 4, 2026

Why:

- the engine already separates blueprints, patterns, and technologies, but the next feature wave mixes app shape, architecture style, data pattern, security mode, and deployment concerns in ways that will drift unless the taxonomy is frozen first
- the shipped catalogs stop short of `Hexagonal`, `Layered`, `CleanArchitecture`, `DDD`, `CQRS`, `Outbox`, `EventSourcing`, `IdentityAccess`, `MultiTenancy`, `HybridCloudRuntime`, `ServiceMeshIntegration`, and `ServerlessHosting`
- phase 8 needs one explicit answer for where each concept lives before code, docs, CLI defaults, or samples expand

Acceptance:

- new pattern ids and technology ids are frozen with aliases where needed
- shipped blueprints remain limited to app shapes instead of exploding into every architecture label
- planning, docs, scaffolding, and runtime catalogs can all reference the same ids
- public descriptors and XML comments explain the intent of every new id that enters the supported catalog

Delivered:

- built-in phase-8 pattern ids now cover `hexagonal-architecture`, `layered-architecture`, `clean-architecture`, `domain-driven-design`, `cqrs`, `outbox`, and `event-sourcing`
- built-in phase-8 technology ids now cover `identity-access`, `multi-tenancy`, `hybrid-cloud-runtime`, `service-mesh-integration`, and `serverless-hosting`
- pattern and technology descriptors now support aliases so config, planning, and scaffold guidance can speak one stable vocabulary without forcing only one spelling

### ENG-047 Structured engine configuration and runtime surface baseline for data, identity, tenancy, audit, and messaging

Status: done
Estimate: 8
Completed: April 4, 2026

Why:

- consumer apps need to turn these features on, off, or combine them through configuration without rewriting host startup
- the current `Engine` settings surface is too shallow for additive data, identity, tenancy, audit, and messaging packs with deeper options
- runtime snapshot and diagnostics surfaces should stay truthful once these new dimensions are introduced

Acceptance:

- structured `Engine:Data`, `Engine:Identity`, `Engine:Tenancy`, `Engine:Audit`, and `Engine:Messaging` sections exist with stable option names
- runtime and introspection surfaces expose the active phase-8 selections without binding the core to Entity Framework, Wolverine, or ASP.NET Core specifics
- config validation catches invalid combinations and missing prerequisites early
- host-specific APIs remain out of `Cephalon.Abstractions`

Delivered:

- `EngineSettings` now carries structured phase-8 settings for data, identity, tenancy, audit, and messaging
- resolved app profiles now surface phase-8 selections through `Data`, `Identity`, `Tenancy`, `Audit`, and `Messaging`
- phase-8 selection validation now catches invalid combinations such as read/write split without `cqrs`, outbox without `outbox`, or messaging provider selection without `event-driven-integration`

### ENG-048 Host-agnostic data, authorization, tenancy, audit, and id contract baseline

Status: done
Estimate: 13
Completed: April 4, 2026

Why:

- the requested phase-8 features need core primitives before any companion package can implement them cleanly
- the current abstractions do not yet carry first-class contracts for commands, queries, read/write stores, projections, outbox/inbox, authorization, tenant context, audit entries, or id generation
- if this contract slice drifts after package work starts, every downstream pack, scaffold, and sample will take breaking renames

Acceptance:

- `Cephalon.Abstractions` gains XML-commented public contracts for commands, queries, read/write stores, projections, outbox/inbox, authorization subjects/resources/policies, tenant context/resolution, audit entries, and id generation
- `Cephalon.Engine` gains matching catalog, descriptor, validation, and runtime-surface hooks without taking on provider-specific logic
- tests cover contract resolution plus runtime and introspection exposure
- host adapters stay out of the core contract slice

Delivered:

- `Cephalon.Abstractions` now includes host-agnostic `Data`, `Authorization`, `Tenancy`, `Audit`, and `Ids` contract families with XML-commented public types
- contract-focused composition tests, reference-doc smoke coverage, and an explicit `Cephalon.Abstractions` package-surface lock now cover the new exported API
- component guidance for `Cephalon.Abstractions` now documents the new phase-8 contract families
- `Cephalon.Engine` now exposes merged projection, inbox, outbox, and authorization-policy catalogs through `/engine/projections`, `/engine/inboxes`, `/engine/outboxes`, `/engine/authorization-policies`, and `/engine/snapshot` without binding the core to concrete providers
- projection ownership validation now fails fast when a module spoofs another module's source id, and runtime tests cover both composition-time and ASP.NET Core introspection exposure

### ENG-049 Relational Entity Framework CQRS, projections, outbox, and Sfid companion baseline

Status: in progress
Estimate: 21

Why:

- Cephalon needs one honest data golden path before it claims support for many storage models
- the repo does not yet ship a first-class data/runtime baseline, and the requested feature wave explicitly calls for an Entity Framework-first path with provider escape hatches when official libraries are needed
- a relational baseline with CQRS read/write split, projections, outbox, and `Sfid` ids is the highest-leverage first slice
- the chosen id path should bind to the official `Sfid.Net` and `Sfid.EntityFramework` packages instead of a Cephalon-specific reimplementation

Acceptance:

- `Cephalon.Data`, `Cephalon.Data.EntityFramework`, and `Cephalon.Ids.Sfid` exist with clear package boundaries
- a relational Entity Framework read/write split, projection, and outbox baseline works through configuration and companion-pack registration instead of host-specific shortcuts
- runtime surfaces and observability hooks expose active stores, projections, outbox state, and id strategy truthfully
- `Cephalon.Ids.Sfid` wraps the official `Sfid.Net` generator, and the Entity Framework baseline integrates through `Sfid.EntityFramework`
- tests prove the baseline without over-claiming non-relational provider breadth yet

Progress:

- `Cephalon.Data` now exists as a runtime-neutral command/query dispatch pack and registers default `IReadStore` / `IWriteStore` implementations backed by the existing handler abstractions
- `Cephalon.Data.EntityFramework` now exists as the first provider-backed data pack and registers single-context or split read/write Entity Framework Core `DbContext` roles through companion-pack registration instead of host-specific startup code
- `Cephalon.Data.EntityFramework` now also exposes an opt-in Entity Framework-backed inbox baseline that records processed `InboxMessage` rows through write-side contexts implementing `IEntityFrameworkInboxContext`
- `Cephalon.Data.EntityFramework` now also exposes an opt-in Entity Framework-backed outbox baseline that stages `OutboxMessage` rows through write-side contexts implementing `IEntityFrameworkOutboxContext`
- `Cephalon.Ids.Sfid` now exists as the first concrete `ENG-049` slice and wraps the official `Sfid.Net` package through `Cephalon.Abstractions.Ids.IIdGenerator`
- the Sfid pack now supports explicit code-first options plus configuration-driven topology under `Engine:Data:Ids:Sfid` and exposes the official generator interface so `Cephalon.Data.EntityFramework` can use `Sfid.EntityFramework`
- `Cephalon.Engine` now exposes additive outbox catalogs through `IOutboxCatalog`, `/engine/outboxes`, and `/engine/snapshot`, and the Entity Framework pack publishes a truthful staged-only outbox descriptor instead of implying a dispatch runtime that does not exist yet
- `Cephalon.Engine` now also exposes additive inbox catalogs through `IInboxCatalog`, `/engine/inboxes`, and `/engine/snapshot`, and the Entity Framework pack publishes a truthful application-managed inbox descriptor instead of implying subscription execution semantics that do not exist yet
- when `EventDrivenIntegration` is active, the Entity Framework pack now also projects staged-only outbox producers into the `event-driven-integration` technology surface instead of claiming a fuller event dispatch runtime
- when `EventDrivenIntegration` is active, the Entity Framework pack now also projects application-managed inbox stores into the `event-driven-integration` technology surface instead of claiming handler dispatch or retry ownership that does not exist yet
- declared event-subscription metadata can now also report when an application-managed inbox store is available for idempotency follow-through without pretending that `Cephalon.Eventing` executes or retries those handlers yet
- when `EventDrivenIntegration` is active and a real outbox path exists, `Cephalon.Eventing` can now accept `EventPublication` requests through `IEventPublisher` and stage them into the active outbox instead of advertising publish support without a concrete handoff path
- `Cephalon.Data.EntityFramework` now also exposes an adapter-neutral Entity Framework-backed `IEventDispatchStore` so later first-class adapters can read pending staged outbox rows and apply durable dispatch outcomes without skipping around the Cephalon outbox contract
- package-surface tests, reference-doc tests, component docs, and solution/test-project wiring now include `Cephalon.Data`, `Cephalon.Data.EntityFramework`, and `Cephalon.Ids.Sfid`
- remaining work is richer projection persistence/runtime surfaces, broader typed-id/documentation examples on top of the shipped `Sfid.EntityFramework` baseline, and fuller subscription/runtime linkage beyond the shipped staged publication and application-managed idempotency baselines

### ENG-050 Eventing runtime uplift and Wolverine companion baseline

Status: in progress
Estimate: 13

Why:

- the shipped `Cephalon.Eventing` surface already models channels and runtime answers, but it does not yet expose the fuller publisher/subscriber and outbox bridge story the next feature wave needs
- the outbox baseline is incomplete unless Cephalon can tell an operator what was published, subscribed, retried, or blocked
- Wolverine is the current first-class adapter path, `MassTransit` is the tracked-later strategic candidate, and the engine contract must stay runtime-neutral
- we need one clear first-class adapter path instead of diluting the phase-8 baseline across several bus frameworks at once

Acceptance:

- event runtime contracts support publish and subscribe semantics plus outbox handoff and idempotent inbox follow-through
- `Cephalon.Eventing` surfaces active channels, subscriptions, and runtime state through the existing introspection model
- optional `Cephalon.Eventing.Wolverine` integration stays outside the engine core and aligns with the same contracts
- observability and diagnostics cover retries, handlers, and outbox bridge state

Progress:

- `Cephalon.Eventing` now exposes public `EventPublication` and `IEventPublisher` contracts so active eventing runtimes can accept staged publication requests without leaking provider-specific APIs into `Cephalon.Abstractions`
- `Cephalon.Eventing` now also exposes public declared-subscription contracts and catalogs so eventing-enabled apps can surface subscription intent through the same pack without pretending that dispatch handlers are already active
- `Cephalon.Eventing` now registers an outbox-backed publisher only when `EventDrivenIntegration` is selected, publishing is enabled, and a real `IOutbox` implementation is present
- the `eventing.publish` capability is now truthful: it only appears when that outbox-backed handoff path exists, and its metadata now calls out `handoff = outbox` plus runtime-state availability without claiming a broker-owned dispatch runtime
- the `event-driven-integration` technology surface now includes `event-subscriptions` and `event-publishers` so operators can see declared subscription intent plus the active staged-publication path alongside `event-channels` and outbox producers
- declared subscription entries can now also link to hosted-execution descriptors, execution graphs, and runtime-story state through hosted-execution metadata, which gives a truthful application-managed execution answer without claiming that `Cephalon.Eventing` itself owns dispatch
- `Cephalon.Eventing` now also exposes public application-managed subscription runtime-state contracts through `EventSubscriptionExecutionReport`, `EventSubscriptionRuntimeState`, `IEventSubscriptionRuntimeReporter`, and `IEventSubscriptionRuntimeCatalog`, and `event-subscriptions` now projects that reported state plus operator-facing `reported.*` metadata alongside inbox, hosted-execution, execution-graph, and runtime-story linkage
- `Cephalon.Eventing` now also exposes public application-managed outbox-dispatch runtime-state contracts through `EventDispatchExecutionReport`, `EventDispatchRuntimeState`, `IEventDispatchRuntimeReporter`, and `IEventDispatchRuntimeCatalog`, and `event-dispatches` now projects that reported state plus operator-facing `reported.*` metadata on top of the staged outbox-backed publication path
- `Cephalon.Eventing` now also exposes the public `EventDispatchItem` plus `IEventDispatchStore` contract so later first-class adapters can read pending staged events and apply durable dispatch outcomes without binding the contract to Wolverine-specific APIs
- `Cephalon.Eventing` now publishes a stable diagnostics convention for staged publications plus application-managed subscription and publication-dispatch outcomes, and the engine now keeps technology runtime surfaces live enough for that reported state to show up after startup instead of freezing at the first catalog resolution
- the current Entity Framework outbox baseline now persists `next_attempt_at_utc` alongside `dispatch_attempt_count` and `dispatched_at_utc`, so the runtime-neutral dispatch-store contract can honor delayed retry intent instead of re-reading every retried row immediately
- `Cephalon.Eventing.Wolverine` now exists as the official first-class adapter slice with the `eventing.wolverine` capability plus the `wolverine-adapter` runtime surface, and it truthfully supports two modes: a host-wiring-only baseline that reports `dispatchBridge = consumer-managed`, plus an opt-in `wolverine-managed` durable staged-dispatch loop on top of `IEventDispatchStore`
- the `event-dispatches` technology surface now also projects configured dispatch-runtime descriptor metadata per outbox path, and the `wolverine-adapter` surface now aggregates latest outcome, retry-pending count, and report totals so operators can see both configuration truth and live runtime follow-through without stitching several surfaces together by hand
- `Cephalon.Eventing.Wolverine` now also contributes its own diagnostics convention so `/engine/diagnostics` and the runtime snapshot advertise the stable `4300-4303` Wolverine dispatch-loop event ids alongside the shared eventing diagnostics range
- `eventing.subscriptions` still exposes declared subscription descriptors rather than a pack-owned bus runner, while `eventing.subscribe` remains intentionally absent until a real subscription/dispatch runtime exists instead of over-claiming bus behavior
- decision lock: phase 8 will treat `Cephalon.Eventing.Wolverine` as the official first-class adapter path, keep `MassTransit` as the tracked-later candidate once that first path is proven, and leave `MediatR`, `LiteBus`, `NServiceBus`, and `SlimMessageBus` as consumer-owned coexistence choices unless a later bridge or adapter package is explicitly shipped
- coexistence rule: one flow should have one durable-messaging owner, so consumer apps should not layer Cephalon-managed durable messaging semantics and a second bus/runtime on the same publish/consume path
- remaining work is richer observability and retry/runtime follow-through for the now-shipped `wolverine-managed` dispatch path, plus later subscription/handler-execution truth beyond the current declarative and application-managed reporting model

### ENG-051 Identity and authorization companion baseline

Status: in progress
Estimate: 13

Why:

- consumer apps need ready-to-use identity and authorization support, but Cephalon currently only has capability-boundary gating at the REST layer
- `RBAC`, `ABAC`, and policy-based evaluation need to be modelled as configuration-driven authorization modes instead of scattered host code
- ASP.NET Core-specific identity wiring should stay inside adapters, not inside `Cephalon.Abstractions`

Acceptance:

- `Cephalon.Identity` and `Cephalon.Identity.AspNetCore` exist with clear package boundaries
- authorization supports `RBAC`, `ABAC`, and policy-based evaluation through config plus host-agnostic contracts
- ASP.NET Core mapping to `ClaimsPrincipal`, schemes, and endpoint policies stays out of `Cephalon.Abstractions`
- runtime surfaces expose the active identity and authorization mode selection truthfully

Progress:

- `Cephalon.Identity` now exists locally as the host-agnostic identity companion pack with `IdentityRuntimeOptions`, declarative `IdentityPolicyMetadataKeys`, and `AddIdentityAccess(...)`
- the pack now registers a default metadata-driven `IAuthorizationEvaluator` that can enforce low-ceremony role, owner, tenant-boundary, and subject/resource/context attribute rules on top of the shipped authorization-policy contracts
- `identity-access` now projects an `identity-authorization` runtime surface that reports selected authorization modes, contributed policy counts, evaluator presence, and declarative convention support through the shared technology catalog
- `/engine/diagnostics` and `/engine/snapshot` can now advertise stable `Cephalon.Identity` diagnostic event ids `4400-4401` for allow/deny outcomes through the runtime diagnostics catalog
- `Cephalon.Identity.AspNetCore` now exists locally as a separate host adapter with `AddCephalonIdentityAspNetCore(...)`, config-driven `Engine:Identity:AspNetCore` options, and a REST-only `RequireCephalonAuthorization(...)` endpoint helper that maps `ClaimsPrincipal`, route values, and request metadata into the shared Cephalon authorization contracts
- the ASP.NET Core adapter currently returns truthful `401` and `403` API responses and keeps authentication scheme ownership with the consumer host instead of pretending to replace ASP.NET Core authentication
- the REST adapter now also defers `401` and `403` responses to ASP.NET Core challenge/forbid behavior when the host or endpoint metadata already declares authentication schemes, including the low-ceremony `WithCephalonAuthenticationSchemes(...)` endpoint helper, which deepens scheme alignment without pushing authentication concerns into `Cephalon.Abstractions`
- the REST adapter now also respects `AllowAnonymous` endpoint metadata inside protected route groups, so consumer hosts can keep standard ASP.NET Core public-route semantics without bypassing Cephalon on the rest of the group
- direct request-factory coverage now locks custom claim-type selection, subject-id fallback behavior, and optional claim/route/query/header projection flags so config-driven ASP.NET Core identity adapter behavior does not silently drift
- the identity pack now also honors `EnableDefaultEvaluator` and `EnableRuntimeSurface` truthfully, so a disabled built-in evaluator falls back to a deterministic deny path instead of a missing-service failure, and an opt-out runtime surface disappears from the merged technology catalog instead of lingering as misleading metadata
- the ASP.NET Core adapter now also exposes a public `[RequireCephalonAuthorization]` attribute for controller and action boundaries, and it projects an `identity-aspnetcore` runtime surface so operator flows can see protected ASP.NET Core endpoint counts, active policy ids, integration modes, and `AllowAnonymous` overrides without guessing from code
- the ASP.NET Core adapter now also bridges authenticated `ClaimsPrincipal` data into `Cephalon.Audit` automatically when the audit pack is active and the host has not already supplied a custom `IAuditActorAccessor`, which gives low-ceremony audit actor resolution without pulling ASP.NET Core APIs into the host-agnostic audit pack
- package-surface tests, reference-doc tests, component docs, solution wiring, and hosting tests now cover both `Cephalon.Identity` and `Cephalon.Identity.AspNetCore`

### ENG-052 Multi-tenancy and audit companion baseline

Status: in progress
Estimate: 13

Why:

- multi-tenancy plus audit/history are part of the promised consumer-ready surface, not incidental sample code
- tenant resolution, membership, domain mapping, and audit trails need additive contracts that remain configurable and overridable
- this slice needs to align with identity and data surfaces instead of inventing separate side channels

Acceptance:

- `Cephalon.MultiTenancy` and `Cephalon.Audit` exist with clear package boundaries
- tenant context/resolution plus audit and history contracts are configurable, optional, and overridable
- baseline support covers tenant-aware runtime state and audit/history event capture without forcing one storage model
- observability and runtime answers surface the active tenancy and audit configuration truthfully

Progress:

- `Cephalon.MultiTenancy` now exists locally as the first host-agnostic tenancy companion pack with `MultiTenancyRuntimeOptions`, `AddMultiTenancy(...)`, a configuration-driven `ITenantResolver`, and an ambient `ITenantContextAccessor`
- the pack now contributes a truthful `tenant-resolution` runtime surface under `multi-tenancy` plus stable `4500-4502` diagnostics-catalog entries for resolved, defaulted, and missed tenant answers
- the built-in configuration-driven resolver now keeps explicit tenant-id, tenant-key, and host-name misses as misses instead of defaulting into another tenant, which closes the first tenant-bleed risk in the v1 baseline
- the built-in resolver can now also be disabled explicitly through `EnableDefaultResolver`, and the runtime surface reports that state instead of pretending the configuration-driven resolver is always active
- solution wiring, package-surface tests, reference-doc tests, and component docs now cover `Cephalon.MultiTenancy`
- `Cephalon.Audit` now exists locally as the first narrow host-agnostic audit companion pack with `AuditRuntimeOptions`, `AuditMetadataKeys`, `AddAudit(...)`, a default `IAuditRecorder`, a default ambient `IAuditActorAccessor`, and an application-managed in-memory writer baseline
- `Cephalon.Audit` now contributes a dedicated audit-store catalog through `IAuditStoreCatalog`, `/engine/audit-stores`, and `/engine/snapshot` instead of overloading technology surfaces or observability-only answers
- the audit baseline now honors `AuditRuntimeOptions.EnableInMemoryWriter` / `Engine:Audit:EnableInMemoryWriter` end to end across service-collection, ASP.NET Core, and Worker host paths, and the runtime audit-store catalog now stays aligned with that choice instead of pretending the memory-backed store is active when it is not
- the audit pack now also preserves additive consumer audit-store contributions when `AddAudit()` is active, so disabling the built-in in-memory writer only removes `audit-default` and no longer clobbers consumer/runtime-provided audit stores from `/engine/audit-stores` or `/engine/snapshot`
- the audit baseline now ships stable `4600-4601` diagnostics-catalog entries for successful and failed audit-entry writes, plus targeted composition, hosting, package-surface, and reference-doc coverage
- the audit baseline now also gets a truthful low-ceremony actor bridge in ASP.NET Core hosts: when `Cephalon.Identity.AspNetCore` is active, authenticated principal data can populate ambient audit actors automatically, while custom audit actor accessors still remain authoritative and `/engine/technology-surfaces/identity-access` reports whether that bridge is active, merely available, or not configured
- remaining work inside `ENG-052` is audit follow-through beyond the narrow recording baseline, not the existence of the companion-pack and catalog foundation itself

### ENG-053 CLI, scaffolding, template-pack, and sample alignment for phase 8

Status: in progress
Estimate: 13

Why:

- phase 8 is not a real adoption surface until `cephalon new`, scaffold plans, template starters, and samples emit the same story
- the shipped scaffolds already imply clean and CQRS-friendly conventions and should be extended instead of replaced
- generation should follow the frozen ids, config sections, and package names instead of driving them
- phase 8 only delivers the intended low-ceremony value if generated hosts and starter assets remove repetitive setup and let consumer apps begin closer to business logic

Acceptance:

- `Cephalon.Cli`, `Cephalon.Scaffolding`, `Cephalon.TemplatePack`, and at least one golden-path sample align with the phase-8 config and package contract
- generated hosts emit the structured `Engine` sections plus the right package hints and folder conventions for clean, CQRS, and DDD-flavored apps
- test projects carry TDD and BDD-friendly starter conventions without moving those ideas into engine runtime contracts
- scaffold, runtime, template, and sample semantics stay aligned through tests and docs
- generated starters keep ceremony low by centralizing common Cephalon wiring, avoiding repeated bootstrap code, and leaving the remaining hand-written code focused on domain and business behavior

Progress:

- `Cephalon.Scaffolding` now emits canonical phase-8 ids in generated `Engine` settings instead of older display-name strings, and generated hosts now carry structured `Engine:Data`, `Engine:Identity`, `Engine:Tenancy`, `Engine:Audit`, and `Engine:Messaging` sections
- phase-8-aware scaffold package hints now add the low-ceremony companion-pack baseline for `Cephalon.Data`, `Cephalon.Ids.Sfid`, `Cephalon.Eventing`, `Cephalon.Eventing.Wolverine`, `Cephalon.Identity`, `Cephalon.Identity.AspNetCore`, `Cephalon.MultiTenancy`, and `Cephalon.Audit` when the selected patterns and technologies require them
- generated `Program.cs` output now centralizes the common phase-8 host wiring through `builder.AddCephalon(engine => ...)`, optional `builder.AddCephalonIdentityAspNetCore()`, and additive pack registrations so consumer apps do not need to hand-write the same bootstrap code before they reach business logic
- `Cephalon.TemplatePack` starter apps now also carry canonical ids, structured phase-8 sections, and a narrow low-ceremony `Sfid` plus `Audit` baseline so `dotnet new` does not drift behind `cephalon new`
- adoption-quality starter samples now mirror that same narrow `Sfid` plus `Audit` baseline through canonical settings and additive host wiring, and hosting coverage now locks those sample app-model answers through `/engine/app-model`
- generated test projects now start with `Architecture/CompositionSmokeTests.cs` plus per-feature `Features/*BehaviorSpecifications.cs` placeholders so TDD/BDD-friendly starter conventions land through scaffolding and CLI output without turning testing style into a runtime contract
- remaining work inside `ENG-053` is broader phase-8 sample/template documentation follow-through and any further starter polish, not the existence of the aligned low-ceremony baseline or the TDD/BDD-friendly test harness itself

### ENG-054 Provider-family and hybrid-runtime follow-through

Status: in progress
Estimate: 80

Why:

- the requested feature wave eventually reaches vector, document, graph, search, time-series, ledger, service-mesh, hybrid-cloud, and serverless follow-through, but the contract is not proven yet
- shipping provider breadth before the golden path would over-claim support and destabilize the core
- this work should stay explicit and adoption-driven after the initial phase-8 baseline lands

Acceptance:

- non-relational provider families are split into explicit companion packages with clear scope and no leakage back into `Cephalon.Engine` or `Cephalon.Abstractions`
- `HybridCloudRuntime`, `ServiceMeshIntegration`, and `ServerlessHosting` remain additive technology follow-through instead of new blueprints or engine-core branches
- docs, runtime surfaces, and observability stay truthful about which provider families and deployment runtimes are actually shipped
- expansion work starts only after the relational-first phase-8 golden path is complete

Progress:

- `Cephalon.Data.MongoDB` now exists as the first non-relational data companion pack: registers `IMongoClient` + `IMongoDatabase`, delivers `IOutbox` and `IInbox` backed by MongoDB collections with idempotent staging via unique index on `MessageId`, publishes `data.mongodb` / `data.document-store` / `data.outbox.mongodb` / `data.inbox.mongodb` capabilities, and projects `outbox-producers` and `inbox-stores` through the `event-driven-integration` technology surface — no changes to `Cephalon.Engine` or `Cephalon.Abstractions`
- `Cephalon.EventSourcing.MongoDB` now exists as the first non-relational event-store provider: implements `IEventStore` against a `event_streams` collection with optimistic concurrency enforced by a compound unique index on `(StreamId, StreamVersion)`, exposes `GetVersionAsync` / `AppendAsync` / `ReadStreamAsync`, serializes through `System.Text.Json`, and round-trips event types via `AssemblyQualifiedName`
- both MongoDB packages ship 12 integration tests via EphemeralMongo in-process runner and full component-guide docs
- companion-pack pattern proven for document-oriented stores — commit `f94dc28` · 599 tests green
- `Cephalon.Data.Redis` now exists as the second non-relational data companion pack: registers `IConnectionMultiplexer` via StackExchange.Redis, delivers `IOutbox` backed by Redis Hash + Sorted Set (idempotent staging via `KeyNotExists` transaction condition) and `IInbox` backed by a Redis Set (naturally idempotent `SADD`), publishes `data.redis` / `data.key-value-store` / `data.outbox.redis` / `data.inbox.redis` capabilities, and projects `outbox-producers` and `inbox-stores` through the `event-driven-integration` technology surface — no changes to `Cephalon.Engine` or `Cephalon.Abstractions`
- `Cephalon.EventSourcing.Redis` now exists as the second non-relational event-store provider: implements `IEventStore` against Redis Streams (`XADD`/`XRANGE`), stores `StreamVersion`, `EventType`, `Payload`, `OccurredAtUtc`, and `AppendedAtUtc` as stream entry fields, performs optimistic pre-insert version check (known limitation: no atomic test-and-set), serializes through `System.Text.Json`, and round-trips event types via `AssemblyQualifiedName`
- both Redis packages ship 8 composition tests (no live Redis required — `abortConnect=false` enables lazy-connect service resolution) and full component-guide docs
- companion-pack pattern proven for key-value stores — Sprint 26 · StackExchange.Redis 2.8.16 added to CPM

### ENG-055 Phase 8 validation, benchmark, and runtime-truth matrix

Status: in progress
Estimate: 8

Why:

- AGENTS requires new runtime features to stay observable, testable, benchmarkable, and truthful through runtime introspection
- the current phase-8 plan introduces many new contracts and companion packages, but without a dedicated validation lane the repo could accidentally claim support before benchmark, diagnostics, or test coverage catches up
- the fastest path is not enough on its own; Cephalon needs a repeatable quality bar that survives future package expansion

Acceptance:

- phase-8 features have an explicit validation matrix that covers config validation, runtime introspection, diagnostics, benchmark guardrails, and representative happy-path plus failure-path tests
- benchmark additions or updates land where hot paths or composition/runtime/scaffolding risks materially change
- runtime snapshot, diagnostics, runtime-story, and package/technology surfaces stay truthful for every shipped phase-8 slice
- release and repo validation guidance stays aligned with the new quality bar

Progress:

- `scripts/validate-phase8-conventions.ps1` now exists as the focused validation replay for the shipped phase-8 baseline, covering settings/profile truth, host-agnostic contracts, runtime surfaces, relational data and `Sfid`, eventing and Wolverine, identity/tenancy/audit, ASP.NET Core adapter follow-through, low-ceremony starter output, and package/reference-doc/documentation truth
- `scripts/validate-release.ps1` now also runs that focused phase-8 suite by default, and the release-validation guidance now documents `-SkipPhase8Conventions` as the explicit escape hatch instead of relying on the wider repo test pass alone
- adoption docs and package readmes now keep the phase-8 starter-test harness truthful by explicitly calling out `Architecture/CompositionSmokeTests.cs` plus per-feature `Features/*BehaviorSpecifications.cs`, and documentation coverage tests now guard that story
- `Cephalon.Benchmarks` now also carries explicit phase-8 composition, runtime-lifecycle, and scaffolding scenarios alongside the earlier baseline paths, and the guardrail catalog has been refreshed against the current BenchmarkDotNet output so `--validate-guardrails` can police both the existing baseline and the shipped low-ceremony phase-8 path truthfully
- the repo-native release-validation entry point now passes again with the phase-8 benchmark filters after aligning reference-module and signed-package `minimumEngineVersion` baselines with the current repo package version, scoping discovery-based tests away from invalid phase-8 negative fixtures plus Entity Framework-only test modules, and making the runtime-route hosting test declare a real event subscription before it expects `eventing.subscriptions`

### ENG-056 Phase 8 docs, XML comments, component-guide, and reference-doc alignment

Status: in progress
Estimate: 8

Why:

- the repo treats hand-authored docs plus public XML comments as part of the product surface, not post-hoc extras
- phase 8 adds new public contracts and companion-package surfaces that will be harder to understand and safer to misuse unless docs and IntelliSense stay aligned from the start
- adoption quality depends on docs, XML comments, component guides, and reference-doc publishing telling the same truth as the code

Acceptance:

- new public phase-8 contracts ship with XML comments suitable for the supported reference-doc pipeline
- relevant component docs under `docs/components/` and hand-authored adoption guidance are updated for the shipped phase-8 slices
- `Cephalon.ReferenceDocs`, hosted reference-doc guidance, and documentation indexes stay aligned if the supported public API surface changes
- checked-in `docs/reference/` output stays aligned with the current `Cephalon.ReferenceDocs` generator through a repo-native drift guard instead of manual spot checks alone
- docs stay explicit about what is shipped now versus what remains later, especially around event sourcing, provider breadth, hybrid runtime, service mesh, and serverless claims

### ENG-057 Event-sourcing follow-through baseline

Status: later
Estimate: 21

Why:

- `EventSourcing` belongs in the phase-8 taxonomy, but the repo does not yet have a truthful delivery baseline for event-store persistence, stream replay, projection rebuilds, or operational answers around those flows
- folding event sourcing into the early relational-first golden path would risk over-claiming a harder storage and lifecycle model before the underlying CQRS, outbox, and runtime contracts settle
- keeping this explicit protects the planning truth and leaves room for a stronger, adoption-driven design later

Acceptance:

- event-store persistence, stream versioning, replay, projection rebuild, and operational/runtime-introspection answers are modelled explicitly instead of implied by taxonomy alone
- any shipped event-sourcing implementation stays additive through companion packages and does not force a storage model into `Cephalon.Engine` or `Cephalon.Abstractions`
- docs, runtime surfaces, and observability clearly distinguish shipped event-driven integration from shipped event-sourcing support
- work starts only after the relational-first phase-8 golden path is complete and its contracts are stable

## Sprint history and next 4 sprints

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
- ENG-029 Kubernetes collector wiring and hosted Kubernetes defaults on top of the OTLP baseline

### Sprint 2

- operational hardening follow-through after the shipped health, telemetry, and CI baselines
- shipped OpenTelemetry companion packaging plus Cassandra contact-point health plus ClickHouse analytics health plus Consul control-plane health plus Elasticsearch cluster health plus HTTP external API, Kafka broker metadata, Memcached cache, MongoDB document database, MQTT broker, MySQL database, NATS broker, Neo4j graph database, OpenSearch cluster health, Oracle database, Postgres database, RabbitMQ broker, Redis/cache, and SQL Server dependency-health companions, together with the shared diagnostics/event-id catalog for active packages, opt-in ASP.NET Core request/response logging with trace correlation, and explicit release-validation guidance for health/export conventions

### Sprint 3

- shipped first execution-graph contract baseline plus hosted-execution follow-through under `ENG-013`
- shipped package distribution and provenance follow-through beyond the original package-loading baseline
- shipped repo-wide XML-comment hygiene for test harnesses through explicit xUnit visibility rules plus tooling-backed guards under `ENG-028`
- ENG-029 self-hosted OTLP collector/runtime-default follow-through plus the shipped Azure Monitor, AWS, GCP, Huawei Cloud, Alibaba Cloud, Red Hat OpenShift, DigitalOcean, VMware Tanzu, Kubernetes, Cloudflare/downstream provider authoring guidance, Grafana Cloud, and New Relic slices

### Sprint 4

- ENG-033 cross-platform validation and shell parity baseline
- ENG-034 first-run adoption and environment doctor path

### Sprint 5

- ENG-035 external module package lifecycle prove-out

### Sprint 6

- ENG-036 containerized local runtime and operations baseline

### Sprint 7

- ENG-037 generated app local package-feed bootstrap baseline

### Sprint 8

- ENG-038 generated app published-output and deployment baseline

### Sprint 9

- ENG-039 generated app Linux systemd deployment baseline

### Sprint 10

- ENG-040 generated app Windows Service deployment baseline

### Sprint 11

- ENG-041 generated app IIS deployment baseline

### Sprint 12

- ENG-042 generated app Azure App Service deployment baseline

### Sprint 13

- ENG-043 generated app Azure Container Apps deployment baseline

### Sprint 14

- ENG-044 generated app Kubernetes deployment baseline

### Sprint 15

- ENG-045 generated app container-image publishing baseline

### Sprint 16

- ENG-046 app-model taxonomy and catalog expansion baseline
- ENG-047 structured engine configuration and runtime surface baseline for data, identity, tenancy, audit, and messaging
- ENG-048 host-agnostic data, authorization, tenancy, audit, and id contract baseline

### Sprint 17

- ENG-049 relational Entity Framework CQRS, projections, outbox, and Sfid companion baseline
- ENG-050 eventing runtime uplift and Wolverine companion baseline

### Sprint 18

- ENG-051 identity and authorization companion baseline
- ENG-052 multi-tenancy and audit companion baseline
- ENG-053 CLI, scaffolding, template-pack, and sample alignment for phase 8

### Sprint 19

- ENG-055 phase 8 validation, benchmark, and runtime-truth matrix
- ENG-056 phase 8 docs, XML comments, component-guide, and reference-doc alignment

### Sprint 20

- ENG-058 M1 ABT foundation: IAppBehavior, IBehaviorContext, BehaviorDispatcher, BehaviorExecutionSlot, CompatibilityMatrix, hosting — **Shipped** commit `9d657da` · 499/499 tests

### Sprint 21

- ENG-058 M2 HTTP Transport Pack: 7 HTTP bindings (rest, jsonrpc, graphql, graphql-sse, graphql-ws, sse, ws), LazyTransportBinding — **Shipped** commit `c957966` · 516/516 tests

### Sprint 22

- ENG-058 M3 Messaging Transport Pack: InMemory, RabbitMQ, Kafka bindings; M2 CTS leak fix — **Shipped** commit `9183407` · 527/527 tests

### Sprint 23

- ENG-058 M4 Pattern Execution Strategies: IBehaviorExecutionStrategy, 5 strategies (cqrs, event-driven, saga-step, process-manager, direct), ISagaStateStore, IProcessCheckpointStore, FrozenDictionary registry; IBehaviorContext.CorrelationId; IProcessCompletion — **Shipped** commit `cc2ab0a` · 575/575 tests

### Sprint 24

- ENG-058 M5 Source Generator: Roslyn `IIncrementalGenerator` + `DiagnosticAnalyzer` (`Cephalon.Behaviors.SourceGen`); ABT0010–ABT0013 diagnostics; `ForAttributeWithMetadataName`; emits `BehaviorRegistrationHints.g.cs` — **Shipped** commit `8455b9a` · 584/584 tests
- ENG-058 M6 Runtime Integration: `BehaviorRuntimeContributor` (ITechnologyRuntimeContributor), `IBehaviorAdvisory` system (contributor/catalog/severity), `IBehaviorContext.EventStore` (IEventStore? wiring), `BehaviorDiagnostics` EventId 5100-5109 — **Shipped** commit `62d386c` · 592/592 tests

### Sprint 25

- ENG-054 Phase 10 non-relational provider baseline: `Cephalon.Data.MongoDB` (IOutbox + IInbox backed by MongoDB collections, idempotent staging via unique index, outbox/inbox runtime surface contribution, `data.mongodb` / `data.document-store` capabilities), `Cephalon.EventSourcing.MongoDB` (IEventStore with optimistic concurrency via compound unique index on StreamId+StreamVersion, System.Text.Json serialization, IAsyncEnumerable stream replay), MongoDB.Driver 3.4.0 in CPM, 12 integration tests via EphemeralMongo, full component docs — **Shipped** commit `f94dc28` · 599/599 tests

### Sprint 26

- ENG-054 Redis non-relational provider: `Cephalon.Data.Redis` (IOutbox backed by Redis Hash + Sorted Set with idempotent KeyNotExists transaction condition, IInbox backed by Redis Set with naturally idempotent SADD, outbox/inbox runtime surface contribution, `data.redis` / `data.key-value-store` capabilities), `Cephalon.EventSourcing.Redis` (IEventStore via Redis Streams with XADD/XRANGE, optimistic pre-insert version check, System.Text.Json serialization, IAsyncEnumerable stream replay), StackExchange.Redis 2.8.16 in CPM, 8 composition tests (abortConnect=false, no live Redis required), full component docs — **Shipped** · 607/607 tests

### Sprint 27

- ENG-054 Neo4j graph-store non-relational provider: `Cephalon.Data.Neo4j` (IOutbox + IInbox backed by Neo4j graph nodes, idempotent staging via Cypher MERGE on messageId, `data.neo4j` / `data.graph-store` capabilities), `Cephalon.EventSourcing.Neo4j` (IEventStore with optimistic concurrency via IS NODE KEY constraint on streamId+streamVersion, System.Text.Json serialization, IAsyncEnumerable stream replay), Neo4j.Driver 6.0.0 already in CPM, 8 composition tests (no live Neo4j required — driver connects lazily), full component docs — **Shipped** · 615/615 tests

### Sprint 28

- ENG-054 Cassandra wide-column non-relational provider: `Cephalon.Data.Cassandra` (IOutbox + IInbox backed by Cassandra tables, idempotent staging via LWT `INSERT IF NOT EXISTS`, `data.cassandra` / `data.wide-column-store` capabilities), `Cephalon.EventSourcing.Cassandra` (IEventStore with composite PK on stream_id+stream_version, LWT concurrency detection, System.Text.Json serialization, IAsyncEnumerable stream replay), CassandraCSharpDriver 3.22.0 already in CPM, 8 composition tests (no live Cassandra required — driver connects lazily), full component docs — **Shipped** · 624/624 tests

### Sprint 29

- ENG-054 ClickHouse analytics non-relational provider: `Cephalon.Data.ClickHouse` (IOutbox + IInbox backed by ClickHouse ReplacingMergeTree tables, eventual idempotency via ORDER BY deduplication + FINAL reads, `data.clickhouse` / `data.analytics-store` capabilities), `Cephalon.EventSourcing.ClickHouse` (IEventStore with MergeTree ORDER BY (stream_id, stream_version), application-layer optimistic concurrency, System.Text.Json serialization, IAsyncEnumerable stream replay), ClickHouse.Driver 1.0.2 already in CPM, 8 composition tests (no live ClickHouse required — connection created per-operation), full component docs — **Shipped** · 632/632 tests

