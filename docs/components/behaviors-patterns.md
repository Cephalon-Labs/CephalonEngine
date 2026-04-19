# Cephalon.Behaviors.Patterns

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
- **SagaChoreographyPublication / SagaChoreographyStepResult** — host-agnostic choreography output contracts
- **IDurableExecution<TState> / IDurableExecution<TInput, TState, TOutput>** — host-agnostic durable workflow contract over `IEventStore` replay
- **DurableExecutionState<TState> / DurableExecutionStepResult<TOutput>** — replay snapshot and step-result contracts for durable execution
- **InMemorySagaStateStore** — `ConcurrentDictionary`-backed saga state with JSON serialization
- **InMemoryProcessCheckpointStore** — `ConcurrentDictionary`-backed checkpoint store
- **InMemorySagaChoreographyPublisher** — in-memory choreography publication collector for local development and tests
- **CqrsExecutionStrategy** — pattern: `"cqrs"`, returns 200 for queries / 202 for commands
- **EventDrivenExecutionStrategy** — pattern: `"event-driven"`, always 202, fire-and-forget
- **SagaExecutionStrategy** — pattern: `"saga-step"`, loads/saves saga state via `ISagaStateStore`
- **ChoreographySagaExecutionStrategy** — pattern: `"saga-choreography"`, stages returned choreography publications through `ISagaChoreographyPublisher`
- **ProcessManagerExecutionStrategy** — pattern: `"process-manager"`, checkpoint lifecycle management
- **DurableExecutionStrategy** — pattern: `"durable-execution"`, replays state from `IEventStore` and appends deterministic continuation events
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
| `durable-execution` | `DurableExecutionStrategy` | 200 | 202 when continuation events were appended, otherwise 204 | No |
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
- one `SagaChoreographyStepResult` when the step needs both local output and publications

The baseline stays host-agnostic on purpose. `Cephalon.Behaviors.Patterns` does not hard-depend on
`Cephalon.Eventing`; instead, it exposes `ISagaChoreographyPublisher` plus an in-memory default so
tests, local development, and explicit bridge packages such as `Cephalon.Eventing.Behaviors` can
all use the same execution contract.

## Durable execution contract

Durable workflows opt in explicitly through `IBehaviorTopologyBuilder.AsDurableExecution()` and the
`IDurableExecution<...>` contracts exported by `Cephalon.Behaviors.Patterns`.

- `ResolveStreamId(...)` keeps stream ownership explicit instead of deriving it from ambient host state
- `CreateInitialState()` seeds the replay state for new streams
- `DurableExecutionStrategy` replays current state from `IBehaviorContext.EventStore`, passes that
  snapshot to `ExecuteDurablyAsync(...)`, validates that returned events continue the stream with
  sequential versions, and appends them through `IEventStore.AppendAsync(...)`
- the strategy returns `200` when the step produced local output, `202` when it only staged
  continuation events, and `204` when the step completed without output

The durable baseline intentionally stays smaller than a full workflow engine. It does not add a
second journal or a transport-specific runner; it reuses the existing `IEventStore` contract so
HTTP, messaging, and tests can share the same replay truth. `Cephalon.Behaviors` also enforces
`ABT-006`, which requires `EventSourcingEnabled = true` whenever a behavior declares the
`durable-execution` pattern.

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

> Status: ✅ Shipped — M4 baseline plus later follow-through for saga choreography and durable execution

## Related components

- `Cephalon.Behaviors` — dispatcher, catalog, resolver (M1)
- `Cephalon.Behaviors.Http` — HTTP transport bindings (M2)
- `Cephalon.Behaviors.Messaging` — messaging transport bindings (M3)
- `Cephalon.Eventing.Behaviors` — explicit saga choreography bridge into the shared outbox-backed
  eventing publish path
- `Cephalon.Eventing` — shared event-driven publication/runtime baseline consumed by the bridge
