# Cephalon Module Authoring

Cephalon now ships a first-class module authoring path alongside app scaffolding.

## Starting points

Choose the starter that matches the package you want to author:

- `dotnet new cephalon-module`
  - host-agnostic module package
  - capability registration
  - lifecycle hooks
  - package-owned localization resources
  - generated `cephalon.package.json` copied to the output folder
- `dotnet new cephalon-rest-module`
  - everything in `cephalon-module`
  - plus `IRestModule` and a localized REST endpoint
  - best for generic REST modules that do not dispatch into Cephalon behaviors
  - generated `cephalon.package.json` copied to the output folder

For behavior-owning modules, add `Cephalon.Behaviors` or `Cephalon.Behaviors.Http` and prefer
`BehaviorModuleBase` or `RestBehaviorModuleBase` instead of implementing
`IBehaviorOwnerModule`/`IRestModule` directly in normal authoring code.

For a concrete reference implementation, use:

- `samples/Cephalon.ReferenceModule.Operations`

## Recommended package shape

Keep module packages small and explicit:

- `Application/`
  - package-owned services and state
- `Contracts/`
  - payloads or envelopes exposed by the package
- `Registration/`
  - the module entry point and transport contribution interfaces
- `cephalon.package.json`
  - package manifest used by `Engine:Discovery:Packages:ManifestPath` and `Engine:Discovery:PackageDirectories`
  - version, compatibility, and optional integrity metadata for independently shipped modules

That keeps the authoring path close to the same module-first ideas used by Cephalon apps.

## Authoring checklist

1. Define a `ModuleDescriptor` with a stable id, display name, description, tags, and version.
2. Register package-owned services in `ConfigureServices(...)`.
3. Register explicit capabilities in `RegisterCapabilities(...)`.
4. Implement lifecycle hooks only when the package owns startup/runtime behavior.
5. Use `ILocalizedResourceContributor` for package-owned text instead of hardcoding strings in hosts.
6. Use `ITechnologyContributor` when the package introduces a future-tech profile, workload convention, or package hint that the host should be able to select through `Engine:Technologies`.
7. Use `ITechnologyServiceContributor` or `ITechnologyCapabilityContributor` when package services or capabilities should only activate for specific technology profiles.
8. If the package extends a shipped technology pack, register the pack-specific contributor service in `ConfigureServices(...)` such as `IAgentToolContributor`, `IKnowledgeCollectionContributor`, `IEventChannelContributor`, or `IEdgeNodeContributor`.
9. Use `ITechnologyRuntimeContributor` when the package or pack needs to expose an operator-facing runtime snapshot through `/engine/technology-surfaces`.
10. Use `IExecutionGraphContributor` when the package needs to publish operator-facing workflow or execution-graph descriptors through `/engine/execution-graphs` and `/engine/snapshot`.
11. Use `IHostedExecutionContributor` when the package needs to publish operator-facing hosted or background execution descriptors through `/engine/hosted-executions`, `/engine/runtime-story`, and `/engine/snapshot`.
12. Use `IProjectionContributor` when the package needs to publish operator-facing projection descriptors through `/engine/projections` and `/engine/snapshot`.
13. Use `IInboxContributor` when the package needs to publish operator-facing inbox descriptors through `/engine/inboxes` and `/engine/snapshot`.
14. Use `IOutboxContributor` when the package needs to publish operator-facing outbox descriptors through `/engine/outboxes` and `/engine/snapshot`.
15. Use `IAuthorizationPolicyContributor` when the package needs to publish operator-facing authorization-policy descriptors through `/engine/authorization-policies` and `/engine/snapshot`.
16. Add transport contribution interfaces only when the package really owns an external surface.
17. When a module explicitly owns Cephalon behaviors, prefer `BehaviorModuleBase` so ownership stays
    host-agnostic and deterministic.
18. When a behavior-owning module exposes REST endpoints, prefer `RestBehaviorModuleBase` so the same
    module can own internal-only behaviors and public REST-backed behaviors without splitting the
    bounded context across multiple module classes.
19. When a module exposes REST endpoints backed by behaviors, map that REST surface in
    `MapEndpoints(...)` with `MapBehaviorRestGroup(...)` and keep REST out of behavior topology.

## Behavior-owning modules

Cephalon now has a first-class module-owned behavior authoring model:

