# Cephalon Engine Backlog

Backlog status in this document reflects the repository state as of `April 17, 2026`.

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

Status: done
Estimate: 21
Completed: April 7, 2026

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

Delivered:

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
- `Cephalon.Data.EntityFramework` now also implements `IProjectionContributor` and registers EF-backed projection descriptors when `RegisterProjections` is enabled, plus a `data.projections.entity-framework` capability and a `projections` runtime surface under `data-management` so operators can see active projection infrastructure through `/engine/technology-surfaces` and `/engine/projections`

Follow-up later:

- broader typed-id/documentation examples on top of the shipped `Sfid.EntityFramework` baseline
- fuller subscription/runtime linkage beyond the shipped staged publication and application-managed idempotency baselines

### ENG-050 Eventing runtime uplift and Wolverine companion baseline

Status: done
Estimate: 13
Completed: April 7, 2026

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

Delivered:

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
- `Cephalon.Eventing.Wolverine` now also contributes its own diagnostics convention so `/engine/diagnostics` and the runtime snapshot advertise the stable `4300-4305` Wolverine dispatch-loop event ids alongside the shared eventing diagnostics range
- `Cephalon.Eventing.Wolverine` now also exposes `System.Diagnostics.ActivitySource` (`Cephalon.Eventing.Wolverine.Dispatch`) and `System.Diagnostics.Metrics.Meter` instrumentation for the dispatch loop, with Activity spans per dispatch item (tagged with message_id, event_type, channel_id, dispatch_attempt, correlation_id, tenant_id) and counters for attempts, successes, failures, retries plus a histogram for dispatch duration in milliseconds — enabling OpenTelemetry-instrumented hosts to capture distributed traces and metrics without additional adapter code
- `eventing.subscriptions` still exposes declared subscription descriptors rather than a pack-owned bus runner, while `eventing.subscribe` remains intentionally absent until a real subscription/dispatch runtime exists instead of over-claiming bus behavior
- decision lock: phase 8 will treat `Cephalon.Eventing.Wolverine` as the official first-class adapter path, keep `MassTransit` as the tracked-later candidate once that first path is proven, and leave `MediatR`, `LiteBus`, `NServiceBus`, and `SlimMessageBus` as consumer-owned coexistence choices unless a later bridge or adapter package is explicitly shipped
- coexistence rule: one flow should have one durable-messaging owner, so consumer apps should not layer Cephalon-managed durable messaging semantics and a second bus/runtime on the same publish/consume path
Follow-up later:

- subscription/handler-execution truth beyond the current declarative and application-managed reporting model
- `MassTransit` as tracked-later strategic candidate once the Wolverine path is proven

### ENG-051 Identity and authorization companion baseline

Status: done
Estimate: 13
Completed: April 7, 2026

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

Status: done
Estimate: 13
Completed: April 7, 2026

Why:

- multi-tenancy plus audit/history are part of the promised consumer-ready surface, not incidental sample code
- tenant resolution, membership, domain mapping, and audit trails need additive contracts that remain configurable and overridable
- this slice needs to align with identity and data surfaces instead of inventing separate side channels

Acceptance:

- `Cephalon.MultiTenancy` and `Cephalon.Audit` exist with clear package boundaries
- tenant context/resolution plus audit and history contracts are configurable, optional, and overridable
- baseline support covers tenant-aware runtime state and audit/history event capture without forcing one storage model
- observability and runtime answers surface the active tenancy and audit configuration truthfully

Delivered:

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

Follow-up later:

- audit follow-through beyond the narrow recording baseline: durable persistence providers, query/replay, retention policies
- tenant membership and domain-mapping enrichment beyond the current configuration-driven resolver

### ENG-053 CLI, scaffolding, template-pack, and sample alignment for phase 8

Status: done
Estimate: 13
Completed: April 7, 2026

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

Delivered:

- `Cephalon.Scaffolding` now emits canonical phase-8 ids in generated `Engine` settings instead of older display-name strings, and generated hosts now carry structured `Engine:Data`, `Engine:Identity`, `Engine:Tenancy`, `Engine:Audit`, and `Engine:Messaging` sections
- phase-8-aware scaffold package hints now add the low-ceremony companion-pack baseline for `Cephalon.Data`, `Cephalon.Ids.Sfid`, `Cephalon.Eventing`, `Cephalon.Eventing.Wolverine`, `Cephalon.Identity`, `Cephalon.Identity.AspNetCore`, `Cephalon.MultiTenancy`, and `Cephalon.Audit` when the selected patterns and technologies require them
- generated `Program.cs` output now centralizes the common phase-8 host wiring through `builder.AddCephalon(engine => ...)`, optional `builder.AddCephalonIdentityAspNetCore()`, and additive pack registrations so consumer apps do not need to hand-write the same bootstrap code before they reach business logic
- generated hosts, templates, and the showcase sample now adopt the split `Configurations/Add*.json` plus `Configurations/{group}/{Environment}.json` convention while still leaving `appsettings.json` and `appsettings.{Environment}.json` available as normal host-level overrides
- `Cephalon.TemplatePack` starter apps now also carry canonical ids, structured phase-8 sections, and a narrow low-ceremony `Sfid` plus `Audit` baseline so `dotnet new` does not drift behind `cephalon new`
- adoption-quality starter samples now mirror that same narrow `Sfid` plus `Audit` baseline through canonical settings and additive host wiring, and hosting coverage now locks those sample app-model answers through `/engine/app-model`
- generated test projects now start with `Architecture/CompositionSmokeTests.cs` plus per-feature `Features/*BehaviorSpecifications.cs` placeholders so TDD/BDD-friendly starter conventions land through scaffolding and CLI output without turning testing style into a runtime contract

Follow-up later:

- broader phase-8 sample/template documentation follow-through
- further starter polish and additional transport-specific template variations

### ENG-054 Provider-family and hybrid-runtime follow-through

Status: done
Estimate: 80
Completed: April 7, 2026

Why:

- the requested feature wave eventually reaches vector, document, graph, search, time-series, ledger, service-mesh, hybrid-cloud, and serverless follow-through, but the contract is not proven yet
- shipping provider breadth before the golden path would over-claim support and destabilize the core
- this work should stay explicit and adoption-driven after the initial phase-8 baseline lands

Acceptance:

- non-relational provider families are split into explicit companion packages with clear scope and no leakage back into `Cephalon.Engine` or `Cephalon.Abstractions`
- `HybridCloudRuntime`, `ServiceMeshIntegration`, and `ServerlessHosting` remain additive technology follow-through instead of new blueprints or engine-core branches
- docs, runtime surfaces, and observability stay truthful about which provider families and deployment runtimes are actually shipped
- expansion work starts only after the relational-first phase-8 golden path is complete

Delivered:

- **Sprint 25 — MongoDB (document-store)**: `Cephalon.Data.MongoDB` (IOutbox + IInbox backed by MongoDB collections, idempotent staging via unique index on MessageId, `data.mongodb` / `data.document-store` capabilities) + `Cephalon.EventSourcing.MongoDB` (IEventStore with optimistic concurrency via compound unique index on StreamId+StreamVersion) — MongoDB.Driver 3.4.0 in CPM, 12 integration tests via EphemeralMongo — commit `f94dc28` · 599/599 tests
- **Sprint 26 — Redis (key-value-store)**: `Cephalon.Data.Redis` (IOutbox backed by Redis Hash + Sorted Set with KeyNotExists transaction condition, IInbox backed by Redis Set with naturally idempotent SADD, `data.redis` / `data.key-value-store` capabilities) + `Cephalon.EventSourcing.Redis` (IEventStore via Redis Streams XADD/XRANGE, optimistic pre-insert version check) — StackExchange.Redis 2.8.16 in CPM, 8 composition tests — 607/607 tests
- **Sprint 27 — Neo4j (graph-store)**: `Cephalon.Data.Neo4j` (IOutbox + IInbox backed by Neo4j graph nodes, idempotent staging via Cypher MERGE on messageId, `data.neo4j` / `data.graph-store` capabilities) + `Cephalon.EventSourcing.Neo4j` (IEventStore with IS NODE KEY constraint on streamId+streamVersion) — Neo4j.Driver 6.0.0, 8 composition tests — 615/615 tests
- **Sprint 28 — Cassandra (wide-column-store)**: `Cephalon.Data.Cassandra` (IOutbox + IInbox backed by Cassandra tables, idempotent staging via LWT INSERT IF NOT EXISTS, `data.cassandra` / `data.wide-column-store` capabilities) + `Cephalon.EventSourcing.Cassandra` (IEventStore with composite PK on stream_id+stream_version, LWT concurrency detection) — CassandraCSharpDriver 3.22.0, 8 composition tests — 624/624 tests
- **Sprint 29 — ClickHouse (analytics-store)**: `Cephalon.Data.ClickHouse` (IOutbox + IInbox backed by ClickHouse ReplacingMergeTree tables, eventual idempotency via ORDER BY deduplication + FINAL reads, `data.clickhouse` / `data.analytics-store` capabilities) + `Cephalon.EventSourcing.ClickHouse` (IEventStore with MergeTree ORDER BY, application-layer optimistic concurrency) — ClickHouse.Driver 1.0.2, 8 composition tests — 632/632 tests
- **Sprint 30 — Elasticsearch + OpenSearch (search-store)**: `Cephalon.Data.Elasticsearch` (IOutbox + IInbox backed by Elasticsearch indices, op_type=create idempotency with 409 swallow, `data.elasticsearch` / `data.search-store` capabilities) + `Cephalon.EventSourcing.Elasticsearch` (IEventStore with compound document id `{streamId}#{streamVersion}`) + `Cephalon.Data.OpenSearch` (OpenSearch.Client mirror, `data.opensearch` / `data.search-store` capabilities) + `Cephalon.EventSourcing.OpenSearch` (OpenSearch event store mirror) — Elastic.Clients.Elasticsearch 8.17.0 + OpenSearch.Client 1.8.0, 8 composition tests — 640/640 tests
- **Sprint 31 — Qdrant + NATS (vector-store + ledger-store)**: `Cephalon.Data.Qdrant` (IOutbox + IInbox backed by Qdrant vector collections with 1D dummy vectors and payload-field storage, deterministic UUID from message ID via SHA-256, `data.qdrant` / `data.vector-store` capabilities) + `Cephalon.EventSourcing.Qdrant` (IEventStore with compound point-ID hash, ScrollAsync-based replay) + `Cephalon.Data.Nats` (IOutbox + IInbox backed by NATS JetStream KV, NatsKVCreateException idempotency, `data.nats` / `data.ledger-store` capabilities) + `Cephalon.EventSourcing.Nats` (IEventStore via JetStream KV with zero-padded keys `{streamId}/{version:D20}`) — Qdrant.Client 1.17.0 + NATS.Net 2.7.3, 8 composition tests — 648/648 tests
- all 9 non-relational provider families shipped as companion packs with no changes to `Cephalon.Engine` or `Cephalon.Abstractions`
- each provider family delivers both `Cephalon.Data.{Provider}` and `Cephalon.EventSourcing.{Provider}` packages
- full component-guide docs for all 18 packages (9 data + 9 event-sourcing)
- `HybridCloudRuntime`, `ServiceMeshIntegration`, and `ServerlessHosting` remain tracked-later until explicit adoption cases

Follow-up later:

- service-mesh and serverless runtime follow-through remain `later` until explicit adoption cases
- hybrid-cloud deployment runtime beyond the shipped provider breadth

### ENG-055 Phase 8 validation, benchmark, and runtime-truth matrix

Status: done
Estimate: 8
Completed: April 7, 2026

Why:

- AGENTS requires new runtime features to stay observable, testable, benchmarkable, and truthful through runtime introspection
- the current phase-8 plan introduces many new contracts and companion packages, but without a dedicated validation lane the repo could accidentally claim support before benchmark, diagnostics, or test coverage catches up
- the fastest path is not enough on its own; Cephalon needs a repeatable quality bar that survives future package expansion

Acceptance:

- phase-8 features have an explicit validation matrix that covers config validation, runtime introspection, diagnostics, benchmark guardrails, and representative happy-path plus failure-path tests
- benchmark additions or updates land where hot paths or composition/runtime/scaffolding risks materially change
- runtime snapshot, diagnostics, runtime-story, and package/technology surfaces stay truthful for every shipped phase-8 slice
- release and repo validation guidance stays aligned with the new quality bar

Delivered:

- `scripts/validate-phase8-conventions.ps1` now exists as the focused validation replay for the shipped phase-8 baseline, covering settings/profile truth, host-agnostic contracts, runtime surfaces, relational data and `Sfid`, eventing and Wolverine, identity/tenancy/audit, ASP.NET Core adapter follow-through, low-ceremony starter output, and package/reference-doc/documentation truth
- `scripts/validate-release.ps1` now also runs that focused phase-8 suite by default, and the release-validation guidance now documents `-SkipPhase8Conventions` as the explicit escape hatch instead of relying on the wider repo test pass alone
- adoption docs and package readmes now keep the phase-8 starter-test harness truthful by explicitly calling out `Architecture/CompositionSmokeTests.cs` plus per-feature `Features/*BehaviorSpecifications.cs`, and documentation coverage tests now guard that story
- `Cephalon.Benchmarks` now also carries explicit phase-8 composition, runtime-lifecycle, and scaffolding scenarios alongside the earlier baseline paths, and the guardrail catalog has been refreshed against the current BenchmarkDotNet output so `--validate-guardrails` can police both the existing baseline and the shipped low-ceremony phase-8 path truthfully
- the repo-native release-validation entry point now passes again with the phase-8 benchmark filters after aligning reference-module and signed-package `minimumEngineVersion` baselines with the current repo package version, scoping discovery-based tests away from invalid phase-8 negative fixtures plus Entity Framework-only test modules, and making the runtime-route hosting test declare a real event subscription before it expects `eventing.subscriptions`

Follow-up later:

- benchmark expansion for runtime hot paths: data layer dispatch, event sourcing, behavior dispatch, authorization, multi-tenancy, outbox, transports (tracked under ENG-059)

### ENG-056 Phase 8 docs, XML comments, component-guide, and reference-doc alignment

Status: done
Estimate: 8
Completed: April 5, 2026

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

Delivered:

- the checked-in `docs/reference/` bundle has been regenerated for the current phase-8 assembly set
- the tooling test lane now guards bundle drift by comparing the checked-in output against the current `Cephalon.ReferenceDocs` generator after normalizing volatile timestamps
- the top-level adoption docs plus blueprint sample READMEs now describe the shipped phase-8 starter baseline truthfully
- component docs for all phase-8 companion packs (data, eventing, identity, multi-tenancy, audit, Sfid) are published

### ENG-057 Event-sourcing follow-through baseline

Status: done
Estimate: 21
Completed: April 5, 2026

Why:

- `EventSourcing` belongs in the phase-8 taxonomy, but the repo does not yet have a truthful delivery baseline for event-store persistence, stream replay, projection rebuilds, or operational answers around those flows
- folding event sourcing into the early relational-first golden path would risk over-claiming a harder storage and lifecycle model before the underlying CQRS, outbox, and runtime contracts settle
- keeping this explicit protects the planning truth and leaves room for a stronger, adoption-driven design later

Acceptance:

- event-store persistence, stream versioning, replay, projection rebuild, and operational/runtime-introspection answers are modelled explicitly instead of implied by taxonomy alone
- any shipped event-sourcing implementation stays additive through companion packages and does not force a storage model into `Cephalon.Engine` or `Cephalon.Abstractions`
- docs, runtime surfaces, and observability clearly distinguish shipped event-driven integration from shipped event-sourcing support
- work starts only after the relational-first phase-8 golden path is complete and its contracts are stable

Delivered:

- `Cephalon.EventSourcing` now exists as the runtime-neutral event-sourcing contract package with `IEventStore` (GetVersionAsync, AppendAsync, ReadStreamAsync), `IDomainEvent`, `EventStreamConcurrencyException`, and System.Text.Json serialization conventions
- `Cephalon.EventSourcing.EntityFramework` now exists as the relational-first event-store provider with optimistic concurrency via unique constraint on (StreamId, StreamVersion), `IAsyncEnumerable` stream replay, and `AddCephalonEntityFrameworkEventSourcing()` builder extension
- 9 additional non-relational event-store providers shipped through ENG-054: MongoDB, Redis, Neo4j, Cassandra, ClickHouse, Elasticsearch, OpenSearch, Qdrant, and NATS — each implementing `IEventStore` with provider-appropriate concurrency enforcement and stream replay
- all event-sourcing implementations stay in companion packages with no changes to `Cephalon.Engine` or `Cephalon.Abstractions`
- `IBehaviorContext.EventStore` wiring in the ABT foundation (ENG-058 M6) connects behaviors to the active event-store provider
- full component-guide docs for all 10 event-store providers

### ENG-059 Runtime hot-path benchmark expansion

Status: done
Estimate: 13
Completed: April 7, 2026

Why:

- the current benchmark suite covers composition, runtime lifecycle, scaffolding, and HTTP logging (~10 benchmarks across 4 classes), but runtime hot paths that execute on every request remain unmeasured
- data layer dispatch (`IReadStore`/`IWriteStore`), event sourcing (`IEventStore` append/read/hydrate), behavior dispatch (`BehaviorDispatcher`), authorization evaluation, multi-tenancy resolution, and outbox staging have zero benchmark coverage
- without baseline measurements and guardrails for these paths, performance regressions in the most frequently executed code will go undetected
- the comprehensive audit on April 7, 2026 identified ~25-29% critical-path coverage as the primary production-readiness gap

Acceptance:

- benchmark coverage reaches critical runtime hot paths: data layer dispatch, event sourcing, behavior dispatch pipeline
- benchmark coverage reaches high-priority paths: authorization evaluation, multi-tenancy resolution, outbox staging, transport handlers
- guardrail thresholds defined for all new benchmark scenarios
- `scripts/validate-release.ps1` validates the expanded guardrail catalog

Delivered:

- `HotPath/DataDispatchBenchmarks.cs`: IReadStore query dispatch (DispatchQuery), IWriteStore void command dispatch (DispatchCommand), result-returning command dispatch (DispatchCommandWithResult) — 8192 ops/iteration, warm-cache steady-state measurement
- `HotPath/BehaviorDispatchBenchmarks.cs`: BehaviorDispatcher.DispatchAsync with frozen-dictionary lookup and compiled delegate invocation (DispatchBehavior) — 8192 ops/iteration with stub catalog/registry
- `HotPath/AuthorizationEvaluationBenchmarks.cs`: MetadataDrivenAuthorizationEvaluator RBAC allow path (EvaluateRbacAllow) and deny path (EvaluateRbacDeny) — 8192 ops/iteration with policy module contributing RBAC policies
- `HotPath/TenantResolutionBenchmarks.cs`: ConfiguredTenantResolver by explicit tenant id (ResolveByTenantId), hostname domain matching (ResolveByHostName), and default-tenant fallback (ResolveDefaultTenant) — 8192 ops/iteration with 3-tenant directory
- `HotPath/EventSourcingBenchmarks.cs`: in-memory IEventStore single-event append (AppendSingleEvent), 100-event stream read (ReadStream), version lookup (GetStreamVersion) — 4096 ops/iteration with in-memory event store
- `HotPath/OutboxStagingBenchmarks.cs`: in-memory IOutbox enqueue staging (StageOutboxMessage) — 4096 ops/iteration with in-memory outbox
- `Support/BenchmarkHotPathTypes.cs`: data layer stubs (BenchmarkQuery/Command/ResultCommand + handlers), behavior stubs (EchoBenchmarkBehavior, StubBehaviorContext/Catalog/TypeRegistry), authorization policy module (BenchmarkAuthorizationPolicyModule), InMemoryBenchmarkEventStore, InMemoryBenchmarkOutbox, BenchmarkDomainEvent
- guardrail catalog expanded from 10 to 23 entries covering all new hot-path benchmarks
- `GuardrailValidatorTests` updated for 23-entry catalog
- benchmark project now references `Cephalon.Behaviors` for behavior dispatch measurement

