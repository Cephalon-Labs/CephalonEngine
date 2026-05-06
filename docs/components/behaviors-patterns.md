# Cephalon.Behaviors.Patterns

> **Maturity:** `M1` · **Ownership:** `application-managed` — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.Behaviors.Patterns` is the M4 pattern execution layer of the Adaptive Behavior Topology (ABT).
It provides built-in strategies that govern how a behavior invocation is dispatched, what
HTTP status code is returned, and how saga/process-manager state, choreography publications, or
durable-execution replay are handled.

## What it owns

- **IBehaviorExecutionStrategy** — core contract: `Pattern` + `ExecuteAsync`
- **BehaviorExecutionContext** — per-invocation context (descriptor, instance, slot, input, behavior context)
- **BehaviorExecutionResult** — result envelope (output, HTTP status code, fire-and-forget flag)
- **ISagaStateStore / IProcessCheckpointStore** — persistence contracts
- **ISagaChoreographyPublisher** — host-agnostic publication handoff contract for choreography steps
- **ISagaChoreographyStepResult / SagaChoreographyStepResult / SagaChoreographyStepResult<TOutput>** — host-agnostic choreography output contracts for shared output-plus-publication normalization
- **ISagaEventReactor<TEvent> / ISagaEventReactor<TEvent, TOutput>** — higher-level choreography authoring helpers that still compile down to the shared behavior contract
- **SagaChoreographyPublication** — host-agnostic choreography publication contract plus typed JSON publication helpers
- **SagaChoreographyRuntimeDescriptor / ISagaChoreographyRuntimeCatalog** — operator-facing choreography catalog derived from shared behavior topology, implementation type, ownership, transports, feature flags, and result-shape metadata
- **IDurableExecution<TState> / IDurableExecution<TInput, TState, TOutput>** — host-agnostic durable workflow contract over `IEventStore` replay
- **DurableExecutionState<TState> / DurableExecutionStepResult<TOutput>** — replay snapshot and step-result contracts for durable execution
- **DurableExecutionPendingTimer / DurableExecutionPendingSignal** — host-agnostic coordination descriptors for pending durable timer and signal waits
- **DurableExecutionCompensationAction** — host-agnostic operator-facing compensation descriptor for durable workflow recovery guidance
- **DurableExecutionSlot** — closed-generic durable adapter metadata used by the strategy and runtime catalog; emitted by source generation for generated behaviors and available for explicit host/module registration
- **DurableExecutionRuntimeDescriptor / IDurableExecutionRuntimeCatalog** — operator-facing durable workflow catalog derived from shared behavior topology, ownership, transports, feature flags, and replay metadata
- **DurableExecutionRuntimeState / IDurableExecutionRuntimeStateCatalog** — operator-facing per-stream durable runtime posture, including last outcome, stage, version progress, append count, pending timers/signals, available compensation actions, completion state, and failure summary
- **InMemorySagaStateStore** — `ConcurrentDictionary`-backed saga state with JSON serialization
- **InMemoryProcessCheckpointStore** — `ConcurrentDictionary`-backed checkpoint store
- **InMemorySagaChoreographyPublisher** — in-memory choreography publication collector for local development and tests
- **CqrsExecutionStrategy** — pattern: `"cqrs"`, returns 200 for queries / 202 for commands
- **EventDrivenExecutionStrategy** — pattern: `"event-driven"`, always 202, fire-and-forget
- **SagaExecutionStrategy** — pattern: `"saga-step"`, loads/saves saga state via `ISagaStateStore`
- **ChoreographySagaExecutionStrategy** — pattern: `"saga-choreography"`, stages returned choreography publications through `ISagaChoreographyPublisher`
- **ProcessManagerExecutionStrategy** — pattern: `"process-manager"`, checkpoint lifecycle management
- **DurableExecutionStrategy** — pattern: `"durable-execution"`, replays state from `IEventStore` and appends deterministic continuation events
- **DurableExecutionRuntimeCatalogSnapshot** — runtime projection used by `IDurableExecutionRuntimeCatalog`, `/engine/durable-executions`, and `snapshot.DurableExecutions`
- **SagaChoreographyRuntimeCatalogSnapshot** — runtime projection used by `ISagaChoreographyRuntimeCatalog`, `/engine/saga-choreographies`, and `snapshot.SagaChoreographies`
- **SagaChoreographyRuntimeSlot** — closed generic metadata slot used by source generation or explicit host wiring so choreography catalog projection does not inspect runtime interface shape
- **DirectExecutionStrategy** — pattern: `"direct"`, 200 with output / 204 with null
- **ExecutionStrategyRegistry** — `FrozenDictionary` O(1) registry for all strategies
- **Hosting** — `AddBehaviorPatterns()` extension on `IBehaviorCollectionBuilder`

## Pattern identifiers

| Pattern | Strategy | HTTP (output) | HTTP (null) | Fire-and-forget |
|---|---|---|---|---|
| `cqrs` | `CqrsExecutionStrategy` | 200 | 202 | No |
| `event-driven` | `EventDrivenExecutionStrategy` | 202 | 202 | Yes |
| `saga-step` | `SagaExecutionStrategy` | 200 | 200 | No |
| `saga-choreography` | `ChoreographySagaExecutionStrategy` | 202 when publications exist, otherwise 200 | 204 | No |
| `process-manager` | `ProcessManagerExecutionStrategy` | 200 | 200 | No |
| `durable-execution` | `DurableExecutionStrategy` | 200 | 202 when continuation events or pending timer/signal coordination remain, otherwise 204 | No |
| `direct` | `DirectExecutionStrategy` | 200 | 204 | No |

## Registration

```csharp
services.AddCephalon(config, engine => engine
    .AddBehaviors(behaviors => behaviors
        .AddBehaviorPatterns()
    )
);
```

## Saga state correlation

The saga and process-manager strategies read the saga/process identifier from
`IBehaviorContext.CorrelationId`. `SagaExecutionStrategy` generates a new GUID when the correlation
id is absent, while `ProcessManagerExecutionStrategy` rejects the invocation because checkpoints
must stay bound to an explicit process identifier.

`ChoreographySagaExecutionStrategy` also uses `IBehaviorContext.CorrelationId` as the default
publication correlation id when a returned `SagaChoreographyPublication` omits it, and it also
fills `TenantId` from behavior metadata (`TenantId`, `tenantId`, or `tenant-id`) when available.

## Choreography output contract

Choreography-based saga steps can return:

- one `SagaChoreographyPublication`
- a sequence of `SagaChoreographyPublication`
- one `SagaChoreographyStepResult`
- one `SagaChoreographyStepResult<TOutput>`
- any `ISagaChoreographyStepResult` implementation when the step needs both local output and
  publications but wants to preserve a custom output contract

Authors who want a lower-ceremony path can implement `ISagaEventReactor<TEvent>` or
`ISagaEventReactor<TEvent, TOutput>` instead of handling the choreography result-shaping
themselves. Those helpers still ride the same `IAppBehavior<,>` topology and the same
`ChoreographySagaExecutionStrategy`, so runtime truth, module ownership, transport projections, and
the existing `200` / `202` / `204` semantics stay unchanged.

`SagaChoreographyPublication.CreateJson(...)` and
`SagaChoreographyPublication.CreateCompensationJson(...)` also let choreography steps publish typed
JSON payloads without re-implementing `JsonSerializerDefaults.Web` handling or hard-depending on
the eventing companion pack.

The baseline stays host-agnostic on purpose. `Cephalon.Behaviors.Patterns` does not hard-depend on
`Cephalon.Eventing`; instead, it exposes `ISagaChoreographyPublisher` plus an in-memory default so
tests, local development, and explicit bridge packages such as `Cephalon.Eventing.Behaviors` can
all use the same execution contract.

That same shared topology now also drives the first choreography operator surface.
`AddBehaviorPatterns()` registers `ISagaChoreographyRuntimeCatalog`,
`SagaChoreographyRuntimeCatalogSnapshot` derives one static descriptor per active choreography
behavior from `IBehaviorCatalog`, `IBehaviorTypeRegistry`, and registered
`SagaChoreographyRuntimeSlot` metadata. `Cephalon.Engine` projects the same answer through
`snapshot.SagaChoreographies`, and ASP.NET Core exposes `/engine/saga-choreographies` plus
id/module/transport drill-down routes. That runtime answer preserves module ownership, transport ids,
required feature ids, typed input/result/local-output metadata, and authoring-model or
publication-shape classification without pretending to be downstream publish or broker-dispatch
truth. Source-generated choreography behaviors register closed
`SagaChoreographyRuntimeSlot.For<TBehavior, TInput, TResult>(...)` metadata automatically when the
pattern runtime slot is referenced; manually wired hosts must register that same slot explicitly when
`AutoRegister=false` or custom choreography result shapes need explicit metadata. Missing slots fail
fast instead of falling back to runtime interface-shape reflection.

The same package now also owns the first live choreography publication-state follow-through.
`AddBehaviorPatterns()` registers `ISagaChoreographyPublicationRuntimeStateCatalog`,
`ChoreographySagaExecutionStrategy` reports `accepted` and `failed` handoff observations into that
shared state catalog, `Cephalon.Engine` projects the same answer through
`snapshot.SagaChoreographyPublicationStates`, and ASP.NET Core exposes
`/engine/saga-choreographies/runtime` plus publication, behavior, module, transport, channel,
correlation, compensation, and failure drill-down routes. That runtime-state answer keeps
publication ownership on the shared choreography execution path and reports only the latest handoff
observations, so the optional `Cephalon.Eventing.Behaviors` bridge and downstream event-dispatch
runtime can remain additive truth instead of being collapsed into one choreography registry.

That same `AddBehaviorPatterns()` activation now also lets the owning `Cephalon.Behaviors` module
publish `behaviors.saga-choreography.runtime-catalog` and
`behaviors.saga-choreography.publication-state` through the runtime manifest and
`/engine/capabilities`. Those entries appear only when the shared choreography runtime services are
actually registered, and their metadata points back to the pattern pack, service contract,
snapshot field, and ASP.NET Core route so operators can distinguish static choreography ownership,
live publication-state observations, and the separate `eventing.behaviors.saga-choreography`
bridge truth.

## Durable execution contract

Durable workflows opt in explicitly through `IBehaviorTopologyBuilder.AsDurableExecution()` and the
`IDurableExecution<...>` contracts exported by `Cephalon.Behaviors.Patterns`.

- `ResolveStreamId(...)` keeps stream ownership explicit instead of deriving it from ambient host state
- `CreateInitialState()` seeds the replay state for new streams
- `DurableExecutionStepResult<TOutput>` can now also carry `pendingTimers`, `pendingSignals`, and
  `compensationActions` so workflows can publish coordination and operator-facing recovery guidance
  through the shared durable runtime state
- `DurableExecutionStrategy` replays current state from `IBehaviorContext.EventStore`, passes that
  snapshot to `ExecuteDurablyAsync(...)`, validates that returned events continue the stream with
  sequential versions, and appends them through `IEventStore.AppendAsync(...)`
- source-generated durable behaviors register closed `DurableExecutionSlot.For<TBehavior, TInput, TState, TOutput>()`
  adapters; manually wired durable workflows must register the same slot shape explicitly before
  `DurableExecutionStrategy` or `IDurableExecutionRuntimeCatalog` can execute/project that workflow
- durable execution no longer materializes missing slots through runtime open-generic reflection; missing slot
  registrations fail fast with guidance to rebuild with `Cephalon.Behaviors.SourceGen` or register
  `DurableExecutionSlot.For<TBehavior, TInput, TState, TOutput>()`
- the strategy returns `200` when the step produced local output, `202` when it staged
  continuation events or is still waiting on durable timers/signals without local output, and `204`
  when the step completed without output

The durable baseline intentionally stays smaller than a full workflow engine. It does not add a
second journal or a transport-specific runner; it reuses the existing `IEventStore` contract so
HTTP, messaging, and tests can share the same replay truth. `Cephalon.Behaviors` also enforces
`ABT-006`, which requires `EventSourcingEnabled = true` whenever a behavior declares the
`durable-execution` pattern.

That same shared topology now also drives the durable operator surface. `AddBehaviorPatterns()`
registers `IDurableExecutionRuntimeCatalog` plus `IDurableExecutionRuntimeStateCatalog`,
`DurableExecutionStrategy` reports per-stream `started`, `succeeded`, `continuation-staged`,
`waiting`, `completed`, and `failed` observations into the shared state catalog, `Cephalon.Engine`
projects both `snapshot.DurableExecutions` and `snapshot.DurableExecutionStates`, and ASP.NET
Core exposes `/engine/durable-executions` plus `/engine/durable-executions/runtime`,
`/engine/durable-executions/runtime/streams/{streamId}`,
`/engine/durable-executions/runtime/behaviors/{behaviorId}`,
`/engine/durable-executions/runtime/modules/{moduleId}`, and
`/engine/durable-executions/runtime/transports/{transportId}` together with the pending
coordination filters `/engine/durable-executions/runtime/timers`,
`/engine/durable-executions/runtime/timers/{timerId}`,
`/engine/durable-executions/runtime/signals`, and
`/engine/durable-executions/runtime/signals/{signalId}` plus the compensation filters
`/engine/durable-executions/runtime/compensations` and
`/engine/durable-executions/runtime/compensations/{compensationId}`. Those runtime surfaces
preserve module ownership, transport ids, required feature ids, typed input/state/output
contracts, the shared `200`/`202`/`204` success posture, replay/version progress, append counts,
pending timer/signal coordination, operator-facing compensation guidance, and durable failure
posture without inventing a second host-specific workflow registry.

## Replacing the default stores

Register your own `ISagaStateStore`, `IProcessCheckpointStore`, or `ISagaChoreographyPublisher`
when the defaults are not enough. `AddBehaviorPatterns()` now uses fallback registrations, so
explicit replacements can safely be registered either before or after the built-in pattern pack:

```csharp
behaviors.AddBehaviorPatterns();
builder.Services.AddSingleton<ISagaStateStore, MyDatabaseSagaStateStore>();
builder.Services.AddSingleton<ISagaChoreographyPublisher, MySagaChoreographyPublisher>();
```

If the app already uses `Cephalon.Eventing` with a durable outbox-backed publish path, the
preferred low-ceremony bridge is the explicit companion pack:

```csharp
services.AddCephalon(config, engine => engine
    .AddBehaviors(behaviors => behaviors.AddBehaviorPatterns())
    .AddEventing(eventing => eventing.AddChannel("catalog-events", "Catalog Events"))
    .AddBehaviorEventingBridge());
```

That bridge preserves explicit `ISagaChoreographyPublisher` overrides instead of replacing them,
and it only activates when the shared `Cephalon.Eventing` publication path is truthful.

## Status

> Status: ✅ Shipped — M4 baseline plus later follow-through for saga choreography, the first saga choreography runtime catalog/operator surface, the first live saga choreography publication-state surface, the module-backed saga choreography capability-publication follow-through, higher-level saga choreography authoring helpers, durable execution, the first durable runtime catalog/operator surface, the first durable per-stream live-state/failure-posture surface, the first durable timer/signal coordination surface, and the first durable compensation-helper surface

## Related components

- `Cephalon.Behaviors` — dispatcher, catalog, resolver (M1)
- `Cephalon.Behaviors.Http` — HTTP transport bindings (M2)
- `Cephalon.Behaviors.Messaging` — messaging transport bindings (M3)
- `Cephalon.Eventing.Behaviors` — explicit saga choreography bridge into the shared outbox-backed
  eventing publish path
- `Cephalon.Eventing` — shared event-driven publication/runtime baseline consumed by the bridge
