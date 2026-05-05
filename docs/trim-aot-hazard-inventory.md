---
maturity: M1 — taxonomy-only inventory
ownership: engine-managed (the inventory lives in the repo and is refreshed alongside slices that move trim/AOT posture)
---

# Trim / Native AOT / Single-file hazard inventory

> See also: [`deployment-mode-support.md`](deployment-mode-support.md), [`compatibility.md`](compatibility.md), [`engineering-standards.md`](engineering-standards.md), [`dotnet11-readiness.md`](dotnet11-readiness.md), [`engine-roadmap.md`](engine-roadmap.md), [`architecture-review-2026-05.md`](architecture-review-2026-05.md), [`conformance-matrix.md`](conformance-matrix.md)

This document records the **actual reflection / dynamic-code patterns observed in shipped `src/Cephalon.*` runtime code as of May 5, 2026**, classified by their impact on a future trim / Native AOT / single-file claim. It is the concrete inventory feeding the validation-harness work declared in [`deployment-mode-support.md`](deployment-mode-support.md) (`representativePublishTargets`, per-package `deploymentModeEligibility`, and `knownTransitiveHazards` manifest fields).

It does **not** widen the support contract. Trim, Native AOT, and single-file remain `not-claimed` until the harness lands, the matching project properties are intentionally set, and `scripts/deployment-mode-support.json` is updated together with the docs and release-validation flow.

## Scope and method