Follow-up later:

- transport handler benchmarks — requires HTTP test infrastructure per transport (partially covered by existing AspNetCoreRequestLoggingBenchmarks)

### ENG-060 Engine-owned database topology and runtime catalog baseline

Status: done
Estimate: 8

Why:

- the current `Engine:Data` section is intentionally logical and too shallow to own physical database roles, migration targeting, or durable history routing
- the shipped provider-pack baseline is strong enough that topology drift is now the next real risk: read/write, outbox, and history layouts can become sample- or pack-specific instead of a stable engine contract
- Cephalon differentiates most when runtime topology is introspectable and configuration-driven across companion packs, not when every provider invents its own host section

Acceptance:

- `Engine:Databases` exists as the engine-owned physical-topology contract separate from the existing logical `Engine:Data` section
- named database roles such as `Write`, `Read`, and `History` can be validated and surfaced through runtime introspection without leaking EF Core or ASP.NET Core specifics into `Cephalon.Abstractions`
- outbox and audit-history follow-through can target a named role instead of duplicating provider/connection settings ad hoc
- `/engine/snapshot` and a dedicated database catalog answer the active role, provider-family, and topology truthfully

Delivered so far:

- `Engine:Databases` now exists as the engine-owned physical-topology contract with `Runtime`, `Write`, `Read`, `Outbox`, `History`, and nested `Migrations`
- the contract now projects into `EngineSettings`, `AppProfile.Databases`, `/engine/databases`, `/engine/app-model`, and `/engine/snapshot`
- the engine now also ships an additive `IDatabaseRoleCatalog` so `/engine/database-roles` and `snapshot.DatabaseRoles` expose requested versus resolved roles, `UseRole` truth, consumers, co-location, and additive metadata over the same topology contract
- the first validation baseline now covers role-pattern alignment, migration-target validity, and mutually exclusive named versus inline connection settings
- the contract now also supports narrow dependent role references through `UseRole` so `Outbox` and `History` can explicitly reuse the concrete `write` role while layering local schema/runtime overrides

Remaining follow-through inside `ENG-060`:

- broaden the current one-step `UseRole -> write` contract only when provider packs and runtime surfaces can keep requested versus resolved roles truthful
- deepen the runtime answer beyond the current role catalog when provider packs begin contributing richer per-role metadata
- keep the contract aligned across docs, templates, and samples as the provider follow-through lands

Planned follow-through:

- add engine-owned configuration types and validation for role-based database topology
- keep provider-family standards intact inside the topology model instead of flattening every store into one fake universal connection shape
- keep the runtime role catalog additive and host-agnostic instead of making provider packs or hosts invent competing topology answers
- keep `Engine:Data` focused on logical selection (`Provider`, `ReadWriteSplit`, `Outbox`, `Ids`) while `Engine:Databases` owns physical deployment detail

### ENG-061 Role-aware relational runtime and migration orchestration baseline

Status: done
Estimate: 13

Why:

- `Cephalon.Data.EntityFramework` currently proves one honest relational path, but it still relies on host-owned `DbContext` registration instead of an engine-owned role topology
- migration policy is still mostly a host concern, which keeps production deployment guidance, startup apply behavior, and operator truth spread across samples instead of the engine product surface
- mandatory `ReadDbContextBase` / `WriteDbContextBase` inheritance would be a weaker engine contract than role-aware registration helpers plus optional shared schema slices and interceptors

Acceptance:

- `Cephalon.Data.EntityFramework` consumes engine-owned database roles instead of inventing a second physical-topology config model
- relational hosts can keep one shared `DbContext` or split read/write/history contexts while still aligning with the same role contract
- migration configuration can target named roles, startup apply remains explicit, and deploy-time bundles or scripts become the documented production path
- runtime answers expose active relational role wiring, outbox routing, and migration targeting truthfully

Delivered so far:

- `Cephalon.Data.EntityFramework` now ships topology-aware `AddEntityFrameworkData(...)` overloads that resolve the engine-owned `write` and optional `read` roles directly from `Engine:Databases`
- the Entity Framework pack now publishes role and migration-policy metadata through the `data-management/database-roles` runtime surface
- `Engine:Databases:Migrations:ApplyOnStartup` now activates a generic-host hosted service inside `Cephalon.Data.EntityFramework` that applies startup schema creation or migrations for the registered `write` and optional `read` `DbContext` roles
- the same migration-registration primitives now also back truthful `history`-role execution when a companion pack registers a dedicated history `DbContext`, which is how `Cephalon.Audit.EntityFramework` plugs into the baseline
- the same baseline now resolves explicit dependent role references for `outbox` and `history`, and the runtime surface reports requested versus resolved roles when those references are active
- the showcase sample now demonstrates configured `WriteDb`, `ReadDb`, and `HistoryDb` root roles for Docker-backed runs while using an explicit `Outbox -> write` role reference instead of duplicating write connection settings

Remaining follow-through inside `ENG-061`:

- add dedicated outbox role execution only when a companion pack can back that role with truthful `DbContext` ownership
- keep marker interfaces, model-builder extensions, and interceptors as the preferred reusable primitives
- treat convenience `DbContext` base classes as optional later DX helpers rather than the primary contract
- add CLI, docs, and sample guidance for separate migrations projects plus bundle/script-first production deployment when multiple relational `DbContext` models share one physical database

### ENG-062 Durable audit-history provider baseline

Status: done
Estimate: 8

Why:

- the current `Cephalon.Audit` baseline is intentionally truthful, but it only solves low-ceremony recording and in-memory storage
- audit history, retention, and role-aware persistence need a durable baseline before Cephalon can claim a real operational story around tenant-aware change history
- durable history should not pull storage assumptions back into `Cephalon.Audit`; it should stay additive and configurable through the same engine-owned topology contract used by data and outbox follow-through

Acceptance:

- durable audit history can be turned on or off through `Engine:Audit` without changing module code
- the first durable history path targets a named database role instead of hardcoding its own connection model
- runtime audit-store answers expose whether history is in-memory only, durable, or disabled
- the baseline keeps replay/export follow-through explicit without pretending those surfaces already exist

Delivered:

- `Cephalon.Audit.EntityFramework` now ships as the first durable audit-history provider pack on the relational golden path
- `Engine:Audit:History` now projects into `EngineSettings`, `AppProfile.Audit`, `/engine/app-model`, and `/engine/snapshot`
- `IAuditStoreRuntimeContributor` now lets additive provider packs publish durable audit-store descriptors without widening `Cephalon.Audit` into a mandatory storage abstraction
- durable audit history now targets a named database role, defaults to `history`, can be redirected through `Engine:Audit:History:DatabaseRole`, and publishes its runtime truth through `/engine/audit-stores` and `/engine/snapshot`
- durable audit history now also consumes the engine-owned `UseRole` contract when the selected database role is a dependent alias, and audit-store runtime metadata now exposes requested versus resolved roles
- the showcase sample now records durable audit history through the new provider pack while keeping dedicated configured `WriteDb`, `ReadDb`, and `HistoryDb` roles for Docker-backed runs plus a truthful zero-setup fallback outside Docker
- the same baseline now includes `Engine:Audit:History:Retention`, the first engine-owned retention pass, `IAuditHistoryReader`, `/engine/audit-history`, and showcase-facing audit-history read endpoints

Remaining follow-through inside `ENG-062`:

- add replay follow-through and richer export formats on top of the shipped durable write, read, export, and retention path
- add non-relational audit-history providers only when they can stay truthful and additive
- keep ASP.NET Core actor bridging and tenant context additive without turning the audit pack into a host-specific storage abstraction

### ENG-063 Durable audit-history query/read and retention operator surface

Status: done
Estimate: 5

Why:

- the first durable audit-history baseline proved write-path persistence, but operators and sample consumers still lacked a truthful read/query surface
- retention policy also needed to become a real engine-owned contract instead of a doc-only promise
- Cephalon needs one host-agnostic reader contract that durable providers can implement without forcing REST or host APIs back into `Cephalon.Audit`

Acceptance:

- durable provider packs can expose filtered, paged reads through a host-agnostic reader contract
- ASP.NET Core hosts expose operator-facing audit-history routes only when a durable reader is active
- durable audit-history retention validates and projects through `EngineSettings`, `AppProfile`, and runtime metadata truthfully
- the showcase sample demonstrates public audit-history routes without pretending every host must expose them

Delivered:

- `Cephalon.Abstractions` now exposes `IAuditHistoryReader`, `AuditHistoryQuery`, `AuditHistoryQueryResult`, and `AuditHistoryEntry`
- `Cephalon.Audit.EntityFramework` now ships a filtered-page reader plus retention hosted service on top of the durable write path
- `/engine/audit-history` and `/engine/audit-history/{auditEntryId}` now expose operator-facing reads when a durable reader is registered
- the showcase sample now publishes `/api/v1/showcase/audit/history` and `/api/v1/showcase/audit/history/{auditEntryId}` through a dedicated module-owned REST surface
- engine runtime registration now supplies baseline `TimeProvider` and `ILogger<T>` services so timer-driven or logged companion services can activate cleanly in code-first hosts and tests

Remaining follow-through inside `ENG-063`:

- add replay workflows over durable history without turning the reader contract into a report engine
- add richer operator filters or aggregation endpoints only when they stay provider-truthful and benchmarkable

### ENG-064 Durable audit-history export baseline

Status: done
Estimate: 5

Why:

- the shipped durable audit-history reader closed the write-only gap, but operators and samples still lacked a truthful export surface for offline analysis, incident handoff, or controlled data movement
- Cephalon needed one additive export contract that durable provider packs can implement without turning `Cephalon.Audit` into a report engine or locking the platform to one host shape
- the first export slice needed to stay narrow, benchmarkable, and replay-friendly without prematurely claiming full replay semantics

Acceptance:

- durable provider packs can expose a bounded export stream through a host-agnostic exporter contract
- ASP.NET Core hosts expose operator-facing audit-history export routes only when export is explicitly enabled
- the showcase sample documents and exercises a public audit-history export endpoint over a real durable provider path
- runtime metadata and app-model projection expose whether export is enabled and how many entries one export may stream

Delivered:

- `Cephalon.Abstractions` now exposes `IAuditHistoryExporter`, `AuditHistoryExportRequest`, and `AuditHistoryExportSelection`
- `Engine:Audit:History:Export` now projects into `EngineSettings`, `AppProfile.Audit`, `/engine/app-model`, and `/engine/snapshot`
- `Cephalon.Audit.EntityFramework` now ships the first bounded export baseline and publishes truthful export metadata through `/engine/audit-stores`
- `/engine/audit-history/export` now exposes operator-facing NDJSON export when a durable exporter is registered and export is enabled
- the showcase sample now publishes `/api/v1/showcase/audit/history/export` through a dedicated module-owned REST surface
- `Cephalon.AspNetCore` now ships `AuditHistoryExportHttpResponseExtensions` so application modules can reuse the NDJSON response wiring instead of rewriting it

Remaining follow-through inside `ENG-064`:

- add replay flows on top of the shipped export baseline without conflating replay with reporting
- add richer export formats such as CSV or provider-native batch handoff only when they stay truthful and benchmarkable
- add non-relational export-capable audit-history providers only when they can implement the same bounded contract honestly

### ENG-065 Truthful dependent database-role reference baseline

Status: done
Estimate: 5

Why:

- the first `Engine:Databases` baseline could describe dependent roles through `UseRole`, but the runtime story still blurred requested versus resolved truth once `Outbox` or `History` reused `write`
- additive provider packs and operator surfaces needed one honest answer for co-located roles before broader role graphs could be considered safely
- the showcase sample needed to prove that one codebase can keep distinct root roles while intentionally aliasing dependent infrastructure roles

Acceptance:

- dependent `Outbox` and `History` roles can explicitly reuse `write` through `UseRole`
- runtime metadata reports requested versus resolved roles truthfully for the first dependent-role baseline
- the showcase sample demonstrates explicit `Outbox -> write` routing while keeping a dedicated configured `HistoryDb` root role for Docker-backed runs

Delivered:

- `Engine:Databases` now supports the narrow `UseRole -> write` baseline for dependent `Outbox` and `History` targets
- `Cephalon.Data.EntityFramework` and `Cephalon.Audit.EntityFramework` now surface requested versus resolved role truth when those references are active
- runtime metadata now reports dependent-role co-location instead of pretending every configured role is always a separate physical target
- the showcase sample now keeps `WriteDb`, `ReadDb`, and `HistoryDb` as explicit configured roots for Docker-backed runs while proving `Outbox -> write` routing end to end

Remaining follow-through inside `ENG-065`:

- broaden role graphs beyond `UseRole -> write` only when runtime surfaces, provider packs, and migration guidance can stay truthful
- add richer operator answers for co-located roles, schema overrides, and provider-family nuance without overfitting the contract to Entity Framework

### ENG-066 Engine-owned database-role catalog and operator surface

Status: done
Estimate: 5

Why:

- `/engine/databases` projected the raw topology contract, but operators and provider packs still lacked one engine-owned answer for resolved runtime roles
- requested versus resolved role truth, role consumers, co-location, and audit-history metadata needed a host-agnostic contract that did not depend on one provider pack's runtime surface
- the broader topology story needed a stable public abstraction before more providers or role-health follow-through could build on it cleanly

Acceptance:

- `Cephalon.Abstractions` exposes a public database-role catalog contract for resolved runtime roles
- `/engine/database-roles`, `/engine/database-roles/{databaseRoleId}`, and `/engine/snapshot` expose the same operator-facing role truth
- the catalog reports requested versus resolved roles, `UseRole`, provider, connection mode, schema, merged runtime tuning, consumers, and co-located role metadata
- durable audit-history metadata can attach to the selected logical role without coupling the role catalog to a specific provider pack

Delivered:

- `IDatabaseRoleCatalog` and `DatabaseRoleDescriptor` now ship in `Cephalon.Abstractions` as the engine-owned runtime catalog contract
- `Cephalon.Engine` now projects resolved database roles into `snapshot.DatabaseRoles` with requested versus resolved role truth, `UseRole` metadata, consumers, co-located roles, and merged runtime tuning
- ASP.NET Core hosts now expose `/engine/database-roles` plus `/engine/database-roles/{databaseRoleId}` as the operator-facing role catalog
- audit-history selection now enriches the selected role with provider, export, and retention metadata so operator answers stay cross-pack and truthful
- the showcase sample and hosting tests now prove the catalog end to end, including `Outbox -> write`, a dedicated configured `HistoryDb` role for Docker-backed runs, zero-setup in-memory fallback outside Docker, and snapshot alignment

Remaining follow-through inside `ENG-066`:

- add live database-role health, migration-execution progress, and richer provider-specific diagnostics only when they can stay truthful and additive
- broaden the catalog beyond the current named roles only when the engine can preserve deterministic ordering, validation, and operator clarity

### ENG-067 Engine-owned database-migration catalog and zero-setup sample follow-through

Status: done
Estimate: 5

Why:

- `Engine:Databases:Migrations` projected requested policy, but operators and hosts still lacked one engine-owned answer for logical migration targets and their runtime state
- provider packs needed a public additive migration catalog contract before more packs could report planned, running, succeeded, failed, or unsupported targets consistently
- the showcase sample still hid those surfaces outside Docker because it skipped Entity Framework pack registration entirely in zero-setup runs

Acceptance:

- `Cephalon.Abstractions` exposes a public database-migration catalog contract for logical migration targets
- `/engine/database-migrations`, `/engine/database-migrations/{databaseMigrationId}`, and `/engine/snapshot` expose the same operator-facing migration truth
- `Cephalon.Data.EntityFramework` contributes migration targets and runtime status through the engine-owned catalog
- the showcase sample keeps database-role, migration, and durable audit-history surfaces active in local/test runs without requiring Docker

Delivered:

- `IDatabaseMigrationCatalog`, `IDatabaseMigrationContributor`, and `DatabaseMigrationDescriptor` now ship in `Cephalon.Abstractions` as the engine-owned migration catalog contract
- `Cephalon.Engine` now projects resolved migration targets into `snapshot.DatabaseMigrations` and ASP.NET Core hosts now expose `/engine/database-migrations` plus `/engine/database-migrations/{databaseMigrationId}`
- `Cephalon.Data.EntityFramework` now contributes planned/running/succeeded/failed migration truth through the engine-owned catalog instead of keeping execution state inside hosted services only
- the showcase sample now always wires Entity Framework data plus durable audit history, uses PostgreSQL in Docker mode, rewrites `Write` / `Read` / `History` to unique in-memory targets outside Docker, keeps separate `write`, `read`, and `history` migration targets truthful, and proves `/engine/database-migrations` plus the public audit-history routes end to end in zero-setup runs

Remaining follow-through inside `ENG-067`:

- add bundle/script orchestration metadata and deploy-time execution truth when Cephalon grows beyond hosted-service or manual migration stories
- add provider-native migration diagnostics only when they stay additive and do not overfit the public catalog to Entity Framework

### ENG-068 Live database-role health and provider-aware migration diagnostics baseline

Status: done
Estimate: 3

Why:

- the engine-owned database-role catalog exposed logical topology truth, but it still stopped short of proving whether the active provider pack could actually connect and what the current migration pressure looked like at runtime
- dependent roles such as `outbox -> write` needed a truthful way to inherit resolved-role runtime health without inventing a second physical runtime surface
- the engine-owned migration catalog now answered target identity and status, but it still stopped short of telling operators what bundle/script/update path they should actually run next
- the database-topology roadmap already treats bundle/script-first deployment as the production path, so that guidance needed to become runtime-introspectable instead of living only in docs
- provider packs needed a truthful way to publish runtime health, migration diagnostics, and deploy-time command templates without pretending the engine already orchestrates bundle generation or execution end to end

Acceptance:

- the engine-owned database-role catalog can carry provider-contributed live runtime health and migration diagnostics per logical role
- dependent `UseRole` targets can inherit resolved-role runtime truth without lying about their logical identity
- the engine-owned migration catalog can carry provider-added operator-facing command templates per logical target
- `Cephalon.Data.EntityFramework` publishes connectivity plus pending-migration diagnostics for registered `DbContext` roles and bundle/script/update guidance for registered migration targets
- `/engine/database-roles`, `/engine/database-migrations`, and `/engine/snapshot` keep the same health/diagnostic/guidance truth visible to operators and tooling
- docs distinguish live provider diagnostics plus command-template guidance from actual bundle/script generation or execution orchestration

Delivered:

