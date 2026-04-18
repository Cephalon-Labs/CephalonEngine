# Cephalon

Cephalon is being rebuilt as a modular .NET engine/framework foundation.

This first cut focuses on the core shape we can keep growing:

- host-agnostic module contracts
- dependency-aware engine composition
- assembly-based module discovery
- package discovery with manifest-driven compatibility and integrity metadata
- runtime manifest and capability discovery
- configuration-driven module and capability policy
- manifest v2 with schema version, engine version, module metadata, and capability source tracking
- blueprint-aware scaffold plans with project, folder, and package conventions
- lifecycle hooks and runtime status tracking
- ASP.NET Core integration for shipping modules over HTTP
- generic-host worker integration for non-HTTP runtime scenarios
- runtime-neutral data companion package with handler-backed read/write stores
- optional Entity Framework Core companion package for read/write DbContext baselines, inbox/outbox storage, and `Sfid.EntityFramework` integration
- optional identity companion packages for host-agnostic authorization plus ASP.NET Core adapter follow-through
- optional multi-tenancy companion package for tenant resolution and ambient tenant context
- optional audit companion package for host-agnostic audit recording and runtime cataloging
- optional `Sfid.Net`-backed identifier companion package for low-ceremony database ids
- optional Wolverine eventing adapter as the current first-class managed dispatch path
- observability package with runtime logs, metrics, and tracing conventions
- optional Cassandra dependency-health companion package for Cassandra readiness reporting
- optional ClickHouse dependency-health companion package for analytics-database readiness reporting
- optional Consul dependency-health companion package for control-plane readiness reporting
- optional Elasticsearch dependency-health companion package for search-cluster readiness reporting
- optional HTTP dependency-health companion package for external API readiness reporting
- optional Kafka dependency-health companion package for broker metadata readiness reporting
- optional Memcached dependency-health companion package for cache readiness reporting
- optional MongoDB dependency-health companion package for document-database readiness reporting
- optional MQTT dependency-health companion package for broker readiness reporting
- optional MySQL dependency-health companion package for MySQL and MariaDB readiness reporting
- optional NATS dependency-health companion package for broker readiness reporting
- optional Neo4j dependency-health companion package for graph-database readiness reporting
- optional OpenSearch dependency-health companion package for search-cluster readiness reporting
- optional Oracle dependency-health companion package for Oracle Database readiness reporting
- optional Postgres dependency-health companion package for database readiness reporting
- optional RabbitMQ dependency-health companion package for broker readiness reporting
- optional Redis dependency-health companion package for cache and Redis readiness reporting
- optional SQL Server dependency-health companion package for SQL Server and Azure SQL readiness reporting
- optional OpenTelemetry exporter companion package for OTLP host wiring
- optional Alibaba Cloud observability companion package for OTLP host wiring and managed OpenTelemetry traces/metrics
- optional AWS observability companion package for X-Ray-compatible OTLP host wiring
- optional Grafana Cloud observability companion package for OTLP host wiring and access-policy authentication guidance
- optional GCP observability companion package for OTLP host wiring and Google-managed traces/metrics defaults
- optional Huawei Cloud observability companion package for OTLP host wiring and managed APM trace ingestion
- optional New Relic observability companion package for OTLP host wiring and `api-key` authentication guidance
- optional Oracle Cloud observability companion package for OTLP host wiring and managed APM traces/metrics ingestion
- optional Kubernetes observability companion package for in-cluster OTLP collector wiring and generic cluster defaults
- optional OpenShift observability companion package for in-cluster OTLP collector wiring and hosted defaults
- optional Tanzu observability companion package for proxy-focused OTLP trace handoff and hosted defaults
- optional Azure Monitor exporter companion package for OTLP host wiring
- optional Serilog provider companion package for `ILogger` host wiring
- a runnable playground and tests that prove the architecture

## Solution layout

- `src/Cephalon.Abstractions`: contracts that modules build against
- `src/Cephalon.Agentics`: companion package for agentic workload runtime services
- `src/Cephalon.Edge`: companion package for edge-native delivery runtime services
- `src/Cephalon.Engine`: composition, dependency ordering, manifest generation
- `src/Cephalon.AspNetCore`: ASP.NET Core host core plus built-in REST, SSE, and WebSocket transport mapping
- `src/Cephalon.AspNetCore.GraphQL`: GraphQL transport adapter for ASP.NET Core
- `src/Cephalon.AspNetCore.JsonRpc`: JSON-RPC transport adapter for ASP.NET Core
- `src/Cephalon.AspNetCore.Grpc`: gRPC transport adapter for ASP.NET Core
- `src/Cephalon.Audit`: host-agnostic audit recording companion package
- `src/Cephalon.Data`: runtime-neutral data companion package for command/query-backed stores
- `src/Cephalon.Data.EntityFramework`: Entity Framework Core companion package for read/write DbContexts plus inbox/outbox baselines
- `src/Cephalon.Eventing`: companion package for event-driven integration runtime services
- `src/Cephalon.Eventing.Wolverine`: official Wolverine adapter companion package for managed event dispatch
- `src/Cephalon.Identity`: host-agnostic identity and authorization companion package
- `src/Cephalon.Identity.AspNetCore`: ASP.NET Core adapter for Cephalon identity and authorization
- `src/Cephalon.Ids.Sfid`: official `Sfid.Net` identifier companion package
- `src/Cephalon.MultiTenancy`: host-agnostic multi-tenancy companion package
- `src/Cephalon.Worker`: Generic Host worker adapter for non-HTTP hosts
- `src/Cephalon.Observability`: observability package for logs, metrics, and tracing conventions
- `src/Cephalon.Observability.CassandraDependencies`: optional Cassandra dependency-health companion package for Cassandra probes
- `src/Cephalon.Observability.ClickHouseDependencies`: optional ClickHouse dependency-health companion package for analytics-database probes
- `src/Cephalon.Observability.ConsulDependencies`: optional Consul dependency-health companion package for control-plane probes
- `src/Cephalon.Observability.ElasticsearchDependencies`: optional Elasticsearch dependency-health companion package for cluster-health probes
- `src/Cephalon.Observability.HttpDependencies`: optional HTTP dependency-health companion package for external API probes
- `src/Cephalon.Observability.KafkaDependencies`: optional Kafka dependency-health companion package for broker metadata probes
- `src/Cephalon.Observability.MemcachedDependencies`: optional Memcached dependency-health companion package for cache probes
- `src/Cephalon.Observability.MongoDbDependencies`: optional MongoDB dependency-health companion package for document-database probes
- `src/Cephalon.Observability.MqttDependencies`: optional MQTT dependency-health companion package for broker protocol probes
- `src/Cephalon.Observability.MySqlDependencies`: optional MySQL dependency-health companion package for MySQL and MariaDB probes
- `src/Cephalon.Observability.NatsDependencies`: optional NATS dependency-health companion package for broker protocol probes
- `src/Cephalon.Observability.Neo4jDependencies`: optional Neo4j dependency-health companion package for graph-database probes
- `src/Cephalon.Observability.OpenSearchDependencies`: optional OpenSearch dependency-health companion package for search-cluster probes
- `src/Cephalon.Observability.OracleDependencies`: optional Oracle dependency-health companion package for Oracle Database probes
- `src/Cephalon.Observability.PostgresDependencies`: optional Postgres dependency-health companion package for database probes
- `src/Cephalon.Observability.RabbitMqDependencies`: optional RabbitMQ dependency-health companion package for broker probes
- `src/Cephalon.Observability.RedisDependencies`: optional Redis dependency-health companion package for cache and Redis probes
- `src/Cephalon.Observability.SqlServerDependencies`: optional SQL Server dependency-health companion package for SQL Server and Azure SQL probes
- `src/Cephalon.Observability.OpenTelemetry`: optional OpenTelemetry OTLP exporter companion package for host integration
- `src/Cephalon.Observability.AlibabaCloud`: optional Alibaba Cloud observability companion package for OTLP host integration
- `src/Cephalon.Observability.Aws`: optional AWS observability companion package for OTLP host integration
- `src/Cephalon.Observability.GrafanaCloud`: optional Grafana Cloud observability companion package for OTLP host integration
- `src/Cephalon.Observability.DigitalOcean`: optional DigitalOcean observability companion package for collector-first OTLP host integration
- `src/Cephalon.Observability.Gcp`: optional GCP observability companion package for OTLP host integration
- `src/Cephalon.Observability.HuaweiCloud`: optional Huawei Cloud observability companion package for OTLP host integration
- `src/Cephalon.Observability.NewRelic`: optional New Relic observability companion package for OTLP host integration
- `src/Cephalon.Observability.OracleCloud`: optional Oracle Cloud observability companion package for OTLP host integration
- `src/Cephalon.Observability.Kubernetes`: optional Kubernetes observability companion package for collector-first OTLP host integration
- `src/Cephalon.Observability.OpenShift`: optional OpenShift observability companion package for OTLP host integration
- `src/Cephalon.Observability.Tanzu`: optional Tanzu observability companion package for OTLP host integration
- `src/Cephalon.Observability.AzureMonitor`: optional Azure Monitor exporter companion package for host integration
- `src/Cephalon.Observability.Serilog`: optional Serilog provider companion package for host integration
- `src/Cephalon.Retrieval`: companion package for retrieval/runtime knowledge services
- `src/Cephalon.ReferenceDocs`: optional reference-doc publishing tool that can turn XML comments into browsable API reference output
- `src/Cephalon.Cli`: command-line surface for blueprint generation, external package staging, and reference-doc workflows, with `CliApplication` as the stable entry point
- `src/Cephalon.Scaffolding`: scaffold generator that turns app profiles into solution/projects/files
- `templates/Cephalon.TemplatePack`: `dotnet new` template pack for the shipped Cephalon blueprints
- `benchmarks/Cephalon.Benchmarks`: BenchmarkDotNet suite for composition, runtime lifecycle, and scaffolding performance
- `samples/*`: adoption-quality sample apps plus reference module packages
- `playground/Cephalon.Playground`: a minimal host that demonstrates the stack
- `playground/Cephalon.WorkerPlayground`: a worker host that demonstrates non-HTTP runtime execution
- `tests/Cephalon.Tests`: unit and integration tests for the engine surface

