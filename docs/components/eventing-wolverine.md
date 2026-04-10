# Cephalon.Eventing.Wolverine

`Cephalon.Eventing.Wolverine` is the official first-class Wolverine companion package for Cephalon event-driven workloads.

## What it owns

- wires the official `WolverineFx` runtime into a Cephalon host when `EventDrivenIntegration` is active
- keeps Wolverine-specific host registration out of `Cephalon.Engine` and `Cephalon.Eventing`
- exposes a dedicated `wolverine-adapter` runtime surface under `event-driven-integration`
- publishes the `eventing.wolverine` capability so operators can see when the official adapter path is active
- defaults to `dispatchBridge = consumer-managed` when the pack is only being used for host wiring and runtime-surface projection
- can opt into a `wolverine-managed` durable dispatch loop on top of `IEventDispatchStore` by enabling `EnableDispatchLoop`
- projects that managed loop back through hosted-execution, execution-graph, capability, and event-dispatch runtime surfaces without leaking Wolverine APIs into the core eventing contract
- contributes named dispatch-runtime descriptors that flow through `/engine/event-dispatch-runtimes`, `/engine/event-dispatches`, `/engine/outboxes`, and `snapshot.EventDispatchRuntimes` / `snapshot.EventDispatchStates`, with canonical aggregate summaries projected back into those runtime descriptors
- contributes a dedicated diagnostics convention so `/engine/diagnostics` can advertise the Wolverine loop event ids and message templates alongside the shared eventing diagnostics catalog

## Main surfaces

- `Configuration/WolverineEventingOptions.cs`
- `Modules/WolverineEventingModule.cs`
- `Registration/WolverineEventingEngineBuilderExtensions.cs`
- `Services/WolverineEventDispatchHostedService.cs`
- `Services/WolverineEventingDiagnosticsConventionContributor.cs`
- `Services/WolverineEventingDispatchRuntimeContributor.cs`
- `Services/WolverineEventingRuntimeSurfaceContributor.cs`

## How it fits

This package is intentionally thin, but it is no longer just a passive host-wiring shim. Cephalon now has an official Wolverine path that can be selected and introspected without pushing Wolverine APIs into the engine core, and it can optionally own the durable staged-event dispatch loop when the app deliberately enables that behavior.

Today the pack does two concrete things truthfully. First, it registers Wolverine host wiring and projects that choice back into Cephalon runtime introspection through `eventing.wolverine` and the `wolverine-adapter` surface. Second, when `EnableDispatchLoop` is turned on and a real `IEventDispatchStore` is available, it runs a hosted `wolverine-managed` dispatch pump that reads staged `EventPublication` payloads, publishes them through Wolverine, records durable dispatch outcomes, and reports execution/runtime metadata back through the shared eventing surfaces. That bridge is no longer limited to the Entity Framework outbox baseline; the current MongoDB, Redis, Elasticsearch, OpenSearch, Neo4j, Qdrant, and NATS outbox packs now register the same runtime-neutral dispatch-store contract as well.

That runtime story is now visible in three complementary places:

- `wolverine-adapter` shows the adapter configuration plus aggregate dispatch state such as total reports, retry-pending count, and the latest observed outcome/error, all sourced from the canonical dispatch-runtime summary
- `/engine/event-dispatch-runtimes` publishes the named runtime descriptors such as the Wolverine-managed loop id, ownership metadata, the outbox/runtime ids it is responsible for, and a live aggregate `Summary` once dispatch reports start arriving
- `/engine/outboxes` now also shows the effective `DispatchPolicy` for each outbox, so the same managed loop is visible as `wolverine-managed` / `runtime-managed` without reading runtime-specific metadata by hand
- `event-dispatches` can now project configured dispatch-runtime descriptor metadata for each outbox path before any runtime report exists, then layer the latest operator-facing report metadata on top once dispatch activity starts
- `/engine/diagnostics` now exposes the Wolverine-specific loop diagnostics convention so host operators can discover the stable `4300-4303` event ids without reading the source

That means the package now has two truthful operating modes:

- baseline mode: Wolverine host wiring is active, while durable dispatch remains consumer-managed
- managed-loop mode: Wolverine owns the staged-event dispatch loop and reports `dispatchBridge = wolverine-managed`

The remaining work is therefore not to invent a first durable bridge from scratch, but to deepen observability, richer retry/runtime answers, broader provider-native dispatch-store follow-through where a pack can truthfully persist dispatch outcomes, and later broker-specific follow-through on top of the same `IEventDispatchStore` and `IEventDispatchRuntimeReporter` contract set. The current deliberate provider gaps are Cassandra and ClickHouse, where storage-model truth is still more important than parity for its own sake.

The showcase sample now wires this package directly, so local and test hosts can expose `/engine/event-dispatch-runtimes`, `/engine/event-dispatches`, `/engine/outboxes`, and matching `snapshot.EventDispatchRuntimes` / `snapshot.EventDispatchStates` answers without custom host-only code.

## Related docs

- [Cephalon.Eventing](eventing.md)
- [Cephalon.Data.EntityFramework](data-entityframework.md)
- [Technology packs](../technology-packs.md)
- [Operations](../operations.md)