- `Cephalon.Data.EntityFramework` now probes registered `DbContext` roles live through the engine-owned database-role runtime surface, publishing connectivity outcome, provider identity, pending migration counts, applied migration counts, and last probe metadata without leaking Entity Framework APIs into `Cephalon.Abstractions`
- the resolved database-role catalog now lets dependent `UseRole` targets such as `outbox -> write` inherit resolved-role runtime truth while staying explicit that the logical role is still `outbox`
- `Cephalon.Abstractions` now ships `DatabaseMigrationCommandDescriptor`, and `DatabaseMigrationDescriptor` now carries a typed `Commands` collection instead of forcing deploy-time guidance into ad-hoc metadata keys only
- `Cephalon.Data.EntityFramework` now decorates logical migration targets with role-health/runtime metadata and publishes `dotnet ef` bundle/script/update templates per target, marking bundle/script as production-recommended
- `DatabaseMigrationCommandDescriptor` now also carries typed operator metadata such as `ToolId`, `ExecutionCategory`, and `WorkingDirectoryHint`, and the EF pack now populates those fields while preserving additive metadata for compatibility
- `DatabaseMigrationDescriptor` now also carries a typed `RecommendedExecutionOrder` hint, the EF pack now publishes recommended `write` / `read` / `history` / `outbox` sequencing additively, and both the engine-owned migration catalog plus the showcase playbook now consume that shared order instead of re-encoding target ids locally
- `DatabaseMigrationDescriptor` now also mirrors resolved-role runtime truth through typed `RoleHealthState`, `RoleHealthDescription`, `RoleMigrationState`, `RoleMigrationDescription`, and `RoleObservedAtUtc` fields, while `Cephalon.Data.EntityFramework` continues to preserve the older `roleHealthState` / `roleMigrationState` metadata keys for compatibility
- the showcase sample plus hosting/composition tests now prove live role health, inherited role runtime, migration-runtime metadata, command templates, durable read-projection jobs, and the adoption-quality `/api/v1/showcase/system/database-topology` operator projection end to end through `/engine/database-roles`, `/engine/database-migrations`, `snapshot`, and a dedicated `Database Topology` section on `/showcase` that now also derives operator insights for aligned versus drifting topology state, preserves rich migration-command guidance instead of collapsing the engine-owned descriptors to raw strings, adapts those published templates into runnable sample commands from the repo root, presents the same target set as an ordered sample migration playbook backed by engine-published order hints before the lower-level migration table, answers readiness directly as `ready`, `attention`, or `blocked`, publishes an ordered operator action plan for what to do next, exposes `/api/v1/showcase/system/database-topology/brief` as a shareable Markdown handoff derived from the same live projection, and now exposes `/api/v1/showcase/system/database-topology/handoff` as a downloadable self-describing package that bundles a package `README.md`, a machine-readable `handoff-manifest.json`, the brief, and the raw projection payload
- the showcase sample now also exposes those typed migration role-runtime fields directly in its database-topology projection and `/showcase` operator console, using metadata previews only for provider-specific extras instead of stable engine-owned fields
- component docs, architecture guidance, backlog, roadmap, and project memory now call out that live provider diagnostics and command templates are shipped while true bundle/script generation or execution orchestration remains a later slice

Remaining follow-through inside `ENG-068`:

- add bundle/script artifact generation or execution orchestration only when the engine can keep provider behavior truthful and additive
- add richer provider-native diagnostics, per-step migration telemetry, or adaptive/provider-specific probe cadence only when the public catalog can stay stable across providers

### ENG-086 Database-role probe freshness policy and stable operator projection follow-through

Status: done
Estimate: 3

Why:

- `ENG-068` proved live provider diagnostics, but the database-role catalog still re-probed every registered `DbContext` on every read, which made operator routes noisier and more expensive than needed
- probe freshness needed to live in the engine-owned topology contract instead of becoming an Entity Framework-only hidden cache
- the showcase operator projection needed to make live-versus-cached answers visible directly so the sample stayed a proving consumer of the engine contract instead of compensating for it with ad-hoc metadata parsing

Acceptance:

- `Engine:Databases:Runtime` and `AppProfile.Databases.Runtime` expose a non-negative `RoleProbeFreshnessSeconds` contract, with `0` as the explicit disable-caching answer
- runtime selection merging keeps shared and role-specific freshness overrides truthful across direct roles and dependent `UseRole` targets
- `Cephalon.Data.EntityFramework` caches role-probe results additively, invalidates them when migration runtime state changes, and publishes stable runtime metadata for live-versus-cache answers
- showcase projection/UI/config plus hosting/composition tests make the stronger engine contract visible end to end
- docs, backlog, roadmap, project memory, and GitHub tracking distinguish the shipped freshness-window baseline from later probe telemetry or cadence follow-through

Delivered:

- `DatabaseRuntimeSettings`, `DatabaseRuntimeSelection`, `AppProfileFactory`, `DatabaseTopologyRoleResolver`, and the runtime role catalog now carry `RoleProbeFreshnessSeconds` as an engine-owned runtime contract, including `0` as the explicit no-cache answer
- `Cephalon.Data.EntityFramework` now resolves effective probe freshness from the merged engine/runtime contract, defaults to a 30-second provider freshness window when none is configured, caches live probe results per logical role, and invalidates cached answers when migration execution updates the same role state
- EF-backed role runtime metadata now keeps `probeCacheEnabled`, `probeFreshnessSeconds`, `probeFreshnessOrigin`, `probeSource`, `probeFreshUntilUtc`, and `probeAgeSeconds` visible alongside existing connectivity and migration-pressure diagnostics
- the showcase sample now configures the engine-owned freshness policy in grouped `Configurations/Engine/Databases/*` files, projects probe freshness/live-versus-cache truth through `/api/v1/showcase/system/database-topology`, and shows observed time, probe source, probe age, and fresh-until timing directly in `/showcase`
- composition and hosting coverage now prove runtime-contract binding, role-resolution merge behavior, EF cache invalidation, and operator-projection output without relying on sample-only hidden state
- database-topology, component docs, roadmap, backlog, and project memory now call out the shipped probe-freshness baseline while keeping finer-grained probe cadence, telemetry, and broader provider parity as later work

Remaining follow-through inside `ENG-086`:

- add adaptive or provider-native probe cadence only when it can stay explicit in the shared engine/runtime contract
- add richer per-step migration telemetry and broader provider-native operational diagnostics without overfitting the public catalog to one provider

### ENG-088 Engine-owned database-topology operational summary and advisory surface

Status: done
Estimate: 5

Why:

- the engine-owned role and migration catalogs kept the raw truth available, but operators and hosts still lacked one canonical engine-owned answer for whether the overall topology was ready, needed attention, or was blocked
- the showcase sample was still re-aggregating readiness and insight logic locally from raw catalogs, which weakened the engine-first boundary and made the sample responsible for an answer that should belong to the engine
- hosts and automation needed one stable posture route for summary, advisories, and ordered operator actions without depending on showcase-only projection logic

Acceptance:

- `Cephalon.Abstractions` exposes a host-agnostic operational snapshot contract for database-topology posture
- `Cephalon.Engine` projects one summary, advisory set, and ordered operator action plan from the database-role and database-migration catalogs and includes it in the runtime snapshot
- ASP.NET Core hosts expose `/engine/database-topology` as the canonical posture route
- the showcase sample consumes the engine-owned posture for readiness and engine-level insights, while keeping only read-model drift/backlog follow-through local
- docs, backlog, roadmap, project memory, and GitHub tracking all stay aligned with the new engine-first ownership line

Delivered:

- `Cephalon.Abstractions` now ships `DatabaseTopologyOperationalAction`, `DatabaseTopologyOperationalActionPlan`, `DatabaseTopologyOperationalSummary`, `DatabaseTopologyOperationalAdvisory`, `DatabaseTopologyOperationalSnapshot`, and `IDatabaseTopologyOperationalSnapshotProvider` as the host-agnostic posture contract
- `Cephalon.Engine` now computes engine-owned database-topology posture from the resolved role and migration catalogs, projects it into `snapshot.DatabaseTopology`, and keeps summary status, advisories, remediation categories, source ids, and ordered action links explicit instead of trapping that answer in a sample
- ASP.NET Core hosts now expose `/engine/database-topology` directly, so operators and tooling can read one canonical `Ready` / `Attention` / `Blocked` answer plus ordered next actions without rebuilding it from lower-level catalogs first
- the showcase sample now uses the engine-owned posture as the baseline for readiness, advisory insights, and the engine portion of its ordered action plan, keeps read-model drift/catch-up/disabled-sync logic as the only sample-specific follow-through, and now carries the engine route through browser config, operator links, UI metadata, brief output, and handoff source-route metadata
- composition, hosting, and package-surface coverage now prove the new posture contract, showcase consumption path, and exported package surface end to end

Remaining follow-through inside `ENG-088`:

- add an explicit engine-owned unknown or unprobed role-health posture only when provider packs can keep that semantics truthful across more than the current EF-backed baseline
### ENG-089 Database-topology action-plan showcase follow-through and contract truthfulness

Status: done
Estimate: 3

Why:

- the engine-owned database-topology action-plan contract had already landed in the shared abstractions and runtime snapshot, but the showcase sample still re-shaped the engine portion of the ordered action plan locally from raw role and migration state
- the sample projection, UI, and brief output were still collapsing engine-owned action metadata instead of preserving stable categories plus source role and migration ids
- package-surface and planning docs needed to stay truthful about the public action-plan types that now ship from `Cephalon.Abstractions`

Acceptance:

- the showcase sample consumes the engine-owned database-topology action plan as the baseline for ordered operator steps
- showcase-specific read-model sync actions append to the same ordered plan without re-deriving engine-owned role or migration remediation logic
- the sample projection, browser console, and operator brief preserve engine-owned action categories plus source role and migration ids
- package-surface, composition, and hosting tests prove the action-plan contract and showcase follow-through
- docs, roadmap, backlog, project memory, and GitHub tracking stay aligned with the shipped action-plan contract

Delivered:

- the showcase database-topology projection now maps `snapshot.DatabaseTopology.ActionPlan` directly into its ordered operator action plan and only appends read-model sync follow-through when the sample needs extra guidance
- the projection, browser console, and Markdown brief now preserve engine-owned action categories plus source role and migration ids instead of collapsing those stable fields away
- package-surface coverage now treats `DatabaseTopologyOperationalAction` plus `DatabaseTopologyOperationalActionPlan` as part of the documented `Cephalon.Abstractions` contract surface
- composition and hosting coverage now assert engine-owned action-plan counts, categories, and source ids end to end through `/engine/database-topology`, `snapshot.DatabaseTopology`, and `/api/v1/showcase/system/database-topology`
- database-topology, engine component docs, roadmap, backlog, and project memory now describe the action-plan contract truthfully instead of documenting only summary plus advisories

### ENG-090 Engine-owned database-migration playbook surface and showcase adoption

Status: done
Estimate: 3

Why:

- the engine-owned migration catalog already exposed recommended order hints and command descriptors, but the one ordered migration playbook still lived in showcase-only projection logic
- operators and tooling still lacked one engine route and one snapshot field that answered the ordered production-versus-manual migration guidance directly
- the showcase sample needed to consume that engine-owned ordered answer and keep only repo-root command adaptation plus read-model follow-through as local logic

Acceptance:

- `Cephalon.Abstractions` exposes host-agnostic ordered migration-playbook contracts for runtime and operator consumption
- `Cephalon.Engine` publishes `/engine/database-migration-playbook` plus `snapshot.DatabaseMigrationPlaybook` as the canonical ordered migration answer over the lower-level migration catalog
- the engine-owned playbook carries generated counts, ordered steps, startup-apply truth, resolved-role identity, current status, and selected production-versus-manual command guidance per target
- the showcase sample consumes that engine-owned playbook directly for ordered migration guidance and keeps only sample-specific command adaptation plus read-model follow-through local
- composition, hosting, and package-surface tests prove the new contract and showcase adoption path end to end
- docs, roadmap, backlog, project memory, and GitHub tracking stay aligned with the shipped engine-first ownership line

Delivered:

- `Cephalon.Abstractions` now ships `DatabaseMigrationOperationalPlaybook`, `DatabaseMigrationOperationalStep`, and `IDatabaseMigrationOperationalPlaybookProvider` as the host-agnostic ordered migration-playbook contract
- `Cephalon.Engine` now computes one ordered playbook from the resolved migration catalog, selects production-recommended and manual/direct command paths when available, projects that answer into `snapshot.DatabaseMigrationPlaybook`, and keeps generated time plus target counts explicit instead of trapping the playbook in a sample
- ASP.NET Core hosts now expose `/engine/database-migration-playbook`, so operators and tooling can read the same ordered answer without reconstructing it from `/engine/database-migrations`
- the showcase sample now consumes the engine-owned playbook for its ordered migration guidance, keeps direct links to the new engine route in the browser config, brief, and handoff metadata, and limits local logic to repo-root runnable command adaptation plus sample-only read-model follow-through
- composition coverage now proves the playbook provider and runtime snapshot contract, hosting coverage now proves the route plus showcase consumption path, and package-surface coverage now locks the exported abstractions surface
- database-topology, engine component docs, roadmap, backlog, project memory, and showcase docs now describe the playbook contract truthfully as engine-owned runtime surface rather than sample-derived sequencing

### ENG-091 Shared physical database migration coordination surface and showcase follow-through

Status: done
Estimate: 3

Why:

- the engine-owned role catalog and migration playbook could already describe logical targets, but they did not yet expose when multiple logical roles or migration targets actually shared one physical database target
- operators still lacked one engine-owned coordination answer for shared-database migration work, which made it too easy to miss deploy-time risk when `write` and `read` reused one connection target or when dependent roles inherited the same database
- the showcase sample needed to consume that shared-target truth from the engine instead of inferring it locally in the operator projection, brief, or UI

Acceptance:

- `Cephalon.Abstractions` exposes host-agnostic physical-target identity and shared-target coordination fields on the shipped database-role and migration-playbook contracts
- `Cephalon.Engine` groups logical roles by physical target, projects that truth through `/engine/database-roles` plus `snapshot.DatabaseRoles`, and exposes shared-target migration coordination through `/engine/database-migration-playbook` plus `/engine/database-topology`
- the engine-owned playbook carries coordination counts, per-step physical-target identity, coordinated migration ids, and operator hints for shared targets
- the engine-owned topology posture adds advisory and action-plan guidance when pending or failed logical migration work spans one physical database target
- the showcase sample consumes the engine-owned physical-target and shared-target coordination answers directly in its JSON projection, browser console, Markdown brief, and handoff package
- composition and hosting coverage prove the shared-target contract end to end, and package-surface coverage locks the exported abstractions surface
- docs, roadmap, backlog, project memory, and GitHub tracking stay aligned with the shipped engine-first ownership line

Delivered:

- `DatabaseRoleDescriptor` now carries `PhysicalTargetId`, `PhysicalTargetDisplayName`, and `PhysicalCoLocatedRoles`, while `DatabaseMigrationOperationalStep` now carries `PhysicalTargetId`, `PhysicalTargetDisplayName`, `CoordinatedMigrationIds`, `CoordinationHint`, and `RequiresPhysicalTargetCoordination`; `DatabaseMigrationOperationalPlaybook` now also carries `CoordinationRequiredTargetCount`
- `Cephalon.Engine` now computes stable physical-target grouping across configured database roles, projects that role truth through the engine-owned role catalog, and uses it to enrich the engine-owned migration playbook and topology posture
- `/engine/database-migration-playbook` plus `snapshot.DatabaseMigrationPlaybook` now surface shared-target coordination counts, per-step coordinated partner targets, and operator-facing coordination hints without requiring a sample or host to rebuild that answer
- `/engine/database-topology` plus `snapshot.DatabaseTopology` now surface shared-physical-target migration-coordination advisories and ordered action-plan entries when pending or failed logical migration work spans one physical database
- the showcase sample now consumes those engine-owned answers directly in `/api/v1/showcase/system/database-topology`, `/showcase`, `/api/v1/showcase/system/database-topology/brief`, and `/api/v1/showcase/system/database-topology/handoff`, while keeping only sample-specific read-model follow-through local
- validation now covers the new shared-target path through composition tests `4/4`, hosting tests `60/60`, and package-surface tests `52/52`
- database-topology, engine component docs, Entity Framework component docs, roadmap, backlog, project memory, and showcase docs now describe the shipped shared-target coordination contract truthfully

### ENG-092 Shared physical database migration execution-group playbook follow-through

Status: done
Estimate: 3

Why:

- the engine-owned migration playbook already surfaced shared-target coordination truth, but operators still had to regroup ordered steps mentally into physical-target batches before planning deploy-time work
- the playbook contract still lacked one aggregate answer for shared physical targets, so status, production guidance coverage, and startup/manual posture were only visible per logical step
- the showcase sample needed to consume that grouped answer directly so the proving surface stayed aligned with the engine instead of inventing its own physical-target batch summary

Acceptance:

- `Cephalon.Abstractions` exposes a host-agnostic execution-group contract on the shipped migration-playbook surface
- `Cephalon.Engine` groups ordered logical playbook steps by physical target and publishes that grouped answer through `/engine/database-migration-playbook` plus `snapshot.DatabaseMigrationPlaybook`
- each execution group carries stable physical-target identity, aggregate status, grouped migration ids, requested and resolved role ids, target counts, production/manual/startup counts, and shared-target coordination hints when relevant
- the showcase sample consumes those execution groups directly in its JSON projection, browser UI, Markdown brief, and handoff package instead of re-grouping logical targets locally
- composition, hosting, and package-surface tests prove the execution-group contract end to end
- docs, roadmap, backlog, project memory, and GitHub tracking stay aligned with the shipped engine-first ownership line

Delivered:

- `DatabaseMigrationOperationalPlaybook` now carries `ExecutionGroupCount`, `CoordinationRequiredGroupCount`, and `ExecutionGroups`, while `Cephalon.Abstractions` now also ships `DatabaseMigrationOperationalExecutionGroup`
- `Cephalon.Engine` now computes physical-target execution groups from the ordered migration playbook, including aggregate status precedence, grouped migration ids, requested versus resolved role ids, coverage counts, and coordinated shared-target hints
- `/engine/database-migration-playbook` plus `snapshot.DatabaseMigrationPlaybook` now surface grouped physical-target execution batches without requiring hosts or tooling to rebuild that answer from lower-level migration rows
- the showcase sample now consumes those engine-owned execution groups directly in `/api/v1/showcase/system/database-topology`, `/showcase`, `/api/v1/showcase/system/database-topology/brief`, and `/api/v1/showcase/system/database-topology/handoff`, while keeping only repo-root command adaptation plus read-model follow-through local
- validation now covers the new grouped execution path through composition tests `4/4`, hosting tests `60/60`, and package-surface tests `52/52`
- database-topology, engine component docs, Entity Framework component docs, roadmap, backlog, project memory, and showcase docs now describe the shipped execution-group contract truthfully

### ENG-093 Shared physical database migration execution-group command-set follow-through

Status: done
Estimate: 3

Why:

- the engine-owned migration playbook already grouped logical targets into physical-target execution batches, but operators still had to open each ordered step to reconstruct the exact production or manual command set for one shared-database batch
- execution groups exposed coverage counts, status, and coordination hints, yet they did not publish the grouped command paths that tooling or operators actually need to review before deploy-time work
- the showcase sample needed to consume those grouped command sets directly so the proving surface stayed aligned with the engine instead of rebuilding batch-level command guidance from per-step rows

Acceptance:

- `Cephalon.Abstractions` exposes a host-agnostic grouped-command contract on the shipped execution-group surface
- `Cephalon.Engine` publishes grouped production and manual command sets per physical-target execution group through `/engine/database-migration-playbook` plus `snapshot.DatabaseMigrationPlaybook`
- each grouped command entry carries execution order, logical migration target id, requested and resolved role ids, and the selected command descriptor for that path
- the showcase sample consumes those grouped command sets directly in its JSON projection, browser UI, Markdown brief, and handoff package instead of reconstructing them from ordered steps
- composition, hosting, and package-surface tests prove the grouped command-set contract end to end
- docs, roadmap, backlog, project memory, and GitHub tracking stay aligned with the shipped engine-first ownership line

