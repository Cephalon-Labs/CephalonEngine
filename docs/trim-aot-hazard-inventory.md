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
- **Patterns flagged (extended via `ENG-433`).** The `ENG-426` first-pass query covered `Activator.CreateInstance`, `Type.GetType(string)`, `MakeGenericType`, and `Assembly.LoadFrom`. The `ENG-432` deeper read of `Cephalon.Behaviors.Http` revealed that real hazards routinely show up under `MakeGenericMethod`, `MethodInfo.Invoke`, and `BindingFlags.NonPublic`-style member lookups — patterns the first-pass query did not match. The current pattern set is therefore: `Activator.CreateInstance`, `Type.GetType(string)`, `MakeGenericType`, `MakeGenericMethod`, `Assembly.LoadFrom`, `MethodInfo.Invoke`, `BindingFlags.\*`, `\.InvokeMember\(`, `Expression.Lambda`, `TypeBuilder` / `DynamicMethod` / `ILGenerator` / `Emit\.\*`, plus `[DynamicallyAccessedMembers]`, `[RequiresUnreferencedCode]`, and `[RequiresDynamicCode]` annotations.
- **Method.** Pattern matches were classified by the actual call site, not by attribute presence. As of May 5, 2026, **no Cephalon runtime type carries `[RequiresUnreferencedCode]` / `[RequiresDynamicCode]` annotations** — every entry below is a pre-annotation hazard. The validation harness will gate annotation rollout on the matching project properties (`PublishTrimmed`, `PublishAot`, etc.) being intentionally set.
- **Known not-yet-classified hazard surface (`ENG-433` first-pass under broadened patterns).** The broadened `Grep` (May 5, 2026) surfaced additional `MakeGenericMethod` / `BindingFlags`-based hazards in `Cephalon.Behaviors` (`Modules/BehaviorModule.cs` lines 347/356/388/494; `Services/BehaviorExecutionSlot.cs` lines 73-74), `Cephalon.Behaviors.Http` beyond the four sites already classified by `ENG-432` (`Hosting/RestBehaviorProjection.cs` lines 43/207, `Hosting/RestBehaviorModuleBuilder.cs` line 230, `Hosting/BehaviorRestProfileResolver.cs` lines 155/192), and `Cephalon.ReferenceDocs` (`Generation/ReferenceDocsGenerator.cs` lines 295/301/308/313 enumerate constructors/fields/properties/methods on arbitrary types — by design for doc generation but trim-hostile). These are flagged here so a future audit slice can classify them; they are **not** yet recorded in the per-tier tables below to avoid landing classifications without a deeper read of each call site (the `ENG-432` precedent showed first-pass classifications materially understate hazards). The CDC capture services in `Cephalon.Data.MySql` / `Cephalon.Data.Postgres` / `Cephalon.Data.Oracle` were on this list and have now been classified through `ENG-434`'s deeper read (see the `high` and `medium` tier tables below). The `Cephalon.Abstractions` enum-field lookups (across `RestEndpointAuthoringPolicySuppressionKind`, `RestEndpointBindingFallbackMode`, `RestEndpointBindingSource`, `RestEndpointCandidateStatus`, `RestEndpointGovernanceRuleSelectionBasis`, `RestEndpointOverrideActionKind`, `RestEndpointOverrideBindingMode`) plus the matching `Cephalon.Behaviors.Http.Abstractions.BehaviorRestMethod` / `BehaviorRestBindingSource` extensions were also on this list and have now been classified through `ENG-435`'s deeper read (see the new `low` tier table below).
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

### `high` — non-public reflective access on a third-party transport type in `Cephalon.Data.MySql` (added via `ENG-434`)

