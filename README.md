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
- observability package with runtime logs, metrics, and tracing conventions
- optional Elasticsearch dependency-health companion package for search-cluster readiness reporting
- optional HTTP dependency-health companion package for external API readiness reporting
- optional Kafka dependency-health companion package for broker metadata readiness reporting
- optional MongoDB dependency-health companion package for document-database readiness reporting
- optional MQTT dependency-health companion package for broker readiness reporting
- optional MySQL dependency-health companion package for MySQL and MariaDB readiness reporting
- optional NATS dependency-health companion package for broker readiness reporting
- optional Postgres dependency-health companion package for database readiness reporting
- optional RabbitMQ dependency-health companion package for broker readiness reporting
- optional Redis dependency-health companion package for cache and Redis readiness reporting
- optional SQL Server dependency-health companion package for SQL Server and Azure SQL readiness reporting
- optional OpenTelemetry exporter companion package for OTLP host wiring
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
- `src/Cephalon.Eventing`: companion package for event-driven integration runtime services
- `src/Cephalon.Worker`: Generic Host worker adapter for non-HTTP hosts
- `src/Cephalon.Observability`: observability package for logs, metrics, and tracing conventions
- `src/Cephalon.Observability.ElasticsearchDependencies`: optional Elasticsearch dependency-health companion package for cluster-health probes
- `src/Cephalon.Observability.HttpDependencies`: optional HTTP dependency-health companion package for external API probes
- `src/Cephalon.Observability.KafkaDependencies`: optional Kafka dependency-health companion package for broker metadata probes
- `src/Cephalon.Observability.MongoDbDependencies`: optional MongoDB dependency-health companion package for document-database probes
- `src/Cephalon.Observability.MqttDependencies`: optional MQTT dependency-health companion package for broker protocol probes
- `src/Cephalon.Observability.MySqlDependencies`: optional MySQL dependency-health companion package for MySQL and MariaDB probes
- `src/Cephalon.Observability.NatsDependencies`: optional NATS dependency-health companion package for broker protocol probes
- `src/Cephalon.Observability.PostgresDependencies`: optional Postgres dependency-health companion package for database probes
- `src/Cephalon.Observability.RabbitMqDependencies`: optional RabbitMQ dependency-health companion package for broker probes
- `src/Cephalon.Observability.RedisDependencies`: optional Redis dependency-health companion package for cache and Redis probes
- `src/Cephalon.Observability.SqlServerDependencies`: optional SQL Server dependency-health companion package for SQL Server and Azure SQL probes
- `src/Cephalon.Observability.OpenTelemetry`: optional OpenTelemetry OTLP exporter companion package for host integration
- `src/Cephalon.Retrieval`: companion package for retrieval/runtime knowledge services
- `src/Cephalon.ReferenceDocs`: optional reference-doc publishing tool that can turn XML comments into browsable API reference output
- `src/Cephalon.Cli`: command-line surface for blueprint generation and reference-doc workflows, with `CliApplication` as the stable entry point
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

- `Configurations/Cors/Local.json`
- `Configurations/Cors/Development.json`
- `Configurations/OpenApi/Local.json`
- `Configurations/OpenApi/Development.json`

This keeps config-driven features such as engine settings, OpenAPI, hosted docs, or future CORS conventions out of one oversized `appsettings.json`. If host code needs those values before `AddCephalon(...)`, call `AddCephalonProjectConfigurations()` first.

## Quick start

```powershell
dotnet build
dotnet test
dotnet run --project src/Cephalon.Cli -- --help
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
.\scripts\validate-release.ps1
.\scripts\publish-reference-docs.ps1
dotnet run --project samples/Cephalon.Sample.ModularMonolith
dotnet run --project samples/Cephalon.Sample.ModularVerticalSlice
dotnet run --project samples/Cephalon.Sample.Microservice
dotnet run --project playground/Cephalon.Playground
dotnet run --project playground/Cephalon.WorkerPlayground
```

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
- `/scalar`
- `/scalar/v1`
- `/scalar/openapi-toggle.js`
- `/scalar/assets/favicon.svg`
- `/api/platform/time`
- `/api/discovery/hello/Codex`
- `/rpc/discovery`
- `/events/discovery/principles`
- `/ws/discovery`