Delivered:

- `Cephalon.Abstractions` now also ships `DatabaseMigrationOperationalExecutionGroupCommand`, while `DatabaseMigrationOperationalExecutionGroup` now carries grouped `ProductionCommands` and `ManualCommands`
- `Cephalon.Engine` now projects the selected per-step production/manual commands back into their physical-target execution groups with stable execution ordering and target-role identity
- `/engine/database-migration-playbook` plus `snapshot.DatabaseMigrationPlaybook` now surface one grouped command-set answer per physical target instead of forcing operators or tooling to rebuild that batch guidance from lower-level rows
- the showcase sample now consumes those engine-owned grouped command sets directly in `/api/v1/showcase/system/database-topology`, `/showcase`, `/api/v1/showcase/system/database-topology/brief`, and `/api/v1/showcase/system/database-topology/handoff`, while keeping only repo-root command adaptation plus read-model follow-through local
- validation now covers the new grouped command-set path through composition tests `4/4`, hosting tests `60/60`, and package-surface tests `52/52`
- database-topology, engine component docs, Entity Framework component docs, roadmap, backlog, project memory, and showcase docs now describe the shipped grouped command-set contract truthfully

### ENG-094 Shared physical database migration execution-group command-batch follow-through

Status: done
Estimate: 3

Why:

- the engine-owned migration playbook already surfaced grouped command sets per physical-target batch, but operators and tooling still had to copy or stitch those commands together manually when they needed one combined batch answer for deploy-time work
- execution groups already knew stable command order, tool ids, and working-directory hints, yet the contract still lacked one deterministic combined command-batch template for the selected production and manual paths
- the showcase sample needed to consume that engine-owned batch answer directly so the proving surface stayed aligned with the engine instead of rebuilding one multiline copy/paste view locally

Acceptance:

- `Cephalon.Abstractions` exposes a host-agnostic command-batch contract on the shipped execution-group surface
- `DatabaseMigrationOperationalExecutionGroup` publishes combined production and manual command-batch templates derived from the selected grouped commands
- each combined command batch preserves playbook order plus target, command-id, tool-id, and working-directory truth for the selected path
- the showcase sample consumes those combined command batches directly in its JSON projection, browser UI, Markdown brief, and handoff package while keeping only repo-root command adaptation local
- composition, hosting, and package-surface tests prove the combined command-batch contract end to end
- docs, roadmap, backlog, project memory, and GitHub tracking stay aligned with the shipped engine-first ownership line

Delivered:

- `Cephalon.Abstractions` now also ships `DatabaseMigrationOperationalExecutionGroupCommandBatch`, while `DatabaseMigrationOperationalExecutionGroup` now carries `ProductionCommandBatch` and `ManualCommandBatch`
- the execution-group contract now derives one deterministic multiline combined command template per selected production or manual path, including stable target ids, command ids, tool ids, and working-directory hints
- `/engine/database-migration-playbook` plus `snapshot.DatabaseMigrationPlaybook` now surface one combined command-batch answer per physical-target path instead of forcing operators or tooling to stitch grouped commands together locally
- the showcase sample now consumes those engine-owned combined command batches directly in `/api/v1/showcase/system/database-topology`, `/showcase`, `/api/v1/showcase/system/database-topology/brief`, and `/api/v1/showcase/system/database-topology/handoff`, while keeping only repo-root command adaptation plus read-model follow-through local
- validation now covers the new combined command-batch path through composition tests `4/4`, hosting tests `60/60`, and package-surface tests `52/52`
- database-topology, engine component docs, Entity Framework component docs, roadmap, backlog, project memory, and showcase docs now describe the shipped combined command-batch contract truthfully

### ENG-095 Phase 12 migration-pattern taxonomy baseline

Status: done
Estimate: 2

Why:

- phase 12 explicitly called for `strangler-fig` and `backend-for-frontend`, but the built-in engine pattern taxonomy still could not express those choices through the same app-model vocabulary as other shipped patterns
- architecture docs and planning guidance still had to describe those phase 12 patterns as recommendations instead of stable runtime descriptors
- teams adopting Cephalon incrementally need those pattern ids available through configuration, `/engine/patterns`, and `AppProfile.Patterns` before routing or client-binding follow-through lands

Acceptance:

- `BuiltInPatterns` exposes stable `strangler-fig` and `backend-for-frontend` descriptors with aliases, tags, and architecture classification
- configuration-driven app-profile selection resolves those new pattern ids and aliases through the same engine-owned path used by existing patterns
- ASP.NET Core hosts surface the new descriptors through `/engine/patterns` and the runtime app model
- docs, backlog, roadmap, project memory, and GitHub tracking stay aligned with the descriptor-only scope, while remaining honest that `IStranglerFigRouter` and client-binding policy follow-through are later slices

Delivered:

- `Cephalon.Engine` now ships `BuiltInPatterns.StranglerFigPattern` and `BuiltInPatterns.BackendForFrontendPattern` as architecture-level descriptors
- the engine now resolves `StranglerFig`, `Strangler`, `BackendForFrontend`, and `BFF` through the same built-in pattern catalog used by configuration-driven app-model selection
- targeted composition and hosting coverage now prove both alias resolution and `/engine/patterns` plus `/engine/app-model` visibility for the new phase 12 taxonomy entries
- architecture inventory, architecture recommendations, engine component docs, roadmap, backlog, and project memory now describe the shipped descriptor baseline truthfully while keeping router and client-binding follow-through explicitly planned

### ENG-096 Phase 12 strangler-fig runtime contract baseline

Status: done
Estimate: 3

Why:

- after `ENG-095`, phase 12 could name strangler fig but still could not express real migration-route ownership or request resolution through engine-owned contracts
- modules and hosts still lacked a reusable, host-agnostic way to declare which path prefixes were under migration and which Cephalon module owned the modern boundary
- operator tooling had no direct runtime answer for the active strangler-fig route catalog or which target would win for a specific request

Acceptance:

- `Cephalon.Abstractions` ships host-agnostic strangler-fig route contribution, runtime-catalog, request, resolution, and router contracts
- `Cephalon.Engine` composes both host-added and module-contributed strangler-fig routes, auto-selects the `strangler-fig` pattern when routes exist, and projects the route catalog into the runtime snapshot
- ASP.NET Core hosts expose direct operator routes for the active strangler-fig route catalog and one request-resolution probe
- docs, backlog, roadmap, project memory, and GitHub tracking stay aligned with the contract-first/runtime-only scope while remaining honest that configuration-driven migration policy, progress tracking, and host-level proxy behavior are later work

Delivered:

- `Cephalon.Abstractions` now ships `IStranglerFigRouteContributor`, `IStranglerFigRouteRegistry`, `IStranglerFigRuntimeCatalog`, `IStranglerFigRouter`, `StranglerFigRequest`, `StranglerFigRouteDescriptor`, `StranglerFigRouteResolution`, and `StranglerFigTarget`
- `Cephalon.Engine` now composes strangler-fig routes from both `EngineBuilder.AddStranglerFigRoute(...)` and module contributors, automatically keeps the `strangler-fig` pattern visible in `AppProfile.Patterns` when routes exist, and projects the route set into `snapshot.StranglerFigRoutes`
- `Cephalon.AspNetCore` now exposes `/engine/strangler-fig`, `/engine/strangler-fig/{routeId}`, and `/engine/strangler-fig/resolve` as direct operator routes over the shared runtime contracts
- targeted composition, hosting, and package-surface coverage now prove route collection, longest-prefix plus fallback request resolution, runtime snapshot projection, and the new abstraction-layer public surface
- architecture recommendations, component docs, roadmap, backlog, and project memory now describe the shipped contract-first strangler-fig baseline truthfully while keeping progress tracking, `Engine:Migration` configuration, and host-specific cutover behavior explicitly planned

### ENG-069 Event-dispatch runtime operator-surface baseline

Status: done
Estimate: 5

Why:

- the current eventing baseline could report live dispatch state, but its descriptor and state read contracts still lived in `Cephalon.Eventing` instead of the host-agnostic abstraction layer that other runtime catalogs use
- `/engine/snapshot` still stopped short of exposing a first-class event-dispatch runtime answer even though operators already needed to understand both configured dispatch ownership and the latest reported state
- ASP.NET Core hosts still lacked direct `/engine/*` operator routes for event-dispatch runtimes and live dispatch state, which forced tooling to reconstruct answers indirectly from broader technology surfaces
- the showcase sample had the pieces for outbox-backed publication, but it did not yet prove the official Wolverine-managed dispatch path and the new operator routes end to end

Acceptance:

- host-agnostic read contracts for configured event-dispatch runtimes and live event-dispatch state live in `Cephalon.Abstractions`
- `/engine/snapshot` carries additive `EventDispatchRuntimes` and `EventDispatchStates` answers when the corresponding catalogs are active
- ASP.NET Core hosts expose `/engine/event-dispatch-runtimes`, `/engine/event-dispatch-runtimes/{dispatchRuntimeId}`, `/engine/event-dispatches`, and `/engine/event-dispatches/{outboxId}`
- `Cephalon.Eventing.Wolverine` projects its managed loop through the new operator surfaces without leaking Wolverine APIs into the abstraction layer
- the showcase sample wires the official Wolverine path so hosting tests can prove the new routes and runtime snapshot truthfully

Delivered:

- `Cephalon.Abstractions` now ships `EventDispatchRuntimeDescriptor`, `EventDispatchRuntimeState`, `IEventDispatchRuntimeCatalog`, and `IEventDispatchRuntimeDescriptorCatalog` as the host-agnostic read layer for configured dispatch ownership and latest reported dispatch state
- `Cephalon.Eventing` now keeps registration, reporting, and catalog implementation in the companion pack while consuming the abstraction-layer contracts for public reads, including a dedicated `EventDispatchRuntimeDescriptorCatalog`
- `Cephalon.Engine` now projects additive `EventDispatchRuntimes` and `EventDispatchStates` into `RuntimeIntrospectionSnapshot` through optional service resolution so the engine core stays additive and host-agnostic
- `Cephalon.AspNetCore` now exposes `/engine/event-dispatch-runtimes`, `/engine/event-dispatch-runtimes/{dispatchRuntimeId}`, `/engine/event-dispatches`, and `/engine/event-dispatches/{outboxId}` as direct operator routes
- `Cephalon.Eventing.Wolverine` now reports its managed dispatch loop through the new runtime-descriptor/state surfaces without holding scoped dependencies incorrectly in singleton services, and the showcase sample now wires the official Wolverine pack so those routes stay truthful end to end
- hosting, composition, tooling, reference-doc, and showcase coverage now lock the new operator contract plus the moved public surface

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

### Infrastructure — Phase 1 Developer Experience

- Solution filter files: `core.slnf`, `data.slnf`, `observability.slnf`, `aspnetcore.slnf` added to repo root for IDE-level project filtering
- Scaffolding scripts: `scripts/New-ProviderPack.ps1` and `scripts/New-ObservabilityPack.ps1` automate the artifact creation steps when adding new provider companion packs


### Sprint 30

- ENG-054 Elasticsearch + OpenSearch search-store non-relational provider: `Cephalon.Data.Elasticsearch` (IOutbox + IInbox backed by Elasticsearch indices, idempotent staging via op_type=create (409 swallow), `data.elasticsearch` / `data.search-store` capabilities), `Cephalon.EventSourcing.Elasticsearch` (IEventStore with compound document id `{streamId}#{streamVersion}` for uniqueness, application-layer optimistic concurrency, System.Text.Json serialization, IAsyncEnumerable stream replay), `Cephalon.Data.OpenSearch` (OpenSearch.Client mirror of Elasticsearch data pack, `data.opensearch` / `data.search-store` capabilities), `Cephalon.EventSourcing.OpenSearch` (OpenSearch mirror of Elasticsearch event store), Elastic.Clients.Elasticsearch 8.17.0 + OpenSearch.Client 1.8.0 in CPM, 8 composition tests (no live server required — client connects lazily), full component docs — **Shipped** · 640/640 tests

### Sprint 31

