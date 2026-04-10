# Cephalon.Eventing

`Cephalon.Eventing` is the baseline technology pack for event-driven integration.

## What it owns

- eventing options
- module and registration entry points for the `EventDrivenIntegration` technology
- channel descriptors, registries, and catalogs
- declared subscription descriptors, registries, and catalogs
- an optional staged publication contract through `IEventPublisher` and `EventPublication`
- application-managed dispatch runtime reporting for outbox-backed publication paths
- additive runtime descriptor registration for named dispatch runtimes that can be consumed through the host-agnostic `Cephalon.Abstractions/Data/*` event-dispatch contracts
- baseline runtime-surface contributions for channel introspection, declared subscription introspection, truthful publication-path introspection, configured dispatch-runtime introspection, and live dispatch-state introspection
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
- `Services/EventSubscriptionExecutionOutcomes.cs`
- `Services/EventSubscriptionExecutionReport.cs`
- `Services/EventSubscriptionRuntimeState.cs`
- `Services/EventDispatchExecutionOutcomes.cs`
- `Services/EventDispatchItem.cs`
- `Services/EventDispatchExecutionReport.cs`
- `Services/IEventSubscriptionContributor.cs`
- `Services/IEventSubscriptionCatalog.cs`
- `Services/IEventSubscriptionRuntimeCatalog.cs`
- `Services/IEventSubscriptionRuntimeReporter.cs`
- `Services/IEventDispatchRuntimeReporter.cs`
- `Services/IEventDispatchStore.cs`
- `Services/EventPublication.cs`
- `Services/IEventPublisher.cs`
- `Services/OutboxBackedEventPublisher.cs`
- `Services/EventDispatchRuntimeCatalog.cs`
- `Services/EventDispatchRuntimeDescriptorCatalog.cs`
- `Services/EventingRuntimeSurfaceContributor.cs`
- `Services/EventingDispatchRuntimeSurfaceContributor.cs`
- `Services/EventingSubscriptionRuntimeSurfaceContributor.cs`
- `Services/EventingPublishingRuntimeSurfaceContributor.cs`
- host-agnostic read contracts in `Cephalon.Abstractions/Data/EventDispatchRuntimeDescriptor.cs`, `Cephalon.Abstractions/Data/EventDispatchRuntimeState.cs`, `Cephalon.Abstractions/Data/IEventDispatchRuntimeCatalog.cs`, and `Cephalon.Abstractions/Data/IEventDispatchRuntimeDescriptorCatalog.cs`

## Source structure

- `Configuration`
- `Modules`
- `Registration`
- `Services`

## How it fits

This pack keeps event-driven primitives out of the engine core while still making them discoverable, selectable, and introspectable through the shared technology model.

Today that baseline is deliberately modest and intentionally truthful. `Cephalon.Eventing` owns channel descriptors plus the `event-channels` runtime surface, it exposes declared subscription descriptors through `event-subscriptions` and the `eventing.subscriptions` capability, it lets application-managed handlers report `started`, `succeeded`, `failed`, `retry-scheduled`, and `skipped` observations through `IEventSubscriptionRuntimeReporter`, and it exposes `IEventPublisher` only when a real `IOutbox`-backed handoff path exists. In that configuration the pack contributes an `event-publishers` runtime surface entry for the staged outbox-backed publisher, exposes the `eventing.publish` capability with runtime-state availability metadata, can optionally expose when an adapter-neutral `IEventDispatchStore` is available to read pending staged events and apply durable dispatch outcomes, registers configured dispatch-runtime descriptors through the host-agnostic `IEventDispatchRuntimeDescriptorCatalog`, and lets application-managed dispatch paths report outbox-owned dispatch observations through `IEventDispatchRuntimeReporter` so operators can see retry intent and `reported.*` metadata through the separate `event-dispatches` surface.

That still stops short of a full bus runtime on purpose. The current subscription surface is still declarative, not a handler-execution guarantee, and `Cephalon.Eventing` does not yet claim runtime-owned inbox linkage, broker-owned retries, or pack-owned handler execution. `eventing.subscribe` therefore remains absent until a truthful implementation lands. Companion packs can continue adding more runtime truth under the same `event-driven-integration` technology when they have something honest to say. For example, the current `Cephalon.Data.EntityFramework` slice projects staged-only outbox producers and application-managed inbox stores into the same technology surface set, the subscription metadata can now say when such an inbox store is available, application-managed handlers can now report their latest runtime state and retry intent through `IEventSubscriptionRuntimeReporter` / `IEventSubscriptionRuntimeCatalog`, application-managed dispatch paths can now report outbox-specific dispatch observations through `IEventDispatchRuntimeReporter` / `IEventDispatchRuntimeCatalog`, configured dispatch runtimes now remain visible through `IEventDispatchRuntimeDescriptorCatalog` even before runtime reports arrive, the `IEventDispatchStore` contract can now bridge pending staged outbox rows plus durable dispatch outcomes without baking a specific broker into the pack, those operator-facing report metadata entries now flow through `event-subscriptions` and `event-dispatches` as `reported.*` keys, `event-dispatches` can now also project configured dispatch-runtime descriptor metadata per outbox path before runtime reports arrive, hosted executions can still link themselves to one or more declared subscriptions through metadata so the surface can report application-managed execution linkage, execution-graph linkage, and live hosted-execution/runtime-story phase data without pretending that delivery, retries, handlers, or subscription execution are already owned by the eventing pack, and the current `Cephalon.Eventing.Wolverine` slice can now opt into a `wolverine-managed` staged-event dispatch loop on top of the same runtime-neutral dispatch-store contract while its adapter surface aggregates the latest dispatch outcome, retry-pending count, and reported totals for operator views.

## Adapter stance

Current Cephalon direction for messaging adapters:

- `Cephalon.Eventing.Wolverine` is the current official first-class adapter path and now supports both a host-wiring baseline plus an opt-in `wolverine-managed` durable dispatch loop on top of `IEventDispatchStore`
- `MassTransit` is the tracked-later adapter candidate once the `Wolverine` path and the underlying runtime-neutral contract are proven strongly enough to justify a second official adapter
- other messaging libraries such as `MediatR`, `LiteBus`, `NServiceBus`, and `SlimMessageBus` are allowed in consumer apps, but they are consumer-owned integrations rather than first-class Cephalon adapters in the current phase
- Cephalon should not claim runtime truth for those consumer-owned integrations unless a dedicated bridge or adapter package actually projects that state back into the engine surfaces
- one message flow should have one durable-messaging owner; avoid mixing Cephalon/Wolverine-managed outbox, inbox, retry, or dispatch semantics with a second durable bus/runtime for the same flow
- adapter-owned diagnostics conventions should be projected through `/engine/diagnostics` when a first-class adapter emits stable event ids, which the current Wolverine slice now does for its managed dispatch loop

## Related docs

- [Technology packs](../technology-packs.md)
- [Module authoring](../module-authoring.md)