`Cephalon.Data.MySql` ships a binlog CDC transport that depends on internal members of the third-party `SciSharp.MySQL.Replication.ReplicationClient` type. The transport reflectively resolves five non-public methods, one non-public hierarchy method, and three non-public fields by string name at type-init time, then invokes the resolved methods through `MethodInfo.Invoke`. Trim and Native AOT will strip non-public members of a third-party assembly because there is no annotation path that reaches across an external compiled binary; remediation requires either an upstream API surface change in `SciSharp.MySQL.Replication` (open-sourcing the non-public members) or rewriting the transport against a fully public API path. This puts `Cephalon.Data.MySql` in the same `high` tier as the assembly-scanning and open-generic adapter blockers — a structural rewrite is required, annotation alone is not sufficient.

| Package | File | Line | Pattern | Notes |
| --- | --- | --- | --- | --- |
| `Cephalon.Data.MySql` | `Services/MySqlBinlogTransport.cs` | 1177 | `typeof(ReplicationClient).GetMethod(name, BindingFlags.Instance \| BindingFlags.NonPublic)` | Resolver behind the five `static readonly MethodInfo` fields at lines 21-24 (`GetBinlogChecksum`, `ConfirmChecksum`, `GetStreamFromMySQLConnection`, `StartDumpBinlog`). Non-public method lookup on a third-party type — not annotatable. |
| `Cephalon.Data.MySql` | `Services/MySqlBinlogTransport.cs` | 1184 | `typeof(ReplicationClient).GetMethod(name, BindingFlags.Instance \| BindingFlags.NonPublic \| BindingFlags.FlattenHierarchy)` | Resolver behind the `SetupConnection` `MethodInfo` field at line 25. Hierarchy-flattened non-public method lookup on a third-party type. |
| `Cephalon.Data.MySql` | `Services/MySqlBinlogTransport.cs` | 1191 | `typeof(ReplicationClient).GetField(name, BindingFlags.Instance \| BindingFlags.NonPublic)` | Resolver behind the three `static readonly FieldInfo` fields at lines 26-28 (`_connection`, `_serverId`, `_stream`). Non-public field lookup on a third-party type. |
| `Cephalon.Data.MySql` | `Services/MySqlBinlogTransport.cs` | 1198 | `method.Invoke(instance, arguments)` | Generic `MethodInfo.Invoke` over the resolved methods. Trim-hostile because the resolved methods themselves can be stripped from the third-party assembly. |

### `medium` — open-generic command/query dispatch in `Cephalon.Data` (added via `ENG-433`)

`Cephalon.Data` ships a runtime command/query dispatcher that resolves handlers reflectively over runtime command/query types via `MakeGenericMethod`. The remediation is a source-gen-emitted dispatch table keyed by command/query identity, equivalent to the path named for `Cephalon.Behaviors.Http` in `ENG-432`.

| Package | File | Line | Pattern | Notes |
| --- | --- | --- | --- | --- |
| `Cephalon.Data` | `Services/HandlerDispatchingWriteStore.cs` | 11 | `typeof(HandlerDispatchingWriteStore).GetMethod(nameof(DispatchCommandAsync), BindingFlags.NonPublic \| BindingFlags.Static)` | Reflective lookup of the non-public static dispatch helper at type-init time. |
| `Cephalon.Data` | `Services/HandlerDispatchingWriteStore.cs` | 14 | `typeof(HandlerDispatchingWriteStore).GetMethod(nameof(DispatchResultCommandAsync), BindingFlags.NonPublic \| BindingFlags.Static)` | Same shape for the result-returning command path. |
| `Cephalon.Data` | `Services/HandlerDispatchingWriteStore.cs` | 41 | `DispatchCommandMethod.MakeGenericMethod(commandType)` | Open-generic *method* dispatch over the runtime `commandType`, then `CreateDelegate`. |
| `Cephalon.Data` | `Services/HandlerDispatchingWriteStore.cs` | 47 | `DispatchResultCommandMethod.MakeGenericMethod(commandType, typeof(TResult))` | Open-generic *method* dispatch over the `(commandType, TResult)` pair. |
| `Cephalon.Data` | `Services/HandlerDispatchingReadStore.cs` | 11 | `typeof(HandlerDispatchingReadStore).GetMethod(nameof(DispatchQueryAsync), BindingFlags.NonPublic \| BindingFlags.Static)` | Reflective lookup of the non-public static query dispatcher. |
| `Cephalon.Data` | `Services/HandlerDispatchingReadStore.cs` | 27 | `DispatchQueryMethod.MakeGenericMethod(queryType, typeof(TResult))` | Open-generic *method* dispatch over the `(queryType, TResult)` pair. |