- ENG-054 Qdrant vector-store + NATS ledger non-relational provider: `Cephalon.Data.Qdrant` (IOutbox + IInbox backed by Qdrant vector collections using 1D dummy vectors and payload-field storage, idempotent staging via point-ID existence check, `data.qdrant` / `data.vector-store` capabilities), `Cephalon.EventSourcing.Qdrant` (IEventStore with compound point-ID `{streamId}:{version}` hash, application-layer optimistic concurrency, Scroll-based stream replay), `Cephalon.Data.Nats` (IOutbox + IInbox backed by NATS JetStream KV, idempotent via KV CreateAsync with NatsKVCreateException swallow, `data.nats` / `data.ledger-store` capabilities), `Cephalon.EventSourcing.Nats` (IEventStore via JetStream KV with zero-padded keys `{streamId}/{version:D20}`, lexicographic-safe ordering, CreateAsync for concurrency), Qdrant.Client 1.17.0 + NATS.Net 2.7.3 in CPM, 8 composition tests (no live server — both clients connect lazily), full component docs — **Shipped** · 648/648 tests
- ENG-054 provider configuration standardization: connection-string-native packs now align on `ConnectionStringName` plus `ConnectionString` (`Cephalon.Data.MongoDB`, `Cephalon.Data.Redis`), URI-first packs now align on `UriName` plus `Uri` (`Cephalon.Data.Elasticsearch`, `Cephalon.Data.OpenSearch`, `Cephalon.Data.Neo4j`, `Cephalon.Data.Nats`), named values resolve from the root `ConnectionStrings` or `Uris` sections as appropriate, packs fail fast when both settings are supplied at once, localhost defaults remain only when neither setting is configured, and the component docs now call out the provider-family contract explicitly while Cassandra/Qdrant remain topology-first and observability dependency definitions stay probe-oriented — **Shipped**
- ENG-058-T30 behavior-aware REST endpoint helpers and OpenAPI follow-through: `MapBehaviorRestGroup(...)` plus `BehaviorRestEndpointGroup.MapBehaviorGet/Post/Put/Patch/Delete(...)` in `Cephalon.Behaviors.Http`, module-major versioned operation names, XML-comment-backed OpenAPI enrichment, route/query/body input composition for behavior DTOs, `DefaultBehaviorContext` event-store DI wiring, showcase cart route deduplication, and sample in-memory event-store coverage — **Shipped** · composition HTTP tests 13/13 + showcase hosting tests 33/33
- ENG-058-T31 behavior REST OpenAPI summary/description split follow-through: `BehaviorRestEndpointGroup` now maps behavior XML `<summary>` to the OpenAPI operation summary and behavior XML `<remarks>` to the OpenAPI operation description without repeating the same text twice in Scalar, showcase cart behavior comments now demonstrate the split, and showcase hosting coverage now locks the generated `/openapi/v1.json` contract — **Shipped** · composition HTTP tests 13/13 + showcase hosting tests 34/34
- ENG-058-T32 behavior REST OpenAPI document-version follow-through: `BehaviorRestEndpointGroup.ApiVersion(...)` now pins behavior-shaped REST endpoints to named OpenAPI documents while preserving module-major operation-name fallback when no API version is supplied, ASP.NET Core host registration now resolves `OpenApi:Documents` plus optional `OpenApi:DefaultDocument`, Scalar now exposes the configured document set, showcase cart routing now opts into `v1`, and hosting coverage now locks `/openapi/v2.json` plus `/scalar/v2` behavior — **Shipped** · composition HTTP tests 14/14 + hosting tests 235/235
- ENG-058-T33 behavior REST OpenAPI versioned-config canonicalization: `Cephalon.AspNetCore` now treats `OpenApi:EnabledVersions` plus `OpenApi:DefaultVersion` as the canonical versioned-document config, keeps legacy `OpenApi:Documents` and `OpenApi:DefaultDocument` for backward compatibility or custom named docs, limits the global `OpenApi:Version` info override to single-document hosts so multi-document metadata stays truthful, and updates the showcase sample plus authoring docs to demonstrate the numeric version contract — **Shipped** · composition HTTP tests 14/14 + hosting tests 235/235
- ENG-058-T34 Scalar multi-document doc-link follow-through: the ASP.NET Core host now keeps `/scalar/v1`, `/scalar/v2`, and similar routes available as pinned document links while preserving Scalar's multi-document source list for version switching; hosting coverage and authoring docs now lock the versioned doc-link behavior — **Shipped** · composition HTTP tests 14/14 + targeted hosting tests 2/2
- ENG-058-T35 canonical Scalar links and version-aligned REST routes: `BehaviorRestEndpointGroup.ApiVersion(...)` now prefixes behavior-shaped REST route groups with `/v{major}` so ASP.NET Core hosts expose `/api/v1/...` alongside `/openapi/v1.json`, the showcase sample's direct REST modules now live under `/api/v1/showcase/...` with `WithGroupName("v1")`, `/scalar` now redirects to the default canonical document such as `/scalar/v1`, and the Scalar JavaScript normalizes hash-based selections back into pinned versioned links — **Shipped** · composition HTTP tests 14/14 + targeted hosting tests 3/3 + showcase hosting tests 34/34
- ENG-058-T36 configurable docs-route surfaces and module-major REST defaults: ASP.NET Core hosts can now move the OpenAPI JSON endpoint through `OpenApi:RoutePattern`, move the Scalar UI base path through `OpenApi:Scalar:RoutePrefix`, move the built-in REST mapper through `ApiRoutes:Prefixes:Rest`, and let `MapBehaviorRestGroup(...)` default its route/document version from the owning module descriptor major version while keeping `.ApiVersion(...)` as an override. That baseline established the version-aligned REST/OpenAPI/Scalar contract before the broader transport-prefix work landed — **Shipped** · composition HTTP tests 14/14 + hosting tests 4/4 + showcase hosting tests 34/34
- ENG-058-T37 shared `BehaviorApiSurface` canonical routing for generic behavior HTTP bindings: `BehaviorTopologyDescriptor` now carries a transport-agnostic `ApiSurface`, `BehaviorApiSurfaceDescriptor.CreateDefault(...)` derives group/operation paths from the behavior id, `WithApiSurface(...)` and the behavior source generator keep fluent and compile-time topology aligned, and the JSON-RPC, GraphQL, GraphQL-SSE, GraphQL-WS, SSE, and WebSocket bindings now project canonical versioned routes from that shared surface. The earlier `/behaviors/{id}` compatibility aliases introduced in that round were later removed by `ENG-058-T39` so the canonical versioned routes remain the only generic behavior HTTP surface — **Shipped** · behavior API-surface tests 4/4 + composition HTTP tests 14/14
- ENG-058-T38 canonical `ApiRoutes:Prefixes` defaults across built-in and generic HTTP transport surfaces: the canonical host config now defaults to `Rest=/api`, `GraphQL=/graphql`, `JsonRpc=/json-rpc`, `Grpc=/grpc`, `Ws=/ws`, `Sse=/sse`, `GraphQLWs=/graphql-ws`, and `GraphQLSse=/graphql-sse`; built-in transport mappers now read the same contract as the generic behavior bindings; the showcase sample now consumes route prefixes through generated client config instead of hard-coded transport paths; and hosting/composition coverage now locks the aligned default and override behavior — **Shipped** · behavior API-surface + HTTP binding tests 22/22 + targeted hosting tests 3/3 + showcase hosting tests 34/34
- ENG-058-T39 remove generic `/behaviors/{id}` aliases from the behavior HTTP surface: canonical versioned routes are now the only generated behavior HTTP endpoints; `BehaviorApiSurfaceRouteResolver` no longer appends behavior-id compatibility aliases; the retired behavior-specific prefix/config aliases have been removed so the generic bindings read the same canonical `ApiRoutes:Prefixes:*` contract as the built-in host mappers; and XML comments, component docs, authoring guidance, and HTTP binding coverage now treat the canonical versioned routes as the single public contract — **Shipped** · composition HTTP tests 24/24
- ENG-058-T40 separate public REST docs from generic behavior HTTP routes and add REST tag metadata: module-owned REST endpoints now own the published REST OpenAPI + Scalar surface, `MapBehaviorRestGroup(...)` can now override tag names and descriptions explicitly, module XML comments can flow into tag descriptions, and hosting/tooling coverage now locks the public REST-vs-generic-adapter distinction — **Shipped** · targeted hosting tests 3/3 + package-surface tests 51/51
- ENG-058-T41 module-owned REST cleanup and removal of behavior-declared REST: `http.rest` is no longer a valid behavior allowlist or topology transport, `ViaHttpRest(...)` and the generic REST binding/config surface were removed, `Engine:Behaviors` now stays focused on auto-registration instead of per-behavior REST overrides, `ApiRoutes:Prefixes:Rest = ""` still mounts versioned module-owned REST routes at the root while `null` still falls back to `/api`, and docs/sourcegen/reference output now treat `MapEndpoints(...)` plus `MapBehaviorRestGroup(...)` as the only REST authoring path — **Shipped** · behavior baseline + behavior source-generator + hosting + tooling coverage
- ENG-058-T42 attribute-only behavior baseline synthesis and transport alias normalization: behaviors can now run from `[BehaviorAllowedPatterns]` plus `[BehaviorAllowedTransports]` alone when exactly one pattern is declared; ambiguous multi-pattern declarations still fail fast until another topology source selects one; `http.grpc` is now accepted as an allowlist alias for canonical `grpc`; and composition/component-doc coverage now lock the attribute-only registration behavior for non-REST transports — **Shipped** · behavior baseline + HTTP binding tests
- ENG-058-T43 module-owned behavior authoring base classes and ownership validation: `IBehaviorModuleBuilder`, `IBehaviorOwnerModule`, and `OwnedBehaviorRegistration` now make module-owned behaviors a first-class host-agnostic contract; `BehaviorModuleBase` and `RestBehaviorModuleBase` give authors a simpler module surface than implementing multiple interfaces directly; engine composition now validates duplicate ownership, behavior auto-registration no longer clobbers explicitly owned topology, REST helpers now reject mappings that target another module's owned behavior, showcase cart now demonstrates the new authoring model, and docs/reference output now describe the ownership-first path truthfully — **Shipped** · behavior baseline tests 47/47 + hosting tests 3/3 + tooling tests 72/72
- ENG-058-T43 explicit module-owned behavior authoring model: `Cephalon.Abstractions` now exposes `IBehaviorOwnerModule`, `IBehaviorModuleBuilder`, and `OwnedBehaviorRegistration`; `Cephalon.Behaviors` now ships `BehaviorModuleBase`; `Cephalon.Behaviors.Http` now ships `RestBehaviorModuleBase`; engine composition now collects and validates module-owned behavior registrations so one module can keep internal-only and REST-exposed behaviors together; and REST helper mapping now rejects attempts to expose a behavior owned by another module — **Shipped** · behavior baseline tests 46/46 + REST OpenAPI hosting tests 3/3 + package-surface tests 51/51 + reference-doc follow-through
- ENG-058-T43 module-owned behavior authoring baseline: `IBehaviorOwnerModule`, `IBehaviorModuleBuilder`, and `OwnedBehaviorRegistration` now make module-owned behavior declarations explicit, `BehaviorModuleBase` and `RestBehaviorModuleBase` give authors a low-friction base-class path, engine composition now rejects duplicate owners, and REST helper mapping now rejects cross-module behavior exposure when ownership is explicit — **Shipped** · composition + hosting + tooling coverage
- ENG-058-T44 single-surface REST behavior-module DSL and opt-in auto-registration fallback: `Cephalon.Behaviors.Http` now exposes `IRestBehaviorModuleBuilder`, `IRestBehaviorEndpointGroupBuilder`, and an upgraded `RestBehaviorModuleBase` so one module can declare public REST routes and internal-only behavior ownership in `ConfigureRestBehaviors(...)` without repeating the same behavior in separate ownership and endpoint methods; `Group(...).MapGet/MapPost/...` now implies ownership automatically while `Internal<TBehavior>()` covers internal-only or custom/manual-route behaviors; `MapAdditionalEndpoints(...)` remains the advanced escape hatch for raw Minimal API work; `Engine:Behaviors:AutoRegister` now defaults to `false` so explicit module ownership is the primary path and assembly scanning is an opt-in fallback; showcase cart, docs, and reference output now all align with the new authoring model — **Shipped** · behavior owner + baseline tests 48/48 + REST OpenAPI + showcase hosting tests 37/37 + package-surface tests 51/51
- ENG-058-T46 transport-neutral behavior results plus optional REST result envelopes: `Cephalon.Abstractions` now exposes `BehaviorResult<T>`, `IBehaviorResult`, `BehaviorResultStatus`, and `BehaviorFaultSeverity` so behaviors can return structured expected outcomes without locking the core contract to HTTP; explicit module-owned behaviors without topology metadata now fall back to `direct` registration automatically; `Cephalon.AspNetCore` now exposes `ResultModel<T>`, `ResultModelError`, and `ResultModelErrorDetail` as the optional REST wire envelope behind `ApiRoutes:ResultEnvelope:Enabled`; REST failures now surface an `errors` collection so validation and other multi-reason outcomes can return more than one structured item; module-owned REST can now wrap both raw `TOutput` and `BehaviorResult<TOutput>` results without forcing transport-specific envelopes back into the behavior contract; module-owned REST OpenAPI now publishes the wrapped success schema without duplicating the error shape on `2xx`; and GraphQL/JSON-RPC stay on their protocol-native envelopes — **Shipped** · behavior result + baseline tests 61/61 + REST OpenAPI hosting tests 2/2 + package-surface tests 51/51
- ENG-058-T47 plural REST error envelopes and multi-fault projection follow-through: the optional REST `ResultModelError` contract now uses an `errors` collection instead of a singular `error` object, `BehaviorRestResponseMapper` now projects `BehaviorFault.InnerFaults` into multiple structured REST errors for validation and other multi-reason failures, `ResultModelDocumentTransformer` now treats `errors` as the canonical REST envelope property without carrying a legacy singular fallback, and hand-authored docs plus REST OpenAPI hosting coverage now lock the plural wire contract — **Shipped** · REST OpenAPI hosting tests 5/5 + package-surface tests 51/51
- ENG-058-T48 showcase `AddToCartBehavior` authoring example for `BehaviorResult<T>`: the cart sample now demonstrates transport-neutral expected outcomes directly in `AddToCartBehavior`, including multi-fault validation through `BehaviorFault.InnerFaults`, a conflict result when the cart is already checked out, showcase-host overrides that opt into the REST result envelope for focused tests, and module-authoring/component docs that now point to the cart sample as the concrete reference for `BehaviorResult<T>` plus REST `errors` projection — **Shipped** · showcase hosting tests 36/36
- ENG-058-T49 concise no-payload `BehaviorResult` factories: `Cephalon.Abstractions` now exposes a public `BehaviorResultDescriptor` plus implicit conversion into `BehaviorResult<T>` so no-payload branches can return `BehaviorResult.Invalid(...)`, `BehaviorResult.NotFound(...)`, `BehaviorResult.Conflict(...)`, `BehaviorResult.Forbidden(...)`, `BehaviorResult.Unauthorized(...)`, and `BehaviorResult.NoContent(...)` without repeating `<T>` in the common async-return path; sample/docs/tests now demonstrate the shorter authoring shape, and authoring guidance now calls out the one remaining C# inference edge case for `Task.FromResult<BehaviorResult<TOut>>(...)` — **Shipped** · behavior result tests 3/3 + package-surface tests 51/51 + clean-worktree REST OpenAPI hosting verification
- ENG-058-T51 concise `Result<T>` / `Result` aliases for transport-neutral outcomes: `Cephalon.Abstractions` now exposes `Result<T>` and `Result` as the preferred authoring names for transport-neutral behavior outcomes, keeps `BehaviorResult<T>` / `BehaviorResult` as compatibility aliases, updates ASP.NET Core REST/OpenAPI detection so both result families project the same HTTP and documentation behavior, and refreshes component/module-authoring guidance to prefer the shorter authoring form without breaking the existing behavior-result contract — **Shipped** · behavior result tests 3/3 + REST OpenAPI hosting tests 6/6 + package-surface tests 51/51
- ENG-058-T50 configurable documented REST response statuses plus default `500`: `OpenApi:BehaviorRest:DocumentedStatusCodes` now controls which HTTP status codes Cephalon's behavior-owned REST helpers publish into OpenAPI + Scalar, the default documented response set now includes `500`, route-group defaults no longer force `400`/`404` when the host narrows the list, and hosting coverage now locks both the default `500` visibility and a custom filtered status-code list — **Shipped** · REST OpenAPI hosting tests 6/6
- ENG-058-T52 Scalar version selector wired to the resolved OpenAPI document set: `Cephalon.AspNetCore` now injects the resolved document-name list plus default document into the bundled `openapi-toggle.js` asset, Scalar renders a top-right selector that follows `OpenApi:EnabledVersions` / `OpenApi:DefaultVersion`, legacy custom document names still work with a generic `Document` label, and targeted hosting coverage now locks both the injected script contract and canonical multi-version `/scalar` routing behavior under an isolated test host — **Shipped** · targeted hosting tests 2/2
- ENG-058-T53 host-level OpenAPI published-version allow-list semantics: `OpenApi:EnabledVersions` and legacy `OpenApi:Documents` now stay authoritative as the published-document allow-list, module/behavior version metadata still declares which document an endpoint belongs to, disabled defaults no longer expand the published document set, and targeted hosting coverage now locks the case where versioned endpoints exist for `v1`, `v2`, and `v3` while the host publishes only `v2` plus `v3` in OpenAPI and Scalar — **Shipped** · targeted hosting tests 3/3
- ENG-058-T54 Scalar version selector mounted into the toolbar header: the injected Scalar selector now mounts into Scalar's own `api-reference-toolbar` row when that host surface is present, keeps the floating overlay only as a fallback when no toolbar host is available, and targeted hosting coverage now locks the header-mount script contract so the version dropdown no longer obscures toolbar actions such as Configure, Share, and Deploy — **Shipped** · targeted hosting tests 2/2
- ENG-058-T55 REST endpoint authoring strategy and precedence model: the repo now records the long-term REST endpoint authoring recommendation in `docs/architecture/rest-endpoint-authoring-strategy.md`, keeping explicit module-owned REST as the canonical public path while defining future shorthand as projection-driven metadata instead of direct behavior-owned REST activation, formalizing suppression and precedence rules for explicit versus implicit projections, and recording the settled cross-thread rules in `docs/project-memory.md` so the next implementation slices can evolve from one stable design baseline — **Shipped** · GitHub issue `#313`
- ENG-058-T56 normalize REST behavior projections beneath the module DSL: `Cephalon.Behaviors.Http` now compiles `IRestBehaviorModuleBuilder` authoring into an internal `RestBehaviorModuleProjection` contract with normalized route-group and endpoint descriptors, reuses that same compiled projection for both behavior ownership registration and route materialization through `RestBehaviorProjectionMaterializer`, keeps the public REST DSL unchanged, and locks duplicate behavior-id, explicit-topology, and projection-reuse semantics through targeted REST projection tests 5/5 — **Shipped** · GitHub issue `#314`
- ENG-058-T57 resolved public REST endpoint runtime catalog and collision validation: `Cephalon.Abstractions` now exposes `IRestEndpointRuntimeCatalog`, `IRestEndpointRuntimeRegistry`, and `RestEndpointRuntimeDescriptor`; `Cephalon.AspNetCore` now publishes `/engine/rest-endpoints`, `/engine/rest-endpoints/{restEndpointId}`, and `snapshot.RestEndpoints`; startup now fails fast when two resolved public REST endpoints collide on the same `HTTP method + route pattern`; and targeted hosting plus package-surface coverage now lock the shipped runtime answer — **Shipped** · GitHub issue `#318` · targeted hosting tests 7/7 + package-surface tests 52/52
- ENG-058-T58 manual module-owned REST runtime-catalog follow-through: `Cephalon.AspNetCore` now materializes the public REST runtime catalog from final ASP.NET Core route endpoints so plain `IRestModule` / `IEndpointModule` routes and `RestBehaviorModuleBase.MapAdditionalEndpoints(...)` behavior-helper routes join `/engine/rest-endpoints` and `snapshot.RestEndpoints` with `sourceKind = manual`, while manual-vs-DSL collisions now fail on the same `HTTP method + route pattern` baseline as projection-backed routes — **Shipped** · GitHub issue `#320` · targeted hosting tests 4/4
- ENG-058-T59 generic behavior transport route-version default follow-through: `Cephalon.AspNetCore` now keeps the generic JSON-RPC, GraphQL, GraphQL-SSE, GraphQL-WS, SSE, and WebSocket adapter route segment driven by `ApiRoutes:DefaultBehaviorDocumentName` or the raw configured `OpenApi:DefaultVersion` instead of reusing the published OpenAPI allow-list default, so `OpenApi:EnabledVersions` and legacy `OpenApi:Documents` continue to govern only OpenAPI + Scalar publishing while the generic adapter routes remain independently versioned; docs and regression coverage now lock both the allow-list separation and the explicit behavior-route override contract — **Shipped** · GitHub issue `#321` · composition HTTP binding tests 12/12 + targeted hosting tests 2/2
- ENG-058-T60 ASP.NET Core hosting determinism and version-aware showcase route follow-through: `Cephalon.AspNetCore` now marks `/engine/*` Minimal API service dependencies explicitly for .NET 10 binding clarity, hosting tests now stay isolated from transitive sample `Configurations/**` output through test-output cleanup plus temporary content roots, the showcase host builder now accepts an explicit content root so sample-host tests load the real project configuration graph, and showcase orders-route assertions now derive their versioned `/api/v{major}` prefix from `OrdersModule` metadata while the showcase transport projection coverage keeps module-owned REST separate from generic behavior transport ids — **Shipped** · GitHub issue `#322` · targeted hosting tests 43/43 + showcase hosting tests 60/60
- ENG-058-T61 metadata-only REST profile diagnostics and source-generated hints: `Cephalon.Behaviors.Http` now exposes `BehaviorRestProfileAttribute`, `BehaviorRestMethod`, and `BehaviorRestProfileDescriptor` as the metadata-only behavior-authored REST profile contract, `Cephalon.Behaviors.SourceGen` now validates those profiles through `ABT0015` through `ABT0018` and emits source-generated `GetRestProfiles()` hints, and the behavior-topology guidance now points authors at `RestBehaviorModuleBase.ConfigureRestBehaviors(...)` while keeping public REST module-owned — **Shipped** · GitHub issue `#324` · source-generator tests 16/16 + package-surface tests 1/1
- ENG-058-T62 explicit module-owned REST profile shorthand consumption: `Cephalon.Behaviors.Http` now lets `IRestBehaviorEndpointGroupBuilder.MapProfile<TBehavior>()` consume metadata-only behavior REST profiles through the existing `RestBehaviorModuleBase` projection pipeline, preferring source-generated `GetRestProfiles()` hints and falling back only to the explicitly targeted behavior type when generated hints are unavailable; profile metadata still contributes only method, relative pattern, and optional candidate API version while the owning module keeps control of the public prefix, tags, and published documents; conflicting profile-declared versions in one group now fail fast until the module resolves them explicitly; and `/engine/rest-endpoints` now keeps `sourceKind = module-dsl` while distinguishing the shorthand path through `metadata.authoringStyle = behavior-module-profile` — **Shipped** · GitHub issue `#325` · targeted hosting tests 16/16 + composition tests 5/5 + package-surface tests 1/1
- ENG-058-T63 explicit REST profile binding descriptors and runtime metadata: `Cephalon.Behaviors.Http` now exposes `BehaviorRestBindingAttribute`, `BehaviorRestBindingDescriptor`, and `BehaviorRestBindingSource`; `BehaviorRestProfileDescriptor` plus source-generated `GetRestProfiles()` hints now preserve explicit route/query/header/body binding plans; module-owned `MapProfile<TBehavior>()` now validates those bindings and composes requests in override mode so explicit bindings win while unbound route placeholders and request bodies can still fill remaining object properties deterministically; body conflicts against explicit non-body bindings now fail fast; and `/engine/rest-endpoints` now exposes additive `metadata.bindingDescriptors` for profile-driven module-owned REST endpoints — **Shipped** · GitHub issue `#326` · source-generator tests 17/17 + targeted hosting tests 23/23 + package-surface tests 1/1
- ENG-058-T64 compile-time REST binding diagnostics and route-placeholder validation: `Cephalon.Behaviors.SourceGen` now rejects invalid `BehaviorRestBindingAttribute` metadata through `ABT0019` through `ABT0025`, including unsupported binding sources, missing or duplicate input-property targets, scalar-input misuse, body bindings on non-body verbs, and route bindings that do not match placeholders declared in `BehaviorRestProfileAttribute.RelativePattern`; generated `GetRestProfiles()` output now resolves method and binding enum names from the actual enum members instead of hard-coded numeric ordinals; and `Cephalon.Behaviors.Http` now re-checks route-placeholder truth during `BehaviorRestProfileResolver` normalization so `MapProfile<TBehavior>()` stays fail-fast even when runtime falls back to direct attribute metadata — **Shipped** · GitHub issue `#327` · source-generator tests 26/26 + targeted hosting tests 26/26 + package-surface tests 1/1
- ENG-058-T65 typed REST endpoint runtime binding descriptors: `Cephalon.Abstractions` now exposes `RestEndpointBindingDescriptor` and `RestEndpointBindingSource` as the transport-owned runtime contract, `RestEndpointRuntimeDescriptor` plus `snapshot.RestEndpoints` now publish explicit profile-driven binding plans through the first-class `BindingDescriptors` property, `Cephalon.Behaviors.Http` now adapts behavior-authored binding metadata into that transport contract during module-owned REST materialization, and `Cephalon.AspNetCore` no longer duplicates the same plan into `metadata.bindingDescriptors` — **Shipped** · GitHub issue `#329` · targeted hosting tests 10/10 + package-surface tests 1/1
- ENG-058-T66 REST projection precedence and suppression visibility: `Cephalon.Abstractions` now exposes `IRestEndpointCandidateRuntimeCatalog`, `IRestEndpointCandidateRuntimeRegistry`, `RestEndpointCandidateRuntimeDescriptor`, and `RestEndpointCandidateStatus`; `Cephalon.AspNetCore` now publishes `/engine/rest-endpoint-candidates` plus `/engine/rest-endpoint-candidates/{candidateId}` and carries the same answer through `snapshot.RestEndpointCandidates`; and `Cephalon.Behaviors.Http` now resolves precedence across normalized module-owned behavior projections so explicit module DSL mappings suppress lower-precedence `MapProfile<TBehavior>()` candidates for the same behavior by default while keeping published-versus-suppressed truth, winning candidate ids, and suppression reasons operator-visible — **Shipped** · GitHub issue `#331` · targeted hosting tests 28/28 + package-surface tests 1/1
- ENG-058-T67 low-code generated module-owned REST projections: `Cephalon.Behaviors.Http` now exposes `IRestBehaviorEndpointGroupBuilder.MapGeneratedProfiles()` plus `MapGeneratedProfiles(string behaviorIdPrefix)` so an owning module can publish every matching profiled behavior beneath one owned route group without restating each behavior individually; `Cephalon.Behaviors.SourceGen` now emits `GetRestProfileBehaviorTypes()` alongside `GetRestProfiles()` so the generated path can stay sourcegen-first; `BehaviorRestProfileResolver` now falls back only to a bounded scan of the explicit owning module assembly when those generated type hints are unavailable; and the normalized candidate pipeline now keeps the effective behavior-projection precedence truthful as `explicit module DSL > MapProfile<TBehavior>() > MapGeneratedProfiles(...)`, with generated shorthand published as `metadata.authoringStyle = behavior-module-generated` and lower-precedence candidates still visible through `/engine/rest-endpoint-candidates` — **Shipped** · GitHub issue `#332` · source-generator tests 26/26 + targeted hosting tests 33/33 + package-surface tests 2/2
- ENG-058-T68 REST endpoint governance and controlled config suppression: `Cephalon.Abstractions` now exposes `IRestEndpointSuppressionRuntimeCatalog` plus `RestEndpointSuppressionDescriptor`, `RestEndpointCandidateRuntimeDescriptor` now distinguishes config-driven governance through `SuppressedBySuppressionId`, `Cephalon.AspNetCore` now binds shorthand-governance rules from `RestApi:Suppressions` and publishes them through `/engine/rest-endpoint-suppressions`, `/engine/rest-endpoint-suppressions/{suppressionId}`, and `snapshot.RestEndpointSuppressions`, shorthand-suppression rules now fail fast when both `Behaviors` and `Modules` are missing, and `Cephalon.Behaviors.Http` now resolves overlapping matching rules deterministically before applying suppression ahead of precedence resolution so ASP.NET Core hosts can suppress `MapProfile<TBehavior>()` or `MapGeneratedProfiles(...)` candidates without rewriting route shape or overriding explicit module DSL/manual module-owned REST endpoints — **Shipped** · GitHub issue `#333` · targeted hosting tests 40/40 + package-surface tests 2/2
- ENG-058-T69 constrained shorthand API-version override governance: `Cephalon.Abstractions` now exposes `IRestEndpointOverrideRuntimeCatalog` plus `RestEndpointOverrideDescriptor`, `RestEndpointCandidateRuntimeDescriptor` now surfaces config-governed version rewrites through `AppliedOverrideId`, `Cephalon.AspNetCore` now binds shorthand-governance rules from `RestApi:Overrides` and publishes them through `/engine/rest-endpoint-overrides`, `/engine/rest-endpoint-overrides/{overrideId}`, and `snapshot.RestEndpointOverrides`, override rules now fail fast when both `Behaviors` and `Modules` are missing or when `ApiVersionMajor` is missing/non-positive, and `Cephalon.Behaviors.Http` now resolves overlapping matching rules deterministically before retargeting descriptor-backed `MapProfile<TBehavior>()` or `MapGeneratedProfiles(...)` candidates to another effective API major version without overriding explicit module DSL/manual module-owned REST endpoints or shorthand groups that already declare `.ApiVersion(...)` explicitly — **Shipped** · GitHub issue `#334` · targeted hosting tests 49/49 + package-surface tests 2/2
- ENG-058-T70 constrained shorthand REST method override governance: `Cephalon.Abstractions` now extends `RestEndpointOverrideDescriptor` so shorthand-governance rules can also declare an effective HTTP `Method`, `Cephalon.AspNetCore` now binds that same action from `RestApi:Overrides`, override rules now fail fast when they omit all override actions or declare an unsupported method, and `Cephalon.Behaviors.Http` now resolves shorthand overrides into an effective projection that ASP.NET Core materializes directly so descriptor-backed `MapProfile<TBehavior>()` and `MapGeneratedProfiles(...)` candidates can move to another effective HTTP method without letting mapped endpoints drift away from `/engine/rest-endpoint-candidates`, `/engine/rest-endpoints`, `/engine/rest-endpoint-overrides`, or `snapshot.RestEndpointOverrides`; explicit module DSL/manual routes remain authoritative, and shorthand groups with explicit `.ApiVersion(...)` still win for version selection while method overrides can still apply — **Shipped** · GitHub issue `#335` · REST projection/hosting tests 54/54 + package-surface tests 2/2
- ENG-058-T71 constrained shorthand REST pattern override governance: `Cephalon.Abstractions` now extends `RestEndpointOverrideDescriptor` so shorthand-governance rules can also declare a relative route `Pattern`, `Cephalon.AspNetCore` now binds that same action from `RestApi:Overrides`, override rules now fail fast when they omit all override actions or declare an invalid route pattern, and `Cephalon.Behaviors.Http` now resolves shorthand overrides into an effective projection that ASP.NET Core materializes directly so descriptor-backed `MapProfile<TBehavior>()` and `MapGeneratedProfiles(...)` candidates can move to another placeholder-preserving relative route pattern without letting mapped endpoints drift away from `/engine/rest-endpoint-candidates`, `/engine/rest-endpoints`, `/engine/rest-endpoint-overrides`, or `snapshot.RestEndpointOverrides`; pattern overrides can change static segments and reorder existing placeholders but fail fast when they add, remove, or rename placeholders, while explicit module DSL/manual routes remain authoritative and shorthand groups with explicit `.ApiVersion(...)` still win for version selection — **Shipped** · GitHub issue `#336` · REST projection/hosting tests 61/61 + package-surface tests 2/2
- ENG-058-T72 constrained shorthand REST binding override governance: `RestEndpointOverrideDescriptor` now also carries explicit `Bindings` actions, `Cephalon.AspNetCore` now binds that action from `RestApi:Overrides`, and `Cephalon.Behaviors.Http` now resolves shorthand overrides into one effective binding plan that ASP.NET Core materializes and revalidates directly so descriptor-backed `MapProfile<TBehavior>()` and `MapGeneratedProfiles(...)` candidates can replace their explicit route/query/header/body binding plan without letting mapped endpoints drift away from `/engine/rest-endpoint-candidates`, `/engine/rest-endpoints`, `/engine/rest-endpoint-overrides`, or `snapshot.RestEndpointOverrides`; binding overrides still leave unbound route placeholders and remaining request-body fields to fill object properties deterministically, invalid effective plans such as body bindings on `GET` now fail fast, and explicit module DSL/manual routes remain authoritative — **Shipped** · GitHub issue `#337` · REST projection/hosting tests 65/65
- ENG-058-T73 constrained shorthand REST placeholder rename governance: `Cephalon.Behaviors.Http` now lets `RestApi:Overrides` rename shorthand route placeholders when the effective explicit route-binding plan covers the renamed placeholder set exactly, so descriptor-backed `MapProfile<TBehavior>()` and `MapGeneratedProfiles(...)` candidates can move from routes such as `/{orderId}` to `/lookup/{id}` without letting mapped endpoints drift away from `/engine/rest-endpoint-candidates`, `/engine/rest-endpoints`, `/engine/rest-endpoint-overrides`, or `snapshot.RestEndpointOverrides`; renamed placeholders still fail fast when the effective route plan relies on inference instead of explicit route coverage, placeholder additions or removals remain later work, and explicit module DSL/manual routes remain authoritative — **Shipped** · GitHub issue `#338` · REST projection/hosting tests 68/68
- ENG-058-T74 constrained shorthand REST placeholder removal governance: `Cephalon.Behaviors.Http` now lets `RestApi:Overrides` remove shorthand route placeholders when the original projection already exposes an explicit route-binding plan that covers the original placeholder set and the effective explicit binding plan keeps every affected originally route-bound property explicitly bound, so descriptor-backed `MapProfile<TBehavior>()` and `MapGeneratedProfiles(...)` candidates can move from routes such as `/{orderId}` to `/lookup` without letting mapped endpoints drift away from `/engine/rest-endpoint-candidates`, `/engine/rest-endpoints`, `/engine/rest-endpoint-overrides`, or `snapshot.RestEndpointOverrides`; removal attempts still fail fast when the original route shape relied on inference or when affected properties lose explicit binding coverage, placeholder additions remain later work, and explicit module DSL/manual routes remain authoritative — **Shipped** · GitHub issue `#339` · REST projection/hosting tests 73/73
- ENG-058-T75 constrained shorthand REST placeholder addition governance: `Cephalon.Behaviors.Http` now lets `RestApi:Overrides` add shorthand route placeholders when the effective explicit route-binding plan covers the full final placeholder set and every newly route-bound property was already explicitly bound in the original projection, so descriptor-backed `MapProfile<TBehavior>()` and `MapGeneratedProfiles(...)` candidates can move from routes such as `/{orderId}` to `/lookup/{orderId}/items/{quantity}` without letting mapped endpoints drift away from `/engine/rest-endpoint-candidates`, `/engine/rest-endpoints`, `/engine/rest-endpoint-overrides`, or `snapshot.RestEndpointOverrides`; addition attempts still fail fast when final route coverage is incomplete or when the override tries to promote an implicitly bound property into the public route, and explicit module DSL/manual routes remain authoritative — **Shipped** · GitHub issue `#340` · REST projection/hosting tests 78/78
- ENG-058-T76 constrained shorthand REST implicit body-fallback route promotion: `Cephalon.Behaviors.Http` now lets `RestApi:Overrides` add shorthand route placeholders when the effective explicit route-binding plan covers the full final placeholder set and every newly route-bound property was either already explicitly bound in the original projection or, for `POST`/`PUT`/`PATCH`, already part of the original deterministic remaining-body fallback surface, so descriptor-backed `MapProfile<TBehavior>()` and `MapGeneratedProfiles(...)` candidates can move values from implicit body fallback into the public route without letting mapped endpoints drift away from `/engine/rest-endpoint-candidates`, `/engine/rest-endpoints`, `/engine/rest-endpoint-overrides`, or `snapshot.RestEndpointOverrides`; promotion attempts still fail fast when final route coverage is incomplete, when the source projection did not accept body input, or when the override would promote any other implicit property into the public route, and explicit module DSL/manual routes remain authoritative — **Shipped** · GitHub issue `#341` · REST projection/hosting tests 82/82
- ENG-058-T77 shorthand REST governance selector expansion: `RestApi:Suppressions` and `RestApi:Overrides` now let ASP.NET Core hosts refine descriptor-backed `MapProfile<TBehavior>()` and `MapGeneratedProfiles(...)` candidates with `ApiVersionMajors`, `Methods`, `RelativePatterns`, and `RouteGroupPrefixes`; those selectors match the original shorthand candidate shape before override actions are applied, suppression now preserves that same original-shape contract even when an override later rewrites the final published route, specificity now also considers the expanded selector dimensions and narrower selector sets, the runtime suppression/override catalogs now expose the selector arrays directly, and explicit module DSL/manual routes remain authoritative — **Shipped** · GitHub issue `#342` · REST projection/hosting tests 91/91 + package-surface tests 53/53
- ENG-058-T78 shorthand REST original-projection runtime visibility: `Cephalon.Abstractions` now exposes `RestEndpointCandidateProjectionDescriptor` plus `RestEndpointCandidateRuntimeDescriptor.OriginalProjection` so operator tooling can compare original shorthand route, method, version, route-group prefix, relative pattern, and binding-plan truth against the final effective `ProjectedEndpoint`; `Cephalon.Behaviors.Http` now publishes that typed original projection before host overrides are applied while keeping `ProjectedEndpoint` as the final mapped answer; and `/engine/rest-endpoint-candidates` plus `snapshot.RestEndpointCandidates` now serialize both surfaces without hiding override-driven rewrites — **Shipped** · GitHub issue `#343` · REST projection/hosting tests 91/91 + package-surface tests 53/53
- ENG-058-T79 merge-mode shorthand REST binding overrides: `Cephalon.Abstractions` now exposes `RestEndpointOverrideBindingMode`, `RestEndpointOverrideDescriptor` plus `RestEndpointOverrideOptions` now surface the typed binding-mode contract, `Cephalon.AspNetCore` now binds `RestApi:Overrides:*:BindingMode`, and `Cephalon.Behaviors.Http` now supports `MergeExplicit` property-by-property binding patches in addition to the default full-plan `ReplaceExplicit` behavior so hosts can retarget shorthand bindings without restating every untouched explicit descriptor; runtime override catalogs and snapshots now keep that merge-versus-replace governance truth visible, `MergeExplicit` without `Bindings` now fails fast, and explicit module DSL/manual routes remain authoritative — **Shipped** · GitHub issue `#344` · REST projection/hosting tests 96/96 + package-surface tests 53/53
- ENG-058-T80 grouped REST publication visibility catalog: `Cephalon.Abstractions` now exposes `IRestEndpointPublicationGroupRuntimeCatalog` plus `RestEndpointPublicationGroupDescriptor`, `Cephalon.AspNetCore` now publishes `/engine/rest-endpoint-publication-groups`, `/engine/rest-endpoint-publication-groups/{behaviorId}`, and `snapshot.RestEndpointPublicationGroups`, and the host now groups the existing candidate-level truth per behavior so operators can inspect published candidates, precedence-suppressed candidates, governance-suppressed candidates, the winning precedence rank when one exists, and the ordered candidate set without manually joining several runtime surfaces — **Shipped** · GitHub issue `#345` · REST projection/hosting tests 41/41 + package-surface tests 53/53
- ENG-058-T81 low-code inline module-owned REST authoring: `Cephalon.Behaviors.Http` now exposes `RestBehaviorEngineBuilderExtensions.AddRestBehaviorModule<TMarker>()` so hosts can register a real behavior-backed REST module without authoring a dedicated `RestBehaviorModuleBase` subclass; the helper still drives the same normalized projection/materialization/candidate/publication-group/governance pipeline as the class-based DSL, uses `TMarker` as the generated-profile source-assembly marker and as the reusable helper's distinct module-type identity, keeps `[AppBehavior]` alone from publishing public REST, and now proves both `MapProfile<TBehavior>()` and `MapGeneratedProfiles(...)` through the inline path in hosting coverage — **Shipped** · GitHub issue `#346` · REST runtime/hosting tests 43/43 + package-surface tests 54/54
- ENG-058-T82 bounded shorthand REST route-group-prefix overrides: `Cephalon.Abstractions` now extends the shorthand override contract with `RouteGroupPrefix`, `Cephalon.AspNetCore` now binds that action from `RestApi:Overrides` and keeps it visible through `/engine/rest-endpoint-overrides` plus `snapshot.RestEndpointOverrides`, `Cephalon.Behaviors.Http` now validates that published group-prefix rewrites stay beneath the active REST root, contain no placeholders, and do not silently change effective API-version truth, and the ASP.NET Core materializer now splits effective shorthand route groups when only some candidates in one authored group are remapped so actual HTTP routes stay aligned with `/engine/rest-endpoint-candidates`, `/engine/rest-endpoints`, and the runtime snapshot — **Shipped** · GitHub issue `#347` · REST projection/hosting tests 111/111
- ENG-058-T83 merge-mode shorthand REST binding withdrawals: `Cephalon.Abstractions` now extends `RestEndpointOverrideDescriptor` with `RemovedBindingProperties`, `Cephalon.AspNetCore` now binds `RestApi:Overrides:*:RemovedBindingProperties` and keeps the removed-property list visible through `/engine/rest-endpoint-overrides` plus `snapshot.RestEndpointOverrides`, and `Cephalon.Behaviors.Http` now lets `BindingMode = MergeExplicit` withdraw selected original explicit bindings without restating untouched descriptors so descriptor-backed `MapProfile<TBehavior>()` and `MapGeneratedProfiles(...)` candidates can fall back to the remaining deterministic route/body contract without drifting away from `/engine/rest-endpoint-candidates`, `/engine/rest-endpoints`, or the runtime snapshot; removal-only rules now normalize to merge mode automatically, `ReplaceExplicit` cannot pair with removals, one merge rule cannot both remove and override the same property, removal targets must already exist in the source shorthand explicit binding plan, and the runtime override catalog now keeps both binding mode and removed-property truth visible — **Shipped** · GitHub issue `#348` · REST projection/hosting tests 117/117 + package-surface tests 54/54
- ENG-058-T84 low-code generated module-path helpers with explicit ownership: `Cephalon.Behaviors.Http` now exposes `IRestBehaviorModuleBuilder.GroupFromBehaviorIdPrefix(string)` so modules can derive `/foo/bar` from `foo.bar` for generated route groups without repeating both forms manually, and `RestBehaviorEngineBuilderExtensions.AddGeneratedRestBehaviorModule<TMarker>()` now wraps that same derivation for inline generated modules while still materializing a real module, feeding the same generated-profile projection/materialization/candidate/publication-group/governance pipeline, and failing fast when the behavior-id prefix cannot produce a deterministic route-group prefix — **Shipped** · GitHub issue `#349` · REST runtime/hosting tests 51/51 + package-surface tests 55/55
- ENG-058-T85 stable shorthand candidate-id governance selectors: shorthand candidate ids now resolve from the original shorthand projection before host-level overrides are applied, `RestApi:Suppressions` and `RestApi:Overrides` now accept `CandidateIds` so a host can govern one exact `MapProfile<TBehavior>()` or `MapGeneratedProfiles(...)` candidate without composing a behavior/module-plus-selector rule first, runtime suppression/override catalogs plus the snapshot now surface those configured candidate ids directly, selector specificity now prefers candidate-targeted rules before broader selector matches, and `ProjectedEndpoint` continues to carry the effective mapped endpoint identity after override rewrites — **Shipped** · GitHub issue `#350` · REST projection tests 72/72 + REST runtime/hosting tests 53/53 + package-surface tests 56/56
- ENG-058-T86 shorthand candidate projected endpoint metadata alignment: `Cephalon.Behaviors.Http` now uses one shared operation-name/documentation convention path for both shorthand candidate resolution and final behavior-backed endpoint materialization, so `ProjectedEndpoint` in `/engine/rest-endpoint-candidates` now keeps endpoint name plus summary/description metadata aligned with `/engine/rest-endpoints` for `MapProfile<TBehavior>()` and `MapGeneratedProfiles(...)` candidates instead of leaving that operator-facing metadata incomplete until after ASP.NET Core materialization — **Shipped** · GitHub issue `#351` · REST projection tests 73/73 + REST runtime/hosting tests 53/53
- ENG-058-T87 governance match visibility for shorthand REST candidates: `Cephalon.Abstractions` now extends `RestEndpointCandidateRuntimeDescriptor` with ordered `MatchedSuppressionIds` and `MatchedOverrideIds`, `Cephalon.Behaviors.Http` now preserves the full specificity-ordered suppression/override match trace for shorthand `MapProfile<TBehavior>()` and `MapGeneratedProfiles(...)` candidates while leaving `SuppressedBySuppressionId` and `AppliedOverrideId` as the selected-winner truth, and the hosting/runtime path now keeps overlapping governance matches visible in `/engine/rest-endpoint-candidates` plus the runtime snapshot without changing the existing specificity or precedence model — **Shipped** · GitHub issue `#352` · REST projection tests 73/73 + REST runtime/hosting tests 53/53 + package-surface tests 56/56
- ENG-058-T88 constrained shorthand REST implicit query-fallback route promotion: `Cephalon.Behaviors.Http` now lets `RestApi:Overrides` add shorthand route placeholders when the effective explicit route-binding plan covers the full final placeholder set and, for shorthand candidates with no explicit binding plan, every newly route-bound property was already part of the original implicit query-fallback surface, so shorthand `GET`-style profiles that still use the default query-plus-route merge path can promote selected values into the public route without losing runtime truth across `/engine/rest-endpoint-candidates`, `/engine/rest-endpoints`, `/engine/rest-endpoint-overrides`, and `snapshot.RestEndpointOverrides`; explicit-binding shorthand candidates remain on the stricter explicit-binding path, and promotion still fails fast when final route coverage is incomplete or when the override would promote any other implicit property into the public route — **Shipped** · GitHub issue `#353` · REST projection tests 74/74 + REST runtime/hosting tests 54/54
- ENG-058-T89 preserve implicit query fallback for partially explicitized shorthand REST overrides: `Cephalon.Behaviors.Http` now preserves the remaining implicit query-fallback surface when a shorthand candidate originally had no explicit binding plan and a host override adds only partial explicit bindings, so reshape-only overrides no longer silently drop untouched query-bound properties; `/engine/rest-endpoint-candidates`, `/engine/rest-endpoints`, and `snapshot` now keep that truth visible through additive `metadata.bindingFallbackMode = preserve-source-implicit-fallback`, while explicit-binding shorthand candidates remain on the stricter existing path — **Shipped** · GitHub issue `#354` · REST projection tests 75/75 + REST runtime/hosting tests 55/55
- ENG-058-T90 typed shorthand REST binding-fallback runtime contract: `Cephalon.Abstractions` now exposes `RestEndpointBindingFallbackMode` plus typed `BindingFallbackMode` properties on `RestEndpointCandidateProjectionDescriptor` and `RestEndpointRuntimeDescriptor`, `Cephalon.Behaviors.Http` plus `Cephalon.AspNetCore` now project the preserved implicit query-fallback answer onto the original shorthand projection and the effective published endpoint directly, and additive `metadata.bindingFallbackMode = preserve-source-implicit-fallback` remains compatibility-only metadata instead of the canonical runtime truth — **Shipped** · GitHub issue `#355` · REST projection/runtime hosting tests 130/130 + package-surface tests 57/57
- ENG-058-T91 published REST endpoint candidate provenance link: `Cephalon.Abstractions` now exposes nullable `RestEndpointRuntimeDescriptor.CandidateId`, `Cephalon.Behaviors.Http` now stamps published behavior-backed endpoints with the original candidate id during materialization, `Cephalon.AspNetCore` now projects that same provenance through `/engine/rest-endpoints` plus `snapshot.RestEndpoints`, manual endpoints stay `null`, and overridden shorthand endpoints keep pointing back to the original candidate instead of drifting to the effective published endpoint id — **Shipped** · GitHub issue `#356` · targeted REST runtime/hosting tests 4/4 + package-surface tests 1/1
- ENG-058-T92 typed published REST authoring-style runtime contract: `Cephalon.Abstractions` now exposes first-class `RestEndpointRuntimeDescriptor.AuthoringStyle`, `Cephalon.AspNetCore` now projects explicit module DSL, profile shorthand, generated shorthand, behavior-helper, and manual Minimal API values through `/engine/rest-endpoints` plus `snapshot.RestEndpoints`, `Cephalon.Behaviors.Http` now keeps candidate-projected endpoints aligned through the same runtime descriptor surface, and additive `metadata.authoringStyle` remains compatibility-only metadata instead of the canonical published-endpoint authorship answer — **Shipped** · GitHub issue `#357` · targeted REST runtime/hosting tests 5/5 + package-surface tests 1/1
- ENG-058-T93 typed published REST route-boundary runtime contract: `Cephalon.Abstractions` now exposes first-class `RestEndpointRuntimeDescriptor.RouteGroupPrefix` plus `RelativePattern`, `Cephalon.AspNetCore` now projects the resolved published route-group boundary for behavior-backed and behavior-helper endpoints through `/engine/rest-endpoints` plus `snapshot.RestEndpoints` while manual Minimal API endpoints stay `null`, `Cephalon.Behaviors.Http` keeps candidate-projected endpoints aligned through the same typed route-boundary surface, and additive `metadata.routeGroupPrefix` plus `metadata.relativePattern` remain compatibility-only metadata instead of the canonical published-endpoint route-boundary answer — **Shipped** · GitHub issue `#358` · targeted REST runtime/hosting tests 14/14 + package-surface tests 1/1
- ENG-058-T94 typed published REST behavior-type runtime contract: `Cephalon.Abstractions` now exposes first-class nullable `RestEndpointRuntimeDescriptor.BehaviorType`, `Cephalon.AspNetCore` now projects the concrete behavior implementation identity for behavior-backed and behavior-helper published endpoints through `/engine/rest-endpoints` plus `snapshot.RestEndpoints` while pure manual Minimal API endpoints stay `null`, `Cephalon.Behaviors.Http` keeps shorthand candidate `ProjectedEndpoint` entries aligned through the same runtime descriptor surface, and additive `metadata.behaviorType` remains compatibility-only metadata instead of the canonical published-endpoint behavior-implementation answer — **Shipped** · GitHub issue `#359` · targeted REST runtime/hosting tests 3/3 + package-surface tests 1/1
- ENG-058-T95 typed published REST source-id runtime contract: `Cephalon.Abstractions` now exposes first-class nullable `RestEndpointRuntimeDescriptor.SourceId`, `Cephalon.AspNetCore` now projects the published source identity for behavior-backed, behavior-helper, and manual module-owned REST endpoints through `/engine/rest-endpoints` plus `snapshot.RestEndpoints`, `Cephalon.Behaviors.Http` keeps shorthand candidate `ProjectedEndpoint` entries aligned through the same runtime descriptor surface, and additive `metadata.sourceId` remains compatibility-only metadata instead of the canonical published-endpoint source-identity answer — **Shipped** · GitHub issue `#360` · targeted REST runtime/hosting tests 3/3 + package-surface tests 1/1
- ENG-058-T96 typed route-boundary materialization cleanup: `Cephalon.Behaviors.Http` now resolves shorthand published route groups from first-class `ProjectedEndpoint.RouteGroupPrefix` during ASP.NET Core materialization, so `metadata.routeGroupPrefix` stays compatibility-only even for internal route mapping; targeted hosting coverage now proves materialization still succeeds when the typed property is present and compatibility metadata is absent, while existing route-group override and split-group coverage stays green — **Shipped** · GitHub issue `#361` · targeted hosting tests 6/6
- ENG-058-T97 shorthand REST endpoint-metadata override governance: `Cephalon.Abstractions` now extends `RestEndpointOverrideDescriptor` with `EndpointName`, `Summary`, and `Description`, `Cephalon.AspNetCore` now binds and publishes those shorthand override actions through `/engine/rest-endpoint-overrides` plus `snapshot.RestEndpointOverrides`, `Cephalon.Behaviors.Http` now projects the same effective endpoint metadata onto shorthand candidates and only marks metadata-only overrides as applied when they materially change the effective answer, and the ASP.NET Core runtime now keeps actual endpoint metadata plus `/engine/rest-endpoints` aligned with that same governed truth — **Shipped** · GitHub issue `#362` · targeted REST projection/runtime hosting tests 9/9 + package-surface tests 1/1
- ENG-058-T98 shorthand REST capability-boundary override governance: `Cephalon.Abstractions` now extends `RestEndpointOverrideDescriptor` with `RequiredCapabilityKey` and `RestEndpointRuntimeDescriptor` with first-class nullable `RequiredCapabilityKey`, `Cephalon.AspNetCore` now binds and publishes that shorthand override action through `/engine/rest-endpoint-overrides` plus `snapshot.RestEndpointOverrides`, materializes the effective capability boundary from actual endpoint metadata for both shorthand and manual module-owned REST routes, and now treats the last `RequireCapability(...)` declaration as authoritative so a later host override can supersede an earlier shorthand `configureEndpoint` guard without double-enforcing both keys, while `Cephalon.Behaviors.Http` now projects the same governed capability boundary onto shorthand candidates and applies it during materialization so `/engine/rest-endpoint-candidates`, `/engine/rest-endpoints`, and `snapshot` stay aligned with the actual runtime trust boundary — **Shipped** · GitHub issue `#363` · targeted hosting tests 192/192 + package-surface tests 65/65
- ENG-058-T99 shorthand REST capability-boundary clear governance: `Cephalon.Abstractions` now extends `RestEndpointOverrideDescriptor` with `ClearRequiredCapability`, `Cephalon.AspNetCore` now binds and publishes that shorthand override action through `/engine/rest-endpoint-overrides` plus `snapshot.RestEndpointOverrides`, rejects any one override rule that tries to both set `RequiredCapabilityKey` and clear it, and materializes the clear through actual endpoint metadata and `/engine/rest-endpoints` so the published runtime truth keeps `RequiredCapabilityKey = null` when the clear wins, while `Cephalon.Behaviors.Http` now projects that same clear onto shorthand candidates and applies it during materialization so `/engine/rest-endpoint-candidates`, `/engine/rest-endpoints`, and `snapshot` stay aligned when a host intentionally removes an inherited shorthand capability boundary — **Shipped** · GitHub issue `#364` · targeted hosting tests 144/144 + package-surface tests 65/65
- ENG-058-T100 published REST capability provenance: `Cephalon.Abstractions` now extends `RestEndpointRuntimeDescriptor` with nullable `OriginalRequiredCapabilityKey` plus `AppliedOverrideId`, `Cephalon.Behaviors.Http` now captures the source shorthand capability boundary during ASP.NET Core materialization before later host capability overrides run, and `Cephalon.AspNetCore` now projects that source-versus-effective capability lineage through `/engine/rest-endpoints` plus `snapshot.RestEndpoints` while suppressing endpoint-level `AppliedOverrideId` for capability-only no-op clear matches that do not change the published answer — **Shipped** · GitHub issue `#365` · targeted hosting tests 146/146 + package-surface tests 66/66
- ENG-058-T101 candidate no-op capability override truth: `Cephalon.Behaviors.Http` now registers shorthand runtime candidates after ASP.NET Core materialization and reconciles published candidate provenance against the actual mapped endpoint metadata, so `/engine/rest-endpoint-candidates` plus `snapshot.RestEndpointCandidates` now leave `AppliedOverrideId = null` when a capability-only override rule matches but does not change the effective published boundary, including both no-op clears and same-key capability rewrites, while `MatchedOverrideIds` still shows that the host rule matched — **Shipped** · GitHub issue `#367` · targeted REST projection/runtime hosting tests 147/147
- ENG-058-T102 published REST matched-override visibility: `Cephalon.Abstractions` now extends `RestEndpointRuntimeDescriptor` with ordered `MatchedOverrideIds`, `Cephalon.Behaviors.Http` now carries that same ordered shorthand override match set through behavior-backed endpoint materialization, and `Cephalon.AspNetCore` now projects it through `/engine/rest-endpoints` plus `snapshot.RestEndpoints` so the final published runtime answer keeps matched host override rules visible, including capability-only no-op matches that still leave `AppliedOverrideId = null` — **Shipped** · GitHub issue `#368` · targeted REST projection/runtime hosting tests 147/147 + package-surface tests 66/66
- ENG-058-T103 published REST original shorthand projection visibility: `Cephalon.Abstractions` now extends `RestEndpointRuntimeDescriptor` with nullable `OriginalProjection`, `Cephalon.Behaviors.Http` now carries that pre-override shorthand method/route/document-version/binding-plan truth through behavior-backed endpoint materialization, and `Cephalon.AspNetCore` now projects it through `/engine/rest-endpoints` plus `snapshot.RestEndpoints` so operators can compare original-versus-effective published endpoint shape without a candidate-catalog join while manual and behavior-helper endpoints stay `null` — **Shipped** · GitHub issue `#369` · targeted REST runtime/hosting tests 60/60 + package-surface tests 67/67
- ENG-058-T104 published REST original shorthand endpoint-metadata visibility: `Cephalon.Abstractions` now extends `RestEndpointRuntimeDescriptor` with nullable `OriginalEndpointName`, `OriginalSummary`, and `OriginalDescription`, `Cephalon.Behaviors.Http` now carries that pre-override shorthand endpoint-metadata truth through shorthand candidate resolution plus behavior-backed endpoint materialization, and `Cephalon.AspNetCore` now projects it through `/engine/rest-endpoints` plus `snapshot.RestEndpoints` while the candidate catalog keeps the same lineage visible on `ProjectedEndpoint`; manual and behavior-helper endpoints keep the original-metadata trio `null` — **Shipped** · GitHub issue `#370` · targeted REST runtime/hosting tests 60/60 + package-surface tests 68/68
- ENG-058-T105 shorthand REST endpoint-metadata clear governance: `Cephalon.Abstractions` now extends `RestEndpointOverrideDescriptor` and `Cephalon.AspNetCore` override options with `ClearEndpointName`, `ClearSummary`, and `ClearDescription`, both public contracts now fail fast when one rule tries to both set and clear the same endpoint-metadata field, `Cephalon.Behaviors.Http` now projects shorthand metadata clears into the same effective candidate/runtime truth used for publication, and `Cephalon.AspNetCore` now removes the effective endpoint name/summary/description from actual ASP.NET Core metadata plus `/engine/rest-endpoints` while preserving original shorthand metadata lineage through `OriginalEndpointName`, `OriginalSummary`, and `OriginalDescription` — **Shipped** · GitHub issue `#371` · targeted REST projection tests 93/93 + targeted REST runtime/hosting tests 61/61 + package-surface tests 68/68
- ENG-058-T106 published REST endpoint-metadata no-op override truth: `Cephalon.Behaviors.Http` now captures source shorthand endpoint metadata during ASP.NET Core materialization, reconciles metadata-only shorthand override provenance against the actual mapped endpoint metadata, and keeps `/engine/rest-endpoint-candidates`, `/engine/rest-endpoints`, and `snapshot` truthful when a matched metadata rewrite or clear rule does not change the published answer because the owning module already set or cleared the same metadata; those matches now keep `MatchedOverrideIds` visible while leaving `AppliedOverrideId = null` for the published candidate and endpoint runtime answers — **Shipped** · GitHub issue `#372` · targeted REST projection tests 96/96 + targeted REST runtime/hosting tests 63/63 + package-surface tests 68/68
- ENG-058-T107 truthful shorthand REST tag override governance: `Cephalon.Abstractions` now extends `RestEndpointOverrideDescriptor` plus `RestEndpointCandidateProjectionDescriptor` with `TagName`, `Cephalon.AspNetCore` now binds and publishes that shorthand override action through `/engine/rest-endpoint-overrides` plus `snapshot.RestEndpointOverrides` while carrying original shorthand tag lineage through `RestEndpointRuntimeDescriptor.OriginalProjection`, and `Cephalon.Behaviors.Http` now applies tag rewrites only when they materially change the effective published answer while splitting effective materialization groups by route-group prefix plus tag so actual ASP.NET Core endpoint tag metadata, `/engine/rest-endpoint-candidates`, `/engine/rest-endpoints`, and `snapshot` stay aligned when only some candidates in one authored group are retagged — **Shipped** · GitHub issue `#373` · targeted REST projection tests 100/100 + targeted REST runtime/hosting tests 64/64 + package-surface tests 70/70
- ENG-058-T108 truthful shorthand REST OpenAPI document-name governance: `Cephalon.Abstractions` now extends `RestEndpointOverrideDescriptor` plus `RestEndpointCandidateProjectionDescriptor` with `OpenApiDocumentName`, `Cephalon.AspNetCore` now binds and publishes that shorthand override action through `/engine/rest-endpoint-overrides` plus `snapshot.RestEndpointOverrides`, and `Cephalon.Behaviors.Http` now adds `.WithOpenApiDocumentName(...)`, keeps same-value document rewrites truthful, re-derives the effective document name from `ApiVersionMajor` only when the authored shorthand group did not pin one explicitly, and splits effective materialization groups by route-group prefix plus document name plus tag so actual ASP.NET Core endpoint-group metadata, `/engine/rest-endpoint-candidates`, `/engine/rest-endpoints`, and `snapshot` stay aligned when only some candidates in one authored group move documents — **Shipped** · GitHub issue `#374` · targeted REST projection tests 106/106 + targeted REST runtime/hosting tests 66/66 + package-surface tests 72/72
- ENG-058-T109 shorthand REST document-and-tag selector targeting: `Cephalon.Abstractions` now extends `RestEndpointSuppressionDescriptor` plus `RestEndpointOverrideDescriptor` with `OpenApiDocumentNames` and `TagNames`, `Cephalon.AspNetCore` now binds and publishes those selector arrays through `/engine/rest-endpoint-suppressions`, `/engine/rest-endpoint-overrides`, and `snapshot`, and `Cephalon.Behaviors.Http` now matches those selectors against the original shorthand document/tag identity before override actions are applied so hosts can target one of several same-behavior candidates without depending on rewritten route shape — **Shipped** · GitHub issue `#375` · targeted REST projection tests 108/108 + targeted REST runtime/hosting tests 68/68 + package-surface tests 73/73
- ENG-058-T110 shorthand REST clear-bindings governance: `Cephalon.Abstractions` now extends `RestEndpointOverrideDescriptor` and `Cephalon.AspNetCore` override options with `ClearBindings`, ASP.NET Core config binding plus `/engine/rest-endpoint-overrides` and `snapshot.RestEndpointOverrides` now publish that shorthand-only reset action, and `Cephalon.Behaviors.Http` now lets a host discard a shorthand candidate's explicit binding plan so request composition returns to the implicit route/query/body baseline while failing fast if the effective route would only remain satisfiable through removed explicit route-binding aliases — **Shipped** · GitHub issue `#376` · targeted REST projection tests 114/114 + targeted REST runtime/hosting tests 70/70 + package-surface tests 74/74