## Component docs

- docs hub: `docs/README.md`
- component catalog: `docs/components/README.md`
- per-package docs cover every `src/Cephalon.*` project in the current engine/runtime/tooling surface
- cross-cutting docs stay in `docs/` for topics such as architecture, app models, operations, technology packs, benchmarking, and reference-doc publishing

## Documentation model

- hand-authored `.md` files are the human-facing product docs for Cephalon capabilities, architecture, and adoption guidance
- detailed XML comments on public contracts are the API explanation layer for IntelliSense and external documentation generators
- `Cephalon.ReferenceDocs` is optional repo-local tooling for publishing reference output from those XML comments when we want a browsable artifact inside this repository

## Configuration conventions

When a host calls `AddCephalon(...)`, Cephalon now also loads split project configuration from the host content root:

- `Configurations/Add*.json`
- `Configurations/{group}/{Environment}.json`

Examples:

- `Configurations/AddOpenApi.json`
- `Configurations/OpenApi/Local.json`
- `Configurations/OpenApi/Development.json`

This keeps config-driven features such as engine settings, OpenAPI, hosted docs, or future CORS conventions out of one oversized `appsettings.json`. If host code needs those values before `AddCephalon(...)`, call `AddCephalonProjectConfigurations()` first.
The split files load before the normal `appsettings.json` and `appsettings.{Environment}.json` layers, so teams can keep grouped defaults under `Configurations/` while still using standard host overrides when they need them.

## Quick start

```powershell
  dotnet build
  dotnet test
  dotnet run --project src/Cephalon.Cli -- --help
  dotnet run --project src/Cephalon.Cli -- doctor
  dotnet pack src/Cephalon.Cli/Cephalon.Cli.csproj -c Release -o artifacts/cli-tool
  dotnet tool install --tool-path .\.tools\cephalon Cephalon.Cli --add-source .\artifacts\cli-tool --ignore-failed-sources --no-cache --prerelease
  .\.tools\cephalon\cephalon doctor
  .\.tools\cephalon\cephalon --help
  pwsh ./scripts/publish-package-artifacts.ps1 -SkipBuild
  dotnet run --project src/Cephalon.Cli -- docs publish --root .
  dotnet run --project src/Cephalon.Cli -- docs publish --root . --open
  dotnet run --project src/Cephalon.Cli -- docs publish --root . --enable-hosting --appsettings playground/Cephalon.Playground/appsettings.json
dotnet run --project src/Cephalon.Cli -- docs publish --root . --enable-hosting --validate-hosting --appsettings playground/Cephalon.Playground/appsettings.json --host-url https://localhost:7235
dotnet run --project src/Cephalon.Cli -- docs publish --root . --enable-hosting --appsettings playground/Cephalon.Playground/appsettings.json --open --host-url https://localhost:7235
dotnet run --project src/Cephalon.Cli -- docs enable-hosting --appsettings playground/Cephalon.Playground/appsettings.json --root .
dotnet run --project src/Cephalon.Cli -- docs validate-hosting --appsettings playground/Cephalon.Playground/appsettings.json --host-url https://localhost:7235
dotnet run --project src/Cephalon.ReferenceDocs -- --help
dotnet pack templates/Cephalon.TemplatePack/Cephalon.TemplatePack.csproj -c Release -o artifacts/template-pack
  dotnet run -c Release --project benchmarks/Cephalon.Benchmarks
  dotnet run -c Release --project benchmarks/Cephalon.Benchmarks -- --validate-guardrails
  pwsh ./scripts/validate-release.ps1
  pwsh ./scripts/validate-container-runtime.ps1
  pwsh ./scripts/validate-generated-app-publish.ps1
  pwsh ./scripts/validate-generated-app-container-image.ps1
  pwsh ./scripts/validate-generated-app-windows-service.ps1
  pwsh ./scripts/validate-generated-app-iis.ps1
  pwsh ./scripts/validate-generated-app-app-service.ps1
  pwsh ./scripts/validate-generated-app-container-apps.ps1
  pwsh ./scripts/validate-generated-app-kubernetes.ps1
  pwsh ./scripts/validate-generated-app-systemd.ps1
  pwsh ./scripts/publish-reference-docs.ps1
  docker compose -f samples/Cephalon.Sample.ModularMonolith/compose.yaml up --build
  dotnet run --project samples/Cephalon.Sample.ModularMonolith
  dotnet run --project samples/Cephalon.Sample.ModularVerticalSlice
  dotnet run --project samples/Cephalon.Sample.Microservice
  dotnet run --project playground/Cephalon.Playground
  dotnet run --project playground/Cephalon.WorkerPlayground
```

For local validation, prefer PowerShell 7 with `pwsh`. The same script entry points now work from Windows, WSL, and Linux-class shells. The current CI baseline proves the full release-validation flow on Windows and the same repo-native flow with `-SkipBenchmarks` on Ubuntu until benchmark guardrails grow an OS-neutral baseline.

If you are adopting Cephalon from a clean machine or package source, start with [docs/getting-started.md](docs/getting-started.md). That guide walks through installing the CLI, running `cephalon doctor`, scaffolding a host, and validating the runtime introspection endpoints.