- `BehaviorModuleBase` keeps behavior ownership host-agnostic through `ConfigureBehaviors(...)`
- `RestBehaviorModuleBase` layers public REST mapping on top of that same ownership contract
- one module can own both internal/process-only behaviors and public REST-backed behaviors
- generic `IRestModule` remains the right fit for REST modules that do not dispatch into Cephalon
  behaviors

Use `BehaviorModuleBase` when the module owns behaviors but does not need to expose a public REST
surface:

```csharp
public sealed class CartModule : BehaviorModuleBase
{
    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void ConfigureBehaviors(IBehaviorModuleBuilder behaviors)
    {
        behaviors.Add<RepriceCartBehavior>();
        behaviors.Add<CheckoutWorkflowBehavior>(topology => topology
            .AsProcessManager()
            .ViaKafka());
    }
}
```

That shape keeps ownership explicit even when the behaviors only run through messaging, generic HTTP
transports, or background orchestration.

## Behavior-first REST authoring

Modules that expose Cephalon behaviors over REST can keep one module as the single owner of the
bounded context. `RestBehaviorModuleBase` keeps behavior ownership and REST exposure together while
still separating host-agnostic behavior registration from ASP.NET Core route mapping.

Behavior declaration stays focused on the interaction pattern plus non-REST transports:

```csharp
[AppBehavior("cart.add-item")]
[BehaviorAllowedPatterns("cqrs")]
[BehaviorAllowedTransports("http.ws", "http.graphql", "http.sse")]
public sealed class AddToCartBehavior : IAppBehavior<AddToCartInput, AddToCartOutput>
{
    public Task<AddToCartOutput> HandleAsync(
        AddToCartInput input,
        IBehaviorContext context,
        CancellationToken cancellationToken = default)
        => throw new NotImplementedException();
}
```

That shape gives the runtime an attribute-only baseline: the single allowed pattern (`cqrs`) plus
the declared transports become the resolved behavior topology when no explicit topology override
exists. Public REST is not part of that baseline; modules own REST explicitly.

If a behavior declares multiple allowed patterns, keep the attributes as an allowlist and add
`ConfigureTopology(...)` or fluent registration so the runtime does not need to guess which pattern
should execute. For authoring convenience, `[BehaviorAllowedTransports("http.grpc")]` is accepted
and normalized to canonical `grpc`.

If the generic route-shaped transports should expose a different logical public path than the default
`behavior-id -> group/operation` split, override it in the same topology declaration:

```csharp
public static void ConfigureTopology(IBehaviorTopologyBuilder builder)
{
    builder.AsCqrs()
        .ViaHttpJsonRpc()
        .ViaHttpGraphQl()
        .ViaHttpSse()
        .ViaWebSocket()
        .WithApiSurface("cart", "get");
}
```

That shared API surface feeds the generic JSON-RPC, GraphQL, GraphQL-SSE, GraphQL-WS, SSE, and
WebSocket behavior bindings, so they can all project canonical versioned routes such as
`/json-rpc/v1/cart/get`, `/graphql/v1/cart/get`, `/graphql-sse/v1/cart/get`,
`/graphql-ws/v1/cart/get`, `/sse/v1/cart/get`, and `/ws/v1/cart/get`. Hosts can move those
canonical prefixes with `ApiRoutes:Prefixes:GraphQL`, `ApiRoutes:Prefixes:JsonRpc`,
`ApiRoutes:Prefixes:Sse`, `ApiRoutes:Prefixes:Ws`, `ApiRoutes:Prefixes:GraphQLWs`, and
`ApiRoutes:Prefixes:GraphQLSse`.

The owning module then keeps both ownership and public REST mapping together:

```csharp
public sealed class CartModule : RestBehaviorModuleBase
{
    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void ConfigureBehaviors(IBehaviorModuleBuilder behaviors)
    {
        behaviors.Add<GetCartBehavior>();
        behaviors.Add<AddToCartBehavior>();
        behaviors.Add<RemoveFromCartBehavior>();
        behaviors.Add<CheckoutCartBehavior>();
        behaviors.Add<RepriceCartBehavior>(); // internal-only
    }

    public override void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapBehaviorRestGroup(this, "/showcase/cart");

        group.MapBehaviorGet<GetCartBehavior>("/{cartId}");
        group.MapBehaviorPost<AddToCartBehavior>("/{cartId}/items");
        group.MapBehaviorDelete<RemoveFromCartBehavior>("/{cartId}/items/{productId}");
        group.MapBehaviorPost<CheckoutCartBehavior>("/{cartId}/checkout");
    }
}
```