- **Sources searched.** Every `src/Cephalon.*/*.cs` file under the repo, plus every `src/Cephalon.*/*.csproj` for explicit trim / AOT property declarations.
- **Patterns flagged.** `Activator.CreateInstance`, `Type.GetType(string)`, `MakeGenericType`, `Assembly.LoadFrom`, plus `[DynamicallyAccessedMembers]`, `[RequiresUnreferencedCode]`, and `[RequiresDynamicCode]` annotations.
- **Method.** Pattern matches were classified by the actual call site, not by attribute presence. As of May 5, 2026, **no Cephalon runtime type carries `[RequiresUnreferencedCode]` / `[RequiresDynamicCode]` annotations** — every entry below is a pre-annotation hazard. The validation harness will gate annotation rollout on the matching project properties (`PublishTrimmed`, `PublishAot`, etc.) being intentionally set.
- **Out of scope.** Test, benchmark, sample, and tooling projects. Concrete provider packs only appear when their `Cephalon.*` runtime surface itself contains a hazard pattern (transitive provider-SDK hazards belong in the harness's `knownTransitiveHazards` list, not here).

## Severity classification

| Severity | Meaning | Required remediation before a `claimed` deployment-mode posture |
| --- | --- | --- |
| `excluded-by-design` | Roslyn analyzer / source-generator project that runs in the compiler, never in published output. Properties already declare `IsTrimmable=false`, `IsAotCompatible=false`, `PublishTrimmed=false`, `PublishAot=false`, `PublishSingleFile=false`. | None — the project is intentionally outside the deployment-mode story. |
| `clean-baseline` | Runtime package with no observed reflection / dynamic-code patterns in its `Cephalon.*` source. | Confirm transitive dependencies are clean against the harness's analyzer phase before adopting any `claimed` mode. |
| `low` | Reflection is bounded to closed-generic instantiation over a small known set of types and is reachable only through engine-internal seams. Likely fixable with `[DynamicallyAccessedMembers]` annotations or a small generic-shape rewrite. | Annotate or rewrite, then re-validate. |
| `medium` | Reflection over types loaded from data (e.g. `Type.GetType(persistedString)`). The provider can lift this to AOT-safe by registering a serializer table or polymorphic JSON converter, but the change is non-trivial and the manifest must declare the provider as `claim-overstated` until done. | Replace with a compile-time type registry or polymorphic `System.Text.Json` `[JsonPolymorphic]` baseline. |
| `high` | Reflection that depends on assembly scanning, runtime-loaded plugins, or open-generic adapter instantiation. Cannot become AOT-safe without a structural rewrite (typically a source generator or compile-time discovery contract). | Replace with a source generator + compile-time descriptor, or document a permanent `not-claimed` posture for that package. |

A package is only as portable as its highest hazard. A `Cephalon.EventSourcing.*` provider that uses `Type.GetType` is `medium` even if the rest of its surface is clean.

## Excluded-by-design (analyzer + source-generator projects)

| Package | csproj declares | Notes |
| --- | --- | --- |
| `Cephalon.Analyzers` | `IsTrimmable=false`, `IsAotCompatible=false`, `PublishTrimmed=false`, `PublishAot=false`, `PublishSingleFile=false` | Roslyn analyzer pack; runs in the compiler, never reaches published output. |
| `Cephalon.Behaviors.SourceGen` | `IsTrimmable=false`, `IsAotCompatible=false`, `PublishTrimmed=false`, `PublishAot=false`, `PublishSingleFile=false` | Roslyn source generator; same posture. |

These are correctly excluded today. They are listed here for completeness so a future harness run does not flag them as missing properties.

## Hazardous runtime patterns (current inventory)

### `high` — assembly scanning and open-generic adapter instantiation

| Package | File | Pattern | Why it is `high` |
| --- | --- | --- | --- |
| `Cephalon.Engine` | `src/Cephalon.Engine/Composition/ModuleDiscovery.cs:113` | `Activator.CreateInstance(moduleType, nonPublic: true)` over types discovered through assembly enumeration | Module discovery walks loaded assemblies and instantiates discovered `IModule` types reflectively. The trim-safe replacement is a compile-time module manifest emitted by a source generator; the runtime contract `IRuntime.Modules` is already a closed read surface, but module *discovery* is open. Annotation alone is not sufficient. |
| `Cephalon.Behaviors.Patterns` | `src/Cephalon.Behaviors.Patterns/Strategies/DurableExecutionStrategy.cs:386,392` | `typeof(DurableExecutionAdapter<,,,>).MakeGenericType(...)` then `Activator.CreateInstance(adapterType)` | The strategy materializes a four-type-arg generic adapter for every behavior contract instance. Fixable through a source-gen-emitted adapter table keyed by behavior identity, but the open-generic shape (`<TBehavior, TInput, TOutput, TState>`) cannot be preserved at AOT-time without that compile-time emission. |
| `Cephalon.Behaviors` | `src/Cephalon.Behaviors/Services/BehaviorTypeRegistry.cs` (+ `IBehaviorTypeRegistry.cs`) | Runtime registry that keys behaviors by `Type` and supports late-bound resolution from descriptors | The registry is the central seam through which runtime descriptors resolve to concrete behavior types. AOT-safe replacement is the same compile-time descriptor + source generator approach as the durable adapter; until that lands, this is the highest-leverage AOT blocker in the behavior family. |

### `medium` — reflection over persisted type names (`Type.GetType(string)`)

The `Cephalon.EventSourcing.*` provider family persists event payloads alongside their CLR type names and reflectively rehydrates them at read time. Every provider below uses the same shape and shares the same remediation path (replace with a compile-time event-type registry or `[JsonPolymorphic]` discriminator table).

| Package | File | Line | Persisted-type call site |
| --- | --- | --- | --- |
| `Cephalon.EventSourcing.Cassandra` | `CassandraEventStore.cs` | 158 | `Type.GetType(eventTypeName, throwOnError: false)` |
| `Cephalon.EventSourcing.ClickHouse` | `ClickHouseEventStore.cs` | 151 | `Type.GetType(eventTypeName, throwOnError: false)` |
| `Cephalon.EventSourcing.Elasticsearch` | `ElasticsearchEventStore.cs` | 95 | `Type.GetType(hit.Source.EventType, throwOnError: false)` |
| `Cephalon.EventSourcing.EntityFramework` | `Services/EntityFrameworkEventStore.cs` | 90 | `Type.GetType(entry.EventType, throwOnError: false)` |
| `Cephalon.EventSourcing.MongoDB` | `MongoDbEventStore.cs` | 130 | `Type.GetType(entry.EventType, throwOnError: false)` |
| `Cephalon.EventSourcing.Nats` | `NatsEventStore.cs` | 170 | `Type.GetType(natsEntry.EventType, throwOnError: false)` |
| `Cephalon.EventSourcing.Neo4j` | `Neo4jEventStore.cs` | 200 | `Type.GetType(entry.EventType, throwOnError: false)` |
| `Cephalon.EventSourcing.OpenSearch` | `OpenSearchEventStore.cs` | 95 | `Type.GetType(hit.Source.EventType, throwOnError: false)` |
| `Cephalon.EventSourcing.Qdrant` | `QdrantEventStore.cs` | 180 | `Type.GetType(eventTypeName, throwOnError: false)` |
| `Cephalon.EventSourcing.Redis` | `RedisEventStore.cs` | 138 | `Type.GetType(eventTypeStr, throwOnError: false)` |

The remediation is uniform: introduce a `Cephalon.EventSourcing` compile-time event-type registry interface that providers consume instead of `Type.GetType`. The interface can be source-generated from the consuming app's known event-contract assembly. Until that ships, every `Cephalon.EventSourcing.*` provider stays `medium` and `not-claimed` for AOT.

### `medium` (reclassified from `low` via `ENG-431`) — REST endpoint group reflection across method dispatch + generic shape

A deeper read of `Cephalon.Behaviors.Http.Hosting.BehaviorRestEndpointGroup` after the `ENG-426` first pass surfaced three additional reflection sites alongside the original `MakeGenericType(ResultModel<>)` site that ENG-426 had logged. The full set is real open-generic *method* dispatch — not just the closed-generic *type* wrapper — so the package belongs in the `medium` tier (uniform remediation through source-gen-emitted dispatch table) rather than the `low` tier (single annotation site).

| Package | File | Line | Pattern | Notes |
| --- | --- | --- | --- | --- |
| `Cephalon.Behaviors.Http` | `Hosting/BehaviorRestEndpointGroup.cs` | 32-36 | Five `static readonly MethodInfo` fields initialized via `GetRequiredCoreMethod(nameof(...))` which calls `typeof(BehaviorRestEndpointGroup).GetMethod(name, BindingFlags.NonPublic \| BindingFlags.Static)` | Reflective lookup of five non-public static helper methods (`MapBehavior{Delete,Get,Patch,Post,Put}Core`) at type-init time. AOT-safe replacement is direct method references (no `MethodInfo` indirection) once the source-gen rewrite below is in place. |
| `Cephalon.Behaviors.Http` | `Hosting/BehaviorRestEndpointGroup.cs` | 486 | `coreMethod.MakeGenericMethod(typeof(TBehavior), contract.InputType, contract.OutputType)` | Open-generic *method* dispatch over an arbitrary `(TBehavior, TInput, TOutput)` triple drawn from runtime `contract` data. This is the dominant hazard in the file. Source-gen-emitted dispatch table keyed by behavior identity is the AOT-safe path; the same generator could also retire the line-32-36 `MethodInfo` fields. |
| `Cephalon.Behaviors.Http` | `Hosting/BehaviorRestEndpointGroup.cs` | 487 | `(RouteHandlerBuilder)closedMethod.Invoke(null, [this, pattern, contract])!` | Reflection-based method invocation following the `MakeGenericMethod`. Removed by the same source-gen rewrite. |
| `Cephalon.Behaviors.Http` | `Hosting/BehaviorRestEndpointGroup.cs` | 700 | `typeof(ResultModel<>).MakeGenericType(contract.ResponseType)` | Original ENG-426 entry. Closed-generic wrapper for the response envelope; same source-gen path emits the closed `ResultModel<TResponse>` directly. |

## Clean-baseline (no observed hazards)

The remaining shipped `Cephalon.*` runtime packages — including (but not limited to) `Cephalon.Abstractions`, `Cephalon.Diagnostics`, `Cephalon.Resilience`, `Cephalon.AspNetCore`, `Cephalon.Worker`, `Cephalon.Eventing`, `Cephalon.Eventing.Wolverine`, `Cephalon.Agentics`, `Cephalon.Retrieval`, `Cephalon.Audit`, `Cephalon.Audit.EntityFramework`, `Cephalon.Identity`, `Cephalon.MultiTenancy`, the `Cephalon.MultiTenancy.Governance.*` companion family, the `Cephalon.Data.*` provider family, the `Cephalon.Edge.*` family, the `Cephalon.Observability.*` family, `Cephalon.Cli`, and `Cephalon.Scaffolding` — show **no `Activator.CreateInstance`, `Type.GetType(string)`, `MakeGenericType`, or `Assembly.LoadFrom` use in their first-party source as of May 5, 2026**.

This is a strong claim only at the *Cephalon source-code* layer. Every package in this tier still depends on transitive packages whose AOT posture must be validated through the harness's analyzer phase; provider SDKs (`AWSSDK.*`, `Azure.*`, `Google.*`, `Confluent.Kafka`, `Cassandra`, `Neo4j.Driver`, `MongoDB.Driver`, `Elastic.Clients.*`, `Microsoft.EntityFrameworkCore`, `Wolverine`, `Polly`) routinely emit `IL2026` / `IL3050` warnings. The `clean-baseline` label means *Cephalon code is not the bottleneck*; it does not mean the package can publish AOT today.

## Aggregate posture

| Tier | Packages | Effective per-package AOT verdict (today) |
| --- | --- | --- |
| `excluded-by-design` | 2 | not part of the deployment-mode story |
| `clean-baseline` | ~88 first-party packages (transitive validation pending) | `not-claimed`, blocked on harness + transitive validation |
| `low` | 0 (the original `Cephalon.Behaviors.Http` entry was reclassified to `medium` via `ENG-431` after a deeper read surfaced open-generic *method* dispatch alongside the original closed-generic *type* wrapper) | n/a |
| `medium` | 11 (10 `Cephalon.EventSourcing.*` providers + `Cephalon.Behaviors.Http`) | `not-claimed`; uniform remediation through compile-time event-type registry (EventSourcing) or source-gen-emitted dispatch table (Behaviors.Http) |
| `high` | 3 (`Cephalon.Engine`, `Cephalon.Behaviors`, `Cephalon.Behaviors.Patterns`) | `not-claimed`; structural remediation required (source-gen-emitted module manifest + adapter table + behavior-type registry) |

These counts will move only when remediation slices ship. The doc must be re-run by the same `Grep` queries described in *Scope and method* whenever a new pattern is introduced or removed; the validation harness will eventually emit this inventory automatically as part of its project-property + reflection-pattern audit phase.

## How this inventory feeds the validation harness

[`deployment-mode-support.md`](deployment-mode-support.md) declares planned schema additions to `scripts/deployment-mode-support.json`:

- `deploymentModeEligibility` (per-package `supportedModes`, `requiredProjectProperties`, `minimumAnalyzerPackVersion`, `knownHazards`)
- `representativePublishTargets`
- `expectedPublishOutputShape`
- `validationStrategy` (`analyzer-only` vs `publish-required` vs `full-flow`)

The inventory above maps directly to those manifest fields:

- **`knownHazards` per-package** — the *Hazardous runtime patterns* tables provide the per-package hazard list (`reflection-open-generic`, `reflection-persisted-type-name`, `reflection-assembly-scan`).
- **`deploymentModeEligibility.supportedModes`** — the *Aggregate posture* table is the per-tier seed for which packages can move to `claim-truthful` first (clean-baseline + low precede medium, which precedes high).
- **`representativePublishTargets`** — the harness's first publish probes should target one package from each tier so a regression at any tier is visible: a clean-baseline reference like `Cephalon.Diagnostics`, the `low` `Cephalon.Behaviors.Http`, one `medium` `Cephalon.EventSourcing.*` provider, and one `high` package.

When the harness ships, this doc is rewritten in place to read from the harness's machine-readable report instead of a manual `Grep` pass. Until then, this doc is the source of truth for the per-package hazard surface.

**Update May 5, 2026 (`ENG-427`):** the per-package entries in this inventory have been seeded into `scripts/deployment-mode-support.json` under `deploymentModeEligibility.packages` (16 entries: 3 `high` + 10 `medium` + 1 `low` + 2 `excluded-by-design`). Each manifest entry carries `packageName`, `nugetId`, `claimAuditTier`, `supportedModes` (today: empty for every entry), `requiredProjectProperties`, `knownHazards` (with `kind` / `site` / `pattern` / `remediation` per entry), `evidence` pointer back to this doc, and `introducedBy` ENG reference. Schema is contract-locked through extended Pester coverage in `tests/Cephalon.Tests.Scripts/deployment-mode-support-manifest.Tests.ps1` (28 tests passing). Refreshing this doc in a future slice now requires updating the manifest entries in the same slice.

## Refresh discipline

This inventory is refreshed in the same slice that:

- introduces a new `Activator.CreateInstance`, `Type.GetType(string)`, `MakeGenericType`, or `Assembly.LoadFrom` call site in any `src/Cephalon.*` package, **or**
- removes / annotates one of the call sites listed above, **or**
- changes the project-property declarations in any `Cephalon.Analyzers` / `Cephalon.Behaviors.SourceGen` style excluded-by-design package, **or**
- ships a structural remediation (source-generator, compile-time registry) that retires one of the entries here.

A no-change month (no slice touched any of those surfaces) does not require an update; the doc records the state-as-of date in its first-paragraph review window.
