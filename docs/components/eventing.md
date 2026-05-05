# Cephalon.Eventing

> **Maturity:** `M3` · **Ownership:** mixed: `application-managed` + `cephalon-managed` — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.Eventing` is the baseline technology pack for event-driven integration.

## What it owns

- eventing options
- module and registration entry points for the `EventDrivenIntegration` technology
- channel descriptors, registries, and catalogs
- declared subscription descriptors, registries, and catalogs
- host-agnostic managed-subscription execution contracts and execution-binding vocabulary that truthful companion packs can project back into runtime introspection
- a public `IEventSubscriptionExecutionBindingCatalog` read contract for active managed subscription bindings contributed by companion packs
- an implementation of the abstraction-level `IEventSubscriptionExecutionReadinessCatalog` read contract that tells hosts and tooling whether each declared subscription is `runtime-bound`, `hosted-execution-linked`, `application-managed-state`, or `declared-only`
- stable `EventSubscriptionRuntimeMetadataKeys` constants for ownership and runtime-state fields projected through the `event-subscriptions` technology surface
- stable `EventDispatchRuntimeMetadataKeys` constants for dispatch retry, retry exhaustion, and terminal failure fields projected through dispatch runtime reports and event-dispatch surfaces
- an opt-in Cephalon-managed in-process subscription execution baseline through `EventingOptions.EnableInProcessSubscriptionExecution`, `IEventPublisher`, and registered `IEventSubscriptionExecutor` services
- bounded process-local retry for that in-process lane through `EventingOptions.InProcessSubscriptionMaxAttempts` and `InProcessSubscriptionRetryDelayMilliseconds`, with `retry-scheduled` observations and runtime metadata when enabled
- bounded process-local duplicate suppression for completed in-process subscription executions through `EventingOptions.EnableInProcessSubscriptionIdempotency` and `InProcessSubscriptionIdempotencyRetentionMinutes`, with `skipped` observations and non-durable idempotency metadata when enabled
- an implementation of the abstraction-level `IEventPublicationDispatcher` command seam so host adapters can request one bounded event publication without referencing `Cephalon.Eventing` implementation types
- an implementation of the abstraction-level `IEventPublicationRuntimeCatalog` read contract so operators can inspect latest event-publication runtime state without referencing `Cephalon.Eventing` implementation types
- an optional staged publication contract through `IEventPublisher` and `EventPublication`
- application-managed dispatch runtime reporting for outbox-backed publication paths, including first-class retry-pending and terminal-failure posture on the abstraction-level state/summary contracts
- additive runtime descriptor registration for named dispatch runtimes that can be consumed through the host-agnostic `Cephalon.Abstractions/Data/*` event-dispatch contracts
- additive outbox dispatch-policy resolution so `/engine/outboxes` and runtime surfaces can distinguish `disabled`, `consumer-managed`, and runtime-managed execution ownership truthfully
- terminal dispatch-failure metadata plus typed `EventDispatchRuntimeState.TerminalFailure`, `TerminalFailureCount`, `EventDispatchRuntimeSummary.TerminalFailureCount`, `TerminalOutboxCount`, and `HasTerminalFailures` answers so dispatch stores and operators can stop poison staged publications from re-entering pending-dispatch reads when a runtime exhausts its retry budget
- baseline runtime-surface contributions for channel introspection, declared subscription introspection, truthful publication-path introspection, live-enriched dispatch-runtime introspection, live dispatch-state introspection, and managed-versus-application-owned subscription execution linkage
- the public `EventingDiagnostics` OpenTelemetry adapter declared against `CephalonActivitySources.Eventing` and `CephalonMeters.Eventing`; the in-process publisher emits one `eventing.publication.dispatch` activity per `IEventPublisher.PublishAsync` call with stable Cephalon-prefix tags (`cephalon.eventing.publisher.id`, `cephalon.eventing.publication.id`, `cephalon.eventing.channel.id`, `cephalon.eventing.event.type`, `cephalon.eventing.publication.outcome`, `cephalon.eventing.matched_subscription.count`) plus a `cephalon.eventing.publications` counter, all routed through the `RedactionPipeline` resolved from DI so consumer-registered redaction filters scrub publisher-emission attributes uniformly with the AspNetCore middleware and engine-runtime emission sites
- a reusable staged-publication seam that explicit bridge packs can hand off into without making `Cephalon.Eventing` own the upstream authoring contract
- the technology bucket that companion packs can extend with additional event-runtime truth such as outbox producers, inbox stores, or richer dispatch/subscription state

## Main surfaces

- `Configuration/EventingOptions.cs`
- `Modules/EventingModule.cs`
- `Registration/EventingEngineBuilderExtensions.cs`
- `Services/EventChannelDescriptor.cs`
- `Services/EventChannelRegistry.cs`
- `Services/EventChannelCatalog.cs`
- `Services/IEventChannelContributor.cs`
- `Services/IEventChannelCatalog.cs`
- `Services/EventSubscriptionDescriptor.cs`
- `Services/EventSubscriptionExecutionContext.cs`
- `Services/EventSubscriptionExecutionBindingDescriptor.cs`
- `Services/EventSubscriptionExecutionOutcomes.cs`
- `Services/EventSubscriptionExecutionReport.cs`
- `Services/EventSubscriptionRuntimeMetadataKeys.cs`
- `Services/EventSubscriptionRuntimeState.cs`
- `Services/EventDispatchExecutionOutcomes.cs`
- `Services/EventDispatchItem.cs`
- `Services/EventDispatchExecutionReport.cs`
- `Services/EventDispatchRuntimeMetadataKeys.cs`
- `Services/IEventSubscriptionContributor.cs`
- `Services/IEventSubscriptionCatalog.cs`
- `Services/IEventSubscriptionExecutor.cs`
- `Services/IEventSubscriptionExecutionBindingCatalog.cs`
- `Services/IEventSubscriptionExecutionBindingContributor.cs`
- `Services/IEventSubscriptionRuntimeCatalog.cs`
- `Services/IEventSubscriptionRuntimeReporter.cs`
- `Services/IEventDispatchRuntimeReporter.cs`
- `Services/IEventDispatchStore.cs`
- `Services/EventPublication.cs`
- `Services/EventPublicationDispatcher.cs`
- `Services/IEventPublisher.cs`
- `Services/OutboxBackedEventPublisher.cs`
- `Services/OutboxDispatchPolicyCatalog.cs`
- `Services/EventDispatchRuntimeCatalog.cs`
- `Services/EventDispatchRuntimeDescriptorCatalog.cs`
- `Services/EventingRuntimeSurfaceContributor.cs`
- `Services/EventingDispatchRuntimeSurfaceContributor.cs`
- `Services/EventingSubscriptionRuntimeSurfaceContributor.cs`
- `Services/EventingPublishingRuntimeSurfaceContributor.cs`
- `Services/EventPublicationRuntimeCatalog.cs`
- `Services/EventPublicationRuntimeReport.cs`
- `Services/IEventPublicationRuntimeReporter.cs`
- `Services/InProcessEventSubscriptionExecutorCatalog.cs`
- `Services/InProcessEventingRetryPolicy.cs`
- `Services/InProcessEventingIdempotencyPolicy.cs`
- `Services/InProcessEventSubscriptionIdempotencyTracker.cs`
- `Services/InProcessEventPublisher.cs`
- `Services/EventingDiagnostics.cs`
- `Services/EventingInProcessPublishingRuntimeSurfaceContributor.cs`
- host-agnostic read and action contracts in `Cephalon.Abstractions/Data/EventDispatchRuntimeDescriptor.cs`, `Cephalon.Abstractions/Data/EventDispatchRuntimeSummary.cs`, `Cephalon.Abstractions/Data/EventDispatchRuntimeState.cs`, `Cephalon.Abstractions/Data/IEventDispatchRuntimeCatalog.cs`, `Cephalon.Abstractions/Data/IEventDispatchRuntimeDescriptorCatalog.cs`, `Cephalon.Abstractions/Data/EventPublicationOutcomes.cs`, `Cephalon.Abstractions/Data/EventPublicationRequest.cs`, `Cephalon.Abstractions/Data/EventPublicationResult.cs`, `Cephalon.Abstractions/Data/IEventPublicationDispatcher.cs`, `Cephalon.Abstractions/Data/EventPublicationRuntimeOutcomes.cs`, `Cephalon.Abstractions/Data/EventPublicationRuntimeState.cs`, `Cephalon.Abstractions/Data/IEventPublicationRuntimeCatalog.cs`, `Cephalon.Abstractions/Data/EventSubscriptionExecutionReadinessDescriptor.cs`, `Cephalon.Abstractions/Data/EventSubscriptionExecutionReadinessStates.cs`, `Cephalon.Abstractions/Data/IEventSubscriptionExecutionReadinessCatalog.cs`, `Cephalon.Abstractions/Data/OutboxDispatchPolicyDescriptor.cs`, and `Cephalon.Abstractions/Data/IOutboxDispatchPolicyCatalog.cs`

## Source structure

- `Configuration`
- `Modules`
- `Registration`
- `Services`

## How it fits

This pack keeps event-driven primitives out of the engine core while still making them discoverable, selectable, and introspectable through the shared technology model.

Today that baseline is still deliberately modest and intentionally truthful. `Cephalon.Eventing` owns channel descriptors plus the `event-channels` runtime surface, it exposes declared subscription descriptors through `event-subscriptions` and the `eventing.subscriptions` capability, it lets application-managed handlers report `started`, `succeeded`, `failed`, `retry-scheduled`, and `skipped` observations through `IEventSubscriptionRuntimeReporter`, it now exposes host-agnostic `IEventSubscriptionExecutor` plus `IEventSubscriptionExecutionBindingContributor` contracts so either the core pack or a companion can bind declared subscriptions to one real managed runtime without leaking adapter APIs into hosts, and it exposes the resulting bindings through `IEventSubscriptionExecutionBindingCatalog` so hosts and packages can inspect the active execution owner without parsing runtime-surface metadata. The implementation of abstraction-level `IEventSubscriptionExecutionReadinessCatalog` answers whether each declared subscription is runtime-bound, linked to a hosted execution, only observed through application-managed reports, or still declared-only with no execution path observed. Because the readiness descriptor and interface live in `Cephalon.Abstractions.Data`, `Cephalon.Engine` and host adapters can project the same answer through `/engine/event-subscription-readiness` and `snapshot.EventSubscriptionExecutionReadiness` without taking a dependency on the eventing pack. The pack now also implements the abstraction-level `IEventPublicationDispatcher`, so ASP.NET Core and other host adapters can request one bounded publication through `/engine/event-publications` or equivalent host surfaces without taking a dependency on `EventPublication` or `IEventPublisher` concrete package types. That dispatcher delegates to the active `IEventPublisher`, so the same action seam works for the opt-in direct in-process lane and the staged outbox-backed lane while keeping the chosen runtime owner explicit in capability and technology-surface metadata. The pack now also implements the abstraction-level `IEventPublicationRuntimeCatalog`, so the same publication path reports latest publication state through `/engine/event-publications/runtime`, `/engine/event-publications/runtime/{publicationId}`, `/engine/event-publications/runtime/channels/{channelId}`, and `snapshot.EventPublicationStates` without making host adapters reference the eventing implementation package. In-process publications report `succeeded`, `failed`, or `skipped` together with matched, started, succeeded, failed, retry-scheduled, and skipped subscription counts; outbox-backed publications report `accepted` when the event is staged for later dispatch, not when downstream delivery has completed. `EventSubscriptionRuntimeMetadataKeys` defines the stable `event-subscriptions` metadata keys such as `dispatchRuntime`, `subscriptionRuntime`, `executionReadiness`, `executionPath`, `executionReadinessReasons`, `executionRuntimeId`, `executionOwnership`, `executionMode`, `binding.*`, and `reported.*` for operator-facing consumers that do read the technology surface directly. `EventDispatchRuntimeMetadataKeys` defines the matching dispatch-runtime retry keys such as `nextRetryAtUtc`, `retryPolicy`, `retryMaxAttempts`, `retryDelaySeconds`, `retryDurability`, `retryScope`, `retryOutcome`, `retryExhausted`, and `terminalFailure`; dispatch stores use those stable keys to tell retryable failures from terminal failures instead of parsing provider-specific metadata, while `EventDispatchRuntimeState` and `EventDispatchRuntimeSummary` now expose `TerminalFailure`, terminal-failure observation counts, terminal outbox counts, and `HasTerminalFailures` as first-class operator fields. When `EnableInProcessSubscriptionExecution` is set and at least one `IEventSubscriptionExecutor` is registered, the core pack now contributes a Cephalon-managed direct in-process binding, exposes `eventing.publish` with `handoff = in-process`, `publicationDispatcher = available`, and `publicationRuntimeState = available`, exposes `eventing.subscribe` with `executionOwnership = cephalon-managed`, invokes matching executors through `IEventPublisher`, and reports started/succeeded/failed observations through the same subscription runtime catalog. When `InProcessSubscriptionMaxAttempts` is greater than `1`, that same process-local lane reports `retryPolicy = bounded-in-process`, `retryMaxAttempts`, `retryDelayMilliseconds`, `retryDurability = none`, and `retryScope = process-local` across capabilities, bindings, `event-publishers`, `event-subscriptions`, and `reported.*` metadata while retrying the failing executor inline before returning success or failure to the caller. When `EnableInProcessSubscriptionIdempotency` is set, the direct lane records successful `subscriptionId + publicationId` executions in a bounded process-local tracker and skips later duplicate completed executions inside the configured retention window, reporting `skipped` with `idempotencyPolicy = completed-publication`, `idempotencyKey = subscription-publication`, `idempotencyRetentionMinutes`, `idempotencyDurability = none`, `idempotencyScope = process-local`, and `idempotencyOutcome = duplicate-skipped` metadata. When a real `IOutbox`-backed handoff path exists and the in-process path is not selected, the pack contributes an `event-publishers` runtime surface entry for the staged outbox-backed publisher, exposes the `eventing.publish` capability with runtime-state availability plus `publicationDispatcher = available` metadata, reports publication runtime state as `accepted` with `handoff = outbox` and `deliveryCompletion = pending-dispatch`, can optionally expose when an adapter-neutral `IEventDispatchStore` is available to read pending staged events and apply durable dispatch outcomes, registers named dispatch-runtime descriptors through the host-agnostic `IEventDispatchRuntimeDescriptorCatalog`, enriches those descriptors with a canonical per-runtime `Summary` when live dispatch reports exist, resolves a first-class `IOutboxDispatchPolicyCatalog` so `/engine/outboxes` can answer whether each outbox is `disabled`, `consumer-managed`, or runtime-managed, and lets both application-managed and companion-managed subscription paths project operator-facing metadata through the same `event-subscriptions` surface.

That still stops short of a full bus runtime on purpose. The core in-process execution path is direct, synchronous, process-local, and non-durable: it does not claim generic runtime-owned inbox linkage, broker-owned durable retries, distributed dispatch, cross-node exactly-once delivery, or arbitrary background subscription polling. The publication runtime catalog is also bounded to the active publication path: direct in-process observations describe local subscription execution, while outbox-backed observations describe accepted staging and remain separate from later dispatch-state or broker-delivery truth. `eventing.subscribe` appears from the core pack only when `EnableInProcessSubscriptionExecution` is explicitly selected and a real executor exists; companion packs such as `Cephalon.Eventing.Wolverine` remain optional provider-managed ways to activate durable staged dispatch and richer retry behavior on top of the same runtime-neutral contracts. In all modes `IEventSubscriptionExecutionBindingCatalog` and `event-subscriptions` distinguish `application-managed`, `cephalon-managed`, and provider-managed runtime-bound ownership through `dispatchRuntime`, `subscriptionRuntime`, `executionRuntimeId`, `executionOwnership`, `executionMode`, and `binding.*` metadata while keeping the underlying core pack host-agnostic. Companion packs can continue adding more runtime truth under the same `event-driven-integration` technology when they have something honest to say. For example, the current `Cephalon.Data.EntityFramework`, `Cephalon.Data.MongoDB`, `Cephalon.Data.Redis`, `Cephalon.Data.Elasticsearch`, `Cephalon.Data.OpenSearch`, `Cephalon.Data.Neo4j`, `Cephalon.Data.Qdrant`, and `Cephalon.Data.Nats` slices can now project staged-only outbox producers and application-managed inbox stores into the same technology surface set, the subscription metadata can now say when such an inbox store is available, application-managed handlers can now report their latest runtime state and retry intent through `IEventSubscriptionRuntimeReporter` / `IEventSubscriptionRuntimeCatalog`, application-managed dispatch paths can now report outbox-specific dispatch observations through `IEventDispatchRuntimeReporter` / `IEventDispatchRuntimeCatalog`, configured dispatch runtimes now remain visible through `IEventDispatchRuntimeDescriptorCatalog` even before runtime reports arrive, the `IEventDispatchStore` contract can now bridge pending staged outbox rows plus durable dispatch outcomes across both relational and the current non-relational outbox packs without baking a specific broker into the pack, runtime descriptors now surface the canonical aggregate `Summary` instead of forcing each consumer to re-aggregate `IEventDispatchRuntimeCatalog` ad hoc, outbox descriptors now carry an effective dispatch policy instead of leaving `/engine/outboxes` as a static staged-only answer, terminal dispatch posture now appears directly as state/summary fields and `event-dispatches` / `event-dispatch-runtimes` metadata before consumers fall back to `reported.*`, `event-dispatches` still projects only the per-outbox detail that belongs to the current path, hosted executions can still link themselves to one or more declared subscriptions through metadata so the surface can report application-managed execution linkage, execution-graph linkage, and live hosted-execution/runtime-story phase data without pretending that delivery, durable retries, handlers, or distributed subscription execution are already owned by the eventing pack, the current `Cephalon.Eventing.Behaviors` slice can now explicitly route `ISagaChoreographyPublisher` traffic into the same publication path without moving choreography ownership into `Cephalon.Eventing` itself, and the current `Cephalon.Eventing.Wolverine` slice can still opt into both a `wolverine-managed` staged-event dispatch loop and runtime-bound subscription execution proof on top of the same runtime-neutral contracts.

ClickHouse is now an explicit unsupported-by-design branch instead of a hidden parity gap: `Cephalon.Data.ClickHouse` still stages durable outbox records, but it now publishes `DispatchPolicy.PolicyId = unsupported` plus `ExecutionMode = disabled` so `/engine/outboxes`, `event-dispatches`, and runtime surfaces say clearly that the current analytics-first replacement semantics are staging-only and do not yet own mutable dispatch state. `Cephalon.Data.Cassandra` now takes the opposite path through a Cassandra-native sharded pending-dispatch index, so wide-column workloads can stay on the same consumer-managed or adapter-managed dispatch path without relaying through a relational bridge.

## Adapter stance

Current Cephalon direction for messaging adapters:

- `Cephalon.Eventing.Wolverine` is the current shipped optional companion adapter proof and now supports host wiring, an opt-in `wolverine-managed` durable dispatch loop on top of `IEventDispatchStore`, bounded dispatch retry with terminal failed state, and a provider-managed runtime-bound subscription execution lane on top of the same staged-publication path
- `Cephalon.Eventing` now also has an opt-in `cephalon-managed` direct in-process subscription execution baseline, with optional bounded process-local retries and duplicate-completed execution suppression, for lightweight hosts that do not need a durable broker or Wolverine
- `MassTransit` is the tracked-later companion adapter candidate once the engine-owned runtime-neutral contract and the current Wolverine proof are strong enough to justify a second shipped adapter package
- other messaging libraries such as `MediatR`, `LiteBus`, `NServiceBus`, and `SlimMessageBus` are allowed in consumer apps, but they are consumer-owned integrations rather than shipped Cephalon companion adapters in the current phase
- Cephalon should not claim runtime truth for those consumer-owned integrations unless a dedicated bridge or adapter package actually projects that state back into the engine surfaces
- engine readiness should come from `Cephalon.Eventing` owning the runtime-neutral contracts and truthful operator surfaces; companion adapters are optional ways for a consumer app to activate one managed runtime on top of those seams
- one message flow should have one durable-messaging owner; avoid mixing Cephalon/Wolverine-managed outbox, inbox, retry, or dispatch semantics with a second durable bus/runtime for the same flow
- adapter-owned diagnostics conventions should be projected through `/engine/diagnostics` when a shipped companion adapter emits stable event ids, which the current Wolverine slice now does for its managed dispatch loop

## Related docs

- [Cephalon.Eventing.Behaviors](eventing-behaviors.md)
- [Technology packs](../technology-packs.md)
- [Module authoring](../module-authoring.md)