Current helper behavior:

- gives behavior authors a base class instead of forcing modules to implement multiple interfaces
  directly
- keeps one module as the owner of both internal-only and REST-exposed behaviors
- validates that a module cannot map another module's explicitly owned behavior through the REST helper layer
- keeps route shape in the ASP.NET Core adapter layer while behavior attributes remain host-agnostic
- dispatches through `BehaviorDispatcher` and `DefaultBehaviorContext`
- merges route values, query-string values, and JSON request bodies into the behavior input payload
- uses the module display name for OpenAPI tags
- lets the module override the published tag name and tag description through `.WithTagName(...)` and `.WithTagDescription(...)`
- defaults the tag description from the module XML `<summary>` plus `<remarks>` when XML docs exist, falling back to `ModuleDescriptor.Description`
- defaults newly mapped endpoints to the owning module descriptor major version when one is available, so a module declared as `1.0.0` automatically joins the `v1` document and gets a `/v1` route prefix without extra code
- keeps `.ApiVersion(major)` as the explicit override when the public API version should differ from the module package major
- prefixes the mapped REST route group with `/v{major}` for the resolved API major version, so ASP.NET Core hosts expose routes such as `/api/v1/showcase/cart/{cartId}`
- uses the resolved API major version as the operation-name version segment, falling back to the owning module descriptor major version
- flows XML comments from the module and behavior assemblies into ASP.NET Core OpenAPI metadata when XML docs are available
- maps behavior `<summary>` to the operation header and behavior `<remarks>` to the operation description so Scalar/OpenAPI content stays non-duplicated

When a host needs more than the default `v1` document, prefer `OpenApi:EnabledVersions` plus `OpenApi:DefaultVersion` so endpoints mapped with `.ApiVersion(2)` or higher, or defaulted from module version `2.x`, have a matching OpenAPI/Scalar surface. In that shape, `/scalar` redirects to the default canonical document such as `/scalar/v2`, `/scalar/` remains available for multi-document selection, and Cephalon normalizes hash-based Scalar selections such as `/scalar/#v2/` back into pinned versioned links. Hosts can also move the docs and REST entry points with `OpenApi:RoutePattern`, `OpenApi:Scalar:RoutePrefix`, and the canonical `ApiRoutes:Prefixes:*` settings. Legacy `OpenApi:Documents` and `OpenApi:DefaultDocument` settings remain available when a host deliberately wants custom named documents instead of `v{major}` API-version documents.

This helper surface is REST-specific. The generic route-shaped behavior transports already share the
`BehaviorApiSurfaceDescriptor` contract for JSON-RPC, GraphQL, GraphQL-SSE, GraphQL-WS, SSE, and
WebSocket routes. Module-owned REST helpers are the right choice when the module needs Minimal API
method selection, concrete REST templates, and OpenAPI metadata that read like a normal application
API, while `BehaviorModuleBase` remains the host-agnostic choice for internal or non-REST behavior
ownership.

## Workflow and orchestration descriptors

Packages that need to describe an execution flow can implement `IExecutionGraphContributor` and publish one or more `ExecutionGraphDescriptor` entries.
Packages that need to describe operator-facing host-managed background work can also implement `IHostedExecutionContributor` and publish one or more `HostedExecutionDescriptor` entries.
Packages that extend `Cephalon.Agentics` can keep agent tools grounded in those same runtime contracts by declaring `capabilityKeys`, `executionGraphId`, or `hostedExecutionId` on `AgentToolDescriptor` instead of inventing a separate AI-specific orchestration registry.

Current baseline behavior:

