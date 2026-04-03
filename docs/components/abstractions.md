# Cephalon.Abstractions

`Cephalon.Abstractions` is the stable contract layer that modules, hosts, and companion packages build against.

## What it owns

- module contracts such as `IModule`, `IModuleLifecycle`, `ModuleBase`, `ModuleDescriptor`, and `ModuleContext`
- capability contracts such as `Capability`, `CapabilityAccess`, and `ICapabilityRegistry`
- app-model contracts such as `AppBlueprint`, `AppProfile`, and scaffold-plan types
- health contracts used across hosts and packages
- localization contracts used by engine resources and package language packs
- pattern, technology, and transport descriptors shared by the whole stack

## Main surfaces

- `Modules/IModule.cs`
- `Modules/IModuleLifecycle.cs`
- `Capabilities/Capability.cs`
- `Capabilities/ICapabilityRegistry.cs`
- `AppModel/AppProfile.cs`
- `AppModel/Scaffolding/ScaffoldPlan.cs`
- `AppModel/Scaffolding/SuiteScaffoldPlan.cs`
- `AppModel/Scaffolding/SuiteScaffoldService.cs`
- `Health/DependencyHealthReport.cs`
- `Localization/ILocalizedResourceContributor.cs`
- `Technologies/ITechnologyRuntimeCatalog.cs`
- `Transports/TransportDescriptor.cs`

## Source structure

- `AppModel`
- `AppModel/Scaffolding`
- `Capabilities`
- `Health`
- `Localization`
- `Modules`
- `Patterns`
- `Technologies`
- `Transports`

## How it fits

The engine should depend on this package for contracts only. New runtime behavior belongs in `Cephalon.Engine` or a companion package unless it must become part of the public module authoring surface.

## Related docs

- [Architecture](../architecture.md)
- [App models](../app-models.md)
- [Module authoring](../module-authoring.md)
