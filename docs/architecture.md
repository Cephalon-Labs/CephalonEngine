# Cephalon Architecture

Editable diagram: `docs/cephalon-architecture.drawio`
Planning docs: `docs/engine-roadmap.md`, `docs/engine-backlog.md`, `docs/app-models.md`
Docs hub: `docs/README.md`
Component docs: `docs/components/README.md`

## North star

Cephalon should evolve into an engine/framework, not a single app shell. That means the foundation needs to optimize for composition, discoverability, and multiple hosts from day one.

## Layers

### 1. Abstractions

`Cephalon.Abstractions` defines the stable contract modules implement:

- `IModule`
- `ModuleDescriptor`
- `Capability`
- `ICapabilityRegistry`

The goal is to keep module authors focused on capabilities and services rather than bootstrapping concerns.

Source structure:

- `AppModel` -> `Cephalon.Abstractions.AppModel`
- `AppModel/Scaffolding` -> `Cephalon.Abstractions.AppModel.Scaffolding`
- `Capabilities` -> `Cephalon.Abstractions.Capabilities`
- `Health` -> `Cephalon.Abstractions.Health`
- `Localization` -> `Cephalon.Abstractions.Localization`
- `Modules` -> `Cephalon.Abstractions.Modules`
- `Patterns` -> `Cephalon.Abstractions.Patterns`
- `Technologies` -> `Cephalon.Abstractions.Technologies`
- `Transports` -> `Cephalon.Abstractions.Transports`

### 2. Engine

`Cephalon.Engine` turns a set of modules into a runtime:

- validates duplicate registrations
- resolves dependency ordering
- discovers modules from assemblies when configured
- loads independently shipped module assemblies from explicit DLL paths, package manifests, or package directories when configured
- validates package compatibility metadata such as engine version and supported target frameworks
- carries package publisher and signature provenance metadata through runtime introspection
- verifies detached package signatures against trusted public keys when signing metadata and trust-store entries are both present
- validates optional package assembly integrity hashes and can trust packages by checksum allow-list
- models future-facing workload profiles as explicit technology descriptors instead of hardcoding every new tech stack into the blueprint set
- lets installed modules and packages contribute additional technology profiles through `ITechnologyContributor`
- lets installed modules adapt service and capability registration to active technology profiles through `ITechnologyServiceContributor` and `ITechnologyCapabilityContributor`
- lets modules register services
- merges built-in and project-supplied language resources
- merges package-provided language packs from modules before project overrides
- collects capabilities into a manifest with source-module tracing
- executes module lifecycle hooks deterministically
- aggregates host-agnostic dependency health contributions
- exposes a runtime object that any host can consume

This is the piece we can later extend with plugin loading, workflows, orchestration, telemetry, and policy.

Source structure:

- `AppModel` -> `Cephalon.Engine.AppModel`
- `AppModel/Scaffolding` -> `Cephalon.Engine.AppModel.Scaffolding`
- `Composition` -> `Cephalon.Engine.Composition`
- `Composition/Packages` -> `Cephalon.Engine.Composition.Packages`
- `Configuration` -> `Cephalon.Engine.Configuration`
- `Diagnostics` -> `Cephalon.Engine.Diagnostics`
- `Localization` -> `Cephalon.Engine.Localization`
- `Manifest` -> `Cephalon.Engine.Manifest`
- `Patterns` -> `Cephalon.Engine.Patterns`
- `Runtime` -> `Cephalon.Engine.Runtime`
- `Technologies` -> `Cephalon.Engine.Technologies`
- `Trust` -> `Cephalon.Engine.Trust`
- `Transports` -> `Cephalon.Engine.Transports`

### 3. Host adapters

`Cephalon.AspNetCore` is the HTTP-first host core. It wires the engine into modern ASP.NET Core patterns and exposes:

