# Cephalon AI Instructions

## Mission

Cephalon is an engine/framework, not a single app shell.

When working in this repository:

- optimize for modular runtime composition
- keep the core host-agnostic
- preserve deterministic behavior and clear boundaries
- prefer reusable engine primitives over product-specific shortcuts
- treat hand-authored `.md` files as the human-facing product and adoption docs for Cephalon
- treat XML comments on public contracts as the API explanation layer for IntelliSense and external documentation generators
- keep public XML comments complete enough for DocFX-style API publishing whenever an assembly is part of the supported documentation set
- keep test projects out of the supported DocFX input set unless we intentionally decide to publish test-harness APIs
- when scope changes, hidden work appears, or historical planning no longer matches the code, update backlog, roadmap, sprint, phase, estimate, task/sub-task placement, labels, and commit references so tracking stays truthful

## Architecture rules

- `Cephalon.Abstractions` must stay host-agnostic
- `Cephalon.Engine` owns composition, manifest generation, runtime behavior, and app-model/pattern selection
- `Cephalon.AspNetCore` is a host adapter, not the core engine
- `Cephalon.Worker` is the generic-host worker adapter for non-HTTP runtime scenarios
- `Cephalon.Observability` is the diagnostics companion package for logs, metrics, and tracing conventions
- `Cephalon.Observability.ConsulDependencies` is the optional Consul dependency-health companion package
- `Cephalon.Observability.ElasticsearchDependencies` is the optional Elasticsearch dependency-health companion package
- `Cephalon.Observability.HttpDependencies` is the optional external API dependency-health companion package for HTTP-based upstreams
- `Cephalon.Observability.KafkaDependencies` is the optional Kafka dependency-health companion package
- `Cephalon.Observability.MemcachedDependencies` is the optional Memcached dependency-health companion package
- `Cephalon.Observability.MongoDbDependencies` is the optional MongoDB dependency-health companion package
- `Cephalon.Observability.MqttDependencies` is the optional MQTT dependency-health companion package
- `Cephalon.Observability.MySqlDependencies` is the optional MySQL dependency-health companion package
- `Cephalon.Observability.NatsDependencies` is the optional NATS dependency-health companion package
- `Cephalon.Observability.OracleDependencies` is the optional Oracle dependency-health companion package
- `Cephalon.Observability.PostgresDependencies` is the optional Postgres dependency-health companion package
- `Cephalon.Observability.RabbitMqDependencies` is the optional RabbitMQ dependency-health companion package
- `Cephalon.Observability.RedisDependencies` is the optional Redis and cache dependency-health companion package
- `Cephalon.Observability.SqlServerDependencies` is the optional SQL Server and Azure SQL dependency-health companion package
- `Cephalon.Observability.OpenTelemetry` is the optional exporter companion package for OpenTelemetry OTLP host wiring
- `Cephalon.Observability.Serilog` is the optional logger-provider companion package for Serilog host wiring
- `Cephalon.Cli` is the user-facing shell for blueprint-driven generation
- `Cephalon.ReferenceDocs` is the optional repo-local reference-doc publishing tool for XML-comment-driven API output
- `Cephalon.Agentics`, `Cephalon.Eventing`, `Cephalon.Retrieval`, and `Cephalon.Edge` are the baseline technology companion packages
- `Cephalon.Scaffolding` is the generation companion package for turning blueprint scaffold plans into concrete files
- `templates/Cephalon.TemplatePack` is the installable `dotnet new` surface for the shipped blueprint starters
- `docs/module-authoring.md` and `samples/Cephalon.ReferenceModule.*` are the module-authoring baseline
- `Cephalon.Benchmarks` is the performance companion project for composition, runtime, and scaffolding regressions
- `samples/` contains adoption-quality blueprint examples and reference module packages; keep them separate from sandbox playgrounds
- hosts should stay thin and push behavior into modules and engine services
- runtime manifest, app blueprint selection, pattern selection, and transport selection are first-class and introspectable
- blueprint scaffold plans are part of the app-model contract and should stay aligned with templates, docs, and manifest output
- keep `Cephalon.TemplatePack` aligned with the same blueprint semantics used by runtime introspection, scaffolding, and CLI generation
- keep module authoring starters aligned with `IModule`, lifecycle, capability, localization, and transport contribution contracts
- keep runtime manifest v2 stable: schema version, engine version, module metadata, and capability source-module mapping are part of the contract now
- keep diagnostics names stable: the `Cephalon.Engine` meter and activity source are now part of the operational surface
- engine app-shape selection is configuration-driven through the `Engine` section by default
- prefer split project configuration through `Configurations/Add*.json` and `Configurations/{group}/{Environment}.json` when host settings start getting too large for one appsettings file
- keep `AddCephalonProjectConfigurations()` and automatic config loading in ASP.NET Core and Worker aligned with that convention
- prefer configuration-driven module discovery through `Engine:Discovery:Assemblies` when a host is meant to stay generic
- prefer `Engine:Discovery:Packages`, `Engine:Discovery:PackageDirectories`, `engine.AddPackageAssembly(...)`, `engine.AddPackageManifest(...)`, or `engine.AddPackageDirectory(...)` when a host needs to load independently shipped module assemblies
- keep `cephalon.package.json` aligned with the engine package contract: `id`, `version`, `compatibility.minimumEngineVersion`, `compatibility.maximumEngineVersion`, `compatibility.supportedTargetFrameworks`, and optional `integrity.sha256`
- keep Cephalon package-version and target-framework defaults aligned across `Cephalon.Cli`, `Cephalon.Scaffolding`, `Cephalon.TemplatePack`, starter manifests, samples, and docs examples
- when provenance matters, keep `cephalon.package.json` aligned with `publisher.id`, publisher display metadata, and either `signature` or `signatures` entries when detached signing is in use
- prefer `Engine:PackagePolicy` when a host needs to require manifest-driven package loading or stricter package metadata guarantees
- prefer `Engine:Trust` publisher and signer allow-lists when governance should track who shipped a package rather than only the assembly file itself
- prefer `Engine:Trust:TrustedSignaturePublicKeys` when hosts need cryptographic verification of detached package signatures
- prefer `Engine:Technologies`, `engine.RegisterTechnology(...)`, or `engine.AddTechnology(...)` for future-facing workload choices instead of overloading blueprint or pattern selection
- allow modules/packages to react to active technology selections through `ITechnologyServiceContributor` and `ITechnologyCapabilityContributor` instead of baking future-tech branches into hosts
- prefer technology companion packages for reusable future-tech runtime primitives instead of expanding `Cephalon.Engine` directly
- prefer pack-specific contributor services such as `IAgentToolContributor`, `IKnowledgeCollectionContributor`, `IEventChannelContributor`, and `IEdgeNodeContributor` when a module only needs to extend a shipped technology pack
- prefer `ITechnologyRuntimeContributor` when a technology pack needs an introspectable runtime snapshot
- prefer `ITechnologyRuntimeCatalog` when host or package code needs to read the merged runtime surface set without referencing `Cephalon.Engine` concrete types
- prefer `IRuntimeIntrospectionSnapshotProvider` and `/engine/snapshot` when host or operator flows need manifest, runtime status, and technology-pack surfaces in one payload
- prefer `IRuntimeDiagnosticsCatalog`, `/engine/diagnostics`, and `/engine/snapshot` when host or operator flows need the merged diagnostics convention and event-id catalog for active packages
- prefer `IRuntime.OperationalStory`, `/engine/runtime-story`, and `/engine/snapshot` when host or operator flows need one ordered answer for what loaded, started, failed, and why
- prefer configuration-driven language selection through `Engine:Localization`, with project code used only to extend or replace it deliberately
- prefer configuration-driven failure semantics through `Engine:FailurePolicy` instead of ad-hoc host try/catch behavior
- prefer configuration-driven package and capability governance through `Engine:Trust`
- prefer configuration-driven package metadata governance through `Engine:PackagePolicy`
- prefer `Engine:Trust:AllowedPackageChecksums` when a package should be trusted by an explicit assembly hash instead of a broader assembly or package allow-list
- allow installed modules/packages to contribute language packs through `ILocalizedResourceContributor`, but keep project overrides authoritative
- allow installed modules/packages to contribute future-tech profiles through `ITechnologyContributor`, but keep the runtime catalog explicit and introspectable
- keep module discovery deterministic; scan assemblies explicitly instead of relying on ambient magic
- keep package loading explicit and introspectable; surface package loads through manifest data and `/engine/packages`, including `kind`, resolved assembly `path`, and discovery `sourcePath`
- keep package compatibility and provenance explicit and introspectable; `/engine/packages` should continue surfacing declared version, compatibility fields, computed checksum, signature verification state, and trust reason
- keep package governance explicit and introspectable; `/engine/package-policy` should continue exposing raw DLL, version, compatibility, and integrity requirements
- keep package provenance explicit and introspectable; `/engine/packages` and `/engine/trust-policy` should continue surfacing publisher, signature key, and signer metadata when available
- keep future-tech choices explicit and introspectable; surface selected technology profiles through `AppProfile.Technologies` and `/engine/technologies`
- keep the merged technology catalog explicit and introspectable; surface available technology profiles through `/engine/technology-catalog`
- keep active technology-pack runtime surfaces explicit and introspectable; surface them through `/engine/technology-surfaces`
- keep the broader runtime introspection snapshot explicit and introspectable; surface it through `/engine/snapshot`
- keep package diagnostics conventions explicit and introspectable; surface them through `IRuntimeDiagnosticsCatalog`, `/engine/diagnostics`, and `/engine/snapshot`
- keep the runtime lifecycle story explicit and introspectable; surface it through `IRuntime.OperationalStory`, `/engine/runtime-story`, and `/engine/snapshot`
- keep public engine, companion-pack, host-adapter, and tooling contracts documented with meaningful XML comments so external doc generators and IntelliSense stay useful
- keep the supported DocFX input set limited to assemblies whose public APIs have complete XML comments
- keep test-only projects such as `tests/Cephalon.Tests` excluded from generated reference-doc and DocFX publishing scope unless they are deliberately promoted into supported docs input
- keep hand-authored `.md` guides focused on capability claims, architecture explanation, and adoption guidance instead of auto-generated API listings
- keep `Cephalon.ReferenceDocs`, `scripts/publish-reference-docs.ps1`, and `docs/reference-docs.md` aligned when XML docs publishing behavior changes, including Markdown indexes and `reference-manifest.json`
- keep `Cephalon.Cli docs publish`, `docs publish --enable-hosting`, `docs publish --validate-hosting`, `docs publish --open`, `docs enable-hosting`, and `docs validate-hosting` aligned with `Cephalon.ReferenceDocs` and the hosted-reference-docs guidance
- keep `Cephalon.Cli` package-surface hardening intact: `CliApplication` is the stable public entry point, while command handlers, parsed options, console helpers, and browser launch helpers stay internal
- keep `Cephalon.ReferenceDocs` package-surface hardening intact: request/generate/write/application types stay public, while assembly-load and browser-render helpers stay internal
- keep `docs/README.md` aligned as the documentation hub for architecture, operations, planning, hand-authored component guides, and optional generated-reference entry points
- keep `docs/compatibility.md` aligned as the repository-wide matrix for package, manifest, template, scaffolding, CLI, and hosted-reference-doc compatibility expectations
- keep `docs/components/README.md` plus per-component docs under `docs/components/` aligned with every shipped `src/Cephalon.*` project
- when ASP.NET Core hosts serve generated reference docs, prefer the host-level `ReferenceDocs` section and keep `/engine/reference-docs` aligned with the configured route prefix
- keep scaffolded hosts and `dotnet new` app starters emitting a disabled-by-default `ReferenceDocs` section so teams can turn on hosted docs without rediscovering the contract
- keep scaffolded hosts and `dotnet new` app starters ready to copy `Configurations/**/*.json` to build and publish output
- prefer `Engine:Options` for module and capability toggles instead of hardcoding enable/disable decisions in hosts
- prefer `Engine:Observability` for startup diagnostics behavior instead of scattering host-specific logging decisions
- prefer `Engine:Observability:Telemetry` for export guidance instead of ad-hoc host notes
- let modules and installed packages contribute dependency health through `IDependencyHealthContributor`, but keep the engine itself infrastructure-agnostic
- keep `Cephalon.Observability.HttpDependencies` protocol-generic: method, headers, auth, timeout, status, body, and TLS expectations can grow there, but provider-aware HTTP semantics should stay in dedicated companion packs
- keep HTTP-based provider packs such as `Cephalon.Observability.ElasticsearchDependencies` separate when they need endpoint shaping, payload-aware health mapping, or product-specific auth semantics beyond a generic HTTP probe
- keep `Cephalon.Observability.NatsDependencies` focused on NATS protocol semantics such as `INFO`, `CONNECT`, `PING`/`PONG`, auth, TLS, and cluster reachability; deeper workload semantics should only join deliberately when the package contract stays clearly NATS-native
- keep logging-provider integration concerns separate from tracing/export concerns: `ILogger` provider wiring such as Serilog can land before cloud-tracing/export work without inventing a new Cephalon logging abstraction
- keep cloud tracing/export integrations such as OpenTelemetry collector, vendor exporters, and hosted cloud wiring as a later concern when the target cloud/runtime context is explicit
- selected transports should gate route mapping instead of exposing every protocol by default
- when `RestApi` is enabled on ASP.NET Core, keep OpenAPI and Scalar docs available for REST endpoints
- keep REST OpenAPI customization inside `Cephalon.AspNetCore.Transformers`
- keep non-REST protocol endpoints out of REST API documentation
- if a transport lives in a companion host package, keep config selection and adapter registration aligned
- if a REST endpoint needs a capability boundary, prefer `RequireCapability(...)` over ad-hoc inline policy checks
- keep gRPC contracts versioned and owned by the adapter package that exposes them
- keep gRPC unary and streaming behavior covered by adapter-level integration tests
- keep module lifecycle hooks deterministic and host-managed where adapters integrate with app startup
- keep generic-host worker startup/shutdown aligned with the same runtime lifecycle guarantees used by ASP.NET Core
- keep observability conventions additive; engine emits the signals, companion packages shape how hosts surface them
- keep benchmark guardrails explicit; if hot-path scenarios change, update the benchmark catalog and validation docs intentionally
- keep `scripts/validate-operational-conventions.ps1`, `scripts/validate-release.ps1`, and the health/export docs aligned when the operational validation contract changes
- keep `scripts/validate-release.ps1` aligned with the benchmark smoke suite and guardrail validation flow
- keep `scripts/validate-release.ps1` aligned with benchmark and reference-doc publishing flows
- keep `.github/workflows/release-validation.yml` aligned with `scripts/validate-release.ps1`; the script stays the source of truth and the workflow should stay thin

