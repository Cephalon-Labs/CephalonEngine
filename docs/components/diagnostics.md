# Cephalon.Diagnostics

> **Maturity:** `M4` · **Ownership:** `cephalon-managed` — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.Diagnostics` is the engine-level OpenTelemetry semantic-convention adapter for Cephalon. It ships well-known `ActivitySource` and `Meter` names plus a small set of stable Cephalon-prefix attribute keys that complement OpenTelemetry semantic conventions where conventions exist (HTTP, DB, messaging, RPC, runtime).

## What it owns

- the stable engine-level `ActivitySource` name set published through `CephalonActivitySources`: `Cephalon.Engine`, `Cephalon.AspNetCore`, `Cephalon.Worker`, `Cephalon.Eventing`, `Cephalon.MultiTenancy.Governance`, `Cephalon.Agentics`, `Cephalon.Retrieval`
- the stable engine-level `Meter` name set published through `CephalonMeters` with the same seven names
- a small set of stable Cephalon-prefix attribute keys (`cephalon.module.id`, `cephalon.behavior.id`, `cephalon.cell.id`, `cephalon.app.blueprint`, `cephalon.tenant.id`) for engine concepts that have no OpenTelemetry semantic-convention name
- the redaction filter contract through `IRedactionFilter` and `RedactionContext` so consumer apps and observability companion packs can register synchronous filters that redact secrets, PII, authentication tokens, and other sensitive values before they leave the engine boundary; at M1 maturity eight engine emission sites pipe values through registered filters via the `RedactionPipeline` resolved from DI: `Cephalon.AspNetCore`'s HTTP request/response logging middleware (HTTP request/response span tags), `Cephalon.Engine`'s module-phase runtime activity tags (`runtime.{phase}` and `module.{phase}` spans during initialize/start/stop), `Cephalon.Eventing.Wolverine`'s dispatch-time activity tags (`wolverine.dispatch` spans including `cephalon.tenant_id` / `cephalon.correlation_id` / `cephalon.message_id`), `Cephalon.Agentics`'s in-process tool-dispatch activity tags (`agentics.tool.dispatch` span emitted under `CephalonActivitySources.Agentics`), `Cephalon.Retrieval`'s knowledge-indexing and knowledge-query activity tags (`retrieval.knowledge.index` and `retrieval.knowledge.query` spans emitted under `CephalonActivitySources.Retrieval` — `KnowledgeIndexer` and `KnowledgeQueryEngine` both consume the redaction pipeline before tagging activities), `Cephalon.Worker`'s lifecycle activity tags (`worker.lifecycle.start` and `worker.lifecycle.stop` spans emitted under `CephalonActivitySources.Worker` with `cephalon.lifecycle.phase` / `cephalon.blueprint` / `cephalon.module.count` tags), `Cephalon.Eventing`'s in-process publication-dispatch activity tags (`eventing.publication.dispatch` span emitted under `CephalonActivitySources.Eventing` with `cephalon.eventing.*` tags via the public `EventingDiagnostics` adapter), and `Cephalon.MultiTenancy.Governance`'s invitation delivery dispatch activity tags (`multitenancy.governance.invitation.delivery.dispatch` spans emitted under `CephalonActivitySources.MultiTenancyGovernance` with `cephalon.multitenancy_governance.*` tags via the public `GovernanceDiagnostics` adapter) — consumer-registered filters actually run on attribute values before exporter dispatch
- starter redaction filter implementations under `Cephalon.Diagnostics.Redaction.Defaults` — `KeyMatchRedactionFilter` (redacts values whose `RedactionContext.AttributeKey` matches a banned set, ordinal-case-insensitive) and `RegexRedactionFilter` (replaces regex-matched substrings inside string values) — so consumer apps don't have to author their own for the obvious cases (authorization headers, cookies, credit-card-number-shaped substrings); both default the replacement to the literal `"[REDACTED]"` and publish it through a `DefaultReplacement` const
- the canonical redaction orchestration helper `RedactionPipeline` (itself an `IRedactionFilter`) that composes an ordered sequence of filters and applies them in registration order; engine emission sites and consumer apps that want to compose multiple filters resolve a single pipeline instance from DI rather than re-implementing the pipe-through loop
- the canonical DI registration helper `IServiceCollection.AddRedactionPipeline()` (in `Cephalon.Diagnostics.Redaction.Extensions`) that registers a singleton `RedactionPipeline` composed of every registered `IRedactionFilter`; consumer apps wire the redaction surface in one fluent call after registering their filters
- public API contract lock-in from day one through `Microsoft.CodeAnalysis.PublicApiAnalyzers`, `PublicAPI.Shipped.txt`, and `PublicAPI.Unshipped.txt`

## Main surfaces

- `CephalonActivitySources.cs`
- `CephalonMeters.cs`
- `CephalonDiagnosticsAttributeKeys.cs`
- `Redaction/IRedactionFilter.cs`
- `Redaction/RedactionContext.cs`
- `Redaction/RedactionPipeline.cs`
- `Redaction/Defaults/KeyMatchRedactionFilter.cs`
- `Redaction/Defaults/RegexRedactionFilter.cs`
- `Redaction/Extensions/RedactionServiceCollectionExtensions.cs`

## Redaction quick start

Consumer apps register one or more `IRedactionFilter` implementations and call `AddRedactionPipeline()` once. The engine's M1-promoted emission sites (AspNetCore HTTP middleware, engine runtime module-phase tags, Cephalon.Eventing.Wolverine dispatch-time tags, Cephalon.Agentics in-process tool-dispatch tags, Cephalon.Retrieval knowledge-indexing + knowledge-query tags, Cephalon.Worker lifecycle-start + lifecycle-stop tags, Cephalon.Eventing in-process publication-dispatch tags, and Cephalon.MultiTenancy.Governance invitation-delivery dispatch tags) automatically route attribute values through the registered pipeline before exporter dispatch.

```csharp
using System.Text.RegularExpressions;
using Cephalon.Diagnostics.Redaction;
using Cephalon.Diagnostics.Redaction.Defaults;
using Cephalon.Diagnostics.Redaction.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.AddCephalon(); // wires AddRedactionPipeline() automatically

