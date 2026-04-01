# Cephalon.Eventing

`Cephalon.Eventing` is the baseline technology pack for event-driven integration.

## What it owns

- eventing options
- module and registration entry points for the `EventDrivenIntegration` technology
- channel descriptors, registries, and catalogs
- runtime-surface contribution for introspection

## Main surfaces

- `Configuration/EventingOptions.cs`
- `Modules/EventingModule.cs`
- `Registration/EventingEngineBuilderExtensions.cs`
- `Services/EventChannelDescriptor.cs`
- `Services/EventChannelRegistry.cs`
- `Services/EventChannelCatalog.cs`
- `Services/IEventChannelContributor.cs`
- `Services/IEventChannelCatalog.cs`
- `Services/EventingRuntimeSurfaceContributor.cs`

## Source structure

- `Configuration`
- `Modules`
- `Registration`
- `Services`

## How it fits

This pack keeps event-driven primitives out of the engine core while still making them discoverable, selectable, and introspectable through the shared technology model.

## Related docs

- [Technology packs](../technology-packs.md)
- [Module authoring](../module-authoring.md)