- engine metadata endpoints
- engine package endpoint
- engine technology endpoint
- engine technology catalog endpoint
- blueprint scaffold endpoint
- engine options and policy endpoint
- engine failure-policy endpoint
- engine diagnostics endpoint
- engine localization endpoint
- engine dependency-health endpoint
- engine trust-policy endpoint
- runtime status endpoint
- health summary, liveness, and readiness endpoints
- OpenAPI and Scalar API docs for REST surfaces
- optional hosted reference-doc surfaces for generated API documentation
- built-in transport-aware module surfaces such as `/api`, `/events`, and `/ws`

Companion adapter packages can extend that host with additional transport surfaces such as `/graphql` for GraphQL, `/rpc` for JSON-RPC, or gRPC bindings with unary and streaming contracts.

`Cephalon.Worker` is the non-HTTP generic-host adapter. It starts and stops the same runtime inside worker processes and keeps module/background behavior aligned with standard hosted-service lifecycles.

`Cephalon.Observability` is the diagnostics companion package. It turns the engine's built-in logs, meter, and activity source into host-friendly startup summaries and conventions that both ASP.NET Core and worker hosts can opt into, and it publishes the active package-level event-id catalog through the runtime diagnostics surface.

`Cephalon.Observability.ElasticsearchDependencies` is the optional Elasticsearch dependency-health companion package. It turns configured cluster-health requests, auth policies, and Elasticsearch `green`/`yellow`/`red` status mapping into `IDependencyHealthContributor` data without pushing Elasticsearch-specific HTTP behavior into `Cephalon.Engine`.

`Cephalon.Observability.HttpDependencies` is the optional external API dependency-health companion package. It turns configured HTTP upstream probes into `IDependencyHealthContributor` data without pushing provider-specific network checks into `Cephalon.Engine`. That package should stay protocol-generic: method, headers, auth, timeout, status, body, and TLS expectations belong there, but product-aware HTTP semantics should stay in dedicated packs when they need endpoint shaping or payload-aware mapping.

`Cephalon.Observability.KafkaDependencies` is the optional Kafka dependency-health companion package. It turns configured broker metadata and optional topic probes into `IDependencyHealthContributor` data without pushing Kafka client configuration into `Cephalon.Engine`.

`Cephalon.Observability.MongoDbDependencies` is the optional MongoDB dependency-health companion package. It turns configured database commands, TLS policy, and connection-string probing into `IDependencyHealthContributor` data without pushing MongoDB-specific connection logic into `Cephalon.Engine`.

`Cephalon.Observability.MqttDependencies` is the optional MQTT dependency-health companion package. It turns configured broker `CONNECT`, `CONNACK`, and `PINGREQ`/`PINGRESP` probes into `IDependencyHealthContributor` data without pushing MQTT protocol handling into `Cephalon.Engine`.

`Cephalon.Observability.MySqlDependencies` is the optional MySQL dependency-health companion package. It turns configured SQL queries, SSL/public-key retrieval policy, and MySQL connection probing into `IDependencyHealthContributor` data without pushing MySQL-specific connection logic into `Cephalon.Engine`.

`Cephalon.Observability.NatsDependencies` is the optional NATS dependency-health companion package. It turns configured broker `INFO`, `CONNECT`, and `PING`/`PONG` probes into `IDependencyHealthContributor` data without pushing NATS protocol handling into `Cephalon.Engine`.

`Cephalon.Observability.PostgresDependencies` is the optional Postgres dependency-health companion package. It turns configured database probes and health queries into `IDependencyHealthContributor` data without pushing Postgres-specific connection logic into `Cephalon.Engine`.

`Cephalon.Observability.RabbitMqDependencies` is the optional RabbitMQ dependency-health companion package. It turns configured broker connection probes into `IDependencyHealthContributor` data without pushing AMQP-specific connection logic into `Cephalon.Engine`.

`Cephalon.Observability.RedisDependencies` is the optional Redis and cache dependency-health companion package. It turns configured Redis `PING` probes, optional authentication, and logical database selection into `IDependencyHealthContributor` data without pushing Redis-specific socket logic into `Cephalon.Engine`.