If you want the published-output baseline for a freshly generated app, continue with [docs/generated-app-publishing.md](docs/generated-app-publishing.md). That guide walks through the shipped `CephalonFolder.pubxml` publish profile, deterministic `artifacts/publish/*` output, and the optional `pwsh ./scripts/validate-generated-app-publish.ps1` replay.

If you want a provider-neutral build/tag/push image baseline for a freshly generated app, continue with [docs/container-image-publishing.md](docs/container-image-publishing.md). That guide walks through the shipped `deploy/container-image/*` assets, the previewable Docker command contract, and the optional `pwsh ./scripts/validate-generated-app-container-image.ps1` replay.

If you want a self-hosted Windows Service baseline for a freshly generated app, continue with [docs/windows-service-deployment.md](docs/windows-service-deployment.md). That guide walks through the shipped `deploy/windows-service/*` assets, the expected `C:\Services\*` install shape, and the optional `pwsh ./scripts/validate-generated-app-windows-service.ps1` replay.

If you want a hosted Windows IIS baseline for a freshly generated app, continue with [docs/iis-deployment.md](docs/iis-deployment.md). That guide walks through the shipped `deploy/iis/*` assets, the expected `C:\inetpub\sites\*` layout, the generated ASP.NET Core Module `web.config` contract, and the optional `pwsh ./scripts/validate-generated-app-iis.ps1` replay.

If you want a hosted Azure App Service baseline for a freshly generated app, continue with [docs/azure-app-service-deployment.md](docs/azure-app-service-deployment.md). That guide walks through the shipped `deploy/azure-app-service/*` assets, the generated ZIP packaging path, the previewable Azure CLI contract, and the optional `pwsh ./scripts/validate-generated-app-app-service.ps1` replay.

If you want a hosted Azure Container Apps baseline for a freshly generated app, continue with [docs/azure-container-apps-deployment.md](docs/azure-container-apps-deployment.md). That guide walks through the shipped `deploy/azure-container-apps/*` assets, the generated Dockerfile/source deployment path, the previewable `az containerapp up --source` contract, and the optional `pwsh ./scripts/validate-generated-app-container-apps.ps1` replay.

If you want a platform-neutral Kubernetes baseline for a freshly generated app, continue with [docs/kubernetes-deployment.md](docs/kubernetes-deployment.md). That guide walks through the shipped `deploy/kubernetes/*` assets, the generated Dockerfile plus manifest path, the previewable `kubectl kustomize` contract, and the optional `pwsh ./scripts/validate-generated-app-kubernetes.ps1` replay.

If you want a self-hosted Linux service-manager baseline for a freshly generated app, continue with [docs/linux-systemd-deployment.md](docs/linux-systemd-deployment.md). That guide walks through the shipped `deploy/linux/systemd/*` assets, the expected `/opt/*` and `/etc/cephalon/*` install shape, and the optional `pwsh ./scripts/validate-generated-app-systemd.ps1` replay.

If you are shipping independently distributed module packages, continue with [docs/external-package-lifecycle.md](docs/external-package-lifecycle.md) for the publish, stage, trust, load, and inspect path.

If you want a reproducible Docker Desktop / WSL runtime smoke for the shipped host surface, continue with [docs/container-runtime.md](docs/container-runtime.md). That guide walks through the modular monolith sample compose stack, OTLP collector handoff, runtime routes, and the optional `pwsh ./scripts/validate-container-runtime.ps1` replay.

Generated app roots from `cephalon new` and the app-focused `dotnet new` starters now also emit `NuGet.config`, `./.cephalon/packages/README.md`, `Properties/PublishProfiles/CephalonFolder.pubxml`, `deploy/windows-service/README.md`, `deploy/windows-service/install-service.ps1`, `deploy/windows-service/remove-service.ps1`, `deploy/iis/README.md`, `deploy/iis/install-site.ps1`, `deploy/iis/remove-site.ps1`, `deploy/azure-app-service/README.md`, `deploy/azure-app-service/deploy-zip.ps1`, `deploy/container-image/README.md`, `deploy/container-image/publish-image.ps1`, `deploy/azure-container-apps/README.md`, `deploy/azure-container-apps/deploy-up.ps1`, `deploy/kubernetes/README.md`, `deploy/kubernetes/apply.ps1`, `deploy/kubernetes/kustomization.yaml`, `deploy/kubernetes/namespace.yaml`, `deploy/kubernetes/deployment.yaml`, `deploy/kubernetes/service.yaml`, `deploy/linux/systemd/README.md`, `deploy/linux/systemd/<App>.service`, `deploy/linux/systemd/<App>.env`, `.dockerignore`, `Dockerfile`, `compose.yaml`, and `otel-collector-config.yaml`, so teams can seed a repo-local package feed, validate a fresh scaffold with deterministic published output, build and publish a provider-neutral container image, install a self-hosted Windows or Linux service shape, preview a hosted IIS site/app-pool path, take either an Azure App Service ZIP deploy path, an Azure Container Apps source-deploy path, or a platform-neutral Kubernetes manifest path, and then use `docker compose up --build` before they add platform-specific deployment packaging.

When the playground is running, open:

- `/`
- `/engine`
- `/engine/manifest`
- `/engine/snapshot`
- `/engine/runtime-story`
- `/engine/app-model`
- `/engine/scaffold`
- `/engine/modules`
- `/engine/packages`
- `/engine/execution-graphs`
- `/engine/execution-graphs/{graphId}`
- `/engine/hosted-executions`
- `/engine/hosted-executions/{hostedExecutionId}`
- `/engine/patterns`
- `/engine/technologies`
- `/engine/technology-catalog`
- `/engine/technology-surfaces`
- `/engine/technology-surfaces/{technologyId}`
- `/engine/transports`
- `/engine/dependencies`
- `/engine/localization`
- `/engine/reference-docs`
- `/engine/options`
- `/engine/package-policy`
- `/engine/failure-policy`
- `/engine/trust-policy`
- `/engine/status`
- `/engine/diagnostics`
- `/engine/capabilities`
- `/health`
- `/health/live`
- `/health/ready`
- `/reference`
- `/reference/browse.html`
- `/reference/members.md`
- `/reference/reference-manifest.json`
- `/openapi/v1.json`
- `/scalar` (redirects to the default canonical document such as `/scalar/v1`)
- `/scalar/` (Scalar shell for multi-document selection)
- `/scalar/v1` (canonical pinned document deep link)
- `/scalar/openapi-toggle.js`
- `/scalar/assets/favicon.svg`
- `/api/platform/time`
- `/api/discovery/hello/Codex`
- `/rpc/discovery`
- `/events/discovery/principles`
- `/ws/discovery`

The default docs and REST prefixes are configurable through `OpenApi:RoutePattern`, `OpenApi:Scalar:RoutePrefix`, and `ApiRoutes:Prefixes:Rest` when a host needs paths other than `/openapi`, `/scalar`, and `/api`.

## Design direction

Cephalon is aiming for a future where new capabilities can be delivered as modules instead of rewrites. The engine owns composition, dependency ordering, runtime introspection, and host integration. Product code should be able to plug into that surface without coupling itself to one transport or one monolith.

The first execution-and-hosted baseline now lets active modules publish operator-facing workflow descriptors through `/engine/execution-graphs` plus operator-facing background or hosted descriptors through `/engine/hosted-executions`, with `/engine/snapshot` carrying both catalogs in one payload. `/engine/runtime-story` now surfaces the lifecycle side of those descriptors without bypassing the existing module and capability model, while the hosted-execution contract stays descriptive instead of introducing a separate Cephalon workflow runner. `Cephalon.Agentics` can now link tools back to capability keys, execution graphs, and hosted executions through those same contracts, and `/engine/technology-surfaces` projects the live orchestration state without inventing a parallel AI runner model.