- execution graphs are discovered only from active modules, so they stay additive to the existing module model
- graph nodes can point back to module ids and capability keys instead of inventing a parallel ownership model
- `/engine/execution-graphs` exposes the standalone catalog, and `/engine/snapshot` carries the same graph descriptors alongside status, diagnostics, and lifecycle data
- `/engine/runtime-story` now carries the operator-facing lifecycle state for each execution graph, including load, activate, and deactivate timestamps
- `/engine/hosted-executions` exposes the hosted/background catalog, `/engine/snapshot` carries the same hosted descriptors, and `/engine/runtime-story` now carries hosted-execution load, activate, and deactivate timestamps
- hosted executions can link back to one execution graph through `executionGraphId`, but they stay descriptive and operator-facing instead of introducing a separate Cephalon runner abstraction
- hosted executions can also declare `metadata.eventSubscriptionId` or `metadata.eventSubscriptionIds` when they own the application-managed execution path for declared event subscriptions, which lets `Cephalon.Eventing` project truthful linkage without inventing a second runtime registry; if the hosted execution also points at an `executionGraphId`, the eventing runtime surface can surface that orchestration link too
- modules that own application-managed event handling can also inject `IEventSubscriptionRuntimeReporter` and report `started`, `succeeded`, `failed`, `retry-scheduled`, or `skipped` observations plus operator-facing metadata such as retry windows or backoff hints so `Cephalon.Eventing` can project truthful live runtime state without claiming a pack-owned bus runner
- modules or adapter packages that own application-managed publication dispatch can inject `IEventDispatchRuntimeReporter` and report `started`, `succeeded`, `failed`, `retry-scheduled`, or `skipped` observations plus operator-facing metadata such as `outboxId`, `channelId`, retry windows, or backoff hints so `Cephalon.Eventing` can project truthful dispatch runtime state without claiming a broker-owned dispatch runtime
- `Cephalon.Agentics` can now project agent-tool links to capability keys, execution graphs, and hosted executions through `/engine/technology-surfaces` and `/engine/snapshot`
- invalid agent-tool references to unknown capability keys, execution graphs, or hosted executions now fail when the agentic runtime catalog is resolved
- the engine validates hosted-execution ids, source modules, and referenced execution graphs at build time so invalid hosted descriptors fail fast
- the engine validates graph ids, entry nodes, edges, referenced modules, and referenced capability keys at build time so invalid descriptors fail fast

## Data and authorization descriptors

Packages that need to publish read-model or projection shape can implement `IProjectionContributor` and register one or more `ProjectionDescriptor` entries.
Packages that need to publish durable processed-message or idempotency-store shape can implement `IInboxContributor` and register one or more `InboxDescriptor` entries.
Packages that need to publish durable outbound message staging shape can implement `IOutboxContributor` and register one or more `OutboxDescriptor` entries.
Packages that need to publish operator-facing authorization choices can implement `IAuthorizationPolicyContributor` and register one or more `AuthorizationPolicyDescriptor` entries.

Current baseline behavior:

- `/engine/projections` exposes the merged projection catalog, and `/engine/snapshot` carries the same projection descriptors alongside manifest, diagnostics, and lifecycle data
- `/engine/inboxes` exposes the merged inbox catalog, and `/engine/snapshot` carries the same inbox descriptors alongside manifest, diagnostics, and lifecycle data
- `/engine/outboxes` exposes the merged outbox catalog, and `/engine/snapshot` carries the same outbox descriptors alongside manifest, diagnostics, and lifecycle data
- `/engine/authorization-policies` exposes the merged authorization-policy catalog, and `/engine/snapshot` carries the same policy descriptors in the broader runtime answer
- projection descriptors stay grounded in module ownership through `sourceModuleId`, target store ids, and optional source contract metadata
- inbox descriptors stay grounded in module ownership through `sourceModuleId`, provider, mode, optional channel ids, and operator-facing metadata such as idempotency scope
- outbox descriptors stay grounded in module ownership through `sourceModuleId`, provider, mode, optional channel ids, and operator-facing metadata such as dispatch ownership
- authorization-policy descriptors stay host-agnostic and can publish supported `RBAC`, `ABAC`, and `Policy` modes without leaking ASP.NET Core or identity-provider types into `Cephalon.Abstractions`

## Package manifest contract

`cephalon.package.json` is now the recommended place to describe both where the package assembly lives and what runtime it expects.