## App model rules

- do not treat architecture patterns, deployment topology, and design patterns as the same kind of choice
- treat transport protocols as a separate app-model dimension from blueprint and pattern selection
- use a blueprint/app-model as the primary project shape
- treat `AppProfile.Scaffold` and `/engine/scaffold` as the source of truth for blueprint-driven project shape
- if generator logic changes, keep `Cephalon.Scaffolding` aligned with the scaffold data exposed by the runtime
- if command syntax or defaults change, keep `Cephalon.Cli` aligned with the same blueprint and transport semantics used by the engine
- keep `Cephalon.TemplatePack` aligned with the same shipped blueprint set and starter shape used by scaffold plans and samples
- keep `cephalon-module` and `cephalon-rest-module` aligned with the recommended module package shape in `docs/module-authoring.md`
- keep `cephalon-module`, `cephalon-rest-module`, and scaffolded module projects emitting `cephalon.package.json` so package discovery flows stay zero-setup
- keep scaffold package-version output aligned with the repository package catalog when test infrastructure packages change
- keep template starters, scaffold output, and CLI defaults aligned whenever blueprint, transport, version, target-framework, or docs-hosting contracts change
- prefer configuration-driven blueprint and pattern selection over hardcoded choices in host startup
- prefer configuration-driven transport selection over hardcoded protocol choices in host startup
- prefer configuration-driven module discovery and policy where a sample or host is meant to demonstrate the engine itself
- v1 blueprints are:
  - `ModularMonolith`
  - `ModularVerticalSlice`
  - `Microservice`