### `medium` — duck-typed exception failure-kind/metadata fallback in CDC capture services (added via `ENG-434`)

The CDC capture hosted services in `Cephalon.Data.Postgres`, `Cephalon.Data.Oracle`, and `Cephalon.Data.MySql` resolve capture-failure metadata through a typed-first / reflection-fallback path. The typed path checks for a known capture-exception type (`PostgresLogicalReplicationCaptureException` / `OracleLogMinerCaptureException` / `MySqlBinlogCaptureException`) and reads its strongly-typed `FailureKind` + `Metadata` properties. When the exception is *not* the typed one, the service falls back to `exception.GetType().GetProperty("FailureKind", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)` plus the same lookup for `"Metadata"` — duck-typing over an open exception type drawn from runtime data. The remediation is uniform: lift the duck-typed fallback to a small interface (e.g. `ICaptureFailureMetadata`) the typed exception already implements, plus a registry of additional exception types whose metadata should be projected, so `[DynamicallyAccessedMembers]` annotations and trim warnings stop at the interface boundary.

| Package | File | Line | Pattern | Notes |
| --- | --- | --- | --- | --- |
| `Cephalon.Data.Postgres` | `Services/PostgresLogicalReplicationCaptureHostedService.cs` | 193 | `exception.GetType().GetProperty("FailureKind", BindingFlags.Instance \| BindingFlags.Public \| BindingFlags.NonPublic)` | Reached only on the non-typed fallback branch (line 186 short-circuits the typed `PostgresLogicalReplicationCaptureException` case). Public + non-public property lookup on a runtime exception type. |
| `Cephalon.Data.Postgres` | `Services/PostgresLogicalReplicationCaptureHostedService.cs` | 201 | `exception.GetType().GetProperty("Metadata", BindingFlags.Instance \| BindingFlags.Public \| BindingFlags.NonPublic)` | Same fallback branch; second of the two duck-typed property reads. |
| `Cephalon.Data.Oracle` | `Services/OracleLogMinerCaptureHostedService.cs` | 194 | `exception.GetType().GetProperty("FailureKind", BindingFlags.Instance \| BindingFlags.Public \| BindingFlags.NonPublic)` | Same shape as the Postgres fallback. |
| `Cephalon.Data.Oracle` | `Services/OracleLogMinerCaptureHostedService.cs` | 202 | `exception.GetType().GetProperty("Metadata", BindingFlags.Instance \| BindingFlags.Public \| BindingFlags.NonPublic)` | Same shape as the Postgres fallback. |
| `Cephalon.Data.MySql` | `Services/MySqlBinlogCaptureHostedService.cs` | 194 | `exception.GetType().GetProperty("FailureKind", BindingFlags.Instance \| BindingFlags.Public \| BindingFlags.NonPublic)` | Same shape as the Postgres / Oracle fallback. `Cephalon.Data.MySql` also carries the separate `high`-tier `ReplicationClient` access listed above, so its overall tier is still `high`; this entry is recorded for completeness so the harness sees every duck-typed call site. |
| `Cephalon.Data.MySql` | `Services/MySqlBinlogCaptureHostedService.cs` | 202 | `exception.GetType().GetProperty("Metadata", BindingFlags.Instance \| BindingFlags.Public \| BindingFlags.NonPublic)` | Same shape. |

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