Baseline example:

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
  "dependencies": [
    {
      "id": "shared-foundation",
      "minimumVersion": "1.0.0",
      "maximumVersion": "2.0.0"
    }
  ],
  "integrity": {
    "sha256": "sha256:3e5d5b9fd0dfb7c60e441d013d7d2a60f41c7b0a0a4fb2d2ad5a9f88d6e7c123"
  }
}
```

For a single signer, use the legacy `signature` object shown above. When a package needs more than one signer or attestation, it can declare `signatures` instead. The engine supports both shapes and exposes per-signer verification results through runtime introspection.

Current behavior:

- `id` should stay stable across releases so trust policy and operators have a predictable handle
- `version` is surfaced through `/engine/packages` and should match the distributed package version
- `compatibility.minimumEngineVersion` blocks older engine versions from loading the package
- `compatibility.maximumEngineVersion` is optional but useful when a package intentionally caps support
- `compatibility.supportedTargetFrameworks` blocks mismatched runtime target frameworks
- `dependencies` is optional, but when declared each entry should reference the stable `id` of another package and can add `minimumVersion` / `maximumVersion` bounds for versioned package dependencies
- version-bounded dependency entries require the referenced package to declare its own `version`
- `publisher.id` should stay stable across releases if operators or trust policy use publisher-level allow-lists
- `distribution.channel` should stay stable enough to distinguish release lanes such as `stable`, `preview`, or `internal`
- `distribution.manifestUri` and `distribution.packageUri` should point to the externally reachable locations operators actually use once the package leaves the repo
- `provenance.sourceRepository` and `provenance.sourceRevision` should line up with the release source you intend operators to audit
- `provenance.buildUri` and `provenance.statementUri` are optional, but they are the right place to point operators to CI evidence or an attestation document
- `signature.keyId` or `signatures[].keyId` should stay stable across releases if hosts map trusted public keys or trusted signing certificates by signing identity
- `signature.fingerprint` or `signatures[].fingerprint` identifies the signing key and is surfaced through diagnostics and trust snapshots
- `signature.value` or `signatures[].value` is optional until a host requires cryptographic verification, but when present it should be a detached signature over the resolved assembly SHA-256 hash
- hosts that want certificate-backed trust can pair `Engine:Trust:TrustedSignatureCertificates` with `Engine:Trust:TrustedSignatureCertificateAuthorities` so package signatures verify against an explicit signing certificate chain instead of only a raw public-key mapping
- `integrity.sha256` is optional, but when present the resolved assembly must match it exactly

For scaffolded modules and `dotnet new` starters, Cephalon now emits this manifest automatically with the current package version and target framework baseline.

## External distribution guidance

When a package is meant to leave the repository, keep the distribution and provenance hints in `cephalon.package.json` truthful enough for operators to answer three questions quickly:

- where should this package be fetched from now
- which source revision produced it
- where is the build or provenance evidence that backs this artifact

Recommended baseline:

- set `distribution.channel` to the release lane you actually publish, such as `stable`, `preview`, or `internal`
- set `distribution.manifestUri` to the externally reachable manifest location when you publish manifests alongside package artifacts
- set `distribution.packageUri` to the archive, NuGet-like feed entry, or artifact page operators use to fetch the package
- set `provenance.sourceRepository` to the canonical source repository URI
- set `provenance.sourceRevision` to a tag, commit SHA, or release ref that unambiguously identifies the shipped source
- set `provenance.buildUri` and `provenance.statementUri` when you have CI/build evidence or a provenance/attestation document to point at

## Compatibility checklist

Keep authored packages aligned with the broader Cephalon compatibility contract:

- `version` and `compatibility.minimumEngineVersion` should reflect the Cephalon package version you intend to support
- `compatibility.supportedTargetFrameworks` should reflect the actual target framework of the compiled module assembly
- if you intentionally cap support, set `compatibility.maximumEngineVersion` explicitly instead of relying on undocumented assumptions
- when you change package version or target framework expectations, update the project file, `cephalon.package.json`, packaging docs, and any starter/template copies together
- use `docs/compatibility.md` as the repository-wide matrix for what else must stay aligned across scaffolding, CLI, templates, and docs

## Staging a published package

Published `.nupkg` files keep their manifest under package content and their runtime assembly under `lib/<tfm>`, so stage them into a loadable directory before pointing host discovery at them:

```powershell
cephalon package stage `
  --package ./artifacts/reference-packages/Cephalon.ReferenceModule.Operations.1.0.0.nupkg `
  --output ./plugins/reference-operations
```

That staged directory becomes the path you feed into `Engine:Discovery:PackageDirectories` or `Engine:Discovery:Packages:ManifestPath`.

For the full publish -> trust -> load -> inspect walkthrough, see [External package lifecycle](external-package-lifecycle.md).

## Loading a package

Package-path discovery:

```json
{
  "Engine": {
    "Discovery": {
      "Packages": [
        {
          "Id": "operations",
          "Path": "plugins/Cephalon.ReferenceModule.Operations.dll"
        }
      ]
    },
    "Transports": [ "RestApi" ]
  }
}
```

Package-manifest discovery:

```json
{
  "Engine": {
    "Discovery": {
      "Packages": [
        {
          "ManifestPath": "plugins/reference-operations/cephalon.package.json"
        }
      ]
    },
    "Transports": [ "RestApi" ]
  }
}
```

Package-directory discovery:

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
    },
    "Transports": [ "RestApi" ]
  }
}
```