`Cephalon.Observability.SqlServerDependencies` is the optional SQL Server and Azure SQL dependency-health companion package. It turns configured SQL queries, SQL authentication, and connection-string probing into `IDependencyHealthContributor` data without pushing SQL Server-specific connection logic into `Cephalon.Engine`.

`Cephalon.Observability.OpenTelemetry` is the optional exporter companion package. It takes the shared `Engine:Observability:Telemetry` contract and turns it into OTLP registration for logs, metrics, and traces without pushing exporter dependencies back into the engine or the baseline observability package.

`Cephalon.Scaffolding` is the adoption companion package. It turns `AppProfile.Scaffold` into concrete solution, project, file, and folder output so future CLIs or templates do not need to re-encode blueprint rules.

`Cephalon.ReferenceDocs` is the optional reference-publishing companion package. It can turn compiled assemblies plus XML comments into publishable Markdown reference output when the repository wants a browsable API artifact, but the hand-authored `.md` guides under `README.md` and `docs/` remain the primary product and adoption documentation. The stable library surface stays centered on request/generate/write flows, while browser rendering and assembly-load plumbing remain internal implementation details.

`Cephalon.Cli` is the user-facing shell over scaffolding and documentation workflows. `CliApplication` is the stable entry point, while the individual command handlers and option objects remain internal implementation details. It parses blueprint, transport, pattern, module, and feature selections for app generation, and it also publishes, enables, validates, and opens generated reference-doc surfaces.

`Cephalon.Agentics`, `Cephalon.Eventing`, `Cephalon.Retrieval`, and `Cephalon.Edge` are the current baseline technology companion packages. They show the preferred pattern for future workloads that need reusable runtime services without bloating the engine core.

`templates/Cephalon.TemplatePack` is the installable `dotnet new` companion surface. It packages the current shipped blueprint starters as lightweight templates for teams that want a fast entry point without using the repo-local CLI.

`docs/module-authoring.md` and `samples/Cephalon.ReferenceModule.*` are the module-authoring companions. They show how a reusable module package should register services, expose capabilities, participate in lifecycle, contribute localization, and optionally own a transport surface.

`Cephalon.Benchmarks` is the performance companion project. It measures composition, runtime lifecycle, and scaffolding paths through public engine APIs so regressions can be caught without relying on playground hosts.

`samples/` now contains both adoption-quality blueprint examples and reference module packages. The blueprint apps show project shape, while the reference packages show how to author distributable Cephalon modules cleanly.

The same runtime contract can later back messaging hosts, desktop shells, or AI orchestration hosts.

Per-component documentation for every `src/Cephalon.*` project now lives under `docs/components/`.

Source structure:

- `Hosting` -> `Cephalon.AspNetCore.Hosting`
- `Documentation` -> `Cephalon.AspNetCore.Documentation`
- `Diagnostics` -> `Cephalon.AspNetCore.Diagnostics`
- `Health` -> `Cephalon.AspNetCore.Health`
- `Modules` -> `Cephalon.AspNetCore.Modules`
- `Transformers` -> `Cephalon.AspNetCore.Transformers`
- `wwwroot/js` -> embedded Scalar/OpenAPI documentation assets owned by `Cephalon.AspNetCore`
- `wwwroot/icons` -> embedded docs branding assets owned by `Cephalon.AspNetCore`
- `Transports/Rest` -> `Cephalon.AspNetCore.Transports.Rest`
- `Transports/ServerSentEvents` -> `Cephalon.AspNetCore.Transports.ServerSentEvents`
- `Transports/WebSockets` -> `Cephalon.AspNetCore.Transports.WebSockets`

Companion adapter packages:

- `src/Cephalon.AspNetCore.JsonRpc/Hosting` -> `Cephalon.AspNetCore.JsonRpc.Hosting`
- `src/Cephalon.AspNetCore.JsonRpc/Modules` -> `Cephalon.AspNetCore.JsonRpc.Modules`
- `src/Cephalon.AspNetCore.JsonRpc/Routing` -> `Cephalon.AspNetCore.JsonRpc.Routing`
- `src/Cephalon.AspNetCore.GraphQL/Hosting` -> `Cephalon.AspNetCore.GraphQL.Hosting`
- `src/Cephalon.AspNetCore.GraphQL/Modules` -> `Cephalon.AspNetCore.GraphQL.Modules`
- `src/Cephalon.AspNetCore.GraphQL/Routing` -> `Cephalon.AspNetCore.GraphQL.Routing`
- `src/Cephalon.AspNetCore.Grpc/Hosting` -> `Cephalon.AspNetCore.Grpc.Hosting`
- `src/Cephalon.AspNetCore.Grpc/Modules` -> `Cephalon.AspNetCore.Grpc.Modules`
- `src/Cephalon.AspNetCore.Grpc/Protos` -> generated `Cephalon.AspNetCore.Grpc.Contracts.*`
- `src/Cephalon.AspNetCore.Grpc/Routing` -> `Cephalon.AspNetCore.Grpc.Routing`
- `src/Cephalon.Worker/Hosting` -> `Cephalon.Worker.Hosting`
- `src/Cephalon.Observability/Configuration` -> `Cephalon.Observability.Configuration`
- `src/Cephalon.Observability/Hosting` -> `Cephalon.Observability.Hosting`
- `src/Cephalon.Observability.ElasticsearchDependencies/Configuration` -> `Cephalon.Observability.ElasticsearchDependencies.Configuration`
- `src/Cephalon.Observability.ElasticsearchDependencies/Hosting` -> `Cephalon.Observability.ElasticsearchDependencies.Hosting`
- `src/Cephalon.Observability.ElasticsearchDependencies/Services` -> `Cephalon.Observability.ElasticsearchDependencies.Services`
- `src/Cephalon.Observability.HttpDependencies/Configuration` -> `Cephalon.Observability.HttpDependencies.Configuration`
- `src/Cephalon.Observability.HttpDependencies/Hosting` -> `Cephalon.Observability.HttpDependencies.Hosting`
- `src/Cephalon.Observability.HttpDependencies/Services` -> `Cephalon.Observability.HttpDependencies.Services`
- `src/Cephalon.Observability.KafkaDependencies/Configuration` -> `Cephalon.Observability.KafkaDependencies.Configuration`
- `src/Cephalon.Observability.KafkaDependencies/Hosting` -> `Cephalon.Observability.KafkaDependencies.Hosting`
- `src/Cephalon.Observability.KafkaDependencies/Services` -> `Cephalon.Observability.KafkaDependencies.Services`
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
- `samples/Cephalon.ReferenceModule.*` -> reference module package examples
- `benchmarks/Cephalon.Benchmarks/Composition` -> `Cephalon.Benchmarks.Composition`
- `benchmarks/Cephalon.Benchmarks/Runtime` -> `Cephalon.Benchmarks.Runtime`
- `benchmarks/Cephalon.Benchmarks/Scaffolding` -> `Cephalon.Benchmarks.Scaffolding`
- `benchmarks/Cephalon.Benchmarks/Support` -> `Cephalon.Benchmarks.Support`

## Current opinionated defaults