- `Shared Foundation` is mandatory for Cephalon apps
- supporting patterns such as `Strategy`, `Pipeline`, `Mediator`, and `Specification` may be mixed on top of a blueprint
- use the `Engine` section as the primary source of truth for `Blueprint`, `Patterns`, `Technologies`, and `Transports`
- if a user says `Modular`, default to `ModularMonolith`
- if a user says `Vertical Slice`, default to `ModularVerticalSlice`
- if a user says `Microservice`, generate one Cephalon-powered service, not a whole distributed suite by default

## Source structure rules

Keep folders and namespaces aligned. Do not add new root-level files to a project when they belong in an existing category.

Current source layout:

- `src/Cephalon.Abstractions/AppModel` -> `Cephalon.Abstractions.AppModel`
- `src/Cephalon.Abstractions/AppModel/Scaffolding` -> `Cephalon.Abstractions.AppModel.Scaffolding`
- `src/Cephalon.Abstractions/Capabilities` -> `Cephalon.Abstractions.Capabilities`
- `src/Cephalon.Abstractions/Health` -> `Cephalon.Abstractions.Health`
- `src/Cephalon.Abstractions/Localization` -> `Cephalon.Abstractions.Localization`
- `src/Cephalon.Abstractions/Modules` -> `Cephalon.Abstractions.Modules`
- `src/Cephalon.Abstractions/Patterns` -> `Cephalon.Abstractions.Patterns`
- `src/Cephalon.Abstractions/Technologies` -> `Cephalon.Abstractions.Technologies`
- `src/Cephalon.Abstractions/Transports` -> `Cephalon.Abstractions.Transports`
- `src/Cephalon.Engine/AppModel` -> `Cephalon.Engine.AppModel`
- `src/Cephalon.Engine/AppModel/Scaffolding` -> `Cephalon.Engine.AppModel.Scaffolding`
- `src/Cephalon.Engine/Composition` -> `Cephalon.Engine.Composition`
- `src/Cephalon.Engine/Composition/Packages` -> `Cephalon.Engine.Composition.Packages`
- `src/Cephalon.Engine/Configuration` -> `Cephalon.Engine.Configuration`
- `src/Cephalon.Engine/Diagnostics` -> `Cephalon.Engine.Diagnostics`
- `src/Cephalon.Engine/Technologies` -> `Cephalon.Engine.Technologies`
- `src/Cephalon.Engine/Localization` -> `Cephalon.Engine.Localization`
- `src/Cephalon.Engine/Manifest` -> `Cephalon.Engine.Manifest`
- `src/Cephalon.Engine/Patterns` -> `Cephalon.Engine.Patterns`
- `src/Cephalon.Engine/Runtime` -> `Cephalon.Engine.Runtime`
- `src/Cephalon.Engine/Trust` -> `Cephalon.Engine.Trust`
- `src/Cephalon.Engine/Transports` -> `Cephalon.Engine.Transports`
- `src/Cephalon.AspNetCore/Hosting` -> `Cephalon.AspNetCore.Hosting`
- `src/Cephalon.AspNetCore/Documentation` -> `Cephalon.AspNetCore.Documentation`
- `src/Cephalon.AspNetCore/Diagnostics` -> `Cephalon.AspNetCore.Diagnostics`
- `src/Cephalon.AspNetCore/Health` -> `Cephalon.AspNetCore.Health`
- `src/Cephalon.AspNetCore/Modules` -> `Cephalon.AspNetCore.Modules`
- `src/Cephalon.AspNetCore/Transformers` -> `Cephalon.AspNetCore.Transformers`
- `src/Cephalon.AspNetCore/wwwroot/js` -> embedded Scalar/OpenAPI documentation assets
- `src/Cephalon.AspNetCore/wwwroot/icons` -> embedded docs branding assets
- `src/Cephalon.AspNetCore/Transports/Rest` -> `Cephalon.AspNetCore.Transports.Rest`
- `src/Cephalon.AspNetCore/Transports/ServerSentEvents` -> `Cephalon.AspNetCore.Transports.ServerSentEvents`
- `src/Cephalon.AspNetCore/Transports/WebSockets` -> `Cephalon.AspNetCore.Transports.WebSockets`
- `src/Cephalon.AspNetCore.JsonRpc/Hosting` -> `Cephalon.AspNetCore.JsonRpc.Hosting`
- `src/Cephalon.AspNetCore.JsonRpc/Modules` -> `Cephalon.AspNetCore.JsonRpc.Modules`
- `src/Cephalon.AspNetCore.JsonRpc/Routing` -> `Cephalon.AspNetCore.JsonRpc.Routing`
- `src/Cephalon.AspNetCore.Grpc/Hosting` -> `Cephalon.AspNetCore.Grpc.Hosting`
- `src/Cephalon.AspNetCore.Grpc/Modules` -> `Cephalon.AspNetCore.Grpc.Modules`
- `src/Cephalon.AspNetCore.Grpc/Protos` -> generated `Cephalon.AspNetCore.Grpc.Contracts.*`
- `src/Cephalon.AspNetCore.Grpc/Routing` -> `Cephalon.AspNetCore.Grpc.Routing`
- `src/Cephalon.Worker/Hosting` -> `Cephalon.Worker.Hosting`
- `src/Cephalon.Observability/Configuration` -> `Cephalon.Observability.Configuration`
- `src/Cephalon.Observability/Hosting` -> `Cephalon.Observability.Hosting`
- `src/Cephalon.Observability.ConsulDependencies/Configuration` -> `Cephalon.Observability.ConsulDependencies.Configuration`
- `src/Cephalon.Observability.ConsulDependencies/Hosting` -> `Cephalon.Observability.ConsulDependencies.Hosting`
- `src/Cephalon.Observability.ConsulDependencies/Services` -> `Cephalon.Observability.ConsulDependencies.Services`
- `src/Cephalon.Observability.ElasticsearchDependencies/Configuration` -> `Cephalon.Observability.ElasticsearchDependencies.Configuration`
- `src/Cephalon.Observability.ElasticsearchDependencies/Hosting` -> `Cephalon.Observability.ElasticsearchDependencies.Hosting`
- `src/Cephalon.Observability.ElasticsearchDependencies/Services` -> `Cephalon.Observability.ElasticsearchDependencies.Services`
- `src/Cephalon.Observability.HttpDependencies/Configuration` -> `Cephalon.Observability.HttpDependencies.Configuration`
- `src/Cephalon.Observability.HttpDependencies/Hosting` -> `Cephalon.Observability.HttpDependencies.Hosting`
- `src/Cephalon.Observability.HttpDependencies/Services` -> `Cephalon.Observability.HttpDependencies.Services`
- `src/Cephalon.Observability.KafkaDependencies/Configuration` -> `Cephalon.Observability.KafkaDependencies.Configuration`
- `src/Cephalon.Observability.KafkaDependencies/Hosting` -> `Cephalon.Observability.KafkaDependencies.Hosting`
- `src/Cephalon.Observability.KafkaDependencies/Services` -> `Cephalon.Observability.KafkaDependencies.Services`
- `src/Cephalon.Observability.MemcachedDependencies/Configuration` -> `Cephalon.Observability.MemcachedDependencies.Configuration`
- `src/Cephalon.Observability.MemcachedDependencies/Hosting` -> `Cephalon.Observability.MemcachedDependencies.Hosting`
- `src/Cephalon.Observability.MemcachedDependencies/Services` -> `Cephalon.Observability.MemcachedDependencies.Services`
- `src/Cephalon.Observability.MongoDbDependencies/Configuration` -> `Cephalon.Observability.MongoDbDependencies.Configuration`
- `src/Cephalon.Observability.MongoDbDependencies/Hosting` -> `Cephalon.Observability.MongoDbDependencies.Hosting`
- `src/Cephalon.Observability.MongoDbDependencies/Services` -> `Cephalon.Observability.MongoDbDependencies.Services`
- `src/Cephalon.Observability.MqttDependencies/Configuration` -> `Cephalon.Observability.MqttDependencies.Configuration`
- `src/Cephalon.Observability.MqttDependencies/Hosting` -> `Cephalon.Observability.MqttDependencies.Hosting`
- `src/Cephalon.Observability.MqttDependencies/Services` -> `Cephalon.Observability.MqttDependencies.Services`
- `src/Cephalon.Observability.MySqlDependencies/Configuration` -> `Cephalon.Observability.MySqlDependencies.Configuration`
- `src/Cephalon.Observability.MySqlDependencies/Hosting` -> `Cephalon.Observability.MySqlDependencies.Hosting`
- `src/Cephalon.Observability.MySqlDependencies/Services` -> `Cephalon.Observability.MySqlDependencies.Services`
- `src/Cephalon.Observability.NatsDependencies/Configuration` -> `Cephalon.Observability.NatsDependencies.Configuration`
- `src/Cephalon.Observability.NatsDependencies/Hosting` -> `Cephalon.Observability.NatsDependencies.Hosting`
- `src/Cephalon.Observability.NatsDependencies/Services` -> `Cephalon.Observability.NatsDependencies.Services`
- `src/Cephalon.Observability.OracleDependencies/Configuration` -> `Cephalon.Observability.OracleDependencies.Configuration`
- `src/Cephalon.Observability.OracleDependencies/Hosting` -> `Cephalon.Observability.OracleDependencies.Hosting`
- `src/Cephalon.Observability.OracleDependencies/Services` -> `Cephalon.Observability.OracleDependencies.Services`
- `src/Cephalon.Observability.PostgresDependencies/Configuration` -> `Cephalon.Observability.PostgresDependencies.Configuration`
- `src/Cephalon.Observability.PostgresDependencies/Hosting` -> `Cephalon.Observability.PostgresDependencies.Hosting`
- `src/Cephalon.Observability.PostgresDependencies/Services` -> `Cephalon.Observability.PostgresDependencies.Services`
- `src/Cephalon.Observability.RabbitMqDependencies/Configuration` -> `Cephalon.Observability.RabbitMqDependencies.Configuration`
- `src/Cephalon.Observability.RabbitMqDependencies/Hosting` -> `Cephalon.Observability.RabbitMqDependencies.Hosting`
- `src/Cephalon.Observability.RabbitMqDependencies/Services` -> `Cephalon.Observability.RabbitMqDependencies.Services`
- `src/Cephalon.Observability.RedisDependencies/Configuration` -> `Cephalon.Observability.RedisDependencies.Configuration`
- `src/Cephalon.Observability.RedisDependencies/Hosting` -> `Cephalon.Observability.RedisDependencies.Hosting`
- `src/Cephalon.Observability.RedisDependencies/Services` -> `Cephalon.Observability.RedisDependencies.Services`
- `src/Cephalon.Observability.SqlServerDependencies/Configuration` -> `Cephalon.Observability.SqlServerDependencies.Configuration`
- `src/Cephalon.Observability.SqlServerDependencies/Hosting` -> `Cephalon.Observability.SqlServerDependencies.Hosting`
- `src/Cephalon.Observability.SqlServerDependencies/Services` -> `Cephalon.Observability.SqlServerDependencies.Services`
- `src/Cephalon.Observability.OpenTelemetry/Hosting` -> `Cephalon.Observability.OpenTelemetry.Hosting`
- `src/Cephalon.Observability.Serilog/Hosting` -> `Cephalon.Observability.Serilog.Hosting`
- `src/Cephalon.Cli/Commands` -> `Cephalon.Cli.Commands`
- `src/Cephalon.Cli/Console` -> `Cephalon.Cli.Console`
- `src/Cephalon.ReferenceDocs/Generation` -> `Cephalon.ReferenceDocs.Generation`
- `src/Cephalon.ReferenceDocs/IO` -> `Cephalon.ReferenceDocs.IO`
- `src/Cephalon.Agentics/Configuration` -> `Cephalon.Agentics.Configuration`
- `src/Cephalon.Agentics/Modules` -> `Cephalon.Agentics.Modules`
- `src/Cephalon.Agentics/Registration` -> `Cephalon.Agentics.Registration`
- `src/Cephalon.Agentics/Services` -> `Cephalon.Agentics.Services`
- `src/Cephalon.Edge/Configuration` -> `Cephalon.Edge.Configuration`
- `src/Cephalon.Edge/Modules` -> `Cephalon.Edge.Modules`
- `src/Cephalon.Edge/Registration` -> `Cephalon.Edge.Registration`
- `src/Cephalon.Edge/Services` -> `Cephalon.Edge.Services`
- `src/Cephalon.Eventing/Configuration` -> `Cephalon.Eventing.Configuration`
- `src/Cephalon.Eventing/Modules` -> `Cephalon.Eventing.Modules`
- `src/Cephalon.Eventing/Registration` -> `Cephalon.Eventing.Registration`
- `src/Cephalon.Eventing/Services` -> `Cephalon.Eventing.Services`
- `src/Cephalon.Retrieval/Configuration` -> `Cephalon.Retrieval.Configuration`
- `src/Cephalon.Retrieval/Modules` -> `Cephalon.Retrieval.Modules`
- `src/Cephalon.Retrieval/Registration` -> `Cephalon.Retrieval.Registration`
- `src/Cephalon.Retrieval/Services` -> `Cephalon.Retrieval.Services`
- `src/Cephalon.Scaffolding/Generation` -> `Cephalon.Scaffolding.Generation`
- `src/Cephalon.Scaffolding/IO` -> `Cephalon.Scaffolding.IO`
- `templates/Cephalon.TemplatePack/templates` -> installable `dotnet new` blueprint templates
- `samples/Cephalon.ReferenceModule.*` -> reference module packages
- `benchmarks/Cephalon.Benchmarks/Composition` -> `Cephalon.Benchmarks.Composition`
- `benchmarks/Cephalon.Benchmarks/Runtime` -> `Cephalon.Benchmarks.Runtime`
- `benchmarks/Cephalon.Benchmarks/Scaffolding` -> `Cephalon.Benchmarks.Scaffolding`
- `benchmarks/Cephalon.Benchmarks/Support` -> `Cephalon.Benchmarks.Support`