The engine is configuration-driven. A Cephalon app can choose its base blueprint, supporting patterns, future-facing technology profiles, and transport surface through the `Engine` section in configuration, while still allowing code-level overrides when needed.

```json
{
  "Engine": {
    "Blueprint": "ModularVerticalSlice",
    "Discovery": {
      "Assemblies": ["Cephalon.Playground"]
    },
    "Patterns": ["StrategyPattern", "PipelinePattern"],
    "Observability": {
      "LogManifestSummary": true,
      "LogModuleSummary": true,
      "LogCapabilitySummary": true,
      "Telemetry": {
        "Provider": "OpenTelemetry",
        "Protocol": "otlp/http",
        "Endpoint": "http://localhost:4318",
        "ExportLogs": true,
        "ExportMetrics": true,
        "ExportTraces": true
      }
    },
    "FailurePolicy": {
      "StartupFailureBehavior": "FailFast",
      "StopFailureBehavior": "BestEffortContinue",
      "AllowManualRestart": true,
      "MaxRestartAttempts": 3
    },
    "PackagePolicy": {
      "AllowAssemblyPathPackages": true,
      "RequireVersion": true,
      "RequireMinimumEngineVersion": true,
      "RequireSupportedTargetFrameworks": true,
      "RequirePublisherId": true,
      "RequireSignatureFingerprint": false,
      "RequireSignatureKeyId": false,
      "RequireSignatureValue": false,
      "RequireSignatureVerification": false,
      "RequireIntegritySha256": false
    },
    "Trust": {
      "RequireTrustedPackages": false,
      "DefaultCapabilityAccess": "Allowed",
      "TrustedPackages": ["operations"],
      "TrustedAssemblies": ["Cephalon.Playground"],
      "TrustedPublishers": ["cephalon-labs"],
      "TrustedSignerFingerprints": [
        "sha256:cephalon-labs-reference-operations"
      ],
      "TrustedSignaturePublicKeys": {
        "cephalon-labs-build": "keys/cephalon-labs-build.public.pem"
      },
      "TrustedSignatureCertificates": {
        "cephalon-labs-signing-cert": "keys/cephalon-labs-signing-cert.pem"
      },
      "TrustedSignatureCertificateAuthorities": [
        "keys/cephalon-labs-root.pem"
      ],
      "AllowedPackageChecksums": {
        "operations": [
          "sha256:3e5d5b9fd0dfb7c60e441d013d7d2a60f41c7b0a0a4fb2d2ad5a9f88d6e7c123"
        ]
      },
      "Capabilities": {
        "restricted.secret": "Denied"
      }
    },
    "Localization": {
      "DefaultCulture": "en",
      "SupportedCultures": ["en", "th"],
      "Resources": {
        "th": {
          "engine.docs.rest.title": "เอกสาร REST ของ Cephalon",
          "engine.docs.rest.description": "พื้นผิว REST ภาษาไทยของ Cephalon"
        }
      }
    },
    "Technologies": ["AgenticWorkloads", "EventDrivenIntegration", "KnowledgeRetrieval", "EdgeNativeDelivery"],
    "Transports": ["RestApi"],
    "Options": {
      "Modules": {
        "discovery": {
          "Enabled": true
        }
      },
      "Capabilities": {
        "platform.clock": true
      }
    }
  }
}
```

Hosts can still register modules manually in code, but they can also let the engine scan one or more assemblies declared in `Engine:Discovery:Assemblies`.

For independently shipped module assemblies, hosts can declare direct DLL paths or package manifests through `Engine:Discovery:Packages`:

```json
{
  "Engine": {
    "Discovery": {
      "Packages": [
        {
          "Id": "operations",
          "Path": "plugins/Cephalon.ReferenceModule.Operations.dll"
        },
        {
          "ManifestPath": "plugins/reference-operations/cephalon.package.json"
        }
      ]
    }
  }
}
```

Package directories can also be scanned for `cephalon.package.json` manifests:

```json
{
  "Engine": {
    "Discovery": {
      "PackageDirectories": [
        {
          "Path": "plugins",
          "IncludeSubdirectories": true
        }
      ]
    }
  }
}
```

The code-level equivalents are available too:

```csharp
builder.AddCephalon(engine =>
{
    engine.AddPackageAssembly("plugins/Cephalon.ReferenceModule.Operations.dll", id: "operations");
    engine.AddPackageManifest("plugins/reference-operations/cephalon.package.json");
    engine.AddPackageDirectory("plugins");
});
```

`cephalon.package.json` now supports lightweight package metadata beyond the assembly path:

```json
{
  "id": "operations",
  "version": "1.0.0",
  "assembly": "Cephalon.ReferenceModule.Operations.dll",
  "publisher": {
    "id": "cephalon-labs",
    "displayName": "Cephalon Labs",
    "website": "https://example.invalid/cephalon-labs"
  },
  "distribution": {
    "channel": "stable",
    "manifestUri": "https://packages.example.invalid/cephalon/operations/1.0.0/cephalon.package.json",
    "packageUri": "https://packages.example.invalid/cephalon/operations/1.0.0/Cephalon.ReferenceModule.Operations.zip"
  },
  "provenance": {
    "sourceRepository": "https://github.com/Cephalon-Labs/CephalonEngine",
    "sourceRevision": "refs/tags/operations-v1.0.0",
    "buildUri": "https://builds.example.invalid/cephalon/operations/1.0.0",
    "statementUri": "https://packages.example.invalid/cephalon/operations/1.0.0/provenance.json"
  },
  "signature": {
    "type": "detached-signature",
    "signer": "Cephalon Labs Build",
    "keyId": "cephalon-labs-build",
    "fingerprint": "sha256:cephalon-labs-reference-operations",
    "algorithm": "RSA-SHA256",
    "value": "<base64-detached-signature>"
  },
  "compatibility": {
    "minimumEngineVersion": "1.0.0",
    "maximumEngineVersion": "2.0.0",
    "supportedTargetFrameworks": ["net10.0"]
  },
  "integrity": {
    "sha256": "sha256:3e5d5b9fd0dfb7c60e441d013d7d2a60f41c7b0a0a4fb2d2ad5a9f88d6e7c123"
  }
}
```

The engine validates that metadata when loading package manifests:

- `compatibility.minimumEngineVersion` and `compatibility.maximumEngineVersion` gate the current engine version
- `compatibility.supportedTargetFrameworks` gates the current runtime target framework
- `publisher.id` and optional publisher display metadata expose provenance information
- `distribution` can describe the public release channel plus where operators should fetch the manifest or packaged artifact from outside the repo
- `provenance` can describe the source repository, source revision, build URI, and provenance statement or attestation URI
- `signature.keyId`, `signature.fingerprint`, and related signer metadata expose a trustable signing identity for policy and diagnostics
- when `signature.value` is present and `Engine:Trust` resolves either a matching trusted public key or a trusted signing certificate plus certificate authority chain, the engine performs detached cryptographic signature verification against the resolved assembly SHA-256 hash
- `integrity.sha256` is optional but, when present, must match the resolved assembly exactly
- `Engine:PackagePolicy` can require manifest-driven package loading and specific metadata such as version, engine compatibility, target frameworks, publisher id, signer fingerprint, signature key id, signature value, signature verification, or integrity hashes
- `Engine:Trust` can trust a package by package id, assembly name, cryptographically verified signature, publisher id, signer fingerprint, or checksum allow-list