### `medium` (reclassified from `low` via `ENG-432`) — REST endpoint group reflection across method dispatch + generic shape

A deeper read of `Cephalon.Behaviors.Http.Hosting.BehaviorRestEndpointGroup` after the `ENG-426` first pass surfaced three additional reflection sites alongside the original `MakeGenericType(ResultModel<>)` site that ENG-426 had logged. The full set is real open-generic *method* dispatch — not just the closed-generic *type* wrapper — so the package belongs in the `medium` tier (uniform remediation through source-gen-emitted dispatch table) rather than the `low` tier (single annotation site).

| Package | File | Line | Pattern | Notes |
| --- | --- | --- | --- | --- |
| `Cephalon.Behaviors.Http` | `Hosting/BehaviorRestEndpointGroup.cs` | 32-36 | Five `static readonly MethodInfo` fields initialized via `GetRequiredCoreMethod(nameof(...))` which calls `typeof(BehaviorRestEndpointGroup).GetMethod(name, BindingFlags.NonPublic \| BindingFlags.Static)` | Reflective lookup of five non-public static helper methods (`MapBehavior{Delete,Get,Patch,Post,Put}Core`) at type-init time. AOT-safe replacement is direct method references (no `MethodInfo` indirection) once the source-gen rewrite below is in place. |
| `Cephalon.Behaviors.Http` | `Hosting/BehaviorRestEndpointGroup.cs` | 486 | `coreMethod.MakeGenericMethod(typeof(TBehavior), contract.InputType, contract.OutputType)` | Open-generic *method* dispatch over an arbitrary `(TBehavior, TInput, TOutput)` triple drawn from runtime `contract` data. This is the dominant hazard in the file. Source-gen-emitted dispatch table keyed by behavior identity is the AOT-safe path; the same generator could also retire the line-32-36 `MethodInfo` fields. |
| `Cephalon.Behaviors.Http` | `Hosting/BehaviorRestEndpointGroup.cs` | 487 | `(RouteHandlerBuilder)closedMethod.Invoke(null, [this, pattern, contract])!` | Reflection-based method invocation following the `MakeGenericMethod`. Removed by the same source-gen rewrite. |
| `Cephalon.Behaviors.Http` | `Hosting/BehaviorRestEndpointGroup.cs` | 700 | `typeof(ResultModel<>).MakeGenericType(contract.ResponseType)` | Original ENG-426 entry. Closed-generic wrapper for the response envelope; same source-gen path emits the closed `ResultModel<TResponse>` directly. |

### `low` — bounded enum-field lookup for `[JsonStringEnumMemberName]` wire-name resolution (added via `ENG-435`)

`Cephalon.Abstractions` and `Cephalon.Behaviors.Http.Abstractions` ship enum-extension types that resolve a wire-name string for each enum value by reading the `[JsonStringEnumMemberName]` attribute off the matching public-static enum field. Every site follows the exact same shape:

```csharp
foreach (var value in Enum.GetValues<TEnum>())
{
    var field = typeof(TEnum).GetField(value.ToString(), BindingFlags.Public | BindingFlags.Static);
    ArgumentNullException.ThrowIfNull(field);
    var wireName = field.GetCustomAttribute<JsonStringEnumMemberNameAttribute>()?.Name;
    // ... fall back to field.Name when the attribute is absent
}
```

The reflection is bounded: it iterates `Enum.GetValues<TEnum>()` for one closed, hardcoded enum type per call site and reads the attribute off each known field. There is no runtime-loaded type, no open generic, and no member discovery beyond the enum's own public-static fields. This is the canonical `low` tier per the [severity classification](#severity-classification): annotation alone is sufficient (`[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]` on the enum types — or, equivalently, on the `typeof(TEnum)` parameter when the helpers are refactored), or — the cleaner long-term path — the same wire-name pairs can be source-generated via `[JsonStringEnumConverter<TEnum>]` (.NET 8+ AOT-safe) so the engine stops asking the runtime for the answer it already knows at compile time.