### Infrastructure — Phase 2 Developer Experience

- Test assembly split: `Cephalon.Tests` monolith (648 tests) split into `Cephalon.Tests.Support` (shared lib), `Cephalon.Tests.Composition` (327 tests — Composition + Behaviors + EventSourcing + Benchmarks), `Cephalon.Tests.Hosting` (200 tests — Hosting/Observability), `Cephalon.Tests.Tooling` (121 tests — Tooling + Scaffolding) — enables parallel test execution per assembly, faster incremental builds, and cleaner dependency boundaries — **Shipped** · 648/648 tests

### Sprint 32

- backlog and roadmap alignment: ENG-054 status updated to done (all 9 non-relational provider families shipped), ENG-056 status updated to done (phase-8 docs and reference-doc alignment complete), ENG-057 status updated to done (event-sourcing follow-through baseline delivered through core contracts plus 10 provider implementations), roadmap sprint alignment extended through Sprint 31, Phase 10 planning notes updated to reflect completion

### Sprint 33

- ENG-049 EF projection contributor: `Cephalon.Data.EntityFramework` now implements `IProjectionContributor` and registers EF-backed projection descriptors with `data.projections.entity-framework` capability plus `projections` runtime surface under `data-management` — closes projection persistence/runtime gap
- ENG-050 Wolverine dispatch observability: `Cephalon.Eventing.Wolverine` dispatch loop now instrumented with `System.Diagnostics.ActivitySource` (Producer spans per dispatch item) and `System.Diagnostics.Metrics.Meter` (attempts, successes, failures, retries counters + duration histogram), diagnostics convention extended from `4300-4303` to `4300-4305` — enables OpenTelemetry trace/metric capture
- ENG-051 status updated to done (all acceptance criteria met)
- ENG-055 validation script fix: `scripts/validate-phase8-conventions.ps1` updated to use new test assembly paths (`Cephalon.Tests.Composition`, `Cephalon.Tests.Hosting`, `Cephalon.Tests.Tooling`) after Infrastructure Phase 2 test split — **Shipped** · 648/648 tests