Current note: the shipped baseline now verifies detached signatures when a package declares `signature.keyId` + `signature.value`, or corresponding entries inside `signatures[]`, and the host configures matching trusted public keys or trusted signing certificates plus certificate authorities. Packages can declare multiple signers; the runtime surfaces per-signer verification results, verification source, signing-certificate thumbprints, and external distribution/provenance metadata when that data is declared in `cephalon.package.json`.

Compatibility expectations across package manifests, scaffold output, CLI defaults, template-pack starters, and hosted reference-doc flows are summarized in `docs/compatibility.md`.

Localization is also part of the engine contract now. `Engine:Localization` defines the default culture, supported cultures, and per-culture resource overrides for built-in engine surfaces such as OpenAPI, Scalar, and `/engine/localization`.

Installed modules can also ship language packs by implementing `ILocalizedResourceContributor`. The engine merges those package-provided resources before applying project-level overrides, so package defaults stay available without taking control away from the app.

Technology profiles follow the same layered model now. Built-in profiles come from the engine, installed modules or package-loaded assemblies can contribute more through `ITechnologyContributor`, and project code can register custom catalog entries with `engine.RegisterTechnology(...)` before selecting them through config or code.
Modules can also react to active selections through `ITechnologyServiceContributor` and `ITechnologyCapabilityContributor`, which lets package authors light up services or capabilities only when a matching technology profile is active.

## Reference docs

Public XML comments are the API-reference source for Cephalon. Teams can feed those XML files into any external documentation generator they prefer, and this repository also ships an optional local pipeline in `Cephalon.ReferenceDocs` plus CLI/hosting helpers when we want to publish a reference bundle from the repo itself.

- optional published output: `docs/reference/index.md`
- landing page: `docs/reference/README.md`
- browser UI: `docs/reference/browse.html`
- namespace index: `docs/reference/namespaces.md`
- type index: `docs/reference/types.md`
- member index: `docs/reference/members.md`
- machine-readable manifest: `docs/reference/reference-manifest.json`
- guide: `docs/reference-docs.md`
- publish script: `scripts/publish-reference-docs.ps1`
- optional hosted surface: `/engine/reference-docs` plus the configured route prefix such as `/reference`
- CLI surface: `dotnet run --project src/Cephalon.Cli -- docs publish --root .`
- hosting helper: `dotnet run --project src/Cephalon.Cli -- docs enable-hosting --appsettings src/MyApp/appsettings.json --root .`
- chained flow: `dotnet run --project src/Cephalon.Cli -- docs publish --root . --enable-hosting --appsettings src/MyApp/appsettings.json`
- chained validation flow: `dotnet run --project src/Cephalon.Cli -- docs publish --root . --enable-hosting --validate-hosting --appsettings src/MyApp/appsettings.json --host-url https://localhost:7235`
- open generated docs: `dotnet run --project src/Cephalon.Cli -- docs publish --root . --open`
- open hosted docs URL: `dotnet run --project src/Cephalon.Cli -- docs publish --root . --enable-hosting --appsettings src/MyApp/appsettings.json --open --host-url https://localhost:7235`
- validate hosted docs config: `dotnet run --project src/Cephalon.Cli -- docs validate-hosting --appsettings src/MyApp/appsettings.json --host-url https://localhost:7235`

Typical flow:

```powershell
.\scripts\publish-reference-docs.ps1
```

Or through the main Cephalon CLI:

```powershell
dotnet run --project src/Cephalon.Cli -- docs publish --root .
```

Open the generated browser UI right after publishing:

```powershell
dotnet run --project src/Cephalon.Cli -- docs publish `
  --root . `
  --open
```

Publish and turn on hosted reference docs in one step:

```powershell
dotnet run --project src/Cephalon.Cli -- docs publish `
  --root . `
  --enable-hosting `
  --appsettings src/Acme.Store.Service/appsettings.json
```

Publish, enable hosting, and validate the host wiring in one step:

```powershell
dotnet run --project src/Cephalon.Cli -- docs publish `
  --root . `
  --enable-hosting `
  --validate-hosting `
  --appsettings src/Acme.Store.Service/appsettings.json `
  --host-url https://localhost:7235
```

Publish, enable hosting, and open the hosted route:

```powershell
dotnet run --project src/Cephalon.Cli -- docs publish `
  --root . `
  --enable-hosting `
  --appsettings src/Acme.Store.Service/appsettings.json `
  --open `
  --host-url https://localhost:7235
```

Validate that an app host is wired correctly before running or deploying it:

```powershell
dotnet run --project src/Cephalon.Cli -- docs validate-hosting `
  --appsettings src/Acme.Store.Service/appsettings.json `
  --host-url https://localhost:7235
```

Or turn on hosted reference docs separately in an app host:

```powershell
dotnet run --project src/Cephalon.Cli -- docs enable-hosting `
  --appsettings src/Acme.Store.Service/appsettings.json `
  --root .
```

The release-validation flow now generates a clean release copy under `artifacts/reference-docs-release/` as part of `.\scripts\validate-release.ps1`.

The static browser can now switch between type search and member search, and the manifest includes member-level metadata for future docs-site or tooling integrations.

ASP.NET Core hosts can also serve the generated output directly through `ReferenceDocs` host configuration, which keeps the published browser, Markdown indexes, and JSON manifest reachable from a running Cephalon app.

Projects can also layer language overrides in code:

```csharp
builder.AddCephalon(engine =>
{
    engine.AddLanguageResources("ja", new Dictionary<string, string>
    {
        ["engine.docs.rest.title"] = "Cephalon REST API 日本語"
    });
});
```

Projects can also register future-tech profiles in code when they need something beyond the built-in catalog:

```csharp
builder.AddCephalon(engine =>
{
    engine.RegisterTechnology(new TechnologyDescriptor(
        id: "digital-twin-orchestration",
        displayName: "Digital Twin Orchestration",
        description: "Project-specific technology profile.",
        kind: TechnologyKind.Experience,
        requiresTransports: ["websocket"]));
});
```

`/engine/technologies` shows the active selection, while `/engine/technology-catalog` shows the full catalog available to that runtime after built-in, package, and project contributions have been merged.

When a technology needs reusable runtime behavior, Cephalon now prefers a companion package instead of expanding the engine core directly. The baseline pattern is documented in `docs/technology-packs.md`, with `Cephalon.Agentics`, `Cephalon.Eventing`, `Cephalon.Retrieval`, and `Cephalon.Edge` as the shipped examples. Installed modules can extend those packs through package-specific contributor services such as `IAgentToolContributor`, `IKnowledgeCollectionContributor`, `IEventChannelContributor`, and `IEdgeNodeContributor` instead of forcing hosts to own every descriptor directly. Agentic tool descriptors can now also link back to capability keys, execution graphs, and hosted executions so AI-facing tooling stays grounded in the same runtime contract. Active pack surfaces can then be inspected through `GET /engine/technology-surfaces`, consumed in code through `ITechnologyRuntimeCatalog`, or folded into one operator-facing payload through `GET /engine/snapshot` and `IRuntimeIntrospectionSnapshotProvider`.

If a project needs full control, it can replace the catalog or settings through DI after installing Cephalon:

```csharp
builder.AddCephalon();
builder.Services.AddSingleton(new LocalizationSettings(
    defaultCulture: "fr",
    supportedCultures: ["en", "fr"],
    resources: new Dictionary<string, IReadOnlyDictionary<string, string>>
    {
        ["fr"] = new Dictionary<string, string>
        {
            ["engine.docs.rest.title"] = "API REST Cephalon"
        }
    }));