## Design direction

Cephalon is aiming for a future where new capabilities can be delivered as modules instead of rewrites. The engine owns composition, dependency ordering, runtime introspection, and host integration. Product code should be able to plug into that surface without coupling itself to one transport or one monolith.

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
- `signature.keyId`, `signature.fingerprint`, and related signer metadata expose a trustable signing identity for policy and diagnostics
- when `signature.value` is present and `Engine:Trust:TrustedSignaturePublicKeys` resolves a matching trusted public key, the engine performs detached cryptographic signature verification against the resolved assembly SHA-256 hash
- `integrity.sha256` is optional but, when present, must match the resolved assembly exactly
- `Engine:PackagePolicy` can require manifest-driven package loading and specific metadata such as version, engine compatibility, target frameworks, publisher id, signer fingerprint, signature key id, signature value, signature verification, or integrity hashes
- `Engine:Trust` can trust a package by package id, assembly name, cryptographically verified signature, publisher id, signer fingerprint, or checksum allow-list

Current note: the shipped baseline now verifies detached signatures when a package declares `signature.keyId` + `signature.value`, or corresponding entries inside `signatures[]`, and the host configures matching trusted public keys. Packages can declare multiple signers; the runtime surfaces per-signer verification results and accepts the package when at least one required signature verifies under the active policy. Remaining work is the broader distribution story around certificate chains, richer provenance attestations, and external package feeds.

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

When a technology needs reusable runtime behavior, Cephalon now prefers a companion package instead of expanding the engine core directly. The baseline pattern is documented in `docs/technology-packs.md`, with `Cephalon.Agentics`, `Cephalon.Eventing`, `Cephalon.Retrieval`, and `Cephalon.Edge` as the shipped examples. Installed modules can extend those packs through package-specific contributor services such as `IAgentToolContributor`, `IKnowledgeCollectionContributor`, `IEventChannelContributor`, and `IEdgeNodeContributor` instead of forcing hosts to own every descriptor directly. Active pack surfaces can then be inspected through `GET /engine/technology-surfaces`, consumed in code through `ITechnologyRuntimeCatalog`, or folded into one operator-facing payload through `GET /engine/snapshot` and `IRuntimeIntrospectionSnapshotProvider`.

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

`/engine` continues to expose manifest v2 data, and `/engine/manifest` is now the explicit alias for that contract. The manifest includes the schema version, engine version, per-module version and metadata, capability source-module mapping, and explicit package load metadata for modules that came from assembly paths, manifest files, or configured package directories. When operators need that manifest plus the current runtime status and active technology-pack surfaces in a single payload, `GET /engine/snapshot` and `IRuntimeIntrospectionSnapshotProvider` are now the preferred integration point.

When operators need the shorter answer to “what loaded, what started, what failed, and why?”, `GET /engine/runtime-story` now exposes loaded packages, per-module lifecycle state, and an ordered runtime timeline in one host-agnostic contract that also folds into `GET /engine/snapshot`.

`Engine:PackagePolicy` is now the baseline governance surface for package metadata and discovery rules. It can disallow raw DLL-path package loads and require `version`, engine compatibility fields, target framework declarations, publisher ids, signer fingerprints, or `integrity.sha256` before a package is allowed to load.

`Engine:Trust` is now the baseline governance surface for package and capability policy. It can require explicit trust for package-loaded assemblies, declare trusted package, assembly, publisher, signer-fingerprint, or checksum allow-lists, and override capability access per capability key.