Assembly-name discovery is still available when the module assembly is already part of the app load context:

```json
{
  "Engine": {
    "Discovery": {
      "Assemblies": [ "Cephalon.ReferenceModule.Operations" ]
    },
    "Transports": [ "RestApi" ]
  }
}
```

Code-driven package path:

```csharp
builder.AddCephalon(engine =>
{
    engine.AddPackageAssembly("plugins/Cephalon.ReferenceModule.Operations.dll", id: "operations");
    engine.AddPackageManifest("plugins/reference-operations/cephalon.package.json");
    engine.AddPackageDirectory("plugins");
});
```

Manual module registration path:

```csharp
builder.AddCephalon(engine =>
{
    engine.AddModule(new OperationsModule());
});
```

`/engine/packages` exposes the package-loading snapshot the runtime resolved, including the package `kind`, resolved assembly `path`, original `sourcePath`, declared `version`, compatibility fields, declared package `dependencies`, external `distribution` metadata, `provenance` metadata, publisher/signature provenance metadata, the top-level signature summary fields kept for backward compatibility, the per-signer `signatures` collection, per-signer `verificationSource` and `certificateThumbprint` details when certificate-backed trust is used, cryptographic verification status, computed `checksumSha256`, and the current `trustReason`. `/engine/modules` continues to show the active module set after policy and ordering have been applied. `/engine/technology-catalog` shows the technology profiles available after built-in, package, and project contributions have been merged. `/engine/technology-surfaces` shows the active runtime surfaces exposed by installed technology packs after host options and module contributors have both been applied, while `/engine/technology-surfaces/{technologyId}` narrows that view to a single selected technology profile. In code, the same merged surface set is available through `ITechnologyRuntimeCatalog`, and the broader operator-facing runtime snapshot is available through `IRuntimeIntrospectionSnapshotProvider` or `GET /engine/snapshot`.

## What the reference package demonstrates

`Cephalon.ReferenceModule.Operations` shows:

- package-owned service registration
- capability registration visible in `/engine/capabilities`
- lifecycle-driven state transitions
- package localization merged into the runtime catalog
- package-contributed technology profiles through `ITechnologyContributor`
- technology-aware service and capability activation through `ITechnologyServiceContributor` and `ITechnologyCapabilityContributor`
- REST endpoint contribution through `IRestModule`
- a distributable `cephalon.package.json` manifest copied to the package output

That makes it the baseline package to copy when authoring a new module package for Cephalon. The scaffold generator and `dotnet new` starters now emit the same manifest convention automatically.

## Trust and capability policy

Module packages should assume capability access may be governed by `Engine:Trust`.

Current baseline:

- package-loaded assemblies may be required to come from manifest-driven discovery through `Engine:PackagePolicy`
- package manifests may be required to declare `version`, compatibility fields, publisher ids, signer fingerprints, signature key ids, signature values, cryptographic signature verification, or `integrity.sha256` through `Engine:PackagePolicy`
- package-loaded assemblies can be blocked unless the package is trusted
- package trust can come from package id, assembly name, publisher id, signer fingerprint, checksum allow-list, or any declared signature that verifies successfully under the active trust policy
- capability metadata can be allowed, trusted-only, or denied
- REST endpoints can opt into request-time enforcement with `RequireCapability("capability.key")`

For independently distributed packages, trust can combine signing keys and checksums:

```json
{
  "Engine": {
    "Trust": {
      "TrustedPublishers": [ "cephalon-labs" ],
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
      },
      "AllowedPackageChecksums": {
        "operations": [
          "sha256:3e5d5b9fd0dfb7c60e441d013d7d2a60f41c7b0a0a4fb2d2ad5a9f88d6e7c123"
        ]
      }
    }
  }
}
```

That means package authors should give capabilities stable keys and use those keys consistently when binding runtime policies.

## Companion packs vs modules

If a package is mainly about a future-tech workload and provides reusable runtime primitives across many apps, prefer the technology-pack pattern documented in `docs/technology-packs.md`.

Use a module package when the package primarily owns domain behavior.
Use a technology pack when the package primarily owns reusable workload services or capability activation for a technology profile.
If a domain module only needs to add descriptors into an existing technology pack, prefer the pack's contributor services instead of creating a new companion package.