```

That gives Cephalon a predictable precedence chain:

1. built-in engine resources
2. module/package language packs
3. `Engine:Localization` configuration
4. `engine.AddLanguageResources(...)`
5. project-owned DI replacement of `LocalizationSettings` or `ILocalizedTextCatalog`

`/engine` continues to expose manifest v2 data, and `/engine/manifest` is now the explicit alias for that contract. The manifest includes the schema version, engine version, per-module version and metadata, capability source-module mapping, and explicit package load metadata for modules that came from assembly paths, manifest files, or configured package directories. When operators need that manifest plus the current runtime status, active execution-graph and hosted-execution catalogs, and active technology-pack surfaces in a single payload, `GET /engine/snapshot` and `IRuntimeIntrospectionSnapshotProvider` are now the preferred integration point.

When operators need the shorter answer to “what loaded, what started, what failed, and why?”, `GET /engine/runtime-story` now exposes loaded packages, per-execution-graph, per-hosted-execution, and per-module lifecycle state, plus an ordered runtime timeline in one host-agnostic contract that also folds into `GET /engine/snapshot`.

`Engine:PackagePolicy` is now the baseline governance surface for package metadata and discovery rules. It can disallow raw DLL-path package loads and require `version`, engine compatibility fields, target framework declarations, publisher ids, signer fingerprints, or `integrity.sha256` before a package is allowed to load.

`Engine:Trust` is now the baseline governance surface for package and capability policy. It can require explicit trust for package-loaded assemblies, declare trusted package, assembly, publisher, signer-fingerprint, or checksum allow-lists, and override capability access per capability key.

Blueprint selection now also materializes into a first-class scaffold plan. `AppProfile.Scaffold` and `/engine/scaffold` expose the intended solution shape for the chosen blueprint, including project templates, folder conventions, and host package hints. Transport selection enriches that plan with adapter package guidance such as `Cephalon.AspNetCore.JsonRpc` or `Cephalon.AspNetCore.Grpc` when those transports are enabled.

For solution-level work, `Cephalon.Abstractions` now also defines `SuiteBlueprint`, `SuiteScaffoldPlan`, and `SuiteScaffoldService`, and `Cephalon.Engine` now ships a built-in `MicroserviceSuite` blueprint that composes repeatable service slots from the existing `Microservice` scaffold contract plus an explicit shared-foundation project. That keeps the suite shape explicit and reusable without overloading the single-app `ScaffoldPlan` contract or inventing a second service-level project model.

Cephalon now also models `Technology` as a first-class app-model layer. This is where future-facing workload choices such as `AgenticWorkloads`, `EventDrivenIntegration`, `KnowledgeRetrieval`, `RealtimeExperience`, or `EdgeNativeDelivery` belong. They are intentionally separate from architecture patterns and transports so new technology stacks can be composed without bending the meaning of blueprint or pattern selection. The active technology set is visible through `AppProfile.Technologies` and `GET /engine/technologies`.

`Cephalon.Scaffolding` now consumes that same scaffold plan and can render a concrete solution skeleton, including `.slnx`, project files, appsettings, module stubs, and placeholder feature folders. This keeps future CLI/template work anchored to the same runtime contract instead of duplicating blueprint logic elsewhere.

`Cephalon.Cli` now exposes that generator as a user-facing command:

```powershell
dotnet run --project src/Cephalon.Cli -- new Acme.Store `
  --blueprint Microservice `
  --module Orders `
  --feature Checkout `
  --transport Grpc `
  --transport JsonRpc `
  --output D:\Apps\Acme.Store
```

`Cephalon.TemplatePack` is now the matching `dotnet new` surface for the same shipped blueprint family. The baseline templates currently cover:

- `cephalon-monolith`
- `cephalon-slice`
- `cephalon-microservice`
- `cephalon-module`
- `cephalon-rest-module`
- `cephalon-rest-behavior-module`

Local install flow:

```powershell
dotnet pack templates/Cephalon.TemplatePack/Cephalon.TemplatePack.csproj -c Release -o artifacts/template-pack
dotnet new install .\artifacts\template-pack\Cephalon.TemplatePack.0.1.0-preview.nupkg
dotnet new cephalon-monolith -n Acme.Store
```

The template pack is intentionally lighter than `Cephalon.Cli`. Use the templates for a fast blueprint starter, and use the CLI when you want richer blueprint, module, feature, pattern, and transport composition from one command.

When version, target-framework, or starter-contract expectations change, keep the template pack aligned with `Cephalon.Cli`, `Cephalon.Scaffolding`, and the manifest rules documented in `docs/compatibility.md`.

Cephalon now also ships a first-class module authoring baseline. `dotnet new cephalon-module` gives a host-agnostic package starter, `dotnet new cephalon-rest-module` gives a generic REST-ready module package starter, `dotnet new cephalon-rest-behavior-module` gives a behavior-backed REST module starter built on `RestBehaviorModuleBase`, and `docs/module-authoring.md` documents the recommended authoring flow. Those starters now emit `cephalon.package.json` and copy it to the build output by default so package discovery works without extra manual setup. The shipped `cephalon-monolith`, `cephalon-slice`, and `cephalon-microservice` starters, together with the matching blueprint samples, now also treat `RestBehaviorModuleBase.ConfigureRestBehaviors(...)` plus `MapProfile<TBehavior>()` as the default public REST ownership path when `RestApi` is enabled, while `cephalon-rest-module` and `samples/Cephalon.ReferenceModule.Operations` remain the generic non-behavior path.

The runtime now also has a package-loading baseline for those authored modules. Explicit package assembly paths, package manifests, and configured package directories can be loaded into the engine, surfaced through `/engine/packages`, and mapped into host transports the same way as in-repo modules. Scaffolded module projects now emit the same `cephalon.package.json` convention, and `/engine/packages` now shows the package `kind`, resolved assembly `path`, original `sourcePath`, declared `version`, compatibility fields, computed `checksumSha256`, and `trustReason` so operators can tell both where a module came from and why the current trust policy accepted or rejected it.

For REST surfaces, Cephalon now also ships a request-time trust hook through `RequireCapability(...)` in `Cephalon.AspNetCore.Transports.Rest`. That lets modules bind an endpoint to a capability key so denied capabilities are rejected at the HTTP boundary, not only hidden from manifest introspection.

The engine now emits built-in observability signals through the `Cephalon.Engine` meter and activity source. `Cephalon.Observability` adds structured manifest, module, capability, operational-health, telemetry-export, and diagnostics-catalog logs on host startup, driven by `Engine:Observability`, while `Cephalon.Observability.CassandraDependencies` turns Cassandra contact-point endpoints into reusable dependency-health contributions, `Cephalon.Observability.ClickHouseDependencies` turns ClickHouse analytics endpoints into reusable dependency-health contributions, `Cephalon.Observability.ConsulDependencies` turns Consul control-plane endpoints into reusable dependency-health contributions, `Cephalon.Observability.ElasticsearchDependencies` turns Elasticsearch clusters into reusable dependency-health contributions, `Cephalon.Observability.HttpDependencies` turns external HTTP upstreams into reusable dependency-health contributions, `Cephalon.Observability.KafkaDependencies` turns Kafka clusters into reusable dependency-health contributions, `Cephalon.Observability.MemcachedDependencies` turns Memcached cache endpoints into reusable dependency-health contributions, `Cephalon.Observability.MongoDbDependencies` turns MongoDB endpoints into reusable dependency-health contributions, `Cephalon.Observability.MqttDependencies` turns MQTT broker endpoints into reusable dependency-health contributions, `Cephalon.Observability.MySqlDependencies` turns MySQL and MariaDB endpoints into reusable dependency-health contributions, `Cephalon.Observability.NatsDependencies` turns NATS broker endpoints into reusable dependency-health contributions, `Cephalon.Observability.Neo4jDependencies` turns Neo4j graph endpoints into reusable dependency-health contributions, `Cephalon.Observability.OpenSearchDependencies` turns OpenSearch search-cluster endpoints into reusable dependency-health contributions, `Cephalon.Observability.OracleDependencies` turns Oracle Database endpoints into reusable dependency-health contributions, `Cephalon.Observability.PostgresDependencies` turns Postgres endpoints into reusable dependency-health contributions, `Cephalon.Observability.RabbitMqDependencies` turns RabbitMQ broker endpoints into reusable dependency-health contributions, `Cephalon.Observability.RedisDependencies` turns Redis and cache endpoints into reusable dependency-health contributions, `Cephalon.Observability.SqlServerDependencies` turns SQL Server and Azure SQL endpoints into reusable dependency-health contributions, `Cephalon.Observability.OpenTelemetry` gives hosts an optional OTLP export path without pushing exporter dependencies into the engine core, `Cephalon.Observability.AlibabaCloud` gives hosts an optional Alibaba Cloud-hosted OTLP path plus an opt-in managed OpenTelemetry traces/metrics path over that same shared contract, `Cephalon.Observability.Aws` gives hosts an optional AWS-hosted OTLP path with X-Ray-compatible tracing defaults over that same shared contract, `Cephalon.Observability.DigitalOcean` gives hosts an optional DigitalOcean collector-first OTLP path plus hosted runtime defaults over that same shared contract, `Cephalon.Observability.GrafanaCloud` gives hosts an optional Grafana Cloud OTLP direct-endpoint path plus access-policy authentication guidance over that same shared contract, `Cephalon.Observability.Gcp` gives hosts an optional GCP-hosted OTLP path plus an opt-in Google-managed traces/metrics path over that same shared contract, `Cephalon.Observability.HuaweiCloud` gives hosts an optional Huawei Cloud-hosted OTLP path plus an opt-in managed APM trace path over that same shared contract, `Cephalon.Observability.NewRelic` gives hosts an optional New Relic native OTLP path plus `api-key` authentication guidance over that same shared contract, `Cephalon.Observability.OracleCloud` gives hosts an optional Oracle Cloud-hosted OTLP path plus an opt-in Oracle Cloud APM traces/metrics path over that same shared contract, `Cephalon.Observability.Kubernetes` gives hosts an optional platform-neutral Kubernetes collector-first OTLP path plus generic cluster resource defaults over that same shared contract, `Cephalon.Observability.OpenShift` gives hosts an optional OpenShift in-cluster collector path plus hosted cluster defaults over that same shared contract, `Cephalon.Observability.Tanzu` gives hosts an optional Tanzu proxy-focused trace handoff path plus hosted cluster defaults over that same shared contract, `Cephalon.Observability.AzureMonitor` gives hosts an optional Azure Monitor / Application Insights export path over the same shared contract, and `Cephalon.Observability.Serilog` gives hosts an optional Serilog provider path without inventing a Cephalon-specific logging abstraction.

The transport catalog currently models `RestApi`, `GraphQL`, `JsonRpc`, `Grpc`, `ServerSentEvents`, and `WebSocket`. The sample host in this repo currently demonstrates `RestApi`, `GraphQL`, `JsonRpc`, `Grpc`, `ServerSentEvents`, and `WebSocket`.

When `RestApi` is selected on ASP.NET Core, the host exposes OpenAPI at `/openapi/v1.json` and Scalar docs through `/scalar`, which redirects to the default canonical document such as `/scalar/v1`. The slash-suffixed Scalar shell at `/scalar/` still stays available for multi-document flows, and Cephalon's Scalar JavaScript normalizes hash-based document selections such as `/scalar/#v1/` back into canonical versioned links. Cephalon also serves its Scalar JavaScript configuration from `/scalar/openapi-toggle.js` and its docs favicon from `/scalar/assets/favicon.svg`, both with cache-busting references and no-store headers so docs assets stay aligned after upgrades. Non-REST protocol endpoints stay out of that REST-facing API description surface.

