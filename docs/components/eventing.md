# Cephalon.Eventing

`Cephalon.Eventing` is the baseline technology pack for event-driven integration.

## What it owns

- eventing options
- module and registration entry points for the `EventDrivenIntegration` technology
- channel descriptors, registries, and catalogs
- declared subscription descriptors, registries, and catalogs
- host-agnostic managed-subscription execution contracts and execution-binding vocabulary that truthful companion packs can project back into runtime introspection
- an optional staged publication contract through `IEventPublisher` and `EventPublication`
- application-managed dispatch runtime reporting for outbox-backed publication paths
- additive runtime descriptor registration for named dispatch runtimes that can be consumed through the host-agnostic `Cephalon.Abstractions/Data/*` event-dispatch contracts
- additive outbox dispatch-policy resolution so `/engine/outboxes` and runtime surfaces can distinguish `disabled`, `consumer-managed`, and runtime-managed execution ownership truthfully
- baseline runtime-surface contributions for channel introspection, declared subscription introspection, truthful publication-path introspection, live-enriched dispatch-runtime introspection, live dispatch-state introspection, and managed-versus-application-owned subscription execution linkage
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
- `Services/EventSubscriptionRuntimeState.cs`
- `Services/EventDispatchExecutionOutcomes.cs`
- `Services/EventDispatchItem.cs`
- `Services/EventDispatchExecutionReport.cs`
- `Services/IEventSubscriptionContributor.cs`
- `Services/IEventSubscriptionCatalog.cs`
- `Services/IEventSubscriptionExecutor.cs`
- `Services/IEventSubscriptionExecutionBindingContributor.cs`
- `Services/IEventSubscriptionRuntimeCatalog.cs`
- `Services/IEventSubscriptionRuntimeReporter.cs`
- `Services/IEventDispatchRuntimeReporter.cs`
- `Services/IEventDispatchStore.cs`
- `Services/EventPublication.cs`
- `Services/IEventPublisher.cs`
- `Services/OutboxBackedEventPublisher.cs`
- `Services/OutboxDispatchPolicyCatalog.cs`
- `Services/EventDispatchRuntimeCatalog.cs`
- `Services/EventDispatchRuntimeDescriptorCatalog.cs`
- `Services/EventingRuntimeSurfaceContributor.cs`
- `Services/EventingDispatchRuntimeSurfaceContributor.cs`
- `Services/EventingSubscriptionRuntimeSurfaceContributor.cs`
- `Services/EventingPublishingRuntimeSurfaceContributor.cs`
- host-agnostic read contracts in `Cephalon.Abstractions/Data/EventDispatchRuntimeDescriptor.cs`, `Cephalon.Abstractions/Data/EventDispatchRuntimeSummary.cs`, `Cephalon.Abstractions/Data/EventDispatchRuntimeState.cs`, `Cephalon.Abstractions/Data/IEventDispatchRuntimeCatalog.cs`, `Cephalon.Abstractions/Data/IEventDispatchRuntimeDescriptorCatalog.cs`, `Cephalon.Abstractions/Data/OutboxDispatchPolicyDescriptor.cs`, and `Cephalon.Abstractions/Data/IOutboxDispatchPolicyCatalog.cs`

## Source structure

- `Configuration`
- `Modules`
- `Registration`
- `Services`

## How it fits

This pack keeps event-driven primitives out of the engine core while still making them discoverable, selectable, and introspectable through the shared technology model.

Today that baseline is still deliberately modest and intentionally truthful. `Cephalon.Eventing` owns channel descriptors plus the `event-channels` runtime surface, it exposes declared subscription descriptors through `event-subscriptions` and the `eventing.subscriptions` capability, it lets application-managed handlers report `started`, `succeeded`, `failed`, `retry-scheduled`, and `skipped` observations through `IEventSubscriptionRuntimeReporter`, it now exposes host-agnostic `IEventSubscriptionExecutor` plus `IEventSubscriptionExecutionBindingContributor` contracts so a companion can bind declared subscriptions to one real managed runtime without leaking adapter APIs into the core pack, and it exposes `IEventPublisher` only when a real `IOutbox`-backed handoff path exists. In that configuration the pack contributes an `event-publishers` runtime surface entry for the staged outbox-backed publisher, exposes the `eventing.publish` capability with runtime-state availability metadata, can optionally expose when an adapter-neutral `IEventDispatchStore` is available to read pending staged events and apply durable dispatch outcomes, registers named dispatch-runtime descriptors through the host-agnostic `IEventDispatchRuntimeDescriptorCatalog`, enriches those descriptors with a canonical per-runtime `Summary` when live dispatch reports exist, resolves a first-class `IOutboxDispatchPolicyCatalog` so `/engine/outboxes` can answer whether each outbox is `disabled`, `consumer-managed`, or runtime-managed, and lets both application-managed and companion-managed subscription paths project operator-facing metadata through the same `event-subscriptions` surface.

