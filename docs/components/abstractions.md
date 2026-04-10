# Cephalon.Abstractions

`Cephalon.Abstractions` is the stable contract layer that modules, hosts, and companion packages build against.

## What it owns

- module contracts such as `IModule`, `IModuleLifecycle`, `ModuleBase`, `ModuleDescriptor`, and `ModuleContext`
- behavior contracts such as `IAppBehavior<TIn, TOut>`, `IBehaviorContext`, `IBehaviorTopologyBuilder`, `IBehaviorOwnerModule`, `IBehaviorModuleBuilder`, and `OwnedBehaviorRegistration`
- capability contracts such as `Capability`, `CapabilityAccess`, and `ICapabilityRegistry`
- app-model contracts such as `AppBlueprint`, `AppProfile`, resilience-selection types, and scaffold-plan types
- phase-8 runtime-neutral contracts for data, authorization, tenancy, audit, and id generation
- health contracts used across hosts and packages
- localization contracts used by engine resources and package language packs
- pattern, technology, and transport descriptors shared by the whole stack

## Main surfaces

- `Modules/IModule.cs`
- `Modules/IModuleLifecycle.cs`
- `Behaviors/IAppBehavior.cs`
- `Behaviors/IBehaviorContext.cs`
- `Behaviors/IBehaviorTopologyBuilder.cs`
- `Behaviors/IBehaviorOwnerModule.cs`
- `Behaviors/IBehaviorModuleBuilder.cs`
- `Behaviors/OwnedBehaviorRegistration.cs`
- `Capabilities/Capability.cs`
- `Capabilities/ICapabilityRegistry.cs`
- `AppModel/AppProfile.cs`
- `AppModel/SuiteBlueprint.cs`
- `AppModel/Scaffolding/ScaffoldPlan.cs`
- `AppModel/Scaffolding/SuiteScaffoldPlan.cs`
- `AppModel/Scaffolding/SuiteScaffoldService.cs`
- `Data/ICommand.cs`
- `Data/IReadStore.cs`
- `Data/ProjectionDescriptor.cs`
- `Data/InboxDescriptor.cs`
- `Data/IInbox.cs`
- `Data/IInboxCatalog.cs`
- `Data/OutboxDescriptor.cs`
- `Data/IOutbox.cs`
- `Data/IOutboxCatalog.cs`
- `Authorization/AuthorizationPolicyDescriptor.cs`
- `Authorization/IAuthorizationEvaluator.cs`
- `Tenancy/TenantContext.cs`
- `Tenancy/ITenantResolver.cs`
- `Audit/AuditEntry.cs`
- `Audit/AuditHistoryExportRequest.cs`
- `Audit/IAuditHistoryExporter.cs`
- `Audit/IAuditHistoryReader.cs`
- `Audit/AuditStoreDescriptor.cs`
- `Audit/IAuditStoreCatalog.cs`
- `Ids/IIdGenerator.cs`
- `Health/DependencyHealthReport.cs`
- `Localization/ILocalizedResourceContributor.cs`
- `Technologies/ITechnologyRuntimeCatalog.cs`
- `Transports/TransportDescriptor.cs`

## Source structure

- `AppModel`
- `AppModel/Scaffolding`
- `Audit`
- `Authorization`
- `Behaviors`
- `Capabilities`
- `Data`
- `Health`
- `Ids`
- `Localization`
- `Modules`
- `Patterns`
- `Tenancy`
- `Technologies`
- `Transports`

## How it fits

The engine should depend on this package for contracts only. New runtime behavior belongs in `Cephalon.Engine` or a companion package unless it must become part of the public module authoring surface.

The behavior ownership contracts now follow that rule directly:

- `IBehaviorOwnerModule` and `IBehaviorModuleBuilder` let one module declare the behaviors it owns without leaking ASP.NET Core or other host APIs into `Cephalon.Abstractions`
- `OwnedBehaviorRegistration` is the normalized ownership record the engine composes at build time
- public REST exposure still belongs in adapter packages such as `Cephalon.Behaviors.Http`, so module ownership and HTTP route mapping stay separate concerns

The phase-8 families stay runtime-neutral on purpose:

- `Data` defines CQRS, projection, outbox/inbox, and outbox-catalog contracts without picking Entity Framework, Wolverine, or any storage engine.
- `Authorization` defines subjects, resources, policies, and evaluation contracts without binding to ASP.NET Core identity types.
- `Tenancy` defines tenant context and resolution contracts without assuming HTTP, DNS, or a single tenancy topology.
- `Audit` defines audit actors, entries, write/query/export contracts, and audit-store descriptors without hard-coding storage or observability sinks.
- `Ids` defines identifier-generation hints and the generator contract without choosing a concrete strategy such as `Sfid`.

The app-model contract now also carries a contract-first resilience family through
`ResilienceSelection`, `RetrySelection`, `TimeoutSelection`, `CircuitBreakerSelection`,
`BulkheadSelection`, and `RateLimitingSelection`. Those types stay transport- and host-agnostic on
purpose: they capture requested resilience intent in the public model without forcing ASP.NET Core,
Polly, or behavior-pipeline enforcement details into `Cephalon.Abstractions`.

## Related docs

- [Architecture](../architecture.md)
- [App models](../app-models.md)
- [Module authoring](../module-authoring.md)