`GET /engine/localization?culture=th` exposes the effective language resources the runtime resolved for a request culture, which makes it easier to verify configuration-driven and project-level overrides without stepping through host internals.

The gRPC adapter now includes unary, server-streaming, and duplex-streaming discovery flows so transport evolution can be exercised beyond request/response only.

For transports that live in adapter packages, configuration is not enough by itself. The host must also register the matching adapter, for example:

```csharp
builder.AddGrpcTransport();
builder.AddJsonRpcTransport();
builder.AddCephalon(engine =>
{
    engine.AddModule(new DiscoveryModule());
});
```

Lifecycle is now host-managed for ASP.NET Core. When the host starts, Cephalon initializes and starts modules automatically through an internal hosted service, and `/engine/status` exposes the current runtime state.

`EngineOptions` is also exposed through `/engine/options`, so module toggles and capability policy are inspectable alongside the manifest.

`Engine:PackagePolicy` is exposed through `/engine/package-policy`, which gives operators the effective package governance rules for raw DLL loads, manifest metadata requirements, provenance requirements, and integrity expectations.

Runtime failure policy is now configuration-driven through `Engine:FailurePolicy`. Startup can either `FailFast` or `CaptureOnly`, stop behavior can either `FailFast` or `BestEffortContinue`, readiness can stay unhealthy for a configured startup warmup window, liveness can stay healthy for a configured shutdown drain window, and explicit `RestartAsync(...)` calls can be delayed by a manual restart backoff window on restartable failures. `/engine/failure-policy` exposes the effective policy, while `/engine/status` now includes restart count, shutdown timing, and the most recent failure context, including when a restart exits backoff.

Operational health is now a first-class host surface too. ASP.NET Core hosts expose `/health`, `/health/live`, and `/health/ready` with JSON payloads backed by the runtime state machine, while `/engine/diagnostics` exposes the engine meter, activity source, counter names, the published package-level diagnostics conventions and event-id catalog, and the current liveness/readiness reports in one place. That diagnostics surface now also publishes execution-graph and hosted-execution lifecycle transitions through the shared `Cephalon.Engine` event-id catalog plus `cephalon.execution-graphs.transitions` and `cephalon.hosted-executions.transitions`. Those health reports now also surface active lifecycle windows such as startup warmup, shutdown drain, and restart backoff so operators can see why a probe is still holding traffic or restart eligibility. The companion `GET /engine/runtime-story` route answers the adjacent operator question of what actually loaded, started, failed, and why, without forcing callers to join manifest, status, and failure payloads by hand.

Modules and installed packages can now also contribute dependency health details through `IDependencyHealthContributor`. That keeps dependency-specific health checks host-agnostic, exposes them through `/engine/dependencies`, and folds them into `/health/live`, `/health/ready`, and `/engine/diagnostics` without hardwiring database or infrastructure assumptions into the engine itself. Hosts that want supported provider-specific probes can pair that contract with `Cephalon.Observability.CassandraDependencies` for Cassandra clusters, `Cephalon.Observability.ClickHouseDependencies` for ClickHouse analytics databases, `Cephalon.Observability.ConsulDependencies` for Consul control planes, `Cephalon.Observability.ElasticsearchDependencies` for Elasticsearch clusters, `Cephalon.Observability.HttpDependencies` for external APIs, `Cephalon.Observability.KafkaDependencies` for Kafka clusters, `Cephalon.Observability.MemcachedDependencies` for Memcached cache endpoints, `Cephalon.Observability.MongoDbDependencies` for MongoDB databases, `Cephalon.Observability.MqttDependencies` for MQTT brokers, `Cephalon.Observability.MySqlDependencies` for MySQL and MariaDB databases, `Cephalon.Observability.NatsDependencies` for NATS brokers, `Cephalon.Observability.Neo4jDependencies` for Neo4j graph databases, `Cephalon.Observability.OpenSearchDependencies` for OpenSearch clusters, `Cephalon.Observability.OracleDependencies` for Oracle databases, `Cephalon.Observability.PostgresDependencies` for Postgres databases, `Cephalon.Observability.RabbitMqDependencies` for RabbitMQ brokers, `Cephalon.Observability.RedisDependencies` for Redis and cache endpoints, or `Cephalon.Observability.SqlServerDependencies` for SQL Server and Azure SQL endpoints.