Tracking the package overall:

- **`Cephalon.Abstractions`** moves from `clean-baseline (pending deeper-read)` to `low`. Its overall package tier is `low` because no other `MakeGenericMethod` / `MethodInfo.Invoke` / `Activator.CreateInstance` / `Type.GetType(string)` / `Assembly.LoadFrom` site exists in its first-party source — only the seven enum-extension lookups listed below.
- **`Cephalon.Behaviors.Http`** keeps its overall package tier of `medium` (the dominant blocker is still the open-generic *method* dispatch in `Hosting/BehaviorRestEndpointGroup.cs:486` per `ENG-432`). The two enum-extension sites below are recorded in the same package's `knownHazards` list because the inventory tracks per-call-site evidence, not just per-package tier.

| Package | File | Line | Pattern | Notes |
| --- | --- | --- | --- | --- |
| `Cephalon.Abstractions` | `Transports/RestEndpointAuthoringPolicySuppressionKindExtensions.cs` | 54 | `typeof(RestEndpointAuthoringPolicySuppressionKind).GetField(value.ToString(), BindingFlags.Public \| BindingFlags.Static)` | Wire-name resolver for the `RestEndpointAuthoringPolicySuppressionKind` enum. |
| `Cephalon.Abstractions` | `Transports/RestEndpointBindingFallbackModeExtensions.cs` | 54 | `typeof(RestEndpointBindingFallbackMode).GetField(value.ToString(), BindingFlags.Public \| BindingFlags.Static)` | Wire-name resolver for the `RestEndpointBindingFallbackMode` enum. |
| `Cephalon.Abstractions` | `Transports/RestEndpointBindingSourceExtensions.cs` | 54 | `typeof(RestEndpointBindingSource).GetField(value.ToString(), BindingFlags.Public \| BindingFlags.Static)` | Wire-name resolver for the `RestEndpointBindingSource` enum. |
| `Cephalon.Abstractions` | `Transports/RestEndpointCandidateStatusExtensions.cs` | 54 | `typeof(RestEndpointCandidateStatus).GetField(value.ToString(), BindingFlags.Public \| BindingFlags.Static)` | Wire-name resolver for the `RestEndpointCandidateStatus` enum. |
| `Cephalon.Abstractions` | `Transports/RestEndpointGovernanceRuleSelectionBasisExtensions.cs` | 56 | `typeof(RestEndpointGovernanceRuleSelectionBasis).GetField(value.ToString(), BindingFlags.Public \| BindingFlags.Static)` | Wire-name resolver for the `RestEndpointGovernanceRuleSelectionBasis` enum. |
| `Cephalon.Abstractions` | `Transports/RestEndpointOverrideActionKindExtensions.cs` | 57 | `typeof(RestEndpointOverrideActionKind).GetField(value.ToString(), BindingFlags.Public \| BindingFlags.Static)` | Wire-name resolver for the `RestEndpointOverrideActionKind` enum. |
| `Cephalon.Abstractions` | `Transports/RestEndpointOverrideBindingModeExtensions.cs` | 57 | `typeof(RestEndpointOverrideBindingMode).GetField(value.ToString(), BindingFlags.Public \| BindingFlags.Static)` | Wire-name resolver for the `RestEndpointOverrideBindingMode` enum. |
| `Cephalon.Behaviors.Http` | `Abstractions/BehaviorRestMethodExtensions.cs` | 54 | `typeof(BehaviorRestMethod).GetField(value.ToString(), BindingFlags.Public \| BindingFlags.Static)` | Wire-name resolver for the `BehaviorRestMethod` enum. Recorded against the `Cephalon.Behaviors.Http` `medium` package tier; the package's overall tier remains `medium` because of `BehaviorRestEndpointGroup`. |
| `Cephalon.Behaviors.Http` | `Abstractions/BehaviorRestBindingSourceExtensions.cs` | 54 | `typeof(BehaviorRestBindingSource).GetField(value.ToString(), BindingFlags.Public \| BindingFlags.Static)` | Wire-name resolver for the `BehaviorRestBindingSource` enum. Same package-tier note as above. |