### Sprint 34

- comprehensive engine audit: WebSocket binding empty catch blocks replaced with high-performance `[LoggerMessage]` source-generated logging delegates (CA1848/CA1873 compliant), flaky test fix for `MapCephalonExposesCapturedStartupFailuresWhenPolicyDoesNotFailFast` using `TryGetProperty` pattern, architecture inventory and recommendations docs
- backlog alignment: ENG-049, ENG-050, ENG-052, ENG-053, ENG-055 status updated to done (all baseline acceptance criteria met, remaining work moved to follow-up sections), ENG-059 created for runtime hot-path benchmark expansion
- docs: `docs/architecture-inventory.md` (comprehensive engine component inventory), `docs/architecture-recommendations.md` (future pattern recommendations, deferred), `docs/architecture.md` cross-references, `docs/engine-roadmap.md` Sprint 33 entry and Phase 11/12/13 planning — **Shipped** · 648/648 tests

### Sprint 35

- ENG-059 runtime hot-path benchmark expansion: 6 new benchmark classes covering data layer dispatch (query/command/result-command), behavior dispatch (frozen-dictionary + compiled delegate), authorization evaluation (RBAC allow/deny paths), tenant resolution (by-id/hostname/default-fallback), event sourcing (append/read/version), outbox staging — 13 new benchmarks
- benchmark support: `BenchmarkHotPathTypes.cs` with data stubs, behavior stubs, authorization policy module, InMemoryBenchmarkEventStore, InMemoryBenchmarkOutbox, BenchmarkDomainEvent
- guardrail catalog expanded from 10 to 23 entries, `GuardrailValidatorTests` updated
- benchmark project now references `Cephalon.Behaviors` for behavior dispatch measurement — **Shipped** · 648/648 tests