## Code design rules

- keep module dependencies explicit and type-based
- keep capabilities first-class and discoverable
- prefer additive extension points over god objects
- every engine feature should be testable without needing the playground host
- do not leak host-specific APIs into abstractions
- runtime status and lifecycle behavior should stay introspectable
- runtime policy state should stay introspectable
- runtime failure context and restart policy should stay introspectable
- runtime diagnostics and health semantics should stay introspectable
- runtime trust policy should stay introspectable
- runtime package loading should stay introspectable
- runtime technology selection should stay introspectable
- runtime dependency health should stay introspectable
- runtime localization state should stay introspectable
- blueprint scaffold metadata should stay serializable and host-agnostic
- benchmark scenarios should stay representative of public engine usage, not private shortcuts
- benchmark guardrail thresholds should be updated deliberately, not silently drift through unrelated refactors

## Naming rules

- do not add the `Cephalon` prefix to every new class or object by default
- if a type already lives in a clear `Cephalon.*` namespace, prefer a concise name
- keep public API renames deliberate and compatibility-aware

## Documentation rules

When changing architecture or engine behavior, keep these files in sync if relevant:

- `README.md`
- `docs/architecture.md`
- `docs/app-models.md`
- `docs/module-authoring.md`
- `docs/technology-packs.md`
- `docs/reference-docs.md`
- `docs/runtime-failure-policy.md`
- `docs/operations.md`
- `docs/engine-roadmap.md`
- `docs/engine-backlog.md`
- `templates/Cephalon.TemplatePack/PACKAGE.md`
- `docs/benchmarking.md`

When work is added, cut, or re-scoped during implementation:

- update the relevant backlog and roadmap entries instead of leaving scope drift undocumented
- move work between phase, sprint, epic, task, or sub-task when the code reality says it belongs elsewhere
- add or revise estimates when the implementation reveals materially different cost
- correct historical planning claims when repository state shows a capability was not actually shipped yet
- keep issue, project, and commit references aligned with the revised plan

## Key references

- `docs/architecture.md`
- `docs/app-models.md`
- `docs/engine-roadmap.md`
- `docs/engine-backlog.md`