`Engine:Observability:Telemetry` is now the shared export contract for operators. It still keeps exporter dependencies out of the engine itself, but hosts can now pair it with `Cephalon.Observability.OpenTelemetry` to turn that same provider, protocol, endpoint, and signal-selection contract into a supported OTLP integration path. Self-hosted deployments can opt into `UseSelfHostedDefaults` and omit `Endpoint`, which falls back to the standard local collector ports (`http://localhost:4317` for `otlp` / `otlp/grpc`, `http://localhost:4318` for `otlp/http`) while the host stamps `deployment.environment.name` from the active environment. Hosts that need a platform-neutral Kubernetes collector-first path can instead pair the same shared telemetry contract with `Cephalon.Observability.Kubernetes` and configure `Engine:Observability:Telemetry:Kubernetes` for cluster name, namespace, pod/node/container resource defaults, service-DNS suffix overrides, in-cluster collector service discovery, optional OTLP headers, and optional HTTPS CA-bundle trust for traces and metrics. Hosts that need Alibaba Cloud-hosted OTLP defaults or an opt-in managed OpenTelemetry traces/metrics path can instead pair the same shared telemetry contract with `Cephalon.Observability.AlibabaCloud` and configure `Engine:Observability:Telemetry:AlibabaCloud` for hosted platform selection, region defaults, and direct managed ingestion when no shared endpoint is configured. Hosts that need AWS-hosted OTLP defaults can instead pair the same shared telemetry contract with `Cephalon.Observability.Aws` and configure `Engine:Observability:Telemetry:Aws` for hosted platform selection, X-Ray-compatible trace IDs, X-Ray propagation, and AWS SDK instrumentation. Hosts that need DigitalOcean collector defaults, best-effort Droplet metadata, or DOKS in-cluster collector wiring can instead pair the same shared telemetry contract with `Cephalon.Observability.DigitalOcean` and configure `Engine:Observability:Telemetry:DigitalOcean` for hosted platform selection, region defaults, Droplet metadata, App Platform bindings, or in-cluster collector service discovery when no shared endpoint is configured. Hosts that need Grafana Cloud direct OTLP endpoint wiring can instead pair the same shared telemetry contract with `Cephalon.Observability.GrafanaCloud` and configure `Engine:Observability:Telemetry:GrafanaCloud` for the Grafana Cloud OTLP endpoint, either raw OTLP headers or the structured `InstanceId` plus `AccessPolicyToken` pair, and optional `ServiceNamespace` stamping when no shared endpoint is configured. Hosts that need GCP-hosted OTLP defaults or an opt-in Google-managed traces/metrics path can instead pair the same shared telemetry contract with `Cephalon.Observability.Gcp` and configure `Engine:Observability:Telemetry:Gcp` for hosted platform selection, location defaults, and Google-managed ingestion when no shared endpoint is configured. Hosts that need Huawei Cloud-hosted OTLP defaults or an opt-in managed APM trace path can instead pair the same shared telemetry contract with `Cephalon.Observability.HuaweiCloud` and configure `Engine:Observability:Telemetry:HuaweiCloud` for hosted platform selection, region defaults, and direct APM trace ingestion when no shared endpoint is configured. Hosts that need New Relic native OTLP endpoint wiring can instead pair the same shared telemetry contract with `Cephalon.Observability.NewRelic` and configure `Engine:Observability:Telemetry:NewRelic` for a regional New Relic OTLP endpoint, either raw OTLP headers or the structured `LicenseKey` that the package converts into the required `api-key` header, and optional `ServiceNamespace` stamping when no shared endpoint is configured. Hosts that need Oracle Cloud APM-hosted OTLP defaults or an opt-in managed traces/metrics path can instead pair the same shared telemetry contract with `Cephalon.Observability.OracleCloud` and configure `Engine:Observability:Telemetry:OracleCloud` for hosted platform selection, region defaults, the APM `DataUploadEndpoint`, public versus private trace-key selection, and the signal-specific data keys required when no shared endpoint is configured. Hosts that need OpenShift in-cluster collector wiring or hosted cluster defaults can instead pair the same shared telemetry contract with `Cephalon.Observability.OpenShift` and configure `Engine:Observability:Telemetry:OpenShift` for cluster name, namespace, in-cluster collector service discovery, optional OTLP headers, and optional HTTPS CA-bundle trust for traces and metrics. Hosts that need VMware Tanzu hosted defaults or proxy-focused trace handoff can instead pair the same shared telemetry contract with `Cephalon.Observability.Tanzu` and configure `Engine:Observability:Telemetry:Tanzu` for cluster name, namespace, explicit proxy service discovery, optional proxy base paths, and optional HTTPS CA-bundle trust while keeping logs and metrics on the shared collector path when the Tanzu proxy handoff mode is used. Hosts that need Azure Monitor / Application Insights export can instead pair the same shared telemetry contract with `Cephalon.Observability.AzureMonitor` and configure `Engine:Observability:Telemetry:AzureMonitor` for the connection string, optional `DefaultAzureCredential` auth, and hosted Azure resource defaults. The same shared contract is intentionally reusable by downstream companion packages too, so teams can ship additional Cloudflare or internal-provider integrations without modifying `Cephalon.Engine`, `Cephalon.Abstractions`, or the shared `ILogger` pipeline. See `docs/observability-provider-authoring.md` for the recommended downstream package shape and the current Cloudflare-specific guidance. Hosts that want richer sink, enricher, or formatting behavior over the same shared `ILogger` pipeline can pair the runtime with `Cephalon.Observability.Serilog` and the standard top-level `Serilog` section instead of introducing a new Cephalon logging layer.

The same runtime now also runs under the generic host through `Cephalon.Worker`. The worker playground uses configuration-driven assembly discovery, module lifecycle hooks, and a background heartbeat service to prove the engine can operate cleanly outside HTTP hosts.

`Cephalon.Benchmarks` gives the repo a first-class performance regression suite over engine composition, runtime lifecycle, and scaffold generation so we can evolve the framework without guessing about cost. The suite now also carries a committed guardrail catalog plus a validation command, so release checks can assert the current hot-path baselines intentionally instead of relying on ad-hoc benchmark runs.

GitHub Actions now runs the same repo-native release validation flow through `.github/workflows/release-validation.yml`, calling `scripts/validate-release.ps1` on both `windows-latest` and `ubuntu-latest`. The Windows leg keeps the full benchmark smoke and guardrail path, while the Ubuntu leg currently uses `-SkipBenchmarks` until the benchmark baseline is made OS-neutral. Local and CI validation still share the same script entry point.

The repository now also carries a first-class sample suite in `samples/`:

- `Cephalon.ReferenceModule.Operations`
- `Cephalon.Sample.ModularMonolith`
- `Cephalon.Sample.ModularVerticalSlice`
- `Cephalon.Sample.Microservice`

These are intentionally different from `playground/`. The playground stays a freeform sandbox, while the sample apps and reference packages show the blueprint shapes and module-package patterns we expect external teams to copy.

## Planning

- `docs/engine-roadmap.md`
- `docs/engine-backlog.md`
- `docs/app-models.md`
- `docs/module-authoring.md`
- `docs/runtime-failure-policy.md`
- `docs/operations.md`
- `docs/benchmarking.md`
- `docs/cephalon-engine-roadmap.drawio`
- `docs/cephalon-app-models.drawio`