- module dependencies are explicit and type-based
- capabilities are first-class and introspectable
- app blueprints and pattern selections are first-class and introspectable
- blueprint scaffold plans are first-class and introspectable
- scaffold generation can be driven from the same app-profile contract used by runtime introspection
- the shipped `dotnet new` template pack should stay aligned with blueprint and scaffold-plan semantics
- module authoring starters should stay aligned with module contracts, lifecycle rules, localization behavior, and transport contribution interfaces
- transport selections are first-class and introspectable
- manifest v2 includes schema version, engine version, module metadata, and capability source-module mapping
- engine app shape is configuration-driven through the `Engine` section
- host builders can also split project configuration through `Configurations/Add*.json` and `Configurations/{group}/{Environment}.json`
- engine module discovery can be driven through `Engine:Discovery:Assemblies`
- engine package assembly loading can be driven through `Engine:Discovery:Packages`
- engine package-directory discovery can be driven through `Engine:Discovery:PackageDirectories`
- package manifests should use `version`, `compatibility.minimumEngineVersion`, `compatibility.maximumEngineVersion`, `compatibility.supportedTargetFrameworks`, and optional `integrity.sha256` when independently shipped packages need stronger compatibility or supply-chain signals
- package manifests can also carry `publisher`, `signature`, or `signatures` metadata so trust and operator diagnostics can reason about provenance
- package signatures can be cryptographically verified through `signature.keyId`, `signature.value`, or equivalent entries in `signatures[]`, together with `Engine:Trust:TrustedSignaturePublicKeys`
- engine package-governance requirements can be driven through `Engine:PackagePolicy`
- engine future-tech selection can be driven through `Engine:Technologies`
- engine future-tech catalog can be extended through `ITechnologyContributor` or `engine.RegisterTechnology(...)`
- active future-tech runtime surfaces can be inspected through `ITechnologyRuntimeContributor` and `/engine/technology-surfaces`
- merged operator-facing runtime introspection should come from `IRuntimeIntrospectionSnapshotProvider` and `/engine/snapshot` instead of recomposing manifest, status, technology surfaces, diagnostics conventions, and lifecycle story data ad hoc in hosts
- future-tech runtime primitives should live in companion packages such as `Cephalon.Agentics`, `Cephalon.Eventing`, `Cephalon.Retrieval`, or `Cephalon.Edge`
- installed modules should extend shipped technology packs through pack-specific contributor services instead of hardcoding host-owned descriptor lists
- engine options can disable modules and capabilities through `Engine:Options`
- engine observability conventions can be tuned through `Engine:Observability`
- engine telemetry export guidance can be tuned through `Engine:Observability:Telemetry`
- runtime diagnostics conventions should flow through `IRuntimeDiagnosticsCatalog`, `/engine/diagnostics`, and `/engine/snapshot`
- runtime lifecycle answers should flow through `IRuntime.OperationalStory`, `/engine/runtime-story`, and `/engine/snapshot`
- hosts can turn Elasticsearch cluster-health checks into reusable dependency-health contributions through `Engine:Observability:DependencyHealth:Elasticsearch` and `Cephalon.Observability.ElasticsearchDependencies`
- hosts can turn external HTTP upstreams into reusable dependency-health contributions through `Engine:Observability:DependencyHealth:Http` and `Cephalon.Observability.HttpDependencies`, but that pack should stay generic across HTTP semantics rather than absorbing product-specific response mapping
- hosts can turn Kafka cluster metadata checks into reusable dependency-health contributions through `Engine:Observability:DependencyHealth:Kafka` and `Cephalon.Observability.KafkaDependencies`
- hosts can turn MongoDB dependencies into reusable dependency-health contributions through `Engine:Observability:DependencyHealth:MongoDb` and `Cephalon.Observability.MongoDbDependencies`
- hosts can turn MQTT broker dependencies into reusable dependency-health contributions through `Engine:Observability:DependencyHealth:Mqtt` and `Cephalon.Observability.MqttDependencies`
- hosts can turn MySQL and MariaDB dependencies into reusable dependency-health contributions through `Engine:Observability:DependencyHealth:MySql` and `Cephalon.Observability.MySqlDependencies`
- hosts can turn NATS broker dependencies into reusable dependency-health contributions through `Engine:Observability:DependencyHealth:Nats` and `Cephalon.Observability.NatsDependencies`, with the package free to grow across NATS-native auth, TLS, and broker reachability semantics without becoming a catch-all for unrelated workload logic
- hosts can turn Postgres dependencies into reusable dependency-health contributions through `Engine:Observability:DependencyHealth:Postgres` and `Cephalon.Observability.PostgresDependencies`
- hosts can turn RabbitMQ dependencies into reusable dependency-health contributions through `Engine:Observability:DependencyHealth:RabbitMq` and `Cephalon.Observability.RabbitMqDependencies`
- hosts can turn Redis and cache endpoints into reusable dependency-health contributions through `Engine:Observability:DependencyHealth:Redis` and `Cephalon.Observability.RedisDependencies`
- hosts can turn SQL Server and Azure SQL dependencies into reusable dependency-health contributions through `Engine:Observability:DependencyHealth:SqlServer` and `Cephalon.Observability.SqlServerDependencies`
- hosts can turn that telemetry contract into a supported OTLP path through `Cephalon.Observability.OpenTelemetry`
- engine trust and capability policy can be tuned through `Engine:Trust`
- engine trust policy can also allow-list package checksums through `Engine:Trust:AllowedPackageChecksums`
- engine trust policy can also allow-list publishers, signer fingerprints, and trusted signature public keys
- engine package metadata requirements can be tuned through `Engine:PackagePolicy`
- engine localization can be tuned through `Engine:Localization`
- engine startup, stop, and restart behavior can be tuned through `Engine:FailurePolicy`, including startup warmup, shutdown drain, and manual restart backoff windows
- installed modules can contribute dependency health through `IDependencyHealthContributor`
- installed modules can contribute localization resources through `ILocalizedResourceContributor`
- the runtime manifest is always available
- runtime lifecycle is explicit and introspectable
- runtime policy state is introspectable through `/engine/options`
- runtime failure policy is introspectable through `/engine/failure-policy`
- runtime diagnostics conventions are introspectable through `IRuntimeDiagnosticsCatalog`, `/engine/diagnostics`, and `/engine/snapshot`
- runtime lifecycle story is introspectable through `IRuntime.OperationalStory`, `/engine/runtime-story`, and `/engine/snapshot`
- runtime trust policy is introspectable through `/engine/trust-policy`
- runtime package loading is introspectable through `/engine/packages`, including package `kind`, resolved assembly `path`, discovery `sourcePath`, declared version/compatibility, computed checksum, signature verification state, and trust reason
- runtime package provenance is introspectable through `/engine/packages` and `/engine/trust-policy`, including publisher id, signature key id, and signer fingerprint when the package manifest declared them
- runtime package governance is introspectable through `/engine/package-policy`
- runtime technology selection is introspectable through `/engine/technologies`
- runtime technology catalog is introspectable through `/engine/technology-catalog`
- runtime dependency health is introspectable through `/engine/dependencies`
- runtime localization state is introspectable through `/engine/localization`
- runtime health semantics are exposed through `/health`, `/health/live`, and `/health/ready`
- blueprint-driven project shape is introspectable through `AppProfile.Scaffold` and `/engine/scaffold`
- the same runtime can be hosted in ASP.NET Core or generic worker hosts without changing module contracts
- the engine emits built-in metrics and tracing through the `Cephalon.Engine` meter and activity source
- the host stays thin and pushes behavior into modules
- selected transports gate which host routes are mapped
- when `RestApi` is selected on ASP.NET Core, REST endpoints should surface through OpenAPI and Scalar
- keep REST OpenAPI customization inside `Cephalon.AspNetCore.Transformers`
- non-REST protocol endpoints should stay excluded from REST API documentation
- adapter packages must be registered for transports that are not built into the host core
- REST endpoints can opt into capability enforcement through `RequireCapability(...)`

## Next expansion points

- richer capability metadata and policy
- startup hooks and lifecycle events
- event bus / workflow runtime
- broader provider-specific dependency-health packs, richer operator-runtime answers, and deeper release-validation guidance on top of the shipped Elasticsearch, HTTP, Kafka, MongoDB, MQTT, MySQL, NATS, Postgres, RabbitMQ, Redis, SQL Server, and OpenTelemetry observability companions
- richer parameterized templates and generators driven by scaffold plans
- richer localization catalogs and package-provided language packs
- sustained benchmark coverage for hot engine paths