The remediation is uniform across all nine sites: either annotate each enum type with `[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]`, or migrate the wire-name + parse helpers to a source-generated converter using `[JsonConverter(typeof(JsonStringEnumConverter<TEnum>))]` so the runtime reflection step disappears entirely.

## Clean-baseline (no observed hazards)

The remaining shipped `Cephalon.*` runtime packages — including (but not limited to) `Cephalon.Diagnostics`, `Cephalon.Resilience`, `Cephalon.AspNetCore`, `Cephalon.Worker`, `Cephalon.Eventing`, `Cephalon.Eventing.Wolverine`, `Cephalon.Agentics`, `Cephalon.Retrieval`, `Cephalon.Audit`, `Cephalon.Audit.EntityFramework`, `Cephalon.Identity`, `Cephalon.MultiTenancy`, the `Cephalon.MultiTenancy.Governance.*` companion family, the relational / non-relational `Cephalon.Data.*` *provider* family (the *core* `Cephalon.Data` package is now `medium` per the table above; `Cephalon.Data.MySql` is now `high` and `Cephalon.Data.Postgres` + `Cephalon.Data.Oracle` are now `medium` per `ENG-434`; the remaining per-provider packs stay clean-baseline at the source-code layer), the `Cephalon.Edge.*` family, the `Cephalon.Observability.*` family, `Cephalon.Cli`, and `Cephalon.Scaffolding` — show **no `Activator.CreateInstance`, `Type.GetType(string)`, `MakeGenericType`, or `Assembly.LoadFrom` use in their first-party source as of May 5, 2026**. `Cephalon.Abstractions` itself is now classified as `low` per the table above and is no longer pending a deeper-read classification.

This is a strong claim only at the *Cephalon source-code* layer. Every package in this tier still depends on transitive packages whose AOT posture must be validated through the harness's analyzer phase; provider SDKs (`AWSSDK.*`, `Azure.*`, `Google.*`, `Confluent.Kafka`, `Cassandra`, `Neo4j.Driver`, `MongoDB.Driver`, `Elastic.Clients.*`, `Microsoft.EntityFrameworkCore`, `Wolverine`, `Polly`) routinely emit `IL2026` / `IL3050` warnings. The `clean-baseline` label means *Cephalon code is not the bottleneck*; it does not mean the package can publish AOT today.

## Aggregate posture

| Tier | Packages | Effective per-package AOT verdict (today) |
| --- | --- | --- |
| `excluded-by-design` | 2 | not part of the deployment-mode story |
| `clean-baseline` | ~84 first-party packages (transitive validation pending) | `not-claimed`, blocked on harness + transitive validation |
| `low` | 1 (`Cephalon.Abstractions` — added via `ENG-435`; the original `Cephalon.Behaviors.Http` low-tier entry was reclassified to `medium` via `ENG-432` after a deeper read surfaced open-generic *method* dispatch alongside the original closed-generic *type* wrapper) | `not-claimed`; annotation alone is sufficient (`[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]` on the enum types, or migration to source-generated `[JsonStringEnumConverter<TEnum>]`) |
| `medium` | 14 (10 `Cephalon.EventSourcing.*` providers + `Cephalon.Behaviors.Http` + `Cephalon.Data` + `Cephalon.Data.Postgres` + `Cephalon.Data.Oracle`; the `Cephalon.Data.MySql` duck-typed fallback also fits this tier but the package's overall tier is `high` per below; the two `Cephalon.Behaviors.Http.Abstractions` enum-extension sites added via `ENG-435` are recorded against `Cephalon.Behaviors.Http`'s existing `medium` row, not as a separate package, because they are sub-`medium` evidence inside an already-`medium` package) | `not-claimed`; uniform remediation through compile-time event-type registry (EventSourcing), source-gen-emitted dispatch table (Behaviors.Http + Data), or a small `ICaptureFailureMetadata` interface plus typed-exception registry (CDC capture services) |
| `high` | 4 (`Cephalon.Engine`, `Cephalon.Behaviors`, `Cephalon.Behaviors.Patterns`, `Cephalon.Data.MySql`) | `not-claimed`; structural remediation required (source-gen-emitted module manifest + adapter table + behavior-type registry; for `Cephalon.Data.MySql`, an upstream `SciSharp.MySQL.Replication` API surface change or a fully public re-implementation of the binlog transport) |

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

