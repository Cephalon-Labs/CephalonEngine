# Cephalon.Behaviors.Patterns

`Cephalon.Behaviors.Patterns` is the M4 pattern execution layer of the Adaptive Behavior Topology (ABT).
It provides five built-in strategies that govern how a behavior invocation is dispatched, what
HTTP status code is returned, and how saga/process-manager state is persisted.

## What it owns

- **IBehaviorExecutionStrategy** — core contract: `Pattern` + `ExecuteAsync`
- **BehaviorExecutionContext** — per-invocation context (descriptor, instance, slot, input, behavior context)
- **BehaviorExecutionResult** — result envelope (output, HTTP status code, fire-and-forget flag)
- **ISagaStateStore / IProcessCheckpointStore** — persistence contracts
- **InMemorySagaStateStore** — `ConcurrentDictionary`-backed saga state with JSON serialization
- **InMemoryProcessCheckpointStore** — `ConcurrentDictionary`-backed checkpoint store
- **CqrsExecutionStrategy** — pattern: `"cqrs"`, returns 200 for queries / 202 for commands
- **EventDrivenExecutionStrategy** — pattern: `"event-driven"`, always 202, fire-and-forget
- **SagaExecutionStrategy** — pattern: `"saga-step"`, loads/saves saga state via `ISagaStateStore`
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
`IBehaviorContext.Metadata["correlation-id"]`. If the key is absent, the behavior descriptor ID is used.

## Replacing the default stores

Register your own `ISagaStateStore` or `IProcessCheckpointStore` after calling `AddBehaviorPatterns()`:

```csharp
behaviors.AddBehaviorPatterns();
builder.Services.AddSingleton<ISagaStateStore, MyDatabaseSagaStateStore>();
```

## Status

> Status: Released (ENG-058 M4)

## Related components

- `Cephalon.Behaviors` — dispatcher, catalog, resolver (M1)
- `Cephalon.Behaviors.Http` — HTTP transport bindings (M2)
- `Cephalon.Behaviors.Messaging` — messaging transport bindings (M3)