### Sprint 36

- ENG-076 resilience contract and taxonomy baseline: `Cephalon.Abstractions` now exports `ResilienceSelection`, `RetrySelection`, `TimeoutSelection`, `CircuitBreakerSelection`, `BulkheadSelection`, and `RateLimitingSelection`; `Cephalon.Engine` now binds `Engine:Resilience` through matching settings types, projects the requested contract into `AppProfile.Resilience`, validates baseline numeric and algorithm values, and completes the built-in pattern taxonomy with `onion-architecture` plus `anti-corruption-layer`; `Cephalon.AspNetCore` now exposes `/engine/resilience`; the showcase sample now demonstrates the config contract; runtime enforcement through behavior/transport pipelines remains a later follow-through — **Shipped** · targeted composition tests 6/6 + hosting tests 1/1 + package-surface tests 52/52
- ENG-077 ASP.NET Core rate-limiting runtime baseline: `Cephalon.Abstractions` now exports `IRateLimitingRuntimeCatalog` plus `RateLimitingRuntimeDescriptor`; `Cephalon.AspNetCore` now enforces `Engine:Resilience:RateLimiting` for public HTTP endpoints through `Microsoft.AspNetCore.RateLimiting`, publishes `/engine/rate-limiting` plus `/engine/rate-limiting/{policyId}`, excludes operator/docs routes such as `/engine`, `/health`, `/openapi`, Scalar, `/favicon.ico`, and hosted reference docs, and keeps behavior-owned REST OpenAPI docs truthful by auto-publishing `429` when the limiter is active; `/engine/snapshot` now carries `RateLimitingPolicies`, and the showcase sample demonstrates the effective runtime surface end to end — **Shipped** · targeted hosting tests 7/7 + package-surface tests 52/52
- ENG-078 ASP.NET Core endpoint-scoped rate-limiting overrides: `Cephalon.Abstractions` now exports `RateLimitingOverrideSelection`, `Cephalon.Engine` now binds `Engine:Resilience:RateLimiting:Overrides` into `AppProfile.Resilience`, validates behavior/transport-scoped override intent, and `Cephalon.AspNetCore` now resolves named endpoint policies with behavior-aware precedence across module-owned REST routes plus generic behavior HTTP bindings while keeping `/engine/rate-limiting`, `/engine/snapshot`, and behavior-owned REST OpenAPI `429` answers truthful per endpoint; the showcase sample now demonstrates a behavior+transport override end to end — **Shipped** · targeted hosting tests 10/10 + package-surface tests 52/52
- ENG-079 behavior-execution resilience runtime baseline: `Cephalon.Abstractions` now exports `BehaviorExecutionResilienceSelection`, `BehaviorResilienceRuntimeDescriptor`, and `IBehaviorResilienceRuntimeCatalog`; `Cephalon.Behaviors` now inserts a shared execution middleware seam into `BehaviorDispatcher`, enforces the first shared timeout-plus-bulkhead baseline through `Microsoft.Extensions.Resilience` across behavior dispatch, normalizes effective timeout truth to the currently enforced single execution budget, suppresses disabled-only policy declarations, and publishes `/engine/behavior-resilience` plus `snapshot.BehaviorResiliencePolicies`; `Cephalon.Behaviors.Http` now also translates timeout and bulkhead rejections into truthful REST `503` / `429` responses while keeping behavior-owned REST OpenAPI status metadata aligned per route; retry and circuit breaker remain contract-only at this layer for now — **Shipped** · targeted composition tests 4/4 + hosting tests 3/3 + package-surface tests 52/52
- ENG-080 behavior-execution resilience overrides baseline: `Cephalon.Abstractions` now exports `BehaviorExecutionResilienceOverrideSelection`, extends `BehaviorResilienceRuntimeDescriptor` with targeted behavior/transport ids, and extends `IBehaviorResilienceRuntimeCatalog` with effective `Resolve(behaviorId, transportId)` lookup; `Cephalon.Engine` now binds additive `Engine:Resilience:BehaviorExecution:Overrides` into `AppProfile.Resilience` and validates scoped override intent; `Cephalon.Behaviors` now resolves default plus named override policies with `behavior+transport > behavior > transport > default` precedence, keeps explicit disable overrides visible in `/engine/behavior-resilience` plus `snapshot.BehaviorResiliencePolicies`, stamps active transport ids into dispatch metadata, and lets REST/OpenAPI `429` / `503` answers reflect the effective per-route behavior-execution policy instead of the shared default alone; the showcase sample now demonstrates a behavior+transport override end to end — **Shipped** · targeted composition tests 8/8 + hosting tests 4/4 + package-surface tests 52/52
- ENG-081 behavior-execution circuit-breaker runtime baseline: `Cephalon.Abstractions` now exports `BehaviorResilienceExceptionContext`, `BehaviorResilienceExceptionHandling`, and `IBehaviorResilienceExceptionClassifier`; `Cephalon.Behaviors` now enforces shared circuit-breaker policy through the existing behavior-execution middleware seam, classifies failures conservatively, keeps per-policy breaker state visible through `/engine/behavior-resilience` plus `snapshot.BehaviorResiliencePolicies`, and applies the same behavior/transport override resolution to timeout, circuit-breaker, and bulkhead answers; `Cephalon.Behaviors.Http` now also translates open-circuit rejections into truthful REST `503` responses with retry-after details while documenting `503` for routes whose effective circuit breaker can reject requests even when timeout is disabled — **Shipped** · targeted composition tests 8/8 + hosting tests 6/6 + package-surface tests 52/52
- ENG-082 behavior-execution retry eligibility baseline: `Cephalon.Abstractions` now exports `BehaviorIdempotencyAttribute` plus `BehaviorIdempotencyMode`, `BehaviorResilienceExceptionContext` now carries declared behavior idempotency, and `Cephalon.Behaviors` now resolves that contract during dispatch so the default resilience classifier can mark retry-eligible transient failures only for explicitly idempotent behaviors while `/engine/behavior-resilience` plus `snapshot.BehaviorResiliencePolicies` keep policy-level `retryEligibilityMode = behavior-dependent` truth and `IBehaviorResilienceRuntimeCatalog.Resolve(behaviorId, transportId)` can now answer per-behavior `behaviorIdempotency` plus retry eligibility as `eligible`, `ineligible`, or `unknown`; automatic retry execution itself remains a later follow-through — **Shipped** · targeted composition tests 10/10 + hosting tests 1/1 + package-surface tests 52/52
- ENG-083 behavior-execution retry runtime baseline: `Cephalon.Behaviors` now enforces shared retry policy in the existing behavior-execution middleware seam, keeps retry gated by explicit `BehaviorIdempotencyAttribute` plus classifier decisions for transient failures, composes retry with total timeout, per-attempt timeout, circuit breaker, and bulkhead in the shared dispatch pipeline, and publishes truthful retry attempts/backoff/delay/jitter metadata through `/engine/behavior-resilience`, `IBehaviorResilienceRuntimeCatalog.Resolve(behaviorId, transportId)`, and `snapshot.BehaviorResiliencePolicies`; behavior- or transport-scoped overrides can now disable inherited retry for narrower surfaces without custom dispatch glue — **Shipped** · targeted composition tests 14/14 + hosting tests 1/1 + package-surface tests 52/52
- ENG-084 built-in GraphQL protocol split plus long-lived transport rate-limiting truth: `Cephalon.AspNetCore.GraphQL` now maps separate built-in GraphQL HTTP, schema, SSE, and WebSocket surfaces under the shared `ApiRoutes:Prefixes` contract (`/graphql`, `/graphql/schema`, `/graphql-sse`, and `/graphql-ws` by default), `Cephalon.AspNetCore` now canonicalizes built-in GraphQL transport ids as `graphql`, `graphql-sse`, and `graphql-ws`, and `/engine/rate-limiting` plus `snapshot.RateLimitingPolicies` now publish truthful long-lived metadata such as `transportKind`, `transportSemantics`, `enforcementMoment`, and `longLivedTransportIds` so stream/connection policies stop collapsing into generic endpoint labels — **Shipped** · GraphQL transport hosting tests 2/2 + targeted hosting regression 6/6
- ENG-085 GraphQL authoring helpers plus protocol-level route prove-out: `Cephalon.AspNetCore.GraphQL` now exposes shared `ConfigureGraphQLMutation(...)` and `ConfigureGraphQLSubscription(...)` helpers alongside query registration, `IGraphQLModule` now documents the shared authoring path plus the need for a concrete Hot Chocolate subscription provider when modules expose subscription fields, and the hosting coverage now proves configured schema, SSE, and WebSocket routes with real SDL downloads, `text/event-stream` payloads, and `graphql-transport-ws` `connection_ack` / `next` / `complete` frames instead of path-only reachability while also locking the additive shared-root story across multiple GraphQL modules — **Shipped** · GraphQL transport hosting tests 5/5 + targeted hosting regression 9/9 + package-surface tests 52/52

### Sprint 31 follow-through

- ENG-069 event-dispatch runtime operator surfaces: host-agnostic event-dispatch runtime/state read contracts now live in `Cephalon.Abstractions`, `/engine/snapshot` now carries `EventDispatchRuntimes` plus `EventDispatchStates`, ASP.NET Core now exposes `/engine/event-dispatch-runtimes` and `/engine/event-dispatches`, `Cephalon.Eventing.Wolverine` now projects its managed loop through those routes truthfully, and the showcase sample now wires the official Wolverine path end to end — **Shipped**
- ENG-070 outbox dispatch-ownership baseline: the engine now enriches `OutboxDescriptor` with a first-class `DispatchPolicy`, `IOutboxDispatchPolicyCatalog` now resolves `disabled`, `consumer-managed`, and runtime-managed ownership without leaking eventing internals into `Cephalon.Engine`, managed dispatch runtimes now declare explicit `OutboxIds`, and `/engine/outboxes`, `/engine/event-dispatch-runtimes`, `/engine/event-dispatches`, and the showcase sample now agree on the same execution owner truth — **Shipped**
- ENG-071 event-dispatch runtime summary and broader provider dispatch-store baseline: `EventDispatchRuntimeDescriptor` now carries a canonical aggregate `Summary`, runtime descriptors are live-enriched from reported dispatch state instead of leaving each consumer to re-aggregate that data ad hoc, the `wolverine-adapter` and shared eventing technology surfaces now reuse that same summary truth, and the MongoDB, Redis, Elasticsearch, and OpenSearch outbox packs now register `IEventDispatchStore` alongside their staged outboxes so the runtime-neutral managed-dispatch path is no longer limited to Entity Framework — **Shipped**
- ENG-072 graph/vector outbox dispatch-store follow-through: `Cephalon.Data.Neo4j` and `Cephalon.Data.Qdrant` now also register provider-native `IEventDispatchStore` implementations alongside their staged outboxes, the same outbox descriptors now resolve to `consumer-managed` when the eventing technology is active, and composition coverage now locks that graph/vector baseline explicitly while Cassandra, ClickHouse, and NATS remained tracked as separate storage-model follow-through work at that point — **Shipped**
- ENG-073 ledger outbox dispatch-store follow-through: `Cephalon.Data.Nats` now also registers a provider-native `IEventDispatchStore` implementation alongside its staged JetStream KV outbox, the same outbox descriptor now resolves to `consumer-managed` when the eventing technology is active, and composition coverage now locks that ledger-store baseline explicitly while Cassandra and ClickHouse remained tracked as separate storage-model follow-through work at that point — **Shipped**
- ENG-074 Cassandra provider-native event-dispatch store baseline: `Cephalon.Data.Cassandra` now also registers a provider-native `IEventDispatchStore` implementation alongside its staged LWT outbox, the pack now maintains a deterministic message-sharded `outbox_pending_dispatch` eligibility table so pending reads stay query-shaped without pretending Cassandra is a globally ordered queue, the `cassandra-outbox` descriptor now resolves to `consumer-managed` when the eventing technology is active, and composition coverage now locks that wide-column baseline explicitly while ClickHouse remains the one deliberate provider-specific dispatch-store gap — **Shipped**
- ENG-075 ClickHouse explicit unsupported dispatch-policy baseline: `Cephalon.Data.ClickHouse` now keeps its `ReplacingMergeTree` outbox staging-only by design, publishes `DispatchPolicy.PolicyId = unsupported` with `ExecutionMode = disabled`, and lets `/engine/outboxes`, `event-dispatches`, and `snapshot.Outboxes` report `dispatchStore = unsupported` / `dispatchRuntime = unsupported` plus provider-specific reason metadata instead of collapsing the pack to a generic "not configured" answer — **Shipped**

### Sprint 38 follow-through

- ENG-086 database-role probe freshness policy: the engine-owned database runtime contract now exposes `RoleProbeFreshnessSeconds`, `Cephalon.Data.EntityFramework` now caches live role probes with explicit live-versus-cache metadata and migration-state invalidation, and the showcase sample now surfaces probe source, age, and freshness timing directly through its database-topology operator projection — **Shipped** · targeted composition tests 7/7 + hosting tests 3/3
- ENG-087 typed database-role probe contract and operator-surface follow-through: `Cephalon.Abstractions` now exposes `DatabaseRoleProbeDescriptor`, `DatabaseRoleRuntimeDescriptor` plus `DatabaseRoleDescriptor` now carry a typed `Probe` answer, `Cephalon.Data.EntityFramework` now projects stable cache/freshness/source timing through that shared contract while keeping metadata only for additive extras and compatibility, and the showcase sample now reads the typed probe surface directly in its projection/UI instead of parsing stable runtime-metadata keys locally — **Shipped** · targeted composition tests 25/25 + hosting tests 59/59 + package-surface tests 52/52
- ENG-088 engine-owned database-topology operational summary and advisory surface: `Cephalon.Abstractions` now exposes `DatabaseTopologyOperationalAction`, `DatabaseTopologyOperationalActionPlan`, `DatabaseTopologyOperationalSummary`, `DatabaseTopologyOperationalAdvisory`, `DatabaseTopologyOperationalSnapshot`, and `IDatabaseTopologyOperationalSnapshotProvider`, `Cephalon.Engine` now publishes `/engine/database-topology` plus `snapshot.DatabaseTopology` as the canonical posture answer over role health, migration status, production-guidance completeness, and ordered operator actions, and the showcase sample now consumes that engine-owned answer for readiness plus core advisories while keeping only read-model drift/backlog follow-through local — **Shipped** · targeted composition tests 3/3 + hosting tests 3/3 + package-surface tests 52/52
- ENG-089 database-topology action-plan showcase follow-through and contract truthfulness: the showcase sample now consumes the engine-owned action-plan contract as the baseline for ordered operator steps, preserves stable action categories plus source role and migration ids in its projection, browser console, and Markdown brief, and the docs/package-surface coverage now describe the shipped contract truthfully — **Shipped** · targeted composition tests 3/3 + hosting tests 3/3 + package-surface tests 52/52
- ENG-090 engine-owned database-migration playbook surface and showcase adoption: `Cephalon.Abstractions` now exposes ordered migration-playbook contracts, `Cephalon.Engine` now publishes `/engine/database-migration-playbook` plus `snapshot.DatabaseMigrationPlaybook` as the canonical ordered migration answer, and the showcase sample now consumes that engine-owned playbook directly while keeping only repo-root command adaptation plus sample-specific read-model follow-through local — **Shipped** · targeted composition tests 3/3 + hosting tests 4/4 + package-surface tests 52/52
- ENG-091 shared physical database migration coordination surface and showcase follow-through: the engine-owned database-role catalog now projects physical-target ids, display names, and physical co-location, the engine-owned migration playbook plus topology posture now surface shared-target coordination counts, per-step partner targets, and warning/action guidance when pending or failed logical migration work spans one physical database, and the showcase sample now consumes that engine-owned answer directly in `/showcase`, the Markdown brief, and the handoff package — **Shipped** · composition tests 4/4 + hosting tests 60/60 + package-surface tests 52/52
- ENG-092 shared physical database migration execution-group playbook follow-through: the engine-owned migration playbook now groups logical targets into physical-target execution batches with aggregate status, coverage counts, grouped migration ids, and shared-target coordination hints, and the showcase sample now consumes that engine-owned grouped answer directly in `/showcase`, the Markdown brief, and the handoff package — **Shipped** · composition tests 4/4 + hosting tests 60/60 + package-surface tests 52/52
- ENG-093 shared physical database migration execution-group command-set follow-through: the engine-owned migration playbook now also publishes grouped production and manual command sets per physical-target batch, and the showcase sample now consumes that engine-owned grouped command answer directly in `/showcase`, the Markdown brief, and the handoff package — **Shipped** · composition tests 4/4 + hosting tests 60/60 + package-surface tests 52/52
- ENG-094 shared physical database migration execution-group command-batch follow-through: the engine-owned migration playbook now also publishes combined production and manual command-batch templates per physical-target batch, and the showcase sample now consumes that engine-owned combined batch answer directly in `/showcase`, the Markdown brief, and the handoff package — **Shipped** · composition tests 4/4 + hosting tests 60/60 + package-surface tests 52/52
- ENG-095 phase 12 migration-pattern taxonomy baseline: the engine now ships `strangler-fig` plus `backend-for-frontend` as built-in architecture descriptors, and ASP.NET Core hosts now surface those phase 12 entries directly through `/engine/patterns` and the runtime app model while router/client-binding follow-through remains later — **Shipped** · composition tests 2/2 + hosting tests 1/1
- ENG-096 phase 12 strangler-fig runtime contract baseline: the engine now composes host-added and module-contributed migration routes through host-agnostic strangler-fig contracts, projects them into `/engine/strangler-fig` plus `snapshot.StranglerFigRoutes`, and exposes request resolution through `/engine/strangler-fig/resolve` while configuration-driven progress and host-level cutover remain later — **Shipped** · composition tests 2/2 + hosting tests 1/1 + package-surface tests 1/1