That still stops short of a full bus runtime on purpose. `Cephalon.Eventing` itself still does not claim generic runtime-owned inbox linkage, broker-owned retries, or arbitrary pack-owned handler execution, and the core pack still does not publish `eventing.subscribe` by itself. That capability now appears only when a truthful companion contributes one real managed execution path. In that mode `event-subscriptions` can distinguish `application-managed` versus runtime-bound ownership through `dispatchRuntime`, `subscriptionRuntime`, `executionRuntimeId`, `executionOwnership`, `executionMode`, and `binding.*` metadata while leaving the underlying core pack host-agnostic. Companion packs can continue adding more runtime truth under the same `event-driven-integration` technology when they have something honest to say. For example, the current `Cephalon.Data.EntityFramework`, `Cephalon.Data.MongoDB`, `Cephalon.Data.Redis`, `Cephalon.Data.Elasticsearch`, `Cephalon.Data.OpenSearch`, `Cephalon.Data.Neo4j`, `Cephalon.Data.Qdrant`, and `Cephalon.Data.Nats` slices can now project staged-only outbox producers and application-managed inbox stores into the same technology surface set, the subscription metadata can now say when such an inbox store is available, application-managed handlers can now report their latest runtime state and retry intent through `IEventSubscriptionRuntimeReporter` / `IEventSubscriptionRuntimeCatalog`, application-managed dispatch paths can now report outbox-specific dispatch observations through `IEventDispatchRuntimeReporter` / `IEventDispatchRuntimeCatalog`, configured dispatch runtimes now remain visible through `IEventDispatchRuntimeDescriptorCatalog` even before runtime reports arrive, the `IEventDispatchStore` contract can now bridge pending staged outbox rows plus durable dispatch outcomes across both relational and the current non-relational outbox packs without baking a specific broker into the pack, runtime descriptors now surface the canonical aggregate `Summary` instead of forcing each consumer to re-aggregate `IEventDispatchRuntimeCatalog` ad hoc, outbox descriptors now carry an effective dispatch policy instead of leaving `/engine/outboxes` as a static staged-only answer, those operator-facing report metadata entries now flow through `event-subscriptions` and `event-dispatches` as `reported.*` keys, `event-dispatches` still projects only the per-outbox detail that belongs to the current path, hosted executions can still link themselves to one or more declared subscriptions through metadata so the surface can report application-managed execution linkage, execution-graph linkage, and live hosted-execution/runtime-story phase data without pretending that delivery, retries, handlers, or subscription execution are already owned by the eventing pack, the current `Cephalon.Eventing.Behaviors` slice can now explicitly route `ISagaChoreographyPublisher` traffic into that same staged publication path without moving choreography ownership into `Cephalon.Eventing` itself, and the current `Cephalon.Eventing.Wolverine` slice can now opt into both a `wolverine-managed` staged-event dispatch loop and the first runtime-bound subscription-execution proof on top of the same runtime-neutral contracts.

ClickHouse is now an explicit unsupported-by-design branch instead of a hidden parity gap: `Cephalon.Data.ClickHouse` still stages durable outbox records, but it now publishes `DispatchPolicy.PolicyId = unsupported` plus `ExecutionMode = disabled` so `/engine/outboxes`, `event-dispatches`, and runtime surfaces say clearly that the current analytics-first replacement semantics are staging-only and do not yet own mutable dispatch state. `Cephalon.Data.Cassandra` now takes the opposite path through a Cassandra-native sharded pending-dispatch index, so wide-column workloads can stay on the same consumer-managed or adapter-managed dispatch path without relaying through a relational bridge.

## Adapter stance

Current Cephalon direction for messaging adapters:

- `Cephalon.Eventing.Wolverine` is the current shipped optional companion adapter proof and now supports host wiring, an opt-in `wolverine-managed` durable dispatch loop on top of `IEventDispatchStore`, and the first truthful runtime-bound subscription execution baseline on top of the same staged-publication path
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
