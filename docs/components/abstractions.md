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
- pattern, migration-routing, technology, and transport contracts shared by the whole stack

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
- `Patterns/IStranglerFigRouter.cs`
- `Patterns/StranglerFigRouteDescriptor.cs`
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
`BulkheadSelection`, and `RateLimitingSelection`. `RateLimitingSelection` now also carries additive
`RateLimitingOverrideSelection` entries, and `ResilienceSelection` now also carries additive
`BehaviorExecutionResilienceOverrideSelection` entries, so the public app model can describe
narrower behavior- or transport-scoped intent without leaking ASP.NET Core-specific endpoint
conventions into `Cephalon.Abstractions`. Those types stay transport- and host-agnostic on purpose:
they capture requested resilience intent in the public model without forcing ASP.NET Core, Polly, or
behavior-pipeline enforcement details into `Cephalon.Abstractions`.

When a host adapter does enforce HTTP rate limiting, the same package now also carries the narrow
runtime-facing `IRateLimitingRuntimeCatalog` and `RateLimitingRuntimeDescriptor` contracts. That
lets hosts publish effective policy truth into operator surfaces and snapshots without leaking
ASP.NET Core middleware types back into engine-core or application behavior code. The same package
now also carries `IBehaviorResilienceRuntimeCatalog`, `BehaviorResilienceRuntimeDescriptor`,
`BehaviorExecutionResilienceSelection`, and `BehaviorExecutionResilienceOverrideSelection` so the
engine can publish both requested override intent and effective behavior-execution timeout,
circuit-breaker, and bulkhead answers without leaking Polly types or host-specific middleware
contracts into consumer code. The `Behaviors` namespace now also exposes
`BehaviorIdempotencyAttribute` plus `BehaviorIdempotencyMode` so behavior authors can declare
whether automatic replay is safe without coupling that contract to HTTP verbs, transport adapters,
or host-specific policies. The same resilience namespace now also exposes
`BehaviorResilienceExceptionContext`, `BehaviorResilienceExceptionHandling`, and
`IBehaviorResilienceExceptionClassifier` so runtime packs can classify failures without hard-coding
Polly-specific exception decisions into consumer modules. The runtime descriptor now also carries
targeted behavior ids plus transport ids so operator tooling can see whether an answer came from the
default policy or from a narrower override, and `Resolve(behaviorId, transportId)` metadata can now
surface behavior-specific retry eligibility as `eligible`, `ineligible`, or `unknown` without
pretending that automatic retry is already enforced.

The same contract-first rule now also covers the first phase 12 migration surface. The `Patterns`
namespace now carries `IStranglerFigRouteContributor`, `IStranglerFigRouteRegistry`,
`IStranglerFigRuntimeCatalog`, `IStranglerFigRouter`, `StranglerFigRequest`,
`StranglerFigRouteDescriptor`, `StranglerFigRouteResolution`, and `StranglerFigTarget` so modules,
hosts, and operator tooling can talk about migration-boundary ownership and request resolution
without leaking ASP.NET Core proxy behavior, YARP, or cloud traffic-manager types into
`Cephalon.Abstractions`.

## Related docs

- [Architecture](../architecture.md)
- [App models](../app-models.md)
- [Module authoring](../module-authoring.md)