Blueprint selection now also materializes into a first-class scaffold plan. `AppProfile.Scaffold` and `/engine/scaffold` expose the intended solution shape for the chosen blueprint, including project templates, folder conventions, and host package hints. Transport selection enriches that plan with adapter package guidance such as `Cephalon.AspNetCore.JsonRpc` or `Cephalon.AspNetCore.Grpc` when those transports are enabled.

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

Local install flow:

```powershell
dotnet pack templates/Cephalon.TemplatePack/Cephalon.TemplatePack.csproj -c Release -o artifacts/template-pack
dotnet new install .\artifacts\template-pack\Cephalon.TemplatePack.0.1.0-preview.nupkg
dotnet new cephalon-monolith -n Acme.Store
```

The template pack is intentionally lighter than `Cephalon.Cli`. Use the templates for a fast blueprint starter, and use the CLI when you want richer blueprint, module, feature, pattern, and transport composition from one command.

When version, target-framework, or starter-contract expectations change, keep the template pack aligned with `Cephalon.Cli`, `Cephalon.Scaffolding`, and the manifest rules documented in `docs/compatibility.md`.

Cephalon now also ships a first-class module authoring baseline. `dotnet new cephalon-module` gives a host-agnostic package starter, `dotnet new cephalon-rest-module` gives a REST-ready module package starter, and `docs/module-authoring.md` documents the recommended authoring flow. Those starters now emit `cephalon.package.json` and copy it to the build output by default so package discovery works without extra manual setup. The concrete reference package lives in `samples/Cephalon.ReferenceModule.Operations`.

The runtime now also has a package-loading baseline for those authored modules. Explicit package assembly paths, package manifests, and configured package directories can be loaded into the engine, surfaced through `/engine/packages`, and mapped into host transports the same way as in-repo modules. Scaffolded module projects now emit the same `cephalon.package.json` convention, and `/engine/packages` now shows the package `kind`, resolved assembly `path`, original `sourcePath`, declared `version`, compatibility fields, computed `checksumSha256`, and `trustReason` so operators can tell both where a module came from and why the current trust policy accepted or rejected it.

For REST surfaces, Cephalon now also ships a request-time trust hook through `RequireCapability(...)` in `Cephalon.AspNetCore.Transports.Rest`. That lets modules bind an endpoint to a capability key so denied capabilities are rejected at the HTTP boundary, not only hidden from manifest introspection.

The engine now emits built-in observability signals through the `Cephalon.Engine` meter and activity source. `Cephalon.Observability` adds structured manifest, module, capability, operational-health, telemetry-export, and diagnostics-catalog logs on host startup, driven by `Engine:Observability`, while `Cephalon.Observability.ElasticsearchDependencies` turns Elasticsearch clusters into reusable dependency-health contributions, `Cephalon.Observability.HttpDependencies` turns external HTTP upstreams into reusable dependency-health contributions, `Cephalon.Observability.KafkaDependencies` turns Kafka clusters into reusable dependency-health contributions, `Cephalon.Observability.MongoDbDependencies` turns MongoDB endpoints into reusable dependency-health contributions, `Cephalon.Observability.MqttDependencies` turns MQTT broker endpoints into reusable dependency-health contributions, `Cephalon.Observability.MySqlDependencies` turns MySQL and MariaDB endpoints into reusable dependency-health contributions, `Cephalon.Observability.NatsDependencies` turns NATS broker endpoints into reusable dependency-health contributions, `Cephalon.Observability.PostgresDependencies` turns Postgres endpoints into reusable dependency-health contributions, `Cephalon.Observability.RabbitMqDependencies` turns RabbitMQ broker endpoints into reusable dependency-health contributions, `Cephalon.Observability.RedisDependencies` turns Redis and cache endpoints into reusable dependency-health contributions, `Cephalon.Observability.SqlServerDependencies` turns SQL Server and Azure SQL endpoints into reusable dependency-health contributions, and `Cephalon.Observability.OpenTelemetry` gives hosts an optional OTLP export path without pushing exporter dependencies into the engine core.