// Block well-known sensitive attribute keys from leaving the engine.
builder.Services.AddSingleton<IRedactionFilter>(new KeyMatchRedactionFilter(
[
    "http.request.header.authorization",
    "http.request.header.cookie",
    "http.request.header.proxy-authorization",
    "http.response.header.set-cookie",
    "cephalon.tenant.secret",
]));

// Strip credit-card-shaped substrings from any string value the engine emits.
builder.Services.AddSingleton<IRedactionFilter>(new RegexRedactionFilter(
    new Regex(@"\b(?:\d[ -]*?){13,19}\b", RegexOptions.Compiled)));

// Strip Bearer tokens from string values (URLs, log fragments, headers reused as values).
builder.Services.AddSingleton<IRedactionFilter>(new RegexRedactionFilter(
    new Regex(@"Bearer\s+[A-Za-z0-9\-_\.]+", RegexOptions.Compiled),
    replacement: "Bearer [REDACTED]"));

var app = builder.Build();
app.MapCephalon();
app.Run();
```

Filters apply in DI registration order. Each filter sees the previous filter's output as input, so consumer apps compose orthogonal concerns without coordination. Filters that don't recognise a value return it unchanged. The pipeline is empty by default — when no filters are registered, the engine emission sites short-circuit to passthrough at near-zero cost.

A runnable companion lives in [`samples/Cephalon.Sample.ModularMonolith`](../../samples/Cephalon.Sample.ModularMonolith/ModularMonolithSampleApp.cs) — the recipe above is wired into the sample composition root, so the same three filters scrub real HTTP, engine-runtime, and Wolverine dispatch emission when the sample is started.

### What is *not* redacted

The `EngineBuilder` build-time activity (`engine.build` span and the three tags `cephalon.blueprint` / `cephalon.module.count` / `cephalon.capability.count`) emits before the DI container is fully wired, so the `RedactionPipeline` is not yet resolvable. Those three values are internally derived from the manifest (blueprint id + module/capability counts) and are non-sensitive by construction — consumer apps that need to redact tenant-aware blueprint naming should rename the blueprint instead of trying to filter the tag. This is a deliberate scope boundary, not an oversight.

To author a custom filter, implement `IRedactionFilter.Filter(RedactionContext, object?)` and register it as a singleton against `IRedactionFilter`. The `RedactionContext` carries the activity source name, meter name, attribute key, and logger category at the call site so a single filter can scope its decision to one emission site or apply globally.

## How it fits

This pack is intentionally narrow at `M0` taxonomy-only. It does not own emission, exporter configuration, redaction, or sampling. The point of the pack today is to publish the *names* the engine and its host adapters will use when they emit spans, metrics, and logs, so consumer observability companion packs (`Cephalon.Observability.OpenTelemetry`, `Cephalon.Observability.Serilog`, `Cephalon.Observability.AzureMonitor`, `Cephalon.Observability.Aws`, etc.) can subscribe to them through stable identifiers.

Where an OpenTelemetry semantic convention already exists for a concept (HTTP server, DB client, messaging system, runtime, exception attributes), engine-emitted instrumentation will use the OpenTelemetry attribute name directly rather than re-declaring it under `cephalon.*`. The Cephalon-prefix keys in `CephalonDiagnosticsAttributeKeys` are deliberately scoped to engine concepts that have no semantic-convention equivalent (module id, behavior id, cell id, app blueprint, tenant id) so this pack stays a complement to OpenTelemetry rather than a replacement.

## Maturity and ownership

- maturity today: `M4` — `cephalon-managed`; the canonical name set is consumed by both the engine and an observability companion pack as a real subscription contract, not just a guidance pattern:
    - `Cephalon.Engine` consumes `CephalonActivitySources.Engine` and `CephalonMeters.Engine` so engine-runtime spans (`engine.build`, `module.{phase}`, `runtime.*`) and metrics flow through the canonical name set; module-lifecycle spans emit `cephalon.module.id` via `CephalonDiagnosticsAttributeKeys.ModuleId`
    - `Cephalon.AspNetCore` declares its diagnostics convention against `CephalonActivitySources.AspNetCore` so structured HTTP request / response / body-capture / trace-correlation diagnostics share the canonical source name with observability companion packs
    - `Cephalon.Worker` declares an internal `WorkerDiagnostics` activity source against `CephalonActivitySources.Worker` and emits `worker.lifecycle.start` / `worker.lifecycle.stop` spans around the hosted-service lifecycle
    - `Cephalon.Agentics` declares the public `AgenticsDiagnostics` adapter against `CephalonActivitySources.Agentics` and `CephalonMeters.Agentics`; the in-process tool dispatcher emits one `agentics.tool.dispatch` activity per `IAgentToolDispatcher.ExecuteAsync` call with stable tags (`cephalon.agentics.dispatcher.id`, `cephalon.agentics.tool.id`, `cephalon.agentics.run.id`, `cephalon.agentics.actor.id`, `cephalon.agentics.correlation.id`, `cephalon.agentics.attempt`, `cephalon.agentics.execution.outcome`) and a `cephalon.agentics.tool_executions` counter, all routed through the registered `RedactionPipeline`
    - `Cephalon.Eventing` declares the public `EventingDiagnostics` adapter against `CephalonActivitySources.Eventing` and `CephalonMeters.Eventing`; the in-process publisher emits one `eventing.publication.dispatch` activity per `IEventPublisher.PublishAsync` call with stable tags (`cephalon.eventing.publisher.id`, `cephalon.eventing.publication.id`, `cephalon.eventing.channel.id`, `cephalon.eventing.event.type`, `cephalon.eventing.publication.outcome`, `cephalon.eventing.matched_subscription.count`) and a `cephalon.eventing.publications` counter, all routed through the registered `RedactionPipeline`
    - `Cephalon.MultiTenancy.Governance` declares the public `GovernanceDiagnostics` adapter against `CephalonActivitySources.MultiTenancyGovernance` and `CephalonMeters.MultiTenancyGovernance`; the invitation-delivery dispatcher emits one `multitenancy.governance.invitation.delivery.dispatch` activity per dispatch attempt with stable tags (`cephalon.tenant.id`, `cephalon.multitenancy_governance.invitation.id`, `cephalon.multitenancy_governance.delivery.channel`, `cephalon.multitenancy_governance.delivery.sender.id`, `cephalon.multitenancy_governance.delivery.outcome`) and a `cephalon.multitenancy_governance.invitation_dispatches` counter, all routed through the registered `RedactionPipeline`
    - **`/engine/diagnostics-conventions`** ships through `Cephalon.AspNetCore` and projects the canonical activity-source names, meter names, and `cephalon.*` attribute keys as a `DiagnosticsConventionsSurface` record; operators introspect the emission contract through one HTTP read, and AI tooling can subscribe to the surface as part of the engine's introspection contract
    - **`Cephalon.Observability.OpenTelemetry`** subscribes to all seven canonical activity sources (`CephalonActivitySources.Engine` / `.AspNetCore` / `.Worker` / `.Eventing` / `.MultiTenancyGovernance` / `.Agentics` / `.Retrieval`) and the matching seven canonical meters when telemetry export is enabled, so consumer apps that opt into the OTLP companion pack export every engine-emitted span and metric without further configuration; `.Retrieval` emits one `retrieval.knowledge.index` activity per `IKnowledgeIndexer.IndexAsync` plus one `retrieval.knowledge.query` activity per `IKnowledgeQueryEngine.QueryAsync` (shipped through `ENG-402`); `.Eventing` emits one `eventing.publication.dispatch` activity per `IEventPublisher.PublishAsync` (shipped through `ENG-371`); `.MultiTenancyGovernance` emits one `multitenancy.governance.invitation.delivery.dispatch` activity per invitation-dispatch attempt (shipped through `ENG-379`)
- ownership: `cephalon-managed` (engine + both host adapters consume this package's constants directly; the `/engine/diagnostics-conventions` route is owned by `Cephalon.AspNetCore`; `Cephalon.Observability.OpenTelemetry` consumes the canonical names as a real subscription contract)

## Cross-references

- [Engineering standards](../engineering-standards.md) — code-quality gates, library / API design, packaging
- [Diagnostic ID registry](../diagnostic-id-registry.md) — authoritative allocation table for per-package `EventId` ranges; complements the `ActivitySource` / `Meter` name set this pack publishes
- [Compatibility](../compatibility.md) — public-API contract artefacts (`PublicAPI.Shipped.txt` / `PublicAPI.Unshipped.txt`)
- [Engine surface maturity audit](../engine-surface-maturity-audit.md) — `M0`–`M4` plus `taxonomy-only` / `application-managed` / `cephalon-managed` / `provider-managed` truth
- [Supply-chain uplift plan](../supply-chain-uplift-plan.md) — multi-sprint plan that includes this pack as `ENG-323`
- [SRE posture](../sre-posture.md) — declares the OpenTelemetry semantic-convention discipline this pack embodies
- [OpenTelemetry semantic conventions](https://opentelemetry.io/docs/specs/semconv/) — authoritative external source
