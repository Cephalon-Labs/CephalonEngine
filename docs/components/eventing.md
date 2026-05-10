# Cephalon.Eventing

> **Maturity:** `M3` · **Ownership:** mixed: `application-managed` + `cephalon-managed` — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.Eventing` is the baseline technology pack for event-driven integration.

## What it owns

- eventing options
- module and registration entry points for the `EventDrivenIntegration` technology
- channel descriptors, registries, and catalogs
- declared subscription descriptors, registries, and catalogs
- configuration-owned event channel descriptors through `Engine:Messaging:Channels`, loaded by `engine.AddEventingFromConfiguration(configuration)` before module contributors so host configuration can override channel metadata by id without editing application code
- code-owned event subscription descriptors and executors through `EventingOptions.Subscriptions`, `IEventSubscriptionContributor`, `IEventSubscriptionExecutor`, and optional `IEventSubscriptionDescriptorProvider`; `Engine:Messaging:Subscriptions` and `Engine:Messaging:SubscriptionHandlers` are deliberately rejected so publish/subscribe behavior stays type-safe and fast
- code-owned direct subscription execution middleware through `IEventSubscriptionExecutionMiddleware` and `EventSubscriptionExecutionStep`, giving modules MassTransit/NServiceBus/MediatR-style filters around the native in-process lane without making middleware binding string-config driven
- host-agnostic managed-subscription execution contracts and execution-binding vocabulary that truthful companion packs can project back into runtime introspection
- a public `IEventSubscriptionExecutionBindingCatalog` read contract for active managed subscription bindings contributed by companion packs
- an implementation of the abstraction-level `IEventSubscriptionExecutionReadinessCatalog` read contract that tells hosts and tooling whether each declared subscription is `runtime-bound`, `hosted-execution-linked`, `application-managed-state`, or `declared-only`
- stable `EventSubscriptionRuntimeMetadataKeys` constants for ownership and runtime-state fields projected through the `event-subscriptions` technology surface
- stable `EventDispatchRuntimeMetadataKeys` constants for dispatch retry, retry exhaustion, and terminal failure fields projected through dispatch runtime reports and event-dispatch surfaces
- an opt-in Cephalon-managed in-process subscription execution baseline through `EventingOptions.EnableInProcessSubscriptionExecution`, `IEventPublisher`, and registered `IEventSubscriptionExecutor` services
- bounded process-local retry for that in-process lane through `EventingOptions.InProcessSubscriptionMaxAttempts`, `InProcessSubscriptionRetryDelayMilliseconds`, `InProcessSubscriptionRetryBackoff`, `InProcessSubscriptionRetryBackoffMultiplier`, `InProcessSubscriptionRetryMaxDelayMilliseconds`, and `InProcessSubscriptionRetryJitterPercent`, with `retry-scheduled` observations, deterministic effective-delay metadata, and fixed or exponential backoff metadata when enabled
- bounded duplicate suppression for completed in-process subscription executions through `EventingOptions.EnableInProcessSubscriptionIdempotency`, `InProcessSubscriptionIdempotencyStore`, and `InProcessSubscriptionIdempotencyRetentionMinutes`, with either process-local or registered-`IInbox` storage, `skipped` observations, and explicit idempotency store/durability/scope metadata when enabled
- an implementation of the abstraction-level `IEventPublicationDispatcher` command seam so host adapters can request one bounded event publication without referencing `Cephalon.Eventing` implementation types
- optional config-driven publication routing through `EventingOptions.EnablePublicationRouting`, `PublicationRoutingAutoChannelId`, `PublicationRoutes`, `PublicationRoutingRequireMatchedRoute`, and `PublicationRoutingRejectMismatchedExplicitChannel`, so callers can publish with a stable logical channel such as `auto` while the dispatcher resolves or validates the effective channel from event type before the active publisher runs
- optional bounded process-local scheduled/delayed publication acceptance through `EventingOptions.EnablePublicationScheduling`, `PublicationSchedulingMaxDelayMilliseconds`, and `PublicationSchedulingMaxPendingCount`, with request metadata keys `scheduledForUtc` or `delayMilliseconds` and explicit `schedule*` runtime metadata when enabled
- an implementation of the abstraction-level `IEventPublicationRuntimeCatalog` read contract so operators can inspect latest event-publication runtime state without referencing `Cephalon.Eventing` implementation types
- an optional staged publication contract through `IEventPublisher` and `EventPublication`
- application-managed dispatch runtime reporting for outbox-backed publication paths, including first-class retry-pending and terminal-failure posture on the abstraction-level state/summary contracts
- additive runtime descriptor registration for named dispatch runtimes that can be consumed through the host-agnostic `Cephalon.Abstractions/Data/*` event-dispatch contracts
- additive outbox dispatch-policy resolution so `/engine/outboxes` and runtime surfaces can distinguish `disabled`, `consumer-managed`, and runtime-managed execution ownership truthfully
- terminal dispatch-failure metadata plus typed `EventDispatchRuntimeState.TerminalFailure`, `TerminalFailureCount`, `EventDispatchRuntimeSummary.TerminalFailureCount`, `TerminalOutboxCount`, and `HasTerminalFailures` answers so dispatch stores and operators can stop poison staged publications from re-entering pending-dispatch reads when a runtime exhausts its retry budget
- a provider-neutral `event-dispatch-remediations` runtime surface plus abstraction-level `IEventDispatchRemediationDispatcher` command seam that turns reported retry-pending, skipped, failed, and terminal-failure dispatch state into bounded dispatch-store commands (`retry-now`, `retry-later`, `skip`, `quarantine`) without requiring Wolverine; broker-owned dead-letter remains deliberately not claimed
- an abstraction-level `IEventDispatchRemediationRuntimeCatalog` command-result read model plus bounded process-local command history controlled by `EventingOptions.RemediationCommandHistoryLimit`, so accepted and rejected operator commands can be audited without inferring command history from the latest outbox state
- baseline runtime-surface contributions for channel introspection, declared subscription introspection, truthful publication-path introspection, live-enriched dispatch-runtime introspection, live dispatch-state introspection, and managed-versus-application-owned subscription execution linkage
- the public `EventingDiagnostics` OpenTelemetry adapter declared against `CephalonActivitySources.Eventing` and `CephalonMeters.Eventing`; the in-process publisher emits one `eventing.publication.dispatch` activity per `IEventPublisher.PublishAsync` call with stable Cephalon-prefix tags (`cephalon.eventing.publisher.id`, `cephalon.eventing.publication.id`, `cephalon.eventing.channel.id`, `cephalon.eventing.event.type`, `cephalon.eventing.publication.outcome`, `cephalon.eventing.matched_subscription.count`) plus a `cephalon.eventing.publications` counter, all routed through the `RedactionPipeline` resolved from DI so consumer-registered redaction filters scrub publisher-emission attributes uniformly with the AspNetCore middleware and engine-runtime emission sites
- a reusable staged-publication seam that explicit bridge packs can hand off into without making `Cephalon.Eventing` own the upstream authoring contract
- the technology bucket that companion packs can extend with additional event-runtime truth such as outbox producers, inbox stores, or richer dispatch/subscription state
- a machine-readable `eventing-superiority-profile` technology surface and `eventing.superiority-profile` capability that compare the active Cephalon-native runtime posture against MassTransit, NServiceBus, Wolverine, and MediatR reference dimensions using explicit `claimed`, `partial`, and `not-claimed` statuses

## Main surfaces

- `Configuration/EventingOptions.cs`
- `Configuration/EventingOptionsConfigurationReader.cs`
- `Modules/EventingModule.cs`
- `Registration/EventingEngineBuilderExtensions.cs`
- `Services/EventChannelDescriptor.cs`
- `Services/EventChannelRegistry.cs`
- `Services/EventChannelCatalog.cs`
- `Services/IEventChannelContributor.cs`
- `Services/IEventChannelCatalog.cs`
- `Services/EventSubscriptionDescriptor.cs`
- `Services/EventSubscriptionExecutionContext.cs`
- `Services/EventSubscriptionExecutionStep.cs`
- `Services/IEventSubscriptionExecutionMiddleware.cs`
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
- `Services/IEventSubscriptionDescriptorProvider.cs`
- `Services/IEventSubscriptionExecutor.cs`
- `Services/IEventSubscriptionExecutionBindingCatalog.cs`
- `Services/IEventSubscriptionExecutionBindingContributor.cs`
- `Services/IEventSubscriptionRuntimeCatalog.cs`
- `Services/IEventSubscriptionRuntimeReporter.cs`
- `Services/IEventDispatchRuntimeReporter.cs`
- `Services/IEventDispatchStore.cs`
- `Services/EventPublication.cs`
- `Services/EventPublicationDispatcher.cs`
- `Services/EventPublicationRoutingPolicy.cs`
- `Services/EventPublicationSchedulingPolicy.cs`
- `Services/EventPublicationScheduleQueue.cs`
- `Services/EventDispatchRemediationDispatcher.cs`
- `Services/EventDispatchRemediationRuntimeCatalog.cs`
- `Services/EventingDispatchRemediationCommandRuntimeSurfaceContributor.cs`
- `Services/IEventPublisher.cs`
- `Services/OutboxBackedEventPublisher.cs`
- `Services/OutboxDispatchPolicyCatalog.cs`
- `Services/EventDispatchRuntimeCatalog.cs`
- `Services/EventDispatchRuntimeDescriptorCatalog.cs`
- `Services/EventingRuntimeSurfaceContributor.cs`
- `Services/EventingSuperiorityProfileRuntimeSurfaceContributor.cs`
- `Services/EventingDispatchRuntimeSurfaceContributor.cs`
- `Services/EventingDispatchRemediationRuntimeSurfaceContributor.cs`
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
- host-agnostic read and action contracts in `Cephalon.Abstractions/Data/EventDispatchRuntimeDescriptor.cs`, `Cephalon.Abstractions/Data/EventDispatchRuntimeSummary.cs`, `Cephalon.Abstractions/Data/EventDispatchRuntimeState.cs`, `Cephalon.Abstractions/Data/IEventDispatchRuntimeCatalog.cs`, `Cephalon.Abstractions/Data/IEventDispatchRuntimeDescriptorCatalog.cs`, `Cephalon.Abstractions/Data/EventDispatchRemediationOperationIds.cs`, `Cephalon.Abstractions/Data/EventDispatchRemediationRequest.cs`, `Cephalon.Abstractions/Data/EventDispatchRemediationResult.cs`, `Cephalon.Abstractions/Data/EventDispatchRemediationRuntimeState.cs`, `Cephalon.Abstractions/Data/IEventDispatchRemediationDispatcher.cs`, `Cephalon.Abstractions/Data/IEventDispatchRemediationRuntimeCatalog.cs`, `Cephalon.Abstractions/Data/EventPublicationOutcomes.cs`, `Cephalon.Abstractions/Data/EventPublicationRequest.cs`, `Cephalon.Abstractions/Data/EventPublicationResult.cs`, `Cephalon.Abstractions/Data/IEventPublicationDispatcher.cs`, `Cephalon.Abstractions/Data/EventPublicationRuntimeOutcomes.cs`, `Cephalon.Abstractions/Data/EventPublicationRuntimeState.cs`, `Cephalon.Abstractions/Data/IEventPublicationRuntimeCatalog.cs`, `Cephalon.Abstractions/Data/EventSubscriptionExecutionReadinessDescriptor.cs`, `Cephalon.Abstractions/Data/EventSubscriptionExecutionReadinessStates.cs`, `Cephalon.Abstractions/Data/IEventSubscriptionExecutionReadinessCatalog.cs`, `Cephalon.Abstractions/Data/OutboxDispatchPolicyDescriptor.cs`, and `Cephalon.Abstractions/Data/IOutboxDispatchPolicyCatalog.cs`

## Source structure

- `Configuration`
- `Modules`
- `Registration`
- `Services`

## How it fits

This pack keeps event-driven primitives out of the engine core while still making them discoverable, selectable, and introspectable through the shared technology model.
Selecting `event-driven-integration` by itself should therefore produce the native `Cephalon.Eventing` baseline only; generated hosts add `Cephalon.Eventing.Wolverine`, `Engine:Messaging:Provider = Wolverine`, and `engine.AddWolverineEventing()` only when the app explicitly selects that provider-managed companion.

Wolverine is the minimum external capability benchmark for this component family. The native Cephalon eventing direction is not to wrap Wolverine more tightly, but to expose a broader provider-neutral contract that can meet or exceed Wolverine-class capabilities across durability, local and brokered transports, routing, scheduled and delayed delivery, inbound and outbound failure policy, bounded and durable retries, dead-letter and replay operations, sagas and process managers, handler/subscription discovery, serialization and versioning, idempotency, multi-tenant/correlation metadata, OpenTelemetry diagnostics, operator actions, runtime topology, compliance/audit evidence, and provider portability. Environment-owned policy and topology capabilities should be selected through `Engine:Messaging` or another engine-owned configuration section, while hot-path publishing and subscription behavior should remain code-first through typed contracts, surfaced through runtime catalogs and `/engine/snapshot`, and only then implemented by the native pack or an optional provider companion.

The broader extraction rule is the same for MassTransit, NServiceBus, Wolverine, MediatR, and later messaging frameworks: learn from their proven capabilities without making their packages or APIs the Cephalon authoring surface. MassTransit contributes useful reference ideas around transport topology, consumers, saga state machines, routing slips, middleware filters, outbox support, scheduling, and test harnesses. NServiceBus contributes useful reference ideas around logical endpoints, recoverability, immediate/delayed retries, error queues, outbox consistency, correlation headers, saga validation, and ServiceControl-style operations. MediatR contributes useful reference ideas around low-ceremony in-process request/response, notifications, streams, and ordered pipeline behaviors. Wolverine contributes useful reference ideas around durable inbox/outbox, scheduled delivery, generated handler pipelines, local plus external transports, error policies, diagnostics, and dead-letter replay. Cephalon should convert the useful parts into provider-neutral contracts, config-driven options, source-generated or descriptor-backed paths where helpful, and truthful runtime/operator surfaces before claiming support.

The acceptance bar is higher than feature parity. A new Cephalon eventing capability should be shaped so it beats the reference frameworks in the dimensions Cephalon controls: configuration-first adoption, swappable providers, low ceremony for app developers, runtime introspection, operator actionability, deterministic package boundaries, multi-host portability, resilience posture, observability, compliance/audit evidence, source-generated or trim-aware implementation paths where appropriate, and focused tests/benchmarks. If a dimension cannot yet beat the benchmark, the docs and maturity rows should say that plainly instead of implying full superiority.

The runtime now carries that honesty as a first-class surface. `eventing-superiority-profile` is emitted by the native pack whenever `event-driven-integration` is active, without requiring Wolverine or another provider adapter. Its entries name comparison dimensions such as configuration-first neutrality, native Wolverine-free operation, routing and provider portability, runtime truth, recoverability, outbox portability, MediatR-style local dispatch with descriptor-provider discovery, code-first subscription execution pipeline, saga/choreography posture, dead-letter/replay remediation, observability/compliance, and test/benchmark evidence. `event-dispatch-remediations` is the matching operator surface for dispatch-failure follow-through: it is populated from `IEventDispatchRuntimeCatalog` reports and projects `remediationState`, `recommendedAction`, retry metadata, terminal-failure metadata, and `operatorCommandState`. When an active `IEventDispatchStore` owns the outbox, that state becomes `bounded-dispatch-store-command-ready` and advertises `retry-now`, `retry-later`, `skip`, and `quarantine` through `/engine/event-dispatches/{outboxId}/commands/{operationId}`; when no mutable dispatch store exists it remains `advisory-only`. Accepted and rejected command results are recorded into `IEventDispatchRemediationRuntimeCatalog`, projected through `event-dispatch-remediation-commands`, and exposed by ASP.NET Core through `/engine/event-dispatch-remediation-commands`, `/engine/event-dispatch-remediation-commands/{commandId}`, `/engine/event-dispatch-remediation-commands/outboxes/{outboxId}`, and `/engine/event-dispatch-remediation-commands/outcomes/{outcome}`. `deadLetterCommand` remains `not-claimed` because broker-owned dead-letter queues are provider-specific. Each superiority entry reports `status`, `referenceFrameworks`, `claimPolicy`, `runtimeEvidence`, `cephalonAdvantage`, and `nextGap`, so `/engine/technology-surfaces`, `/engine/snapshot`, and `eventing.superiority-profile` can say exactly what is already better, what is partial, and what remains not claimed.

Today that baseline is still deliberately modest and intentionally truthful. `Cephalon.Eventing` owns channel descriptors plus the `event-channels` runtime surface, it exposes declared subscription descriptors through `event-subscriptions` and the `eventing.subscriptions` capability, it can load channel metadata from `Engine:Messaging:Channels`, and it keeps subscription descriptors plus executor bindings code-owned through `EventingOptions.Subscriptions`, `IEventSubscriptionContributor`, `IEventSubscriptionExecutor`, and optional `IEventSubscriptionDescriptorProvider` so publish/subscribe behavior is typed and not string-config driven. A descriptor-bearing executor can now register the missing `EventSubscriptionDescriptor` automatically when the in-process lane is active; manual descriptors contributed through options or `IEventSubscriptionContributor` remain authoritative, mismatched executor/descriptor ids fail closed, and discovered descriptors carry `descriptorSource`, `descriptorDiscovery`, and `descriptorProvider` metadata through `event-subscriptions`. `engine.AddEventingFromConfiguration(configuration)` rejects `Engine:Messaging:Subscriptions`, `Engine:Messaging:EventSubscriptions`, `Engine:Messaging:SubscriptionHandlers`, and `Engine:Messaging:EventSubscriptionHandlers` with a code-first message instead of silently creating runtime behavior from configuration. It lets application-managed handlers report `started`, `succeeded`, `failed`, `retry-scheduled`, and `skipped` observations through `IEventSubscriptionRuntimeReporter`, it now exposes host-agnostic `IEventSubscriptionExecutor` plus `IEventSubscriptionExecutionBindingContributor` contracts so either the core pack or a companion can bind declared subscriptions to one real managed runtime without leaking adapter APIs into hosts, and it exposes the resulting bindings through `IEventSubscriptionExecutionBindingCatalog` so hosts and packages can inspect the active execution owner without parsing runtime-surface metadata. The implementation of abstraction-level `IEventSubscriptionExecutionReadinessCatalog` answers whether each declared subscription is runtime-bound, linked to a hosted execution, only observed through application-managed reports, or still declared-only with no execution path observed. Because the readiness descriptor and interface live in `Cephalon.Abstractions.Data`, `Cephalon.Engine` and host adapters can project the same answer through `/engine/event-subscription-readiness` and `snapshot.EventSubscriptionExecutionReadiness` without taking a dependency on the eventing pack. The pack now also implements the abstraction-level `IEventPublicationDispatcher`, so ASP.NET Core and other host adapters can request one bounded event publication through operator surfaces without taking a dependency on `EventPublication` or `IEventPublisher` concrete package types. Business publishing should still use the typed `IEventPublisher` API from application code on the hot path; the host action seam delegates to the active publisher and is intended for bounded operator or adapter scenarios. The pack now also implements the abstraction-level `IEventPublicationRuntimeCatalog`, so the same publication path reports latest publication state through `/engine/event-publications/runtime`, `/engine/event-publications/runtime/{publicationId}`, `/engine/event-publications/runtime/channels/{channelId}`, and `snapshot.EventPublicationStates` without making host adapters reference the eventing implementation package. In-process publications report `succeeded`, `failed`, or `skipped` together with matched, started, succeeded, failed, retry-scheduled, and skipped subscription counts; outbox-backed publications report `accepted` when the event is staged for later dispatch, not when downstream delivery has completed. `EventSubscriptionRuntimeMetadataKeys` defines the stable `event-subscriptions` metadata keys such as `dispatchRuntime`, `subscriptionRuntime`, `executionReadiness`, `executionPath`, `executionReadinessReasons`, `executionRuntimeId`, `executionOwnership`, `executionMode`, `binding.*`, and `reported.*` for operator-facing consumers that do read the technology surface directly. `EventDispatchRuntimeMetadataKeys` defines the matching dispatch-runtime retry keys such as `nextRetryAtUtc`, `retryPolicy`, `retryMaxAttempts`, `retryDelaySeconds`, `retryDurability`, `retryScope`, `retryOutcome`, `retryExhausted`, and `terminalFailure`; dispatch stores use those stable keys to tell retryable failures from terminal failures instead of parsing provider-specific metadata, while `EventDispatchRuntimeState` and `EventDispatchRuntimeSummary` now expose `TerminalFailure`, terminal-failure observation counts, terminal outbox counts, and `HasTerminalFailures` as first-class operator fields. The `event-dispatch-remediations` technology surface reads that same catalog and emits only actionable retry-pending, skipped, failed, or terminal states, and the abstraction-level `IEventDispatchRemediationDispatcher` can apply `retry-now`, `retry-later`, `skip`, and `quarantine` as durable dispatch-store outcomes when an `IEventDispatchStore` owns the outbox. A host without Wolverine therefore gets provider-neutral remediation posture, bounded operator commands, and stable reported metadata without needing a broker-specific dead-letter queue; dead-letter remains provider-specific until a companion actually owns that path. When `EnableInProcessSubscriptionExecution` is set and at least one `IEventSubscriptionExecutor` is registered, the core pack now contributes a Cephalon-managed direct in-process binding, exposes `eventing.publish` with `handoff = in-process`, `publicationDispatcher = available`, and `publicationRuntimeState = available`, exposes `eventing.subscribe` with `executionOwnership = cephalon-managed`, invokes matching executors through `IEventPublisher`, and reports started/succeeded/failed observations through the same subscription runtime catalog. Hosts and modules can also register `IEventSubscriptionExecutionMiddleware` services in DI; the native publisher composes them as a code-first `EventSubscriptionExecutionStep` chain before the final executor, reports `subscriptionExecutionPipeline = code-first` plus `subscriptionExecutionMiddlewareCount` across capability, binding, publication, and subscription runtime metadata, and advertises the claim through the `code-first-subscription-execution-pipeline` superiority-profile row. No `Engine:Messaging` section binds those descriptor providers or middleware services, so validation, tenancy, auditing, short-circuiting, or policy checks stay typed and hot-path friendly. When `InProcessSubscriptionMaxAttempts` is greater than `1`, that same process-local lane reports `retryPolicy = bounded-in-process`, `retryMaxAttempts`, `retryDelayMilliseconds`, `retryDurability = none`, and `retryScope = process-local` across capabilities, bindings, `event-publishers`, `event-subscriptions`, and `reported.*` metadata while retrying the failing executor inline before returning success or failure to the caller. When `EnableInProcessSubscriptionIdempotency` is set, the direct lane records successful `subscriptionId + publicationId` executions in the configured idempotency store and skips later duplicate completed executions inside the configured retention window, reporting `skipped` with `idempotencyPolicy = completed-publication`, `idempotencyKey = subscription-publication`, `idempotencyStore`, `idempotencyRetentionMinutes`, `idempotencyDurability`, `idempotencyScope`, `inbox`, and `idempotencyOutcome = duplicate-skipped` metadata. The default store remains `process-local` with `idempotencyDurability = none` and `idempotencyScope = process-local`; setting `InProcessSubscriptionIdempotencyStore = inbox` or `Engine:Messaging:InProcessSubscriptions:Idempotency:Store = inbox` makes the publisher use exactly one registered `IInbox`, reports `idempotencyDurability = inbox` and `idempotencyScope = durable-store`, and fails closed during composition if that inbox is missing or ambiguous. When a real `IOutbox`-backed handoff path exists and the in-process path is not selected, the pack contributes an `event-publishers` runtime surface entry for the staged outbox-backed publisher, exposes the `eventing.publish` capability with runtime-state availability plus `publicationDispatcher = available` metadata, reports publication runtime state as `accepted` with `handoff = outbox` and `deliveryCompletion = pending-dispatch`, can optionally expose when an adapter-neutral `IEventDispatchStore` is available to read pending staged events and apply durable dispatch outcomes, registers named dispatch-runtime descriptors through the host-agnostic `IEventDispatchRuntimeDescriptorCatalog`, enriches those descriptors with a canonical per-runtime `Summary` when live dispatch reports exist, resolves a first-class `IOutboxDispatchPolicyCatalog` so `/engine/outboxes` can answer whether each outbox is `disabled`, `consumer-managed`, or runtime-managed, and lets both application-managed and companion-managed subscription paths project operator-facing metadata through the same `event-subscriptions` surface.

When `EnablePublicationScheduling` is set, the publication dispatcher can also accept one delayed publication request without Wolverine. Callers express that request through publication metadata with either `scheduledForUtc` or `delayMilliseconds`; the dispatcher validates the configured delay and pending-count bounds, reports an immediate `accepted` publication runtime state with `schedulePolicy = bounded-process-local`, and then hands the publication to the active `IEventPublisher` when the local timer becomes due. Runtime metadata includes `scheduleState`, `scheduleScope`, `scheduleDurability`, `scheduleSource`, `scheduledForUtc`, `scheduleDelayMilliseconds`, and `scheduledPublicationPendingCount`, while the `event-publishers` technology surface reports the active policy, max delay, max pending count, current pending count, and next due time.

When `EnablePublicationRouting` is set, the publication dispatcher resolves routing before scheduling or publishing. Requests whose `channelId` equals `PublicationRoutingAutoChannelId` (default `auto`) must match `PublicationRoutes`; exact event-type keys match first and trailing-`*` keys match by prefix. The dispatcher then creates the effective publication on the configured route channel and records `routingPolicy = event-type-map`, `routingState`, `routingSource`, `routingRequestedChannelId`, `routingEffectiveChannelId`, `routingRule`, and `routingRuleChannelId` metadata. Explicit channel requests can be allowed, required to match a route, or rejected when they disagree with the configured route, depending on `PublicationRoutingRequireMatchedRoute` and `PublicationRoutingRejectMismatchedExplicitChannel`. This is provider-neutral publication routing and channel-governance truth; broker-specific exchanges, queues, topics, partitions, and topology materialization still belong to provider companion packs.

The command-result read model is intentionally separate from the dispatch-state catalog. `IEventDispatchRuntimeCatalog` answers the latest state of an outbox dispatch path, while `IEventDispatchRemediationRuntimeCatalog` answers which operator commands were accepted or rejected, including command id, outbox id, message id, operation, outcome, dispatch outcome, timestamp, error, and safe metadata. ASP.NET Core exposes those records through `/engine/event-dispatch-remediation-commands*`, and the technology-surface entry `event-dispatch-remediation-commands` keeps the same command-result truth available to snapshot consumers through `TechnologySurfaces`.

Native eventing reads environment-owned channel metadata, direct in-process subscription execution settings, and delayed publication settings from `Engine:Messaging` through `engine.AddEventingFromConfiguration(configuration)`. A host that wants a Wolverine-free managed subscription lane can configure channel and policy settings, but must declare subscription descriptors and execution code through typed code:

```json
{
  "Engine": {
    "Messaging": {
      "Channels": {
        "audit": {
          "DisplayName": "Audit",
          "Description": "Audit integration events.",
          "Tags": [ "audit", "configuration" ]
        }
      },
      "InProcessSubscriptions": {
        "EnableExecution": true,
        "MaxAttempts": 3,
        "RetryDelayMilliseconds": 250,
        "RetryBackoff": "exponential",
        "RetryBackoffMultiplier": 2,
        "RetryMaxDelayMilliseconds": 5000,
        "RetryJitterPercent": 15,
        "Idempotency": {
          "Enabled": true,
          "Store": "inbox",
          "RetentionMinutes": 45
        }
      }
    }
  }
}
```

The matching subscription belongs in code, for example through `options.Subscriptions.Add(...)` plus an `IEventSubscriptionExecutor` registration, through a reusable module that implements `IEventSubscriptionContributor`, or by letting a registered executor also implement `IEventSubscriptionDescriptorProvider` when the executor owns its descriptor metadata. Code-owned cross-cutting steps belong in DI too, through `IEventSubscriptionExecutionMiddleware`; the native publisher runs those middleware steps before the executor and surfaces `subscriptionExecutionPipeline` metadata for operators. `Engine:Messaging:Subscriptions` and `Engine:Messaging:SubscriptionHandlers` are rejected by `AddEventingFromConfiguration` so hot-path publish/subscribe behavior stays code-first; configuration remains for runtime policy such as retry, idempotency, routing, scheduling, and channel metadata.

That same in-process retry lane keeps `retryPolicy = bounded-in-process` and now also reports `retryBackoff`, `retryBackoffMultiplier`, `retryMaxDelayMilliseconds`, `retryJitterPercent`, and `retryEffectiveDelayMilliseconds` on `retry-scheduled` observations. `fixed` preserves the original delay behavior; `exponential` multiplies each failed attempt by the configured multiplier and caps it at the configured max delay; jitter is deterministic from publication, channel, event type, subscription, and attempt metadata so retries are shaped without introducing opaque randomness into operator evidence.

The same host can accept bounded process-local delayed publication requests without Wolverine by adding:

```json
{
  "Engine": {
    "Messaging": {
      "Publications": {
        "Scheduling": {
          "Enabled": true,
          "MaxDelayMilliseconds": 5000,
          "MaxPendingCount": 4
        }
      }
    }
  }
}
```

The same host can route publication requests by event type before the active publisher runs:

```json
{
  "Engine": {
    "Messaging": {
      "Publications": {
        "Routing": {
          "Enabled": true,
          "AutoChannelId": "auto",
          "RejectMismatchedExplicitChannel": true,
          "Routes": {
            "audit.created": "audit",
            "billing.*": "billing-events"
          }
        }
      }
    }
  }
}
```

Callers can then publish with `"channelId": "auto"` and let the engine resolve the effective channel. Operators can verify the active route policy through `eventing.publish`, `event-publishers`, publication runtime metadata, and the `routing-and-provider-portability` entry in `eventing-superiority-profile`.

The request still uses the existing event-publication command surface; callers add exactly one scheduling metadata key:

```json
{
  "metadata": {
    "scheduledForUtc": "2026-05-10T10:00:00.0000000+00:00"
  }
}
```

`delayMilliseconds` is the relative-delay alternative when the caller does not want to compute an absolute timestamp.

This scheduler is intentionally process-local. Pending delayed publications live in memory, are lost on process stop, are not coordinated across nodes, and do not replace a durable outbox dispatcher, broker scheduler, or provider-managed delayed-delivery lane.

The retry backoff and jitter knobs are intentionally process-local as well. They give lightweight Wolverine-free hosts configurable failure shaping and truthful retry metadata, but they do not create a durable retry queue, broker error policy, cross-node lease, or downstream delivery guarantee.

That still stops short of a full bus runtime on purpose. The core in-process execution path is direct, synchronous, and process-local for handler invocation, code-first middleware execution, and retry timing: inbox-backed idempotency can make completed-execution duplicate suppression durable through a registered `IInbox`, but it does not turn the lane into a broker, distributed dispatcher, durable retry queue, cross-node exactly-once delivery guarantee, or arbitrary background subscription poller. Native routing chooses or validates the effective Cephalon event channel before publication, but it does not create broker topology or claim partition/exchange/queue ownership. The publication runtime catalog is also bounded to the active publication path: direct in-process observations describe local subscription execution, while outbox-backed observations describe accepted staging and remain separate from later dispatch-state or broker-delivery truth. `eventing.subscribe` appears from the core pack only when `EnableInProcessSubscriptionExecution` is explicitly selected and a real executor exists; companion packs such as `Cephalon.Eventing.Wolverine` remain optional provider-managed ways to activate durable staged dispatch and richer retry behavior on top of the same runtime-neutral contracts. In all modes `IEventSubscriptionExecutionBindingCatalog` and `event-subscriptions` distinguish `application-managed`, `cephalon-managed`, and provider-managed runtime-bound ownership through `dispatchRuntime`, `subscriptionRuntime`, `executionRuntimeId`, `executionOwnership`, `executionMode`, `subscriptionExecutionPipeline`, and `binding.*` metadata while keeping the underlying core pack host-agnostic. Companion packs can continue adding more runtime truth under the same `event-driven-integration` technology when they have something honest to say. For example, the current `Cephalon.Data.EntityFramework`, `Cephalon.Data.MongoDB`, `Cephalon.Data.Redis`, `Cephalon.Data.Elasticsearch`, `Cephalon.Data.OpenSearch`, `Cephalon.Data.Neo4j`, `Cephalon.Data.Qdrant`, and `Cephalon.Data.Nats` slices can now project staged-only outbox producers and application-managed inbox stores into the same technology surface set, the subscription metadata can now say when such an inbox store is available, the native in-process lane can deliberately consume one such inbox for completed-execution idempotency, application-managed handlers can now report their latest runtime state and retry intent through `IEventSubscriptionRuntimeReporter` / `IEventSubscriptionRuntimeCatalog`, application-managed dispatch paths can now report outbox-specific dispatch observations through `IEventDispatchRuntimeReporter` / `IEventDispatchRuntimeCatalog`, configured dispatch runtimes now remain visible through `IEventDispatchRuntimeDescriptorCatalog` even before runtime reports arrive, the `IEventDispatchStore` contract can now bridge pending staged outbox rows plus durable dispatch outcomes across both relational and the current non-relational outbox packs without baking a specific broker into the pack, runtime descriptors now surface the canonical aggregate `Summary` instead of forcing each consumer to re-aggregate `IEventDispatchRuntimeCatalog` ad hoc, outbox descriptors now carry an effective dispatch policy instead of leaving `/engine/outboxes` as a static staged-only answer, terminal dispatch posture now appears directly as state/summary fields and `event-dispatches` / `event-dispatch-runtimes` metadata before consumers fall back to `reported.*`, `event-dispatches` still projects only the per-outbox detail that belongs to the current path, hosted executions can still link themselves to one or more declared subscriptions through metadata so the surface can report application-managed execution linkage, execution-graph linkage, and live hosted-execution/runtime-story phase data without pretending that delivery, durable retries, handlers, or distributed subscription execution are already owned by the eventing pack, the current `Cephalon.Eventing.Behaviors` slice can now explicitly route `ISagaChoreographyPublisher` traffic into the same publication path without moving choreography ownership into `Cephalon.Eventing` itself, and the current `Cephalon.Eventing.Wolverine` slice can still opt into both a `wolverine-managed` staged-event dispatch loop and runtime-bound subscription execution proof on top of the same runtime-neutral contracts.

ClickHouse is now an explicit unsupported-by-design branch instead of a hidden parity gap: `Cephalon.Data.ClickHouse` still stages durable outbox records, but it now publishes `DispatchPolicy.PolicyId = unsupported` plus `ExecutionMode = disabled` so `/engine/outboxes`, `event-dispatches`, and runtime surfaces say clearly that the current analytics-first replacement semantics are staging-only and do not yet own mutable dispatch state. `Cephalon.Data.Cassandra` now takes the opposite path through a Cassandra-native sharded pending-dispatch index, so wide-column workloads can stay on the same consumer-managed or adapter-managed dispatch path without relaying through a relational bridge.

## Adapter stance

Current Cephalon direction for messaging adapters:

- `Cephalon.Eventing` now also has an opt-in `cephalon-managed` direct in-process subscription execution baseline, with config-driven channel descriptor discovery, code-first subscription descriptors, descriptor-bearing executors, and execution middleware, optional bounded process-local retries using fixed/exponential backoff plus deterministic jitter metadata, duplicate-completed execution suppression backed either by process-local memory or by exactly one registered `IInbox`, config-driven publication routing, and bounded process-local scheduled publication acceptance, for lightweight hosts that do not need a durable broker or Wolverine
- `Cephalon.Eventing.Wolverine` is the current shipped optional companion adapter proof and now supports host wiring, an opt-in `wolverine-managed` durable dispatch loop on top of `IEventDispatchStore`, bounded dispatch retry with terminal failed state, and a provider-managed runtime-bound subscription execution lane on top of the same staged-publication path
- `MassTransit` and `NServiceBus` are reference benchmarks for transport topology, routing, saga, recoverability, outbox, and operator tooling semantics; they should not become shipped adapter dependencies unless a future slice explicitly targets interop or migration compatibility
- other messaging libraries such as `MediatR`, `LiteBus`, `NServiceBus`, and `SlimMessageBus` are allowed in consumer apps, but they are consumer-owned integrations rather than shipped Cephalon companion adapters in the current phase
- Cephalon should not claim runtime truth for those consumer-owned integrations unless a dedicated bridge or adapter package actually projects that state back into the engine surfaces
- engine readiness should come from `Cephalon.Eventing` owning the runtime-neutral contracts and truthful operator surfaces; companion adapters are optional ways for a consumer app to activate one managed runtime on top of those seams
- one message flow should have one durable-messaging owner; avoid mixing Cephalon/Wolverine-managed outbox, inbox, retry, or dispatch semantics with a second durable bus/runtime for the same flow
- adapter-owned diagnostics conventions should be projected through `/engine/diagnostics` when a shipped companion adapter emits stable event ids, which the current Wolverine slice now does for its managed dispatch loop

## Related docs

- [Cephalon.Eventing.Behaviors](eventing-behaviors.md)
- [Technology packs](../technology-packs.md)
- [Module authoring](../module-authoring.md)