**Update May 5, 2026 (`ENG-431`):** the manifest contract tests now drift-protect the inventory ↔ manifest seeding at the path-truth layer. Three new Pester `It` cases lift the suite from 28 → 31:

- every `representativePublishTargets.projects` entry must resolve to a `.csproj` file that exists on disk (a moved or renamed sample is no longer silently skipped by the harness's `Invoke-PublishProbe` default-targets path);
- every `deploymentModeEligibility.packages[].packageName` must resolve to `src/<packageName>/<packageName>.csproj` on disk (a renamed Cephalon runtime package can no longer leave a stale name in the per-package hazard list);
- every non-excluded package's first `knownHazards.site` must point at a `.cs` file that exists on disk (the trailing `:line` suffix is intentionally not validated because line numbers shift with unrelated edits, but a moved or renamed hazard file forces a manifest update in the same slice).

The harness publish surface, the deployment-mode claims (`not-claimed` for trim / nativeAot / singleFile), and the `representativePublishTargets.projects` value (still `samples/Cephalon.Sample.ModularMonolith/Cephalon.Sample.ModularMonolith.csproj` only) are unchanged by this slice; only the contract test layer is strengthened.

**Update May 5, 2026 (`ENG-434`):** the CDC capture services flagged as not-yet-classified by `ENG-433` have now been classified through a deeper read of the actual call sites, and three additional packages have been seeded into `scripts/deployment-mode-support.json` under `deploymentModeEligibility.packages`:

- `Cephalon.Data.MySql` is now `high` because `Services/MySqlBinlogTransport.cs` reflectively resolves five non-public methods, one non-public hierarchy method, and three non-public fields by string name on the third-party `SciSharp.MySQL.Replication.ReplicationClient` type (lines 1177 / 1184 / 1191), then invokes the resolved methods through `MethodInfo.Invoke` (line 1198); the same package's `Services/MySqlBinlogCaptureHostedService.cs` also carries the duck-typed exception-metadata fallback (lines 194 / 202) but the dominant blocker is the third-party non-public access. Remediation requires either an upstream API surface change in `SciSharp.MySQL.Replication` or a fully public re-implementation of the binlog transport.
- `Cephalon.Data.Postgres` is now `medium` because `Services/PostgresLogicalReplicationCaptureHostedService.cs` falls back to `exception.GetType().GetProperty("FailureKind", ...)` plus `"Metadata"` when the exception is not the typed `PostgresLogicalReplicationCaptureException` (lines 193 / 201). Remediation: a small `ICaptureFailureMetadata` interface plus a registry of additional exception types whose metadata should be projected, so the duck-typed fallback can be retired.
- `Cephalon.Data.Oracle` is now `medium` for the same reason (`Services/OracleLogMinerCaptureHostedService.cs` lines 194 / 202) and shares the same remediation path.

The aggregate posture table moves from `3 high + 12 medium + 0 low + 2 excluded-by-design = 17 entries` to `4 high + 14 medium + 0 low + 2 excluded-by-design = 20 entries`. The contract test suite at `tests/Cephalon.Tests.Scripts/deployment-mode-support-manifest.Tests.ps1` (31 tests after `ENG-431`) continues to pass against the seeded entries because the per-package shape (`packageName` / `nugetId` / `claimAuditTier` / `supportedModes` / `requiredProjectProperties` / `knownHazards.{kind,site,pattern,remediation}` / `evidence` / `introducedBy`), the path-truth assertions (`src/<packageName>/<packageName>.csproj` exists, first hazard `.cs` site exists), and the `claimAuditTier` enum are all preserved. The harness publish surface, the deployment-mode claims (`not-claimed` for trim / nativeAot / singleFile), and the `representativePublishTargets.projects` value are unchanged by this slice.

**Update May 5, 2026 (`ENG-435`):** the `Cephalon.Abstractions` enum-field lookups flagged as not-yet-classified by `ENG-433` have now been classified through a deeper read of the seven enum-extension call sites in `src/Cephalon.Abstractions/Transports/Rest*Extensions.cs`. The same pattern also covers the two enum-extension sites in `src/Cephalon.Behaviors.Http/Abstractions/BehaviorRest{Method,BindingSource}Extensions.cs:54`. All nine sites use the identical `typeof(TEnum).GetField(value.ToString(), BindingFlags.Public | BindingFlags.Static)` + `GetCustomAttribute<JsonStringEnumMemberNameAttribute>()` shape over a single closed enum type per call, so they fit the `low` tier (bounded reflection over a small known set of types; annotation alone is sufficient remediation). One new package has been seeded into `scripts/deployment-mode-support.json` under `deploymentModeEligibility.packages`:

- `Cephalon.Abstractions` is now `low` because the seven enum-extension sites listed in the new `low`-tier table are its only first-party reflection surface; no `Activator.CreateInstance`, `MakeGenericMethod`, `MethodInfo.Invoke`, `Type.GetType(string)`, or `Assembly.LoadFrom` use exists in its source as of May 5, 2026. Remediation is `[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]` on the enum types, or — the cleaner path — migration of the wire-name + parse helpers to `[JsonConverter(typeof(JsonStringEnumConverter<TEnum>))]` so the runtime reflection step disappears entirely.

The two `Cephalon.Behaviors.Http.Abstractions.BehaviorRest{Method,BindingSource}Extensions.cs:54` sites are appended to the existing `Cephalon.Behaviors.Http` package entry's `knownHazards` list rather than splitting the package into a separate row, because the package's overall tier remains `medium` (the dominant blocker is still the open-generic *method* dispatch in `BehaviorRestEndpointGroup`, classified by `ENG-432`).

The aggregate posture table moves from `4 high + 14 medium + 0 low + 2 excluded-by-design = 20 entries` (after `ENG-434`) to `4 high + 14 medium + 1 low + 2 excluded-by-design = 21 entries`. The contract test suite at `tests/Cephalon.Tests.Scripts/deployment-mode-support-manifest.Tests.ps1` (31 tests after `ENG-431`) continues to pass against the seeded entries because the per-package shape, the path-truth assertions (`src/<packageName>/<packageName>.csproj` exists, first hazard `.cs` site exists), and the `claimAuditTier` enum (which already includes `low`) are all preserved. The harness publish surface, the deployment-mode claims (`not-claimed` for trim / nativeAot / singleFile), and the `representativePublishTargets.projects` value are unchanged by this slice.

## Refresh discipline

This inventory is refreshed in the same slice that:

- introduces a new `Activator.CreateInstance`, `Type.GetType(string)`, `MakeGenericType`, or `Assembly.LoadFrom` call site in any `src/Cephalon.*` package, **or**
- removes / annotates one of the call sites listed above, **or**
- changes the project-property declarations in any `Cephalon.Analyzers` / `Cephalon.Behaviors.SourceGen` style excluded-by-design package, **or**
- ships a structural remediation (source-generator, compile-time registry) that retires one of the entries here.

A no-change month (no slice touched any of those surfaces) does not require an update; the doc records the state-as-of date in its first-paragraph review window.
