# Cephalon.Eventing.Wolverine

> **Maturity:** `M3` · **Ownership:** `provider-managed` — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.Eventing.Wolverine` is an optional Wolverine companion package for Cephalon event-driven workloads.

## What it owns

- wires the optional `WolverineFx` runtime into a Cephalon host only when the app installs and registers this companion
- keeps Wolverine-specific host registration out of `Cephalon.Engine` and `Cephalon.Eventing`
- exposes a dedicated `wolverine-adapter` runtime surface under `event-driven-integration`
- publishes the `eventing.wolverine` capability so operators can see when the optional adapter choice is active
- defaults to `dispatchBridge = consumer-managed` when the pack is only being used for host wiring and runtime-surface projection
- can opt into a `wolverine-managed` durable dispatch loop on top of `IEventDispatchStore` by enabling `EnableDispatchLoop`
- schedules bounded fixed-delay dispatch retries through the shared dispatch-store eligibility contract and reports terminal `failed` state when `DispatchMaxAttempts` is exhausted
- can opt into `wolverine-managed` declared subscription execution on top of that staged-event dispatch loop by enabling `EnableSubscriptionExecution`
- keeps managed subscription ownership explicit by requiring declared `IEventSubscriptionExecutor` registrations and projecting binding metadata back through `IEventSubscriptionExecutionBindingCatalog` plus the shared eventing runtime surfaces
- keeps managed subscription readiness explicit through `IEventSubscriptionExecutionReadinessCatalog` and `event-subscriptions` metadata such as `executionReadiness`, `executionPath`, and `executionReadinessReasons`
- projects that managed loop back through hosted-execution, execution-graph, capability, and event-dispatch runtime surfaces without leaking Wolverine APIs into the core eventing contract
- schedules bounded fixed-delay managed subscription retries through Wolverine's own scheduled-message pipeline instead of inventing a second retry subsystem, and reports terminal `failed` state when `SubscriptionMaxAttempts` is exhausted
- contributes named dispatch-runtime descriptors that flow through `/engine/event-dispatch-runtimes`, `/engine/event-dispatches`, `/engine/event-dispatches/terminal-failures`, `/engine/outboxes`, and `snapshot.EventDispatchRuntimes` / `snapshot.EventDispatchStates`, with canonical aggregate summaries and terminal-failure counters projected back into those runtime descriptors
- contributes a dedicated diagnostics convention so `/engine/diagnostics` can advertise the Wolverine loop and managed-subscription event ids and message templates alongside the shared eventing diagnostics catalog
- the internal `WolverineDispatchInstrumentation` activity-source declared against the dedicated `Cephalon.Eventing.Wolverine.Dispatch` source (separate from `CephalonActivitySources.Eventing` because the dispatch loop is provider-specific); `WolverineEventDispatchHostedService` emits one `wolverine.dispatch` activity per attempted publish with stable Cephalon-prefix tags (`cephalon.message_id`, `cephalon.event_type`, `cephalon.channel_id`, `cephalon.dispatch_attempt`, plus `cephalon.correlation_id` and `cephalon.tenant_id` when carried on the staged item), all routed through the `RedactionPipeline` resolved from DI so consumer-registered redaction filters scrub Wolverine dispatch-emission attributes uniformly with the AspNetCore middleware, engine-runtime emission, Cephalon.Agentics dispatcher emission, Cephalon.Retrieval indexer / query emission, and Cephalon.Worker lifecycle emission sites; the same instrumentation publishes `cephalon.wolverine.dispatch.attempts` / `.successes` / `.failures` / `.retries` counters plus a `cephalon.wolverine.dispatch.duration` histogram. This is the third M1 redaction emission site shipped through `ENG-374` / `ENG-376`

## Main surfaces

- `Configuration/WolverineEventingOptions.cs`
- `Modules/WolverineEventingModule.cs`
- `Registration/WolverineEventingEngineBuilderExtensions.cs`
- `Services/WolverineEventDispatchHostedService.cs`
- `Services/WolverineDispatchInstrumentation.cs`
- `Services/WolverineEventingDiagnosticsConventionContributor.cs`
- `Services/WolverineEventingDispatchRuntimeContributor.cs`
- `Services/WolverineEventingRuntimeSurfaceContributor.cs`
- `Services/WolverineEventingRetryPolicy.cs`
- `Services/WolverineManagedEventSubscriptionExecutorCatalog.cs`
- `Services/WolverineManagedEventSubscriptionDispatcher.cs`
- `Services/WolverineManagedEventSubscriptionExecutionProcessor.cs`
- `Services/WolverineManagedEventSubscriptionExecutionRequest.cs`
- `Services/WolverineManagedEventSubscriptionExecutionHandler.cs`

## How it fits

This package is intentionally thin, but it is no longer just a passive host-wiring shim. Cephalon now has a shipped Wolverine companion path that can be selected and introspected without pushing Wolverine APIs into the engine core, and it can optionally own both the durable staged-event dispatch loop and one truthful managed subscription-execution lane when the app deliberately enables that behavior. Selecting `event-driven-integration` alone does not generate this package, `Engine:Messaging:Provider = Wolverine`, or `engine.AddWolverineEventing()`; those appear only for an explicit Wolverine provider/adoption choice.

Today the pack does three concrete things truthfully. First, it registers Wolverine host wiring and projects that choice back into Cephalon runtime introspection through `eventing.wolverine` and the `wolverine-adapter` surface. Second, when `EnableDispatchLoop` is turned on and a real `IEventDispatchStore` is available, it runs a hosted `wolverine-managed` dispatch pump that reads staged `EventPublication` payloads, publishes them through Wolverine, records durable dispatch outcomes, and reports execution/runtime metadata back through the shared eventing surfaces. `DispatchMaxAttempts` defaults to `3`, can be set to `1` to disable dispatch retries, and causes both no-destination and Wolverine `PublishAsync(...)` exception paths on the final exhausted attempt to report `failed` with `retryExhausted = true` / `terminalFailure = true` metadata instead of re-entering pending dispatch forever. Third, when `EnableSubscriptionExecution` is turned on, `EnableDispatchLoop` is already active, `EnableHostWiring` is left on, and at least one declared `IEventSubscriptionExecutor` is registered, the same staged-publication flow can trigger `wolverine-managed` execution of matching declared subscriptions with bounded fixed-delay retry scheduling through Wolverine's own scheduled-message pipeline. `SubscriptionMaxAttempts` defaults to `3`, can be set to `1` to disable requeue retries, and causes the final exhausted attempt to report `failed` with `retryExhausted = true` / `terminalFailure = true` metadata instead of silently requeueing forever. That bridge is no longer limited to the Entity Framework outbox baseline; the current MongoDB, Redis, Elasticsearch, OpenSearch, Neo4j, Qdrant, and NATS outbox packs now register the same runtime-neutral dispatch-store contract as well.

That runtime story is now visible in several complementary places:

- `wolverine-adapter` shows the adapter configuration plus aggregate dispatch state and managed-subscription state such as `managedSubscriptionCount`, retry-pending counts, and the latest observed outcome, all sourced from the canonical dispatch and subscription runtime summaries
- `/engine/event-dispatch-runtimes` publishes the named runtime descriptors such as the Wolverine-managed loop id, ownership metadata, the outbox/runtime ids it is responsible for, and a live aggregate `Summary` once dispatch reports start arriving, including terminal-failure observation counts, terminal outbox counts, and `HasTerminalFailures`
- `/engine/outboxes` now also shows the effective `DispatchPolicy` for each outbox, so the same managed loop is visible as `wolverine-managed` / `runtime-managed` without reading runtime-specific metadata by hand
- `event-dispatches` can now project configured dispatch-runtime descriptor metadata for each outbox path before any runtime report exists, then layer the latest operator-facing report metadata, `terminalFailure`, and `terminalFailureCount` on top once dispatch activity starts
- `/engine/event-dispatches/terminal-failures` filters the live dispatch-state catalog to outbox paths whose latest report is terminally failed, without claiming broker-specific dead-letter ownership
- dispatch retry metadata now reports `retryPolicy = bounded-fixed-delay`, `retryMaxAttempts`, `retryDelaySeconds`, `retryDurability = dispatch-store-delayed-eligibility`, `retryScope = provider-managed`, `routing = publish` or `routing = no-destinations`, and terminal failure posture when the Wolverine dispatch loop exhausts the configured attempt budget
- `IEventSubscriptionExecutionBindingCatalog` and `event-subscriptions` can now project `dispatchRuntime = wolverine-managed`, `subscriptionRuntime = runtime-bound`, `executionRuntimeId`, `executionOwnership`, `executionMode`, and `binding.*` metadata for the declared subscriptions this pack actually owns
- `IEventSubscriptionExecutionReadinessCatalog` and `event-subscriptions` can now answer that the same subscriptions are `runtime-bound` with an observed execution path and a `managed-binding-available` reason instead of forcing operators to infer readiness from adapter metadata
- retry metadata now reports `retryPolicy = bounded-fixed-delay`, `retryMaxAttempts`, `retryDelaySeconds`, `retryDurability = wolverine-scheduled-message`, `retryScope = provider-managed`, and terminal failure posture when a managed subscription exhausts the configured attempt budget
- `eventing.subscribe` now appears only when the managed subscription path is truly enabled, so capability discovery stops overstating what the pack owns
- `/engine/diagnostics` now exposes the Wolverine-specific diagnostics convention so host operators can discover the stable `4300-4306` event ids without reading the source

That means the package now has three truthful operating modes:

- baseline mode: Wolverine host wiring is active, while durable dispatch remains consumer-managed
- managed-loop mode: Wolverine owns the staged-event dispatch loop, reports `dispatchBridge = wolverine-managed`, and keeps failed dispatch retry bounded through terminal failure metadata
- managed-subscription mode: Wolverine owns execution for the declared subscriptions that have registered `IEventSubscriptionExecutor` implementations, with retry scheduling surfaced as part of the same runtime story

Those modes are optional companion behavior, not the definition of engine completeness. The engine side of that readiness lives in the runtime-neutral contracts and surfaces owned by `Cephalon.Eventing`; consumer apps can choose to install this companion, skip it, or later adopt a different companion pack without changing the core eventing contract itself.

The current managed-subscription baseline is intentionally narrow. It is triggered from the staged `EventPublication` dispatch flow rather than an arbitrary inbound broker-consumption path, it only owns subscriptions that have declared executors, and it currently uses bounded fixed-delay retry scheduling over Wolverine scheduled messages rather than a broader broker-owned retry vocabulary. The managed dispatch loop is also bounded on purpose: terminal `failed` dispatch observations stop the supported dispatch stores from returning that staged publication as pending again, but the package still does not claim broker-specific dead-letter queues, generic inbound consumption, or durable inbox ownership. The remaining work is therefore not to invent a first durable bridge from scratch, but to deepen observability, broader provider-native dispatch-store follow-through where a pack can truthfully persist dispatch outcomes, richer inbound-consumption stories when they are real, and later broker-specific follow-through on top of the same `IEventDispatchStore`, `IEventSubscriptionExecutor`, and `IEventSubscriptionRuntimeReporter` contract set. ClickHouse now stays explicit about the opposite answer: its outbox is visible to the runtime as `DispatchPolicy.PolicyId = unsupported` with `ExecutionMode = disabled`, so the Wolverine bridge and the shared eventing surfaces do not pretend the current analytics-first storage model already owns mutable dispatch state.

The showcase sample and test hosts can wire this package directly, so local and validation hosts can expose `/engine/event-dispatch-runtimes`, `/engine/event-dispatches`, `/engine/event-dispatches/terminal-failures`, `/engine/outboxes`, `event-subscriptions`, and matching `snapshot` answers without custom host-only code.

## Related docs

- [Cephalon.Eventing](eventing.md)
- [Cephalon.Data.EntityFramework](data-entityframework.md)
- [Technology packs](../technology-packs.md)
- [Operations](../operations.md)