The transport catalog currently models `RestApi`, `GraphQL`, `JsonRpc`, `Grpc`, `ServerSentEvents`, and `WebSocket`. The sample host in this repo currently demonstrates `RestApi`, `GraphQL`, `JsonRpc`, `Grpc`, `ServerSentEvents`, and `WebSocket`.

When `RestApi` is selected on ASP.NET Core, the host exposes OpenAPI at `/openapi/v1.json` and Scalar docs through `/scalar` with the document route at `/scalar/v1`. Cephalon also serves its Scalar JavaScript configuration from `/scalar/openapi-toggle.js` and its docs favicon from `/scalar/assets/favicon.svg`, both with cache-busting references and no-store headers so docs assets stay aligned after upgrades. Non-REST protocol endpoints stay out of that REST-facing API description surface.

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

Operational health is now a first-class host surface too. ASP.NET Core hosts expose `/health`, `/health/live`, and `/health/ready` with JSON payloads backed by the runtime state machine, while `/engine/diagnostics` exposes the engine meter, activity source, counter names, the published package-level diagnostics conventions and event-id catalog, and the current liveness/readiness reports in one place. Those health reports now also surface active lifecycle windows such as startup warmup, shutdown drain, and restart backoff so operators can see why a probe is still holding traffic or restart eligibility. The companion `GET /engine/runtime-story` route answers the adjacent operator question of what actually loaded, started, failed, and why, without forcing callers to join manifest, status, and failure payloads by hand.

Modules and installed packages can now also contribute dependency health details through `IDependencyHealthContributor`. That keeps dependency-specific health checks host-agnostic, exposes them through `/engine/dependencies`, and folds them into `/health/live`, `/health/ready`, and `/engine/diagnostics` without hardwiring database or infrastructure assumptions into the engine itself. Hosts that want supported provider-specific probes can pair that contract with `Cephalon.Observability.ElasticsearchDependencies` for Elasticsearch clusters, `Cephalon.Observability.HttpDependencies` for external APIs, `Cephalon.Observability.KafkaDependencies` for Kafka clusters, `Cephalon.Observability.MongoDbDependencies` for MongoDB databases, `Cephalon.Observability.MqttDependencies` for MQTT brokers, `Cephalon.Observability.MySqlDependencies` for MySQL and MariaDB databases, `Cephalon.Observability.NatsDependencies` for NATS brokers, `Cephalon.Observability.PostgresDependencies` for Postgres databases, `Cephalon.Observability.RabbitMqDependencies` for RabbitMQ brokers, `Cephalon.Observability.RedisDependencies` for Redis and cache endpoints, or `Cephalon.Observability.SqlServerDependencies` for SQL Server and Azure SQL endpoints.

`Engine:Observability:Telemetry` is now the shared export contract for operators. It still keeps exporter dependencies out of the engine itself, but hosts can now pair it with `Cephalon.Observability.OpenTelemetry` to turn that same provider, protocol, endpoint, and signal-selection contract into a supported OTLP integration path.

The same runtime now also runs under the generic host through `Cephalon.Worker`. The worker playground uses configuration-driven assembly discovery, module lifecycle hooks, and a background heartbeat service to prove the engine can operate cleanly outside HTTP hosts.

`Cephalon.Benchmarks` gives the repo a first-class performance regression suite over engine composition, runtime lifecycle, and scaffold generation so we can evolve the framework without guessing about cost. The suite now also carries a committed guardrail catalog plus a validation command, so release checks can assert the current hot-path baselines intentionally instead of relying on ad-hoc benchmark runs.

GitHub Actions now runs the same repo-native release validation flow through `.github/workflows/release-validation.yml`, which calls `scripts/validate-release.ps1` on `windows-latest` and uploads benchmark reports as artifacts. Local and CI validation are intentionally the same path.

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
