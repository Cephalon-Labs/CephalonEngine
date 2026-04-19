# Cephalon.Behaviors.Patterns

`Cephalon.Behaviors.Patterns` is the M4 pattern execution layer of the Adaptive Behavior Topology (ABT).
It provides built-in strategies that govern how a behavior invocation is dispatched, what
HTTP status code is returned, and how saga/process-manager state or choreography publications are handled.

## What it owns

- **IBehaviorExecutionStrategy** — core contract: `Pattern` + `ExecuteAsync`
- **BehaviorExecutionContext** — per-invocation context (descriptor, instance, slot, input, behavior context)
- **BehaviorExecutionResult** — result envelope (output, HTTP status code, fire-and-forget flag)
- **ISagaStateStore / IProcessCheckpointStore** — persistence contracts
- **ISagaChoreographyPublisher** — host-agnostic publication handoff contract for choreography steps
- **SagaChoreographyPublication / SagaChoreographyStepResult** — host-agnostic choreography output contracts
- **InMemorySagaStateStore** — `ConcurrentDictionary`-backed saga state with JSON serialization
- **InMemoryProcessCheckpointStore** — `ConcurrentDictionary`-backed checkpoint store
- **InMemorySagaChoreographyPublisher** — in-memory choreography publication collector for local development and tests
- **CqrsExecutionStrategy** — pattern: `"cqrs"`, returns 200 for queries / 202 for commands
- **EventDrivenExecutionStrategy** — pattern: `"event-driven"`, always 202, fire-and-forget
- **SagaExecutionStrategy** — pattern: `"saga-step"`, loads/saves saga state via `ISagaStateStore`
- **ChoreographySagaExecutionStrategy** — pattern: `"saga-choreography"`, stages returned choreography publications through `ISagaChoreographyPublisher`
- **ProcessManagerExecutionStrategy** — pattern: `"process-manager"`, checkpoint lifecycle management
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
tests, local development, and future bridge packages can all use the same execution contract.

## Replacing the default stores

Register your own `ISagaStateStore`, `IProcessCheckpointStore`, or `ISagaChoreographyPublisher`
after calling `AddBehaviorPatterns()`:

```csharp
behaviors.AddBehaviorPatterns();
builder.Services.AddSingleton<ISagaStateStore, MyDatabaseSagaStateStore>();
builder.Services.AddSingleton<ISagaChoreographyPublisher, MySagaChoreographyPublisher>();
```

## Status

> Status: ✅ Shipped — commit cc2ab0a · 575/575 tests

## Related components

- `Cephalon.Behaviors` — dispatcher, catalog, resolver (M1)
- `Cephalon.Behaviors.Http` — HTTP transport bindings (M2)
- `Cephalon.Behaviors.Messaging` — messaging transport bindings (M3)
- `Cephalon.Eventing` — future bridge target when choreography publications should map onto the
  shared outbox-backed eventing runtime instead of the default in-memory publisher
