using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Execution;
using Cephalon.Abstractions.Technologies;
using Cephalon.Eventing.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;

namespace Cephalon.Eventing.Services;

internal sealed class EventingSuperiorityProfileRuntimeSurfaceContributor(
    EventingOptions options,
    EventingRuntimeTopology topology,
    IServiceScopeFactory scopeFactory) : ITechnologyRuntimeContributor
{
    private const string ReferenceFrameworks = "MassTransit,NServiceBus,Wolverine,MediatR";
    private const string ClaimPolicy = "claimed-only-with-runtime-evidence";
    private const string RemediationFilteredReadBenchmarks = "FilterSummaryByMessageId,FilterRetentionByMessageId,FilterLatestByCorrelationId,FilterOldestByDispatchOutcome,FilterOperatorDashboardSelectors";
    private const string BehaviorEventingRuntimeSurfaceContributorTypeName = "Cephalon.Eventing.Behaviors.Services.BehaviorEventingRuntimeSurfaceContributor";
    private const string EventingSagaChoreographyPublisherTypeName = "Cephalon.Eventing.Behaviors.Services.EventingSagaChoreographyPublisher";

    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        var routeCount = options.PublicationRoutes.Count.ToString(CultureInfo.InvariantCulture);
        var routingStatus = options.EnablePublicationRouting && options.PublicationRoutes.Count > 0
            ? "claimed"
            : options.EnablePublicationRouting ? "partial" : "not-claimed";
        var routingEvidence = options.EnablePublicationRouting
            ? $"policy={EventPublicationRoutingPolicy.GetPolicyId(options)} routes={routeCount} autoChannel={EventPublicationRoutingPolicy.GetAutoChannelId(options)}"
            : "publication routing is not enabled.";
        var brokerTopology = ResolveBrokerTopologyProfile(routeCount);
        var providerPartition = ResolveProviderPartitionProfile(routeCount);
        var downstreamDeliveryCompletion = ResolveDownstreamDeliveryCompletionProfile();
        var brokerInboundConsumption = ResolveBrokerInboundConsumptionProfile();
        var serializationVersioning = ResolveSerializationVersioningProfile();
        var tenantCorrelation = ResolveTenantCorrelationProfile();
        var scheduledDelivery = ResolveScheduledDeliveryProfile();
        var durableRetryQueue = ResolveDurableRetryQueueProfile();
        var idempotencyOwnership = ResolveIdempotencyOwnershipProfile();
        var subscriptionConcurrency = ResolveSubscriptionConcurrencyProfile();
        var subscriptionOrdering = ResolveSubscriptionOrderingProfile();
        var processManagerState = ResolveProcessManagerStateProfile();
        var choreographyHandoff = ResolveChoreographyHandoffEvidence();
        var remediationReadPerformanceStatus = topology.HasOutboxPublishingPath ? "claimed" : "partial";
        var remediationReadPerformanceEvidence = topology.HasOutboxPublishingPath
            ? $"benchmarks={RemediationFilteredReadBenchmarks}; readPolicy=single-pass-retained-catalog; materialization=not-required; wolverineRequired=false"
            : $"benchmark guardrails exist for {RemediationFilteredReadBenchmarks}; no outbox-backed command path is active.";
        var brokerDeadLetterReplay = ResolveBrokerDeadLetterReplayProfile();
        var commandJournalDescriptor = ResolveCommandJournalDescriptor();
        var durableCommandJournalStatus = ResolveDurableCommandJournalStatus(commandJournalDescriptor);
        var durableCommandJournalEvidence = ResolveDurableCommandJournalEvidence(commandJournalDescriptor);
        var durableCommandJournalNextGap = ResolveDurableCommandJournalNextGap(commandJournalDescriptor);
        var durableCommandJournalReplayStatus = ResolveDurableCommandJournalReplayStatus(commandJournalDescriptor);
        var durableCommandJournalReplayEvidence = ResolveDurableCommandJournalReplayEvidence(commandJournalDescriptor);
        var durableCommandJournalReplayNextGap = ResolveDurableCommandJournalReplayNextGap(commandJournalDescriptor);

        return new TechnologyRuntimeSurface(
            technologyId: "event-driven-integration",
            surfaceId: "eventing-superiority-profile",
            displayName: "Eventing Superiority Profile",
            description: "Runtime evidence for the Cephalon-native eventing target compared with established .NET messaging and mediator frameworks.",
            entries:
            [
                CreateEntry(
                    id: "configuration-first-provider-neutrality",
                    displayName: "Configuration-first Provider Neutrality",
                    description: "Keeps the app authoring surface on Cephalon configuration and runtime contracts instead of external bus APIs.",
                    status: "claimed",
                    evidence: "Engine:Technologies=EventDrivenIntegration; provider adapters remain explicit opt-ins.",
                    advantage: "Consumers can switch native, in-process, outbox, or provider-managed paths without rewriting application handlers around a vendor API.",
                    nextGap: "Keep adding provider-specific companion packs only behind explicit Engine:Messaging choices."),
                CreateEntry(
                    id: "routing-and-provider-portability",
                    displayName: "Routing And Provider Portability",
                    description: "Routes event publications through a Cephalon-owned event-type map before any in-process, outbox, or provider-managed publisher receives the message.",
                    status: routingStatus,
                    evidence: routingEvidence,
                    advantage: "Application code can publish against a stable Cephalon route contract while channel ownership moves through configuration instead of Wolverine, MassTransit, NServiceBus, or MediatR APIs.",
                    nextGap: options.EnablePublicationRouting && options.PublicationRoutes.Count > 0
                        ? "Extend this same route truth into future broker-topology and generated contract validation surfaces."
                        : "Configure Engine:Messaging:Publications:Routing:Routes before claiming routing evidence."),
                CreateEntry(
                    id: "broker-topology-materialization-ownership",
                    displayName: "Broker Topology Materialization Ownership",
                    description: "Makes broker exchange, queue, topic, and partition topology ownership explicit instead of inferring it from Cephalon publication routing.",
                    status: brokerTopology.Status,
                    evidence: brokerTopology.Evidence,
                    advantage: "Teams can use Cephalon channel routing without assuming the engine silently provisions provider-specific topology or binds application code to a broker API.",
                    nextGap: brokerTopology.NextGap),
                CreateEntry(
                    id: "provider-partition-ownership",
                    displayName: "Provider Partition Ownership",
                    description: "Makes provider-specific partition assignment, affinity, rebalancing, and ordering guarantees explicit instead of inferring them from Cephalon route or broker-topology metadata.",
                    status: providerPartition.Status,
                    evidence: providerPartition.Evidence,
                    advantage: "Teams can route events through Cephalon without assuming the engine silently owns provider partition placement or per-partition ordering semantics.",
                    nextGap: providerPartition.NextGap),
                CreateEntry(
                    id: "downstream-delivery-completion-ownership",
                    displayName: "Downstream Delivery Completion Ownership",
                    description: "Makes provider destination delivery completion, subscriber acknowledgements, and exactly-once completion explicit instead of inferring them from outbox accepted handoff or dispatch reports.",
                    status: downstreamDeliveryCompletion.Status,
                    evidence: downstreamDeliveryCompletion.Evidence,
                    advantage: "Teams can read Cephalon publication and dispatch truth without assuming the engine silently proves broker/provider destination delivery or subscriber acknowledgement.",
                    nextGap: downstreamDeliveryCompletion.NextGap),
                CreateEntry(
                    id: "broker-inbound-consumption-ownership",
                    displayName: "Broker Inbound Consumption Ownership",
                    description: "Makes provider-owned inbound broker consumption, acknowledgements, and offset checkpoints explicit instead of inferring them from declared subscriptions or in-process execution.",
                    status: brokerInboundConsumption.Status,
                    evidence: brokerInboundConsumption.Evidence,
                    advantage: "Teams can use Cephalon subscription descriptors, direct in-process execution, and optional provider bindings without assuming the core pack silently owns a generic broker consumer loop.",
                    nextGap: brokerInboundConsumption.NextGap),
                CreateEntry(
                    id: "serialization-and-contract-versioning-ownership",
                    displayName: "Serialization And Contract Versioning Ownership",
                    description: "Makes serializer selection, schema registry availability, event contract version negotiation, upcasting, and compatibility validation explicit instead of inferring them from event type or channel metadata.",
                    status: serializationVersioning.Status,
                    evidence: serializationVersioning.Evidence,
                    advantage: "Teams can use Cephalon event catalogs, routing, and runtime publication evidence without assuming the core pack silently owns a wire schema registry or version migration pipeline.",
                    nextGap: serializationVersioning.NextGap),
                CreateEntry(
                    id: "tenant-and-correlation-context-ownership",
                    displayName: "Tenant And Correlation Context Ownership",
                    description: "Makes tenant identity, correlation, causation, baggage, and message-header propagation explicit instead of inferring them from remediation command metadata, diagnostics tags, or route metadata.",
                    status: tenantCorrelation.Status,
                    evidence: tenantCorrelation.Evidence,
                    advantage: "Teams can use Cephalon operator correlation metadata, publication routing, and diagnostic tags without assuming the core pack silently owns cross-boundary context propagation.",
                    nextGap: tenantCorrelation.NextGap),
                CreateEntry(
                    id: "scheduled-and-delayed-delivery-ownership",
                    displayName: "Scheduled And Delayed Delivery Ownership",
                    description: "Makes bounded process-local delayed publication acceptance separate from durable scheduled delivery, broker delay queues, provider-owned scheduler recovery, and cross-node coordination.",
                    status: scheduledDelivery.Status,
                    evidence: scheduledDelivery.Evidence,
                    advantage: "Teams can use Cephalon's Wolverine-free delayed-publication acceptance without assuming the core pack silently owns durable cross-node scheduling or broker-native delayed delivery.",
                    nextGap: scheduledDelivery.NextGap),
                CreateEntry(
                    id: "durable-retry-queue-ownership",
                    displayName: "Durable Retry Queue Ownership",
                    description: "Makes bounded in-process retry, dispatch retry reports, and provider-managed retry observations separate from durable retry queues, broker error queues, retry persistence, and cross-node retry coordination.",
                    status: durableRetryQueue.Status,
                    evidence: durableRetryQueue.Evidence,
                    advantage: "Teams can use Cephalon retry metadata and Wolverine-free in-process retry without assuming the core pack silently owns a durable retry queue, broker error queue, or cross-node retry scheduler.",
                    nextGap: durableRetryQueue.NextGap),
                CreateEntry(
                    id: "idempotency-ownership",
                    displayName: "Idempotency Ownership",
                    description: "Makes process-local and inbox-backed completed-execution duplicate suppression separate from broker deduplication, exactly-once delivery, durable inbox command ownership, and cross-node idempotency leases.",
                    status: idempotencyOwnership.Status,
                    evidence: idempotencyOwnership.Evidence,
                    advantage: "Teams can use Cephalon's Wolverine-free duplicate-completed suppression metadata without assuming the core pack silently owns broker deduplication, exactly-once delivery, or a generic durable inbox command processor.",
                    nextGap: idempotencyOwnership.NextGap),
                CreateEntry(
                    id: "subscription-concurrency-ownership",
                    displayName: "Subscription Concurrency Ownership",
                    description: "Makes direct in-process subscription execution and code-first middleware separate from handler concurrency limits, consumer prefetch, backpressure, leases, and distributed work sharing.",
                    status: subscriptionConcurrency.Status,
                    evidence: subscriptionConcurrency.Evidence,
                    advantage: "Teams can use Cephalon's Wolverine-free direct execution path without assuming the core pack silently owns provider-grade concurrency, prefetch, or backpressure controls.",
                    nextGap: subscriptionConcurrency.NextGap),
                CreateEntry(
                    id: "subscription-ordering-ownership",
                    displayName: "Subscription Ordering Ownership",
                    description: "Makes direct in-process subscription execution and code-first middleware separate from handler ordering, per-key ordering, partition ordering, causal ordering, replay ordering, and cross-node ordering guarantees.",
                    status: subscriptionOrdering.Status,
                    evidence: subscriptionOrdering.Evidence,
                    advantage: "Teams can use Cephalon's Wolverine-free direct execution path without assuming local fan-out or provider bindings silently create ordering guarantees.",
                    nextGap: subscriptionOrdering.NextGap),
                CreateEntry(
                    id: "process-manager-state-ownership",
                    displayName: "Process Manager State Ownership",
                    description: "Makes declared subscriptions, direct execution, choreography bridge handoff, and outbox publication separate from durable saga or process-manager state ownership.",
                    status: processManagerState.Status,
                    evidence: processManagerState.Evidence,
                    advantage: "Teams can compose Cephalon subscriptions and choreography handoff without assuming the core eventing pack silently owns a saga state machine, timeout scheduler, or recovery journal.",
                    nextGap: processManagerState.NextGap),
                CreateEntry(
                    id: "choreography-handoff-ownership",
                    displayName: "Choreography Handoff Ownership",
                    description: "Makes behavior-owned choreography catalogs, live publication observations, optional Eventing bridge handoff, and outbox-backed publication separate from saga state ownership.",
                    status: choreographyHandoff.Status,
                    evidence: choreographyHandoff.Evidence,
                    advantage: "Teams can see whether choreography handoff is local, observed, or outbox-backed without adopting Wolverine or assuming a process-manager state machine exists.",
                    nextGap: choreographyHandoff.NextGap),
                CreateEntry(
                    id: "native-wolverine-free-baseline",
                    displayName: "Native Wolverine-free Baseline",
                    description: "Shows whether the active runtime has a Cephalon-owned eventing path without the Wolverine companion.",
                    status: topology.HasPublishingPath && !topology.HasExternalManagedSubscriptionExecutionBindings ? "claimed" : "partial",
                    evidence: topology.HasPublishingPath
                        ? "A native publication path is active; Wolverine remains optional."
                        : "Eventing descriptors are active, but no publication path is currently configured.",
                    advantage: "The core pack can remain host-agnostic and package-light while companion packs stay replaceable.",
                    nextGap: topology.HasPublishingPath
                        ? "Promote more durable operator actions into provider-neutral contracts."
                        : "Configure in-process execution or an outbox-backed publication path to activate runtime-owned publication."),
                CreateEntry(
                    id: "runtime-truth-and-operator-surfaces",
                    displayName: "Runtime Truth And Operator Surfaces",
                    description: "Projects channels, subscriptions, publication state, dispatch state, remediation advice, readiness, and claim maturity through Cephalon introspection.",
                    status: "claimed",
                    evidence: "event-channels,event-subscriptions,event-publishers,event-dispatches,event-dispatch-remediations,event-dispatch-remediation-commands,event-dispatch-runtimes,eventing-superiority-profile",
                    advantage: "Operators get one Cephalon snapshot instead of reverse-engineering the selected bus, mediator, hosted service, and outbox combination.",
                    nextGap: "Keep any new eventing claim paired with a runtime surface and snapshot field before documenting it as supported."),
                CreateEntry(
                    id: "recoverability-and-terminal-failure-posture",
                    displayName: "Recoverability And Terminal Failure Posture",
                    description: "Captures retry, retry exhaustion, skipped work, and terminal-failure posture through provider-neutral metadata.",
                    status: topology.HasDispatchRuntimeContributors || topology.HasInProcessSubscriptionExecutionPath
                        ? "claimed"
                        : topology.HasPublishingPath ? "partial" : "not-claimed",
                    evidence: topology.HasInProcessSubscriptionExecutionPath
                        ? $"in-process retry={InProcessEventingRetryPolicy.GetPolicyId(options)} attempts={InProcessEventingRetryPolicy.GetMaxAttempts(options).ToString(CultureInfo.InvariantCulture)} backoff={InProcessEventingRetryPolicy.GetBackoff(options)} jitter={InProcessEventingRetryPolicy.GetJitterPercent(options).ToString(CultureInfo.InvariantCulture)}"
                        : topology.HasDispatchRuntimeContributors ? "dispatch runtime contributors are configured." : "no managed retry runtime is active.",
                    advantage: "Retry truth is emitted as stable Cephalon metadata rather than hidden in a provider-specific error queue or middleware pipeline.",
                    nextGap: "Add provider-neutral broker dead-letter ownership before claiming full remediation parity."),
                CreateEntry(
                    id: "durability-and-outbox-portability",
                    displayName: "Durability And Outbox Portability",
                    description: "Separates staged publication, dispatch-store ownership, and provider-managed dispatch loops.",
                    status: topology.HasDispatchStore && topology.HasDispatchRuntimeContributors
                        ? "claimed"
                        : topology.HasDispatchStore ? "partial" : "not-claimed",
                    evidence: topology.HasDispatchStore
                        ? topology.HasDispatchRuntimeContributors ? "dispatch store and dispatch runtime contributor are active." : "dispatch store is available without a managed dispatch runtime."
                        : "no dispatch store is configured.",
                    advantage: "Cephalon can make outbox ownership truthful per provider instead of assuming one bus owns every delivery path.",
                    nextGap: "Broaden broker dead-letter, replay, and audit evidence only when a package owns those paths."),
                CreateEntry(
                    id: "mediator-style-in-process-low-ceremony",
                    displayName: "Mediator-style In-process Low Ceremony",
                    description: "Provides a lightweight direct execution lane for apps that need local notifications without a durable bus dependency.",
                    status: topology.HasInProcessSubscriptionExecutionPath ? "claimed" : "not-claimed",
                    evidence: topology.HasInProcessSubscriptionExecutionPath
                        ? topology.HasInProcessSubscriptionDescriptorDiscovery
                            ? "EnableInProcessSubscriptionExecution=true with registered IEventSubscriptionExecutor services and code-first descriptor discovery."
                            : "EnableInProcessSubscriptionExecution=true with registered IEventSubscriptionExecutor services."
                        : "in-process subscription execution is not enabled.",
                    advantage: "Teams get MediatR-style local dispatch while preserving the same event catalog, readiness, diagnostics, and future provider handoff seams.",
                    nextGap: topology.HasInProcessSubscriptionDescriptorDiscovery
                        ? "Add source-generated registration helpers and benchmark evidence before promoting larger-scale generated handler discovery."
                        : "Use EventSubscriptionAttribute or IEventSubscriptionDescriptorProvider on registered executors when the native path needs lower ceremony at larger scale."),
                CreateEntry(
                    id: "code-first-subscription-execution-pipeline",
                    displayName: "Code-first Subscription Execution Pipeline",
                    description: "Lets modules and hosts add type-safe middleware around native in-process subscription execution without string configuration.",
                    status: topology.SubscriptionExecutionMiddlewareCount > 0
                        ? "claimed"
                        : topology.HasInProcessSubscriptionExecutionPath ? "partial" : "not-claimed",
                    evidence: topology.HasInProcessSubscriptionExecutionPath
                        ? $"pipeline={topology.SubscriptionExecutionPipeline} middlewareCount={topology.SubscriptionExecutionMiddlewareCount.ToString(CultureInfo.InvariantCulture)}"
                        : "in-process subscription execution is not enabled.",
                    advantage: "Teams can add validation, tenancy, auditing, short-circuiting, or policy checks with MassTransit/NServiceBus-style filters while keeping publish/subscribe code-first and Wolverine-free.",
                    nextGap: topology.SubscriptionExecutionMiddlewareCount > 0
                        ? "Attach benchmark evidence and source-generated registration helpers before promoting lower-ceremony large-scale handler discovery."
                        : "Register IEventSubscriptionExecutionMiddleware services in code before claiming pipeline execution evidence."),
                CreateEntry(
                    id: "workflow-choreography-and-sagas",
                    displayName: "Workflow Choreography And Sagas",
                    description: "Keeps saga and process-manager ideas as Cephalon-owned choreography surfaces rather than bus-specific state-machine APIs.",
                    status: topology.HasSubscriptionContributors || topology.HasManagedSubscriptionExecutionBindings ? "partial" : "not-claimed",
                    evidence: topology.HasSubscriptionContributors
                        ? "declared subscriptions are available for choreography bridges."
                        : "no declared subscription contributors are active.",
                    advantage: "Choreography can move through Cephalon.Behaviors and event publication seams without forcing one saga runtime into the core pack.",
                    nextGap: "Add provider-neutral process-manager state, timeout, compensation, and correlation contracts before claiming full saga superiority."),
                CreateEntry(
                    id: "dead-letter-replay-and-remediation",
                    displayName: "Dead-letter Replay And Remediation",
                    description: "Distinguishes terminal failures from retryable failures and exposes bounded dispatch-store dead-letter intent before claiming broker queue ownership.",
                    status: topology.HasOutboxPublishingPath ? "partial" : "not-claimed",
                    evidence: topology.HasOutboxPublishingPath
                        ? "event-dispatch-remediations derives retry-pending, skipped, failed, and terminal-failure posture from reported dispatch state; supported dispatch stores expose retry-now, retry-later, skip, quarantine, and dispatch-store dead-letter commands plus bounded command-result reads with benchmark-guarded filtered selectors."
                        : "no outbox-backed dispatch reporting path is active.",
                    advantage: "The engine can explain remediation posture without depending on Wolverine, MassTransit, NServiceBus, or a broker-specific dead-letter API.",
                    nextGap: "Ship broker dead-letter queue ownership only when a provider companion can prove that path with audit evidence."),
                CreateEntry(
                    id: "broker-dead-letter-replay-ownership",
                    displayName: "Broker Dead-letter Replay Ownership",
                    description: "Makes broker dead-letter queue ownership and broker replay support explicit instead of inferring them from dispatch-store dead-letter intent or durable command-journal replay.",
                    status: brokerDeadLetterReplay.Status,
                    evidence: brokerDeadLetterReplay.Evidence,
                    advantage: "Operators can see that Cephalon-owned command journals and dispatch-store remediation do not silently promise broker DLQ mutation, broker replay, or a Wolverine dependency.",
                    nextGap: brokerDeadLetterReplay.NextGap),
                CreateEntry(
                    id: "native-remediation-operator-read-performance",
                    displayName: "Native Remediation Operator Read Performance",
                    description: "Keeps remediation dashboard summary, retention, latest, oldest, and combined selector reads on Cephalon-owned single-pass catalog paths.",
                    status: remediationReadPerformanceStatus,
                    evidence: remediationReadPerformanceEvidence,
                    advantage: "Operator dashboards can drill into retained command posture through engine read models with guardrails, without bus-specific consoles, external dashboards, or config-driven subscription wiring.",
                    nextGap: "Extend the same benchmark evidence to broker dead-letter replay ownership."),
                CreateEntry(
                    id: "durable-remediation-command-audit",
                    displayName: "Durable Remediation Command Audit",
                    description: "Shows whether the active remediation command journal can preserve operator command audit and idempotency evidence beyond one process.",
                    status: durableCommandJournalStatus,
                    evidence: durableCommandJournalEvidence,
                    advantage: "Operators can audit and de-duplicate remediation commands through a Cephalon-owned journal contract while Wolverine, MassTransit, NServiceBus, and broker consoles remain optional.",
                    nextGap: durableCommandJournalNextGap),
                CreateEntry(
                    id: "durable-command-journal-replay-cursor",
                    displayName: "Durable Command-journal Replay Cursor",
                    description: "Shows whether the active remediation command journal exposes a durable, ordered replay cursor for command records without treating broker replay as claimed.",
                    status: durableCommandJournalReplayStatus,
                    evidence: durableCommandJournalReplayEvidence,
                    advantage: "Provider code can resume command-journal replay through a Cephalon-owned cursor contract while ASP.NET Core paging, broker dead-letter queues, and Wolverine remain separate optional concerns.",
                    nextGap: durableCommandJournalReplayNextGap),
                CreateEntry(
                    id: "observability-compliance-and-auditability",
                    displayName: "Observability Compliance And Auditability",
                    description: "Publishes stable diagnostics, redaction-aware activity tags, runtime states, and claim maturity for review.",
                    status: "claimed",
                    evidence: "Cephalon.Eventing diagnostics convention; eventing publication activity tags; runtime metadata; claim statuses.",
                    advantage: "Audit and operations teams can see what is supported, partial, or unclaimed without reading provider internals.",
                    nextGap: "Attach benchmark results and release-scorecard evidence to each promoted claim."),
                CreateEntry(
                    id: "testability-and-benchmark-evidence",
                    displayName: "Testability And Benchmark Evidence",
                    description: "Requires every promoted capability to carry focused tests, docs, and later benchmark evidence.",
                    status: "partial",
                    evidence: $"composition and hosting tests prove current runtime surfaces; remediation filtered-read guardrails now cover {RemediationFilteredReadBenchmarks}; broader native and provider-managed eventing benchmarks remain later gates.",
                    advantage: "Cephalon separates tested runtime truth from aspirational roadmap items.",
                    nextGap: "Promote repeatable cold-start, broker dispatch, durable journal, and provider-managed eventing benchmarks.")
            ]);
    }

    private EventDispatchRemediationCommandJournalDescriptor? ResolveCommandJournalDescriptor()
    {
        using var scope = scopeFactory.CreateScope();
        return scope.ServiceProvider.GetService<IEventDispatchRemediationCommandJournal>()?.Descriptor;
    }

    private ChoreographyHandoffProfile ResolveChoreographyHandoffEvidence()
    {
        using var scope = scopeFactory.CreateScope();
        var choreographyCatalog = scope.ServiceProvider.GetService<ISagaChoreographyRuntimeCatalog>();
        var publicationStateCatalog = scope.ServiceProvider.GetService<ISagaChoreographyPublicationRuntimeStateCatalog>();
        var eventingBridgeConfigured = IsEventingBehaviorBridgeConfigured(scope.ServiceProvider);

        var choreographies = choreographyCatalog?.SagaChoreographies ?? [];
        var publicationStates = publicationStateCatalog?.States ?? [];
        var choreographyCount = choreographies.Count.ToString(CultureInfo.InvariantCulture);
        var publicationStateCount = publicationStates.Count.ToString(CultureInfo.InvariantCulture);
        var acceptedHandoffs = publicationStates.Sum(static state => state.AcceptedCount).ToString(CultureInfo.InvariantCulture);
        var failedHandoffs = publicationStates.Sum(static state => state.FailedCount).ToString(CultureInfo.InvariantCulture);
        var compensationHandoffs = publicationStates.Count(static state => state.IsCompensation).ToString(CultureInfo.InvariantCulture);
        var handoffProvenCount = publicationStates
            .Count(state => IsChoreographyHandoffProof(state, eventingBridgeConfigured, topology.HasOutboxPublishingPath))
            .ToString(CultureInfo.InvariantCulture);
        var handoffState = SelectBestChoreographyHandoffProof(
            publicationStates,
            eventingBridgeConfigured,
            topology.HasOutboxPublishingPath);
        var choreographyCatalogState = choreographyCatalog is null ? "not-present" : "present";
        var publicationStateCatalogState = publicationStateCatalog is null ? "not-present" : "present";
        var eventingBridge = eventingBridgeConfigured && topology.HasOutboxPublishingPath
            ? "active"
            : eventingBridgeConfigured ? "blocked" : "not-active";
        var outboxHandoff = topology.HasOutboxPublishingPath ? "available" : "not-active";
        var handoffDurability = eventingBridgeConfigured && topology.HasOutboxPublishingPath
            ? "outbox-backed"
            : eventingBridgeConfigured ? "missing-outbox" : "not-active";
        var status = eventingBridgeConfigured && topology.HasOutboxPublishingPath
            ? "claimed"
            : choreographyCatalog is not null || publicationStateCatalog is not null
                ? "partial"
                : "not-claimed";
        var nextGap = eventingBridgeConfigured && topology.HasOutboxPublishingPath
            ? "Add provider-neutral process-manager state ownership only through the separate process-manager dimension."
            : choreographyCatalog is not null || publicationStateCatalog is not null
                ? "Activate the explicit Eventing behavior bridge with an outbox-backed publish path before claiming choreography handoff ownership."
                : "Activate behavior choreography catalogs and the explicit Eventing bridge before claiming choreography handoff ownership.";

        var evidence = handoffState is null
            ? string.Create(
                CultureInfo.InvariantCulture,
                $"choreographyCatalog={choreographyCatalogState}; choreographyCount={choreographyCount}; publicationStateCatalog={publicationStateCatalogState}; publicationStateCount={publicationStateCount}; acceptedHandoffs={acceptedHandoffs}; failedHandoffs={failedHandoffs}; compensationHandoffs={compensationHandoffs}; choreographyHandoffProofSelection=latest-proven-publication-state; choreographyHandoffStateCount={publicationStateCount}; choreographyHandoffProvenCount={handoffProvenCount}; publicationStateId=not-reported; behaviorId=not-reported; publicationId=not-reported; channelId=not-reported; handoffPublisher=not-reported; handoffSourceModule=not-reported; lastOutcome=not-reported; lastObservedAtUtc=not-reported; eventingBridge={eventingBridge}; outboxHandoff={outboxHandoff}; handoffDurability={handoffDurability}; processManagerState=not-claimed; sagaStatePersistence=not-claimed; wolverineRequired=false")
            : string.Create(
                CultureInfo.InvariantCulture,
                $"choreographyCatalog={choreographyCatalogState}; choreographyCount={choreographyCount}; publicationStateCatalog={publicationStateCatalogState}; publicationStateCount={publicationStateCount}; acceptedHandoffs={acceptedHandoffs}; failedHandoffs={failedHandoffs}; compensationHandoffs={compensationHandoffs}; choreographyHandoffProofSelection=latest-proven-publication-state; choreographyHandoffStateCount={publicationStateCount}; choreographyHandoffProvenCount={handoffProvenCount}; publicationStateId={handoffState.Id}; behaviorId={handoffState.BehaviorId}; publicationId={handoffState.PublicationId}; channelId={handoffState.ChannelId}; handoffPublisher={FormatMetadataValue(handoffState.LastPublisherType)}; handoffSourceModule={FormatMetadataValue(handoffState.SourceModuleId)}; lastOutcome={FormatMetadataValue(handoffState.LastOutcome)}; lastObservedAtUtc={FormatObservedAt(handoffState.LastObservedAtUtc)}; eventingBridge={eventingBridge}; outboxHandoff={outboxHandoff}; handoffDurability={handoffDurability}; processManagerState=not-claimed; sagaStatePersistence=not-claimed; wolverineRequired=false");

        return new ChoreographyHandoffProfile(status, evidence, nextGap);
    }

    private static bool IsEventingBehaviorBridgeConfigured(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetServices<ITechnologyRuntimeContributor>().Any(
            static contributor => string.Equals(
                contributor.GetType().FullName,
                BehaviorEventingRuntimeSurfaceContributorTypeName,
                StringComparison.Ordinal));
    }

    private BrokerTopologyProfile ResolveBrokerTopologyProfile(string routeCount)
    {
        var publicationPath = topology.HasPublishingPath ? "active" : "not-active";
        var dispatchRuntime = topology.HasDispatchRuntimeContributors ? "reported" : "not-reported";
        var routingPolicy = options.EnablePublicationRouting
            ? EventPublicationRoutingPolicy.GetPolicyId(options)
            : "not-enabled";
        var autoChannel = options.EnablePublicationRouting
            ? EventPublicationRoutingPolicy.GetAutoChannelId(options)
            : "not-configured";

        using var scope = scopeFactory.CreateScope();
        var dispatchRuntimeCatalog = scope.ServiceProvider.GetService<IEventDispatchRuntimeCatalog>();
        var topologyStates = dispatchRuntimeCatalog?.States
            .Where(static state =>
                state.Metadata.TryGetValue(EventDispatchRuntimeMetadataKeys.BrokerTopologyMaterialization, out var value) &&
                string.Equals(value, "provider-reported", StringComparison.OrdinalIgnoreCase))
            .ToArray() ?? [];
        var topologyProvenCount = topologyStates.Count(IsBrokerTopologyProof);
        var topologyState = SelectBestDispatchProof(
            topologyStates,
            IsBrokerTopologyProof);

        if (!topology.HasPublishingPath)
        {
            return new BrokerTopologyProfile(
                "not-claimed",
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"publicationPath={publicationPath}; routingPolicy={routingPolicy}; routes={routeCount}; autoChannel={autoChannel}; dispatchRuntime={dispatchRuntime}; brokerTopologyProofSelection=latest-proven-dispatch-state; brokerTopologyStateCount={topologyStates.Length.ToString(CultureInfo.InvariantCulture)}; brokerTopologyProvenCount={topologyProvenCount.ToString(CultureInfo.InvariantCulture)}; brokerTopologyMaterialization=not-claimed; brokerTopologyMaterializationSource=not-reported; exchangeProvisioning=not-claimed; exchangeProvisioningId=not-reported; queueProvisioning=not-claimed; queueProvisioningId=not-reported; topicProvisioning=not-claimed; topicProvisioningId=not-reported; partitionProvisioning=not-claimed; partitionProvisioningId=not-reported; topologyVerification=not-claimed; topologyVerificationId=not-reported; providerOwnedTopology=not-present; providerTopologyId=not-reported; wolverineRequired=false"),
                "Add a publishing path plus provider-owned broker topology proof before claiming topology materialization.");
        }

        if (topologyState is not null)
        {
            var metadata = topologyState.Metadata;
            var source = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.BrokerTopologyMaterializationSource,
                "not-reported");
            var brokerTopologyMaterialization = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.BrokerTopologyMaterialization,
                "not-claimed");
            var exchangeProvisioning = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.ExchangeProvisioning,
                "not-claimed");
            var exchangeProvisioningId = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.ExchangeProvisioningId,
                "not-reported");
            var queueProvisioning = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.QueueProvisioning,
                "not-claimed");
            var queueProvisioningId = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.QueueProvisioningId,
                "not-reported");
            var topicProvisioning = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.TopicProvisioning,
                "not-claimed");
            var topicProvisioningId = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.TopicProvisioningId,
                "not-reported");
            var partitionProvisioning = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.PartitionProvisioning,
                "not-claimed");
            var partitionProvisioningId = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.PartitionProvisioningId,
                "not-reported");
            var topologyVerification = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.TopologyVerification,
                "not-claimed");
            var topologyVerificationId = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.TopologyVerificationId,
                "not-reported");
            var providerOwnedTopology = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.ProviderOwnedTopology,
                "not-present");
            var providerTopologyId = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.ProviderTopologyId,
                "not-reported");
            var status = EventDispatchBrokerTopologyMetadata.IsTopologyMaterialized(metadata)
                ? "claimed"
                : "partial";
            var nextGap = status == "claimed"
                ? "Keep provider broker topology provisioning and verification proof covered by provider integration tests."
                : "Complete exchange, queue, topic, partition, verification, and provider topology evidence before claiming broker topology materialization.";

            return new BrokerTopologyProfile(
                status,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"publicationPath=active; routingPolicy={routingPolicy}; routes={routeCount}; autoChannel={autoChannel}; dispatchRuntime={dispatchRuntime}; brokerTopologyProofSelection=latest-proven-dispatch-state; brokerTopologyStateCount={topologyStates.Length.ToString(CultureInfo.InvariantCulture)}; brokerTopologyProvenCount={topologyProvenCount.ToString(CultureInfo.InvariantCulture)}; brokerTopologyMaterialization={brokerTopologyMaterialization}; brokerTopologyMaterializationSource={source}; exchangeProvisioning={exchangeProvisioning}; exchangeProvisioningId={exchangeProvisioningId}; queueProvisioning={queueProvisioning}; queueProvisioningId={queueProvisioningId}; topicProvisioning={topicProvisioning}; topicProvisioningId={topicProvisioningId}; partitionProvisioning={partitionProvisioning}; partitionProvisioningId={partitionProvisioningId}; topologyVerification={topologyVerification}; topologyVerificationId={topologyVerificationId}; providerOwnedTopology={providerOwnedTopology}; providerTopologyId={providerTopologyId}; outboxId={topologyState.OutboxId}; lastOutcome={topologyState.LastOutcome ?? "unknown"}; lastObservedAtUtc={FormatObservedAt(topologyState.LastObservedAtUtc)}; wolverineRequired=false"),
                nextGap);
        }

        return new BrokerTopologyProfile(
            "not-claimed",
            string.Create(
                CultureInfo.InvariantCulture,
                $"publicationPath=active; routingPolicy={routingPolicy}; routes={routeCount}; autoChannel={autoChannel}; dispatchRuntime={dispatchRuntime}; brokerTopologyProofSelection=latest-proven-dispatch-state; brokerTopologyStateCount={topologyStates.Length.ToString(CultureInfo.InvariantCulture)}; brokerTopologyProvenCount={topologyProvenCount.ToString(CultureInfo.InvariantCulture)}; brokerTopologyMaterialization=not-claimed; brokerTopologyMaterializationSource=not-reported; exchangeProvisioning=not-claimed; exchangeProvisioningId=not-reported; queueProvisioning=not-claimed; queueProvisioningId=not-reported; topicProvisioning=not-claimed; topicProvisioningId=not-reported; partitionProvisioning=not-claimed; partitionProvisioningId=not-reported; topologyVerification=not-claimed; topologyVerificationId=not-reported; providerOwnedTopology=not-present; providerTopologyId=not-reported; wolverineRequired=false"),
            "Add a provider-owned broker topology descriptor plus exchange, queue, topic, partition, and verification evidence before claiming topology materialization.");
    }

    private ProviderPartitionProfile ResolveProviderPartitionProfile(string routeCount)
    {
        var publicationPath = topology.HasPublishingPath ? "active" : "not-active";
        var dispatchRuntime = topology.HasDispatchRuntimeContributors ? "reported" : "not-reported";
        var routingPolicy = options.EnablePublicationRouting
            ? EventPublicationRoutingPolicy.GetPolicyId(options)
            : "not-enabled";
        var autoChannel = options.EnablePublicationRouting
            ? EventPublicationRoutingPolicy.GetAutoChannelId(options)
            : "not-configured";

        using var scope = scopeFactory.CreateScope();
        var dispatchRuntimeCatalog = scope.ServiceProvider.GetService<IEventDispatchRuntimeCatalog>();
        var partitionStates = dispatchRuntimeCatalog?.States
            .Where(static state =>
                state.Metadata.TryGetValue(EventDispatchRuntimeMetadataKeys.ProviderPartitionOwnership, out var value) &&
                string.Equals(value, "provider-reported", StringComparison.OrdinalIgnoreCase))
            .ToArray() ?? [];
        var partitionProvenCount = partitionStates.Count(IsProviderPartitionProof);
        var partitionState = SelectBestDispatchProof(
            partitionStates,
            IsProviderPartitionProof);

        if (!topology.HasPublishingPath)
        {
            return new ProviderPartitionProfile(
                "not-claimed",
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"publicationPath={publicationPath}; routingPolicy={routingPolicy}; routes={routeCount}; autoChannel={autoChannel}; dispatchRuntime={dispatchRuntime}; providerPartitionProofSelection=latest-proven-dispatch-state; providerPartitionStateCount={partitionStates.Length.ToString(CultureInfo.InvariantCulture)}; providerPartitionProvenCount={partitionProvenCount.ToString(CultureInfo.InvariantCulture)}; providerPartitionOwnership=not-claimed; providerPartitionOwnershipSource=not-reported; partitionAssignment=not-claimed; partitionAssignmentId=not-reported; partitionAffinity=not-claimed; partitionAffinityId=not-reported; partitionRebalancing=not-claimed; partitionRebalancingId=not-reported; partitionOrderingGuarantee=not-claimed; partitionOrderingGuaranteeId=not-reported; providerOwnedPartitioning=not-present; providerPartitioningId=not-reported; wolverineRequired=false"),
                "Add a publishing path plus provider-owned partition proof before claiming provider partition ownership.");
        }

        if (partitionState is not null)
        {
            var metadata = partitionState.Metadata;
            var source = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.ProviderPartitionOwnershipSource,
                "not-reported");
            var providerPartitionOwnership = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.ProviderPartitionOwnership,
                "not-claimed");
            var partitionAssignment = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.PartitionAssignment,
                "not-claimed");
            var partitionAssignmentId = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.PartitionAssignmentId,
                "not-reported");
            var partitionAffinity = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.PartitionAffinity,
                "not-claimed");
            var partitionAffinityId = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.PartitionAffinityId,
                "not-reported");
            var partitionRebalancing = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.PartitionRebalancing,
                "not-claimed");
            var partitionRebalancingId = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.PartitionRebalancingId,
                "not-reported");
            var partitionOrderingGuarantee = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.PartitionOrderingGuarantee,
                "not-claimed");
            var partitionOrderingGuaranteeId = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.PartitionOrderingGuaranteeId,
                "not-reported");
            var providerOwnedPartitioning = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.ProviderOwnedPartitioning,
                "not-present");
            var providerPartitioningId = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.ProviderPartitioningId,
                "not-reported");
            var status = EventDispatchProviderPartitionMetadata.IsPartitionOwnershipProven(metadata)
                ? "claimed"
                : "partial";
            var nextGap = status == "claimed"
                ? "Keep provider partition assignment, affinity, rebalancing, and ordering proof covered by provider integration tests."
                : "Complete assignment, affinity, rebalancing, ordering, and provider-owned partition evidence before claiming provider partition ownership.";

            return new ProviderPartitionProfile(
                status,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"publicationPath=active; routingPolicy={routingPolicy}; routes={routeCount}; autoChannel={autoChannel}; dispatchRuntime={dispatchRuntime}; providerPartitionProofSelection=latest-proven-dispatch-state; providerPartitionStateCount={partitionStates.Length.ToString(CultureInfo.InvariantCulture)}; providerPartitionProvenCount={partitionProvenCount.ToString(CultureInfo.InvariantCulture)}; providerPartitionOwnership={providerPartitionOwnership}; providerPartitionOwnershipSource={source}; partitionAssignment={partitionAssignment}; partitionAssignmentId={partitionAssignmentId}; partitionAffinity={partitionAffinity}; partitionAffinityId={partitionAffinityId}; partitionRebalancing={partitionRebalancing}; partitionRebalancingId={partitionRebalancingId}; partitionOrderingGuarantee={partitionOrderingGuarantee}; partitionOrderingGuaranteeId={partitionOrderingGuaranteeId}; providerOwnedPartitioning={providerOwnedPartitioning}; providerPartitioningId={providerPartitioningId}; outboxId={partitionState.OutboxId}; lastOutcome={partitionState.LastOutcome ?? "unknown"}; lastObservedAtUtc={FormatObservedAt(partitionState.LastObservedAtUtc)}; wolverineRequired=false"),
                nextGap);
        }

        return new ProviderPartitionProfile(
            "not-claimed",
            string.Create(
                CultureInfo.InvariantCulture,
                $"publicationPath=active; routingPolicy={routingPolicy}; routes={routeCount}; autoChannel={autoChannel}; dispatchRuntime={dispatchRuntime}; providerPartitionProofSelection=latest-proven-dispatch-state; providerPartitionStateCount={partitionStates.Length.ToString(CultureInfo.InvariantCulture)}; providerPartitionProvenCount={partitionProvenCount.ToString(CultureInfo.InvariantCulture)}; providerPartitionOwnership=not-claimed; providerPartitionOwnershipSource=not-reported; partitionAssignment=not-claimed; partitionAssignmentId=not-reported; partitionAffinity=not-claimed; partitionAffinityId=not-reported; partitionRebalancing=not-claimed; partitionRebalancingId=not-reported; partitionOrderingGuarantee=not-claimed; partitionOrderingGuaranteeId=not-reported; providerOwnedPartitioning=not-present; providerPartitioningId=not-reported; wolverineRequired=false"),
            "Add a provider-owned partition descriptor plus assignment, affinity, rebalancing, and ordering evidence before claiming provider partition ownership.");
    }

    private DownstreamDeliveryCompletionProfile ResolveDownstreamDeliveryCompletionProfile()
    {
        using var scope = scopeFactory.CreateScope();
        var dispatchRuntimeCatalog = scope.ServiceProvider.GetService<IEventDispatchRuntimeCatalog>();
        var completedStates = dispatchRuntimeCatalog?.States
            .Where(static state =>
                state.Metadata.TryGetValue(EventDispatchRuntimeMetadataKeys.DownstreamDeliveryCompletion, out var value) &&
                string.Equals(value, "provider-reported", StringComparison.OrdinalIgnoreCase))
            .ToArray() ?? [];
        var completedProvenCount = completedStates.Count(IsDownstreamDeliveryCompletionProof);
        var completedState = SelectBestDispatchProof(
            completedStates,
            IsDownstreamDeliveryCompletionProof);
        var dispatchRuntime = topology.HasDispatchRuntimeContributors ? "reported" : "not-reported";

        if (!topology.HasPublishingPath)
        {
            return new DownstreamDeliveryCompletionProfile(
                "not-claimed",
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"no publication path is active; dispatchRuntime={dispatchRuntime}; downstreamDeliveryProofSelection=latest-proven-dispatch-state; downstreamDeliveryStateCount={completedStates.Length.ToString(CultureInfo.InvariantCulture)}; downstreamDeliveryProvenCount={completedProvenCount.ToString(CultureInfo.InvariantCulture)}; downstreamDeliveryCompletion=not-claimed; providerDeliveryReceipt=not-present; subscriberAcknowledgement=not-claimed; destinationCommit=not-claimed; exactlyOnceDelivery=not-claimed; wolverineRequired=false"),
                "Add a publishing path before claiming downstream delivery completion evidence.");
        }

        var handoff = topology.HasOutboxPublishingPath ? "outbox-accepted" : "direct-or-provider-publisher";
        if (completedState is not null)
        {
            var metadata = completedState.Metadata;
            var providerReceipt = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.ProviderDeliveryReceipt,
                "not-present");
            var subscriberAcknowledgement = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.SubscriberAcknowledgement,
                "not-claimed");
            var destinationCommit = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.DestinationCommit,
                "not-claimed");
            var exactlyOnceDelivery = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.ExactlyOnceDelivery,
                "not-claimed");
            var source = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.DownstreamDeliveryCompletionSource,
                "not-reported");
            var receiptId = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.ProviderDeliveryReceiptId,
                "not-reported");
            var subscriberAcknowledgementId = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.SubscriberAcknowledgementId,
                "not-reported");
            var destinationCommitId = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.DestinationCommitId,
                "not-reported");
            var exactlyOnceDeliverySource = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.ExactlyOnceDeliverySource,
                "not-reported");
            var exactlyOnceDeliveryProofId = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.ExactlyOnceDeliveryProofId,
                "not-reported");
            var exactlyOnceDeliveryStrategy = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.ExactlyOnceDeliveryStrategy,
                "not-reported");
            var status = IsDownstreamDeliveryCompletionProof(completedState)
                ? "claimed"
                : "partial";
            var nextGap = status == "claimed"
                ? "Keep provider completion, subscriber acknowledgement, destination commit, and exactly-once proof covered by provider integration tests."
                : "Add subscriber acknowledgement, destination commit, and exactly-once proof before claiming full downstream delivery completion ownership.";

            return new DownstreamDeliveryCompletionProfile(
                status,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"publicationPath=active; handoff={handoff}; dispatchRuntime={dispatchRuntime}; downstreamDeliveryProofSelection=latest-proven-dispatch-state; downstreamDeliveryStateCount={completedStates.Length.ToString(CultureInfo.InvariantCulture)}; downstreamDeliveryProvenCount={completedProvenCount.ToString(CultureInfo.InvariantCulture)}; downstreamDeliveryCompletion=provider-reported; downstreamDeliveryCompletionSource={source}; providerDeliveryReceipt={providerReceipt}; providerDeliveryReceiptId={receiptId}; subscriberAcknowledgement={subscriberAcknowledgement}; subscriberAcknowledgementId={subscriberAcknowledgementId}; destinationCommit={destinationCommit}; destinationCommitId={destinationCommitId}; exactlyOnceDelivery={exactlyOnceDelivery}; exactlyOnceDeliverySource={exactlyOnceDeliverySource}; exactlyOnceDeliveryProofId={exactlyOnceDeliveryProofId}; exactlyOnceDeliveryStrategy={exactlyOnceDeliveryStrategy}; outboxId={completedState.OutboxId}; lastOutcome={completedState.LastOutcome ?? "unknown"}; lastObservedAtUtc={FormatObservedAt(completedState.LastObservedAtUtc)}; wolverineRequired=false"),
                nextGap);
        }

        return new DownstreamDeliveryCompletionProfile(
            "not-claimed",
            string.Create(
                CultureInfo.InvariantCulture,
                $"publicationPath=active; handoff={handoff}; dispatchRuntime={dispatchRuntime}; downstreamDeliveryProofSelection=latest-proven-dispatch-state; downstreamDeliveryStateCount={completedStates.Length.ToString(CultureInfo.InvariantCulture)}; downstreamDeliveryProvenCount={completedProvenCount.ToString(CultureInfo.InvariantCulture)}; downstreamDeliveryCompletion=not-claimed; providerDeliveryReceipt=not-present; subscriberAcknowledgement=not-claimed; destinationCommit=not-claimed; exactlyOnceDelivery=not-claimed; wolverineRequired=false"),
            "Add a provider-owned delivery completion descriptor plus acknowledgement, receipt, and completion-evidence catalog before claiming downstream delivery completion.");
    }

    private BrokerInboundConsumptionProfile ResolveBrokerInboundConsumptionProfile()
    {
        var declaredSubscriptions = topology.HasSubscriptionContributors ? "present" : "not-present";
        var inProcessExecution = topology.HasInProcessSubscriptionExecutionPath ? "active" : "not-active";
        var managedSubscriptionBindings = topology.HasManagedSubscriptionExecutionBindings ? "present" : "not-present";
        var externalManagedSubscriptionBindings = topology.HasExternalManagedSubscriptionExecutionBindings ? "present" : "not-present";
        var inboxPath = topology.HasInboxPath ? "present" : "not-present";
        using var scope = scopeFactory.CreateScope();
        var subscriptionRuntimeCatalog = scope.ServiceProvider.GetService<IEventSubscriptionRuntimeCatalog>();
        var brokerConsumedStates = subscriptionRuntimeCatalog?.States
            .Where(static state =>
                state.Metadata.TryGetValue(EventSubscriptionRuntimeMetadataKeys.BrokerInboundConsumption, out var value) &&
                string.Equals(value, "provider-reported", StringComparison.OrdinalIgnoreCase))
            .ToArray() ?? [];
        var brokerConsumedProvenCount = brokerConsumedStates.Count(IsBrokerInboundConsumptionProof);
        var brokerConsumedState = SelectBestSubscriptionProof(
            brokerConsumedStates,
            IsBrokerInboundConsumptionProof);

        if (brokerConsumedState is not null)
        {
            var metadata = brokerConsumedState.Metadata;
            var source = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.BrokerInboundConsumptionSource,
                "not-reported");
            var brokerConsumerLoop = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.BrokerConsumerLoop,
                "not-present");
            var brokerConsumerLoopId = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.BrokerConsumerLoopId,
                "not-reported");
            var inboundAcknowledgement = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.InboundAcknowledgement,
                "not-claimed");
            var inboundAcknowledgementId = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.InboundAcknowledgementId,
                "not-reported");
            var consumerLease = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.ConsumerLease,
                "not-claimed");
            var consumerLeaseId = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.ConsumerLeaseId,
                "not-reported");
            var inboundRetryPolicy = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.InboundRetryPolicy,
                "not-claimed");
            var poisonMessageHandling = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.PoisonMessageHandling,
                "not-claimed");
            var consumerOffsetCheckpoint = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.ConsumerOffsetCheckpoint,
                "not-claimed");
            var consumerOffsetCheckpointId = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.ConsumerOffsetCheckpointId,
                "not-reported");
            var status = IsBrokerInboundConsumptionProof(brokerConsumedState)
                ? "claimed"
                : "partial";
            var nextGap = status == "claimed"
                ? "Keep provider consumer-loop, acknowledgement, lease, retry/poison, and offset-checkpoint proof covered by provider integration tests."
                : "Complete provider consumer-loop, acknowledgement, lease, retry/poison, and offset-checkpoint evidence before claiming broker inbound consumption.";

            return new BrokerInboundConsumptionProfile(
                status,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"declaredSubscriptions={declaredSubscriptions}; inProcessExecution={inProcessExecution}; managedSubscriptionBindings={managedSubscriptionBindings}; externalManagedSubscriptionBindings={externalManagedSubscriptionBindings}; inboxPath={inboxPath}; brokerInboundConsumptionProofSelection=latest-proven-subscription-state; brokerInboundConsumptionStateCount={brokerConsumedStates.Length.ToString(CultureInfo.InvariantCulture)}; brokerInboundConsumptionProvenCount={brokerConsumedProvenCount.ToString(CultureInfo.InvariantCulture)}; brokerInboundConsumption=provider-reported; brokerInboundConsumptionSource={source}; brokerConsumerLoop={brokerConsumerLoop}; brokerConsumerLoopId={brokerConsumerLoopId}; providerOwnedConsumer=reported; inboundAcknowledgement={inboundAcknowledgement}; inboundAcknowledgementId={inboundAcknowledgementId}; consumerLease={consumerLease}; consumerLeaseId={consumerLeaseId}; inboundRetryPolicy={inboundRetryPolicy}; poisonMessageHandling={poisonMessageHandling}; consumerOffsetCheckpoint={consumerOffsetCheckpoint}; consumerOffsetCheckpointId={consumerOffsetCheckpointId}; subscriptionId={brokerConsumedState.SubscriptionId}; lastOutcome={brokerConsumedState.LastOutcome ?? "unknown"}; lastObservedAtUtc={FormatObservedAt(brokerConsumedState.LastObservedAtUtc)}; wolverineRequired=false"),
                nextGap);
        }

        return new BrokerInboundConsumptionProfile(
            "not-claimed",
            string.Create(
                CultureInfo.InvariantCulture,
                $"declaredSubscriptions={declaredSubscriptions}; inProcessExecution={inProcessExecution}; managedSubscriptionBindings={managedSubscriptionBindings}; externalManagedSubscriptionBindings={externalManagedSubscriptionBindings}; inboxPath={inboxPath}; brokerInboundConsumptionProofSelection=latest-proven-subscription-state; brokerInboundConsumptionStateCount={brokerConsumedStates.Length.ToString(CultureInfo.InvariantCulture)}; brokerInboundConsumptionProvenCount={brokerConsumedProvenCount.ToString(CultureInfo.InvariantCulture)}; brokerInboundConsumption=not-claimed; brokerInboundConsumptionSource=not-reported; brokerConsumerLoop=not-present; brokerConsumerLoopId=not-reported; providerOwnedConsumer=not-present; inboundAcknowledgement=not-claimed; inboundAcknowledgementId=not-reported; consumerLease=not-claimed; consumerLeaseId=not-reported; inboundRetryPolicy=not-claimed; poisonMessageHandling=not-claimed; consumerOffsetCheckpoint=not-claimed; consumerOffsetCheckpointId=not-reported; wolverineRequired=false"),
            "Add a provider-owned inbound consumption descriptor plus acknowledgement, retry, lease, poison handling, and offset-checkpoint evidence before claiming broker inbound consumption.");
    }

    private static string GetMetadataValue(IReadOnlyDictionary<string, string> metadata, string key, string fallback) =>
        metadata.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : fallback;

    private static EventDispatchRuntimeState? SelectBestDispatchProof(
        IEnumerable<EventDispatchRuntimeState> states,
        Func<EventDispatchRuntimeState, bool> isProven)
    {
        return states
            .OrderByDescending(isProven)
            .ThenByDescending(static state => state.LastObservedAtUtc ?? DateTimeOffset.MinValue)
            .ThenBy(static state => state.OutboxId, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    private static EventSubscriptionRuntimeState? SelectBestSubscriptionProof(
        IEnumerable<EventSubscriptionRuntimeState> states,
        Func<EventSubscriptionRuntimeState, bool> isProven)
    {
        return states
            .OrderByDescending(isProven)
            .ThenByDescending(static state => state.LastObservedAtUtc ?? DateTimeOffset.MinValue)
            .ThenBy(static state => state.SubscriptionId, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    private static SagaChoreographyPublicationRuntimeState? SelectBestChoreographyHandoffProof(
        IEnumerable<SagaChoreographyPublicationRuntimeState> states,
        bool eventingBridgeConfigured,
        bool hasOutboxPublishingPath)
    {
        return states
            .OrderByDescending(state => IsChoreographyHandoffProof(
                state,
                eventingBridgeConfigured,
                hasOutboxPublishingPath))
            .ThenByDescending(static state => state.LastObservedAtUtc ?? DateTimeOffset.MinValue)
            .ThenBy(static state => state.SourceModuleId ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static state => state.BehaviorId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static state => state.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    private static bool IsChoreographyHandoffProof(
        SagaChoreographyPublicationRuntimeState state,
        bool eventingBridgeConfigured,
        bool hasOutboxPublishingPath)
    {
        return eventingBridgeConfigured &&
            hasOutboxPublishingPath &&
            state.IsAccepted &&
            state.AcceptedCount > 0 &&
            string.Equals(
                state.LastPublisherType,
                EventingSagaChoreographyPublisherTypeName,
                StringComparison.Ordinal);
    }

    private static bool IsSuccessfulSerializationExecutionProof(EventDispatchRuntimeState state) =>
        string.Equals(state.LastOutcome, EventDispatchExecutionOutcomes.Succeeded, StringComparison.OrdinalIgnoreCase) &&
        EventDispatchSerializationExecutionMetadata.IsSerializationExecutionProven(state.Metadata);

    private static bool IsSuccessfulWireContractProof(EventDispatchRuntimeState state) =>
        string.Equals(state.LastOutcome, EventDispatchExecutionOutcomes.Succeeded, StringComparison.OrdinalIgnoreCase) &&
        EventDispatchWireContractMetadata.IsWireContractProven(state.Metadata);

    private static bool IsSuccessfulScheduledDeliveryProof(EventDispatchRuntimeState state) =>
        string.Equals(state.LastOutcome, EventDispatchExecutionOutcomes.Succeeded, StringComparison.OrdinalIgnoreCase) &&
        EventDispatchScheduledDeliveryMetadata.IsScheduledDeliveryProven(state.Metadata);

    private static bool IsBrokerTopologyProof(EventDispatchRuntimeState state) =>
        string.Equals(state.LastOutcome, EventDispatchExecutionOutcomes.Succeeded, StringComparison.OrdinalIgnoreCase) &&
        EventDispatchBrokerTopologyMetadata.IsTopologyMaterialized(state.Metadata);

    private static bool IsProviderPartitionProof(EventDispatchRuntimeState state) =>
        string.Equals(state.LastOutcome, EventDispatchExecutionOutcomes.Succeeded, StringComparison.OrdinalIgnoreCase) &&
        EventDispatchProviderPartitionMetadata.IsPartitionOwnershipProven(state.Metadata);

    private static bool IsDownstreamDeliveryCompletionProof(EventDispatchRuntimeState state) =>
        string.Equals(state.LastOutcome, EventDispatchExecutionOutcomes.Succeeded, StringComparison.OrdinalIgnoreCase) &&
        EventDispatchDeliveryCompletionMetadata.IsCompleted(state.Metadata) &&
        EventDispatchExactlyOnceDeliveryProofMetadata.IsProviderProven(state.Metadata) &&
        HasMetadataValue(state.Metadata, EventDispatchRuntimeMetadataKeys.ProviderDeliveryReceipt, "reported") &&
        HasMetadataValue(state.Metadata, EventDispatchRuntimeMetadataKeys.SubscriberAcknowledgement, "reported") &&
        HasMetadataValue(state.Metadata, EventDispatchRuntimeMetadataKeys.DestinationCommit, "reported") &&
        HasMetadata(state.Metadata, EventDispatchRuntimeMetadataKeys.ProviderDeliveryReceiptId) &&
        HasMetadata(state.Metadata, EventDispatchRuntimeMetadataKeys.SubscriberAcknowledgementId) &&
        HasMetadata(state.Metadata, EventDispatchRuntimeMetadataKeys.DestinationCommitId);

    private static bool IsBrokerDeadLetterReplayProof(EventDispatchRuntimeState state) =>
        string.Equals(state.LastOutcome, EventDispatchExecutionOutcomes.Failed, StringComparison.OrdinalIgnoreCase) &&
        EventDispatchBrokerDeadLetterReplayMetadata.IsBrokerDeadLetterReplayProven(state.Metadata);

    private static bool IsBrokerInboundConsumptionProof(EventSubscriptionRuntimeState state) =>
        string.Equals(state.LastOutcome, EventSubscriptionExecutionOutcomes.Succeeded, StringComparison.OrdinalIgnoreCase) &&
        EventSubscriptionBrokerInboundConsumptionMetadata.IsBrokerConsumed(state.Metadata);

    private static bool HasTenantCorrelationContextDispatchMetadata(IReadOnlyDictionary<string, string> metadata) =>
        metadata.ContainsKey(EventDispatchRuntimeMetadataKeys.DurableDispatchContextPropagation) ||
        metadata.ContainsKey(EventDispatchRuntimeMetadataKeys.ProviderBrokerContextHeaders) ||
        metadata.ContainsKey(EventDispatchRuntimeMetadataKeys.ProviderSideContextPersistence) ||
        metadata.ContainsKey(EventDispatchRuntimeMetadataKeys.CrossNodeContextHandoff);

    private static bool IsTenantCorrelationContextDispatchProof(EventDispatchRuntimeState state) =>
        string.Equals(state.LastOutcome, EventDispatchExecutionOutcomes.Succeeded, StringComparison.OrdinalIgnoreCase) &&
        HasMetadataValue(state.Metadata, EventDispatchRuntimeMetadataKeys.DurableDispatchContextPropagation, "dispatch-report-metadata") &&
        HasMetadataValue(state.Metadata, EventDispatchRuntimeMetadataKeys.ProviderBrokerContextHeaders, "projected") &&
        EventDispatchProviderContextPersistenceMetadata.IsPersisted(state.Metadata) &&
        EventDispatchCrossNodeContextHandoffMetadata.IsHandoffProven(state.Metadata) &&
        IsDownstreamDeliveryCompletionProof(state);

    private static bool IsConsumerContextExtractionProof(EventSubscriptionRuntimeState state) =>
        string.Equals(state.LastOutcome, EventSubscriptionExecutionOutcomes.Succeeded, StringComparison.OrdinalIgnoreCase) &&
        HasMetadataValue(state.Metadata, EventSubscriptionRuntimeMetadataKeys.ConsumerContextExtraction, "extracted") &&
        HasMetadata(state.Metadata, EventSubscriptionRuntimeMetadataKeys.ConsumerContextHeaderNames);

    private static bool IsDurableRetryQueueProof(EventDispatchRuntimeState state) =>
        string.Equals(state.LastOutcome, EventDispatchExecutionOutcomes.RetryScheduled, StringComparison.OrdinalIgnoreCase) &&
        EventDispatchDurableRetryQueueMetadata.IsDurableRetryQueueProven(state.Metadata);

    private static bool IsProviderIdempotencyProof(EventSubscriptionRuntimeState state) =>
        string.Equals(state.LastOutcome, EventSubscriptionExecutionOutcomes.Succeeded, StringComparison.OrdinalIgnoreCase) &&
        EventSubscriptionProviderIdempotencyMetadata.IsProviderIdempotencyProven(state.Metadata);

    private static bool IsSubscriptionConcurrencyProof(EventSubscriptionRuntimeState state) =>
        string.Equals(state.LastOutcome, EventSubscriptionExecutionOutcomes.Succeeded, StringComparison.OrdinalIgnoreCase) &&
        EventSubscriptionConcurrencyMetadata.IsConcurrencyProven(state.Metadata);

    private static bool IsSubscriptionOrderingProof(EventSubscriptionRuntimeState state) =>
        string.Equals(state.LastOutcome, EventSubscriptionExecutionOutcomes.Succeeded, StringComparison.OrdinalIgnoreCase) &&
        EventSubscriptionOrderingMetadata.IsOrderingProven(state.Metadata);

    private static bool IsProcessManagerStateProof(EventSubscriptionRuntimeState state) =>
        string.Equals(state.LastOutcome, EventSubscriptionExecutionOutcomes.Succeeded, StringComparison.OrdinalIgnoreCase) &&
        EventSubscriptionProcessManagerStateMetadata.IsProcessManagerStateProven(state.Metadata);

    private static string FormatObservedAt(DateTimeOffset? observedAtUtc) =>
        observedAtUtc.HasValue
            ? observedAtUtc.Value.ToString("O", CultureInfo.InvariantCulture)
            : "not-reported";

    private static string FormatMetadataValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "not-reported" : value;

    private static bool HasMetadataValue(
        IReadOnlyDictionary<string, string> metadata,
        string key,
        string expected) =>
        metadata.TryGetValue(key, out var value) &&
        string.Equals(value, expected, StringComparison.OrdinalIgnoreCase);

    private static bool HasMetadata(IReadOnlyDictionary<string, string> metadata, string key) =>
        metadata.TryGetValue(key, out var value) &&
        !string.IsNullOrWhiteSpace(value);

    private SerializationVersioningProfile ResolveSerializationVersioningProfile()
    {
        using var scope = scopeFactory.CreateScope();
        var contractCatalog = scope.ServiceProvider.GetService<IEventContractCatalog>();
        var serializerCatalog = scope.ServiceProvider.GetService<IEventSerializerCatalog>();
        var schemaRegistryCatalog = scope.ServiceProvider.GetService<IEventSchemaRegistryCatalog>();
        var upcasterCatalog = scope.ServiceProvider.GetService<IEventUpcasterCatalog>();
        var contracts = contractCatalog?.Contracts ?? [];
        var serializers = serializerCatalog?.Serializers ?? [];
        var schemaRegistries = schemaRegistryCatalog?.Registries ?? [];
        var upcasters = upcasterCatalog?.Upcasters ?? [];
        var contractCount = contracts.Count;
        var serializerRuntimeCount = serializers.Count;
        var schemaRegistryRuntimeCount = schemaRegistries.Count;
        var upcasterRuntimeCount = upcasters.Count;
        var contractCountText = contractCount.ToString(CultureInfo.InvariantCulture);
        var versionedContractCount = contracts.Count(static contract => !string.IsNullOrWhiteSpace(contract.Version));
        var contentTypeContractCount = contracts.Count(static contract => !string.IsNullOrWhiteSpace(contract.ContentType));
        var serializerReferenceCount = contracts.Count(static contract => !string.IsNullOrWhiteSpace(contract.SerializerId));
        var resolvedSerializerContractCount = serializerCatalog is null
            ? 0
            : contracts.Count(contract => serializerCatalog.TryGetForContract(contract, out _));
        var unresolvedSerializerContractCount = serializerReferenceCount - resolvedSerializerContractCount;
        var schemaRegistryReferenceCount = serializers.Count(static serializer => !string.IsNullOrWhiteSpace(serializer.SchemaRegistryId));
        var schemaRegistryRequiredSerializerCount = serializers.Count(static serializer => serializer.RequiresSchemaRegistry);
        var resolvedSchemaRegistrySerializerCount = schemaRegistryCatalog is null
            ? 0
            : serializers.Count(serializer => schemaRegistryCatalog.TryGetForSerializer(serializer, out _));
        var unresolvedSchemaRegistrySerializerCount = schemaRegistryReferenceCount - resolvedSchemaRegistrySerializerCount;
        var resolvedUpcasterSourceContractCount = contractCatalog is null
            ? 0
            : upcasters.Count(upcaster => contractCatalog.TryGetVersion(upcaster.EventType, upcaster.FromVersion, out _));
        var resolvedUpcasterTargetContractCount = contractCatalog is null
            ? 0
            : upcasters.Count(upcaster => contractCatalog.TryGetVersion(upcaster.EventType, upcaster.ToVersion, out _));
        var resolvedUpcasterTransitionCount = contractCatalog is null
            ? 0
            : upcasters.Count(upcaster =>
                contractCatalog.TryGetVersion(upcaster.EventType, upcaster.FromVersion, out _) &&
                contractCatalog.TryGetVersion(upcaster.EventType, upcaster.ToVersion, out _));
        var envelopeSchemaCount = contracts
            .Select(static contract => contract.EnvelopeSchema)
            .Where(static schema => !string.IsNullOrWhiteSpace(schema))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        var compatibilityPolicyCount = contracts
            .Select(static contract => contract.CompatibilityPolicy)
            .Where(static policy => !string.IsNullOrWhiteSpace(policy))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        var channelCatalog = topology.HasChannelContributors ? "present" : "not-present";
        var subscriptionCatalog = topology.HasSubscriptionContributors ? "present" : "not-present";
        var publicationPath = topology.HasPublishingPath ? "active" : "not-active";
        var publicationRouting = options.EnablePublicationRouting ? "configured" : "not-configured";
        var eventContractCatalog = contractCatalog is null ? "not-present" : "present";
        var eventSerializerCatalog = serializerRuntimeCount > 0 ? "present" : "not-present";
        var eventSchemaRegistryCatalog = schemaRegistryRuntimeCount > 0 ? "present" : "not-present";
        var eventUpcasterCatalog = upcasterRuntimeCount > 0 ? "present" : "not-present";
        var serializerSelection = resolvedSerializerContractCount > 0
            ? "catalog-backed"
            : serializerReferenceCount > 0 ? "descriptor-backed" : "not-claimed";
        var wireSerializationRuntime = serializerRuntimeCount > 0 ? "serializer-catalog-declared" : "not-claimed";
        var messageEnvelopeSchema = envelopeSchemaCount > 0 ? "descriptor-backed" : "not-claimed";
        var schemaRegistry = resolvedSchemaRegistrySerializerCount > 0
            ? "catalog-backed"
            : schemaRegistryReferenceCount > 0 ? "descriptor-backed" : schemaRegistryRuntimeCount > 0 ? "catalog-declared" : "not-present";
        var contractVersionNegotiation = versionedContractCount > 0 ? "descriptor-backed" : "not-claimed";
        var upcasterPipeline = upcasterRuntimeCount > 0 ? "catalog-declared" : "not-present";
        var compatibilityValidation = compatibilityPolicyCount > 0 ? "descriptor-backed" : "not-claimed";

        var dispatchRuntimeCatalog = scope.ServiceProvider.GetService<IEventDispatchRuntimeCatalog>();
        var dispatchStates = dispatchRuntimeCatalog?.States ?? [];
        var serializationExecutionStates = dispatchStates
            .Where(static state =>
                state.Metadata.TryGetValue(EventDispatchRuntimeMetadataKeys.SerializationExecutionOwnership, out var value) &&
                string.Equals(value, "provider-reported", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var wireContractStates = dispatchStates
            .Where(static state => state.Metadata.ContainsKey(EventDispatchRuntimeMetadataKeys.WireContractOwnership))
            .ToArray();
        var serializationExecutionProvenCount = serializationExecutionStates.Count(IsSuccessfulSerializationExecutionProof);
        var wireContractProvenCount = wireContractStates.Count(IsSuccessfulWireContractProof);
        var serializationExecutionState = SelectBestDispatchProof(
            serializationExecutionStates,
            IsSuccessfulSerializationExecutionProof);
        var wireContractState = SelectBestDispatchProof(
            wireContractStates,
            IsSuccessfulWireContractProof);

        var serializationExecutionOwnership = "not-claimed";
        var serializationExecutionOwnershipSource = "not-reported";
        var serializationDurability = "not-claimed";
        var serializationScope = "not-claimed";
        var payloadSerializationExecution = "not-claimed";
        var payloadSerializationExecutionId = "not-reported";
        var schemaLookupExecution = "not-claimed";
        var schemaLookupExecutionId = "not-reported";
        var upcasterExecution = "not-claimed";
        var upcasterExecutionId = "not-reported";
        var compatibilityValidationExecution = "not-claimed";
        var compatibilityValidationExecutionId = "not-reported";
        var providerSerialization = "not-claimed";
        var providerSerializationId = "not-reported";
        var serializationExecutionOutboxId = "not-reported";
        var serializationExecutionLastOutcome = "not-reported";
        var serializationExecutionLastObservedAtUtc = "not-reported";
        var serializationExecutionProven = false;
        var wireContractOwnership = "not-claimed";
        var wireContractOwnershipSource = "not-reported";
        var wireEnvelopeSchemaExecution = "not-claimed";
        var wireEnvelopeSchemaExecutionId = "not-reported";
        var contractVersionNegotiationExecution = "not-claimed";
        var contractVersionNegotiationExecutionId = "not-reported";
        var wireContractProofId = "not-reported";
        var wireContractOutboxId = "not-reported";
        var wireContractLastOutcome = "not-reported";
        var wireContractLastObservedAtUtc = "not-reported";
        var wireContractProven = false;

        if (serializationExecutionState is not null)
        {
            var metadata = serializationExecutionState.Metadata;
            serializationExecutionProven = string.Equals(
                serializationExecutionState.LastOutcome,
                EventDispatchExecutionOutcomes.Succeeded,
                StringComparison.OrdinalIgnoreCase) &&
                EventDispatchSerializationExecutionMetadata.IsSerializationExecutionProven(metadata);
            serializationExecutionOwnership = GetMetadataValue(metadata, EventDispatchRuntimeMetadataKeys.SerializationExecutionOwnership, "not-claimed");
            serializationExecutionOwnershipSource = GetMetadataValue(metadata, EventDispatchRuntimeMetadataKeys.SerializationExecutionOwnershipSource, "not-reported");
            serializationDurability = GetMetadataValue(metadata, EventDispatchRuntimeMetadataKeys.SerializationDurability, "not-claimed");
            serializationScope = GetMetadataValue(metadata, EventDispatchRuntimeMetadataKeys.SerializationScope, "not-claimed");
            payloadSerializationExecution = GetMetadataValue(metadata, EventDispatchRuntimeMetadataKeys.PayloadSerializationExecution, "not-claimed");
            payloadSerializationExecutionId = GetMetadataValue(metadata, EventDispatchRuntimeMetadataKeys.PayloadSerializationExecutionId, "not-reported");
            schemaLookupExecution = GetMetadataValue(metadata, EventDispatchRuntimeMetadataKeys.SchemaLookupExecution, "not-claimed");
            schemaLookupExecutionId = GetMetadataValue(metadata, EventDispatchRuntimeMetadataKeys.SchemaLookupExecutionId, "not-reported");
            upcasterExecution = GetMetadataValue(metadata, EventDispatchRuntimeMetadataKeys.UpcasterExecution, "not-claimed");
            upcasterExecutionId = GetMetadataValue(metadata, EventDispatchRuntimeMetadataKeys.UpcasterExecutionId, "not-reported");
            compatibilityValidationExecution = GetMetadataValue(metadata, EventDispatchRuntimeMetadataKeys.CompatibilityValidationExecution, "not-claimed");
            compatibilityValidationExecutionId = GetMetadataValue(metadata, EventDispatchRuntimeMetadataKeys.CompatibilityValidationExecutionId, "not-reported");
            providerSerialization = GetMetadataValue(metadata, EventDispatchRuntimeMetadataKeys.ProviderSerialization, "not-claimed");
            providerSerializationId = GetMetadataValue(metadata, EventDispatchRuntimeMetadataKeys.ProviderSerializationId, "not-reported");
            serializationExecutionOutboxId = serializationExecutionState.OutboxId;
            serializationExecutionLastOutcome = serializationExecutionState.LastOutcome ?? "unknown";
            serializationExecutionLastObservedAtUtc = FormatObservedAt(serializationExecutionState.LastObservedAtUtc);
        }

        if (wireContractState is not null)
        {
            var metadata = wireContractState.Metadata;
            wireContractProven = string.Equals(
                wireContractState.LastOutcome,
                EventDispatchExecutionOutcomes.Succeeded,
                StringComparison.OrdinalIgnoreCase) &&
                EventDispatchWireContractMetadata.IsWireContractProven(metadata);
            wireContractOwnership = GetMetadataValue(metadata, EventDispatchRuntimeMetadataKeys.WireContractOwnership, "not-claimed");
            wireContractOwnershipSource = GetMetadataValue(metadata, EventDispatchRuntimeMetadataKeys.WireContractOwnershipSource, "not-reported");
            wireEnvelopeSchemaExecution = GetMetadataValue(metadata, EventDispatchRuntimeMetadataKeys.WireEnvelopeSchemaExecution, "not-claimed");
            wireEnvelopeSchemaExecutionId = GetMetadataValue(metadata, EventDispatchRuntimeMetadataKeys.WireEnvelopeSchemaExecutionId, "not-reported");
            contractVersionNegotiationExecution = GetMetadataValue(metadata, EventDispatchRuntimeMetadataKeys.ContractVersionNegotiationExecution, "not-claimed");
            contractVersionNegotiationExecutionId = GetMetadataValue(metadata, EventDispatchRuntimeMetadataKeys.ContractVersionNegotiationExecutionId, "not-reported");
            wireContractProofId = GetMetadataValue(metadata, EventDispatchRuntimeMetadataKeys.WireContractProofId, "not-reported");
            wireContractOutboxId = wireContractState.OutboxId;
            wireContractLastOutcome = wireContractState.LastOutcome ?? "unknown";
            wireContractLastObservedAtUtc = FormatObservedAt(wireContractState.LastObservedAtUtc);
        }

        var status = wireContractProven
            ? "claimed"
            : serializationExecutionProven || contractCount > 0 || serializerRuntimeCount > 0 || schemaRegistryRuntimeCount > 0 || upcasterRuntimeCount > 0
                ? "partial"
                : "not-claimed";
        var nextGap = wireContractProven
            ? "Keep payload serialization, wire-envelope schema, schema lookup, contract-version negotiation, upcaster execution, compatibility validation, provider serialization, and wire-contract proof covered by provider or engine integration tests."
            : serializationExecutionProven
            ? "Add executable wire-envelope schema and contract-version negotiation proof before claiming full serialization and contract-version ownership."
            : upcasterRuntimeCount > 0
            ? "Add executable payload serialization, executable schema lookup, upcaster execution, and provider-owned compatibility validation before claiming full serialization and contract-version ownership."
            : schemaRegistryRuntimeCount > 0
            ? "Add code-first event upcaster descriptors, executable payload serialization, schema lookup, and compatibility validation execution before claiming full serialization and contract-version ownership."
            : serializerRuntimeCount > 0
            ? "Add executable payload serialization, schema registry, upcaster pipeline, and compatibility validation execution before claiming full serialization and contract-version ownership."
            : contractCount > 0
                ? "Register code-first event serializer descriptors that satisfy the declared contract serializer ids before claiming serializer runtime evidence."
                : "Register code-first event contract and serializer descriptors before claiming contract-version evidence.";

        var evidence = string.Create(
            CultureInfo.InvariantCulture,
            $"channelCatalog={channelCatalog}; subscriptionCatalog={subscriptionCatalog}; publicationPath={publicationPath}; publicationRouting={publicationRouting}; eventContractCatalog={eventContractCatalog}; eventSerializerCatalog={eventSerializerCatalog}; eventSchemaRegistryCatalog={eventSchemaRegistryCatalog}; eventUpcasterCatalog={eventUpcasterCatalog}; eventContractCount={contractCountText}; versionedContracts={versionedContractCount.ToString(CultureInfo.InvariantCulture)}; contentTypeContracts={contentTypeContractCount.ToString(CultureInfo.InvariantCulture)}; serializerDescriptors={serializerReferenceCount.ToString(CultureInfo.InvariantCulture)}; serializerRuntimeCount={serializerRuntimeCount.ToString(CultureInfo.InvariantCulture)}; resolvedSerializerContracts={resolvedSerializerContractCount.ToString(CultureInfo.InvariantCulture)}; unresolvedSerializerContracts={unresolvedSerializerContractCount.ToString(CultureInfo.InvariantCulture)}; schemaRegistryRuntimeCount={schemaRegistryRuntimeCount.ToString(CultureInfo.InvariantCulture)}; schemaRegistryReferences={schemaRegistryReferenceCount.ToString(CultureInfo.InvariantCulture)}; schemaRegistryRequiredSerializers={schemaRegistryRequiredSerializerCount.ToString(CultureInfo.InvariantCulture)}; resolvedSchemaRegistrySerializers={resolvedSchemaRegistrySerializerCount.ToString(CultureInfo.InvariantCulture)}; unresolvedSchemaRegistrySerializers={unresolvedSchemaRegistrySerializerCount.ToString(CultureInfo.InvariantCulture)}; upcasterRuntimeCount={upcasterRuntimeCount.ToString(CultureInfo.InvariantCulture)}; upcasterTransitions={upcasterRuntimeCount.ToString(CultureInfo.InvariantCulture)}; resolvedUpcasterSourceContracts={resolvedUpcasterSourceContractCount.ToString(CultureInfo.InvariantCulture)}; resolvedUpcasterTargetContracts={resolvedUpcasterTargetContractCount.ToString(CultureInfo.InvariantCulture)}; resolvedUpcasterTransitions={resolvedUpcasterTransitionCount.ToString(CultureInfo.InvariantCulture)}; envelopeSchemas={envelopeSchemaCount.ToString(CultureInfo.InvariantCulture)}; compatibilityPolicies={compatibilityPolicyCount.ToString(CultureInfo.InvariantCulture)}; runtimeProofSelection=latest-proven-dispatch-state; serializationExecutionStateCount={serializationExecutionStates.Length.ToString(CultureInfo.InvariantCulture)}; serializationExecutionProvenCount={serializationExecutionProvenCount.ToString(CultureInfo.InvariantCulture)}; wireContractStateCount={wireContractStates.Length.ToString(CultureInfo.InvariantCulture)}; wireContractProvenCount={wireContractProvenCount.ToString(CultureInfo.InvariantCulture)}; serializerSelection={serializerSelection}; wireSerializationRuntime={wireSerializationRuntime}; messageEnvelopeSchema={messageEnvelopeSchema}; schemaRegistry={schemaRegistry}; contractVersionNegotiation={contractVersionNegotiation}; upcasterPipeline={upcasterPipeline}; compatibilityValidation={compatibilityValidation}; serializationExecutionOwnership={serializationExecutionOwnership}; serializationExecutionOwnershipSource={serializationExecutionOwnershipSource}; serializationDurability={serializationDurability}; serializationScope={serializationScope}; payloadSerializationExecution={payloadSerializationExecution}; payloadSerializationExecutionId={payloadSerializationExecutionId}; schemaLookupExecution={schemaLookupExecution}; schemaLookupExecutionId={schemaLookupExecutionId}; upcasterExecution={upcasterExecution}; upcasterExecutionId={upcasterExecutionId}; compatibilityValidationExecution={compatibilityValidationExecution}; compatibilityValidationExecutionId={compatibilityValidationExecutionId}; providerSerialization={providerSerialization}; providerSerializationId={providerSerializationId}; serializationExecutionOutboxId={serializationExecutionOutboxId}; serializationExecutionLastOutcome={serializationExecutionLastOutcome}; serializationExecutionLastObservedAtUtc={serializationExecutionLastObservedAtUtc}; wireContractOwnership={wireContractOwnership}; wireContractOwnershipSource={wireContractOwnershipSource}; wireEnvelopeSchemaExecution={wireEnvelopeSchemaExecution}; wireEnvelopeSchemaExecutionId={wireEnvelopeSchemaExecutionId}; contractVersionNegotiationExecution={contractVersionNegotiationExecution}; contractVersionNegotiationExecutionId={contractVersionNegotiationExecutionId}; wireContractProofId={wireContractProofId}; wireContractOutboxId={wireContractOutboxId}; wireContractLastOutcome={wireContractLastOutcome}; wireContractLastObservedAtUtc={wireContractLastObservedAtUtc}; wolverineRequired=false");

        return new SerializationVersioningProfile(status, evidence, nextGap);
    }

    private TenantCorrelationProfile ResolveTenantCorrelationProfile()
    {
        using var scope = scopeFactory.CreateScope();
        var contextPolicyCatalog = scope.ServiceProvider.GetService<IEventContextPolicyCatalog>();
        var dispatchRuntimeCatalog = scope.ServiceProvider.GetService<IEventDispatchRuntimeCatalog>();
        var subscriptionRuntimeCatalog = scope.ServiceProvider.GetService<IEventSubscriptionRuntimeCatalog>();
        var policies = contextPolicyCatalog?.Policies ?? [];
        var policyCount = policies.Count;
        var tenantPolicyCount = policies.Count(static policy => policy.DeclaresTenantContext);
        var correlationPolicyCount = policies.Count(static policy => policy.DeclaresCorrelationId);
        var causationPolicyCount = policies.Count(static policy => policy.DeclaresCausationId);
        var baggagePolicyCount = policies.Count(static policy => policy.DeclaresBaggage);
        var headerValidationPolicyCount = policies.Count(static policy => policy.ValidatesMessageHeaders);
        var declaredHeaderCount = policies
            .SelectMany(static policy => policy.HeaderNames)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        var channelCatalog = topology.HasChannelContributors ? "present" : "not-present";
        var subscriptionCatalog = topology.HasSubscriptionContributors ? "present" : "not-present";
        var publicationPath = topology.HasPublishingPath ? "active" : "not-active";
        var publicationRouting = options.EnablePublicationRouting ? "configured" : "not-configured";
        var inProcessExecution = topology.HasInProcessSubscriptionExecutionPath ? "active" : "not-active";
        var eventContextPolicyCatalog = policyCount > 0 ? "present" : "not-present";
        var hasInProcessContextExecution = policyCount > 0 && topology.HasInProcessSubscriptionExecutionPath;
        var hasPublisherContextValidation = headerValidationPolicyCount > 0 && topology.HasPublishingPath;
        var hasOutboxContextHandoff = policyCount > 0 && topology.HasOutboxPublishingPath;
        var tenantContextPropagation = tenantPolicyCount > 0
            ? hasInProcessContextExecution ? "in-process-direct" : "policy-declared"
            : "not-claimed";
        var correlationContextPropagation = correlationPolicyCount > 0
            ? hasInProcessContextExecution ? "in-process-direct" : "policy-declared"
            : "not-claimed";
        var causationIdPropagation = causationPolicyCount > 0
            ? hasInProcessContextExecution ? "in-process-direct" : "policy-declared"
            : "not-claimed";
        var baggagePropagation = baggagePolicyCount > 0
            ? hasInProcessContextExecution ? "in-process-direct" : "policy-declared"
            : "not-claimed";
        var messageHeaderPolicy = headerValidationPolicyCount > 0
            ? hasPublisherContextValidation ? "publisher-enforced" : "policy-declared"
            : declaredHeaderCount > 0 ? "policy-declared" : "not-claimed";
        var outboxContextHandoff = hasOutboxContextHandoff
            ? headerValidationPolicyCount > 0 ? "staged-headers" : "staged-policy-metadata"
            : "not-claimed";
        var dispatchStates = dispatchRuntimeCatalog?.States ?? [];
        var subscriptionStates = subscriptionRuntimeCatalog?.States ?? [];
        var contextDispatchStates = dispatchStates
            .Where(static state => HasTenantCorrelationContextDispatchMetadata(state.Metadata))
            .ToArray();
        var consumerContextStates = subscriptionStates
            .Where(static state => state.Metadata.ContainsKey(EventSubscriptionRuntimeMetadataKeys.ConsumerContextExtraction))
            .ToArray();
        var contextDispatchProvenCount = contextDispatchStates.Count(IsTenantCorrelationContextDispatchProof);
        var consumerContextProvenCount = consumerContextStates.Count(IsConsumerContextExtractionProof);
        var contextDispatchState = SelectBestDispatchProof(
            contextDispatchStates,
            IsTenantCorrelationContextDispatchProof);
        var consumerContextState = SelectBestSubscriptionProof(
            consumerContextStates,
            IsConsumerContextExtractionProof);
        var hasDispatchReportContextMetadata = hasOutboxContextHandoff && contextDispatchStates.Any(static state =>
            HasMetadataValue(state.Metadata, EventDispatchRuntimeMetadataKeys.DurableDispatchContextPropagation, "dispatch-report-metadata"));
        var hasProviderBrokerContextHeaderProjection = hasDispatchReportContextMetadata && contextDispatchStates.Any(static state =>
            HasMetadataValue(state.Metadata, EventDispatchRuntimeMetadataKeys.ProviderBrokerContextHeaders, "projected"));
        var hasProviderSideContextPersistence = hasProviderBrokerContextHeaderProjection && contextDispatchStates.Any(static state =>
            EventDispatchProviderContextPersistenceMetadata.IsPersisted(state.Metadata));
        var hasCrossNodeContextHandoff = contextDispatchStates.Any(static state =>
            EventDispatchCrossNodeContextHandoffMetadata.IsHandoffProven(state.Metadata));
        var hasDownstreamDeliveryCompletion = contextDispatchStates.Any(static state =>
            EventDispatchDeliveryCompletionMetadata.IsCompleted(state.Metadata));
        var hasExactlyOnceDelivery = contextDispatchStates.Any(static state =>
            EventDispatchExactlyOnceDeliveryProofMetadata.IsProviderProven(state.Metadata));
        var hasConsumerContextExtraction = consumerContextStates.Any(IsConsumerContextExtractionProof);
        var durableDispatchContextPropagation = hasDispatchReportContextMetadata
            ? "dispatch-report-metadata"
            : hasOutboxContextHandoff && topology.HasDispatchStore
                ? "dispatch-store-read"
                : "not-claimed";
        var dispatchReportContextMetadata = hasDispatchReportContextMetadata ? "reported" : "not-reported";
        var providerBrokerContextHeaders = hasProviderBrokerContextHeaderProjection ? "projected" : "not-claimed";
        var providerSideContextPersistence = hasProviderSideContextPersistence ? "dispatch-store-persisted" : "not-claimed";
        var consumerContextExtraction = hasConsumerContextExtraction ? "extracted" : "not-claimed";
        var crossNodeContextHandoff = hasCrossNodeContextHandoff ? "provider-reported" : "not-claimed";
        var downstreamDeliveryCompletion = hasDownstreamDeliveryCompletion ? "provider-reported" : "not-claimed";
        var exactlyOnceDelivery = hasExactlyOnceDelivery ? "provider-proven" : "not-claimed";
        var executablePropagation = hasInProcessContextExecution ? "in-process-direct" : "not-claimed";
        var executableValidation = hasPublisherContextValidation ? "publisher-enforced" : "not-claimed";
        var hasCompleteContextDispatchProof = contextDispatchState is not null &&
            IsTenantCorrelationContextDispatchProof(contextDispatchState);
        var hasCompleteConsumerContextProof = consumerContextState is not null &&
            IsConsumerContextExtractionProof(consumerContextState);
        var status = policyCount > 0 && hasCompleteContextDispatchProof && hasCompleteConsumerContextProof
            ? "claimed"
            : policyCount > 0 ? "partial" : "not-claimed";

        var providerBrokerContextHeaderProjection = "not-reported";
        var providerBrokerContextHeaderCount = "not-reported";
        var providerBrokerContextHeaderNames = "not-reported";
        var providerSideContextPersistenceSource = "not-reported";
        var providerSideContextPersistenceHeaderCount = "not-reported";
        var providerSideContextPersistenceHeaderNames = "not-reported";
        var crossNodeContextHandoffSource = "not-reported";
        var crossNodeContextHandoffProducerNodeId = "not-reported";
        var crossNodeContextHandoffConsumerNodeId = "not-reported";
        var crossNodeContextHandoffHeaderCount = "not-reported";
        var crossNodeContextHandoffHeaderNames = "not-reported";
        var downstreamDeliveryCompletionSource = "not-reported";
        var providerDeliveryReceiptId = "not-reported";
        var subscriberAcknowledgementId = "not-reported";
        var destinationCommitId = "not-reported";
        var exactlyOnceDeliveryProofId = "not-reported";
        var exactlyOnceDeliveryStrategy = "not-reported";
        var contextDispatchOutboxId = "not-reported";
        var contextDispatchLastOutcome = "not-reported";
        var contextDispatchLastObservedAtUtc = "not-reported";
        var consumerContextExtractionSource = "not-reported";
        var consumerContextHeaderCount = "not-reported";
        var consumerContextHeaderNames = "not-reported";
        var consumerContextSubscriptionId = "not-reported";
        var consumerContextLastOutcome = "not-reported";
        var consumerContextLastObservedAtUtc = "not-reported";

        if (contextDispatchState is not null)
        {
            var metadata = contextDispatchState.Metadata;
            providerBrokerContextHeaderProjection = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.ProviderBrokerContextHeaderProjection,
                "not-reported");
            providerBrokerContextHeaderCount = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.ProviderBrokerContextHeaderCount,
                "not-reported");
            providerBrokerContextHeaderNames = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.ProviderBrokerContextHeaderNames,
                "not-reported");
            providerSideContextPersistenceSource = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.ProviderSideContextPersistenceSource,
                "not-reported");
            providerSideContextPersistenceHeaderCount = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.ProviderSideContextPersistenceHeaderCount,
                "not-reported");
            providerSideContextPersistenceHeaderNames = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.ProviderSideContextPersistenceHeaderNames,
                "not-reported");
            crossNodeContextHandoffSource = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.CrossNodeContextHandoffSource,
                "not-reported");
            crossNodeContextHandoffProducerNodeId = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.CrossNodeContextHandoffProducerNodeId,
                "not-reported");
            crossNodeContextHandoffConsumerNodeId = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.CrossNodeContextHandoffConsumerNodeId,
                "not-reported");
            crossNodeContextHandoffHeaderCount = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.CrossNodeContextHandoffHeaderCount,
                "not-reported");
            crossNodeContextHandoffHeaderNames = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.CrossNodeContextHandoffHeaderNames,
                "not-reported");
            downstreamDeliveryCompletionSource = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.DownstreamDeliveryCompletionSource,
                "not-reported");
            providerDeliveryReceiptId = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.ProviderDeliveryReceiptId,
                "not-reported");
            subscriberAcknowledgementId = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.SubscriberAcknowledgementId,
                "not-reported");
            destinationCommitId = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.DestinationCommitId,
                "not-reported");
            exactlyOnceDeliveryProofId = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.ExactlyOnceDeliveryProofId,
                "not-reported");
            exactlyOnceDeliveryStrategy = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.ExactlyOnceDeliveryStrategy,
                "not-reported");
            contextDispatchOutboxId = contextDispatchState.OutboxId;
            contextDispatchLastOutcome = contextDispatchState.LastOutcome ?? "unknown";
            contextDispatchLastObservedAtUtc = FormatObservedAt(contextDispatchState.LastObservedAtUtc);
        }

        if (consumerContextState is not null)
        {
            var metadata = consumerContextState.Metadata;
            consumerContextExtractionSource = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.ConsumerContextExtractionSource,
                "not-reported");
            consumerContextHeaderCount = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.ConsumerContextHeaderCount,
                "not-reported");
            consumerContextHeaderNames = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.ConsumerContextHeaderNames,
                "not-reported");
            consumerContextSubscriptionId = consumerContextState.SubscriptionId;
            consumerContextLastOutcome = consumerContextState.LastOutcome ?? "unknown";
            consumerContextLastObservedAtUtc = FormatObservedAt(consumerContextState.LastObservedAtUtc);
        }

        var evidence = string.Create(
            CultureInfo.InvariantCulture,
            $"channelCatalog={channelCatalog}; subscriptionCatalog={subscriptionCatalog}; publicationPath={publicationPath}; publicationRouting={publicationRouting}; inProcessExecution={inProcessExecution}; operatorCorrelationMetadata=metadata-only; eventContextPolicyCatalog={eventContextPolicyCatalog}; contextPolicyCount={policyCount.ToString(CultureInfo.InvariantCulture)}; tenantPolicyCount={tenantPolicyCount.ToString(CultureInfo.InvariantCulture)}; correlationPolicyCount={correlationPolicyCount.ToString(CultureInfo.InvariantCulture)}; causationPolicyCount={causationPolicyCount.ToString(CultureInfo.InvariantCulture)}; baggagePolicyCount={baggagePolicyCount.ToString(CultureInfo.InvariantCulture)}; headerValidationPolicyCount={headerValidationPolicyCount.ToString(CultureInfo.InvariantCulture)}; declaredHeaderCount={declaredHeaderCount.ToString(CultureInfo.InvariantCulture)}; tenantContextPropagation={tenantContextPropagation}; correlationContextPropagation={correlationContextPropagation}; causationIdPropagation={causationIdPropagation}; baggagePropagation={baggagePropagation}; messageHeaderPolicy={messageHeaderPolicy}; outboxContextHandoff={outboxContextHandoff}; contextDispatchProofSelection=latest-proven-dispatch-state; contextDispatchStateCount={contextDispatchStates.Length.ToString(CultureInfo.InvariantCulture)}; contextDispatchProvenCount={contextDispatchProvenCount.ToString(CultureInfo.InvariantCulture)}; consumerContextProofSelection=latest-proven-subscription-state; consumerContextStateCount={consumerContextStates.Length.ToString(CultureInfo.InvariantCulture)}; consumerContextProvenCount={consumerContextProvenCount.ToString(CultureInfo.InvariantCulture)}; durableDispatchContextPropagation={durableDispatchContextPropagation}; dispatchReportContextMetadata={dispatchReportContextMetadata}; providerBrokerContextHeaders={providerBrokerContextHeaders}; providerBrokerContextHeaderProjection={providerBrokerContextHeaderProjection}; providerBrokerContextHeaderCount={providerBrokerContextHeaderCount}; providerBrokerContextHeaderNames={providerBrokerContextHeaderNames}; providerSideContextPersistence={providerSideContextPersistence}; providerSideContextPersistenceSource={providerSideContextPersistenceSource}; providerSideContextPersistenceHeaderCount={providerSideContextPersistenceHeaderCount}; providerSideContextPersistenceHeaderNames={providerSideContextPersistenceHeaderNames}; consumerContextExtraction={consumerContextExtraction}; consumerContextExtractionSource={consumerContextExtractionSource}; consumerContextHeaderCount={consumerContextHeaderCount}; consumerContextHeaderNames={consumerContextHeaderNames}; crossNodeContextHandoff={crossNodeContextHandoff}; crossNodeContextHandoffSource={crossNodeContextHandoffSource}; crossNodeContextHandoffProducerNodeId={crossNodeContextHandoffProducerNodeId}; crossNodeContextHandoffConsumerNodeId={crossNodeContextHandoffConsumerNodeId}; crossNodeContextHandoffHeaderCount={crossNodeContextHandoffHeaderCount}; crossNodeContextHandoffHeaderNames={crossNodeContextHandoffHeaderNames}; downstreamDeliveryCompletion={downstreamDeliveryCompletion}; downstreamDeliveryCompletionSource={downstreamDeliveryCompletionSource}; providerDeliveryReceiptId={providerDeliveryReceiptId}; subscriberAcknowledgementId={subscriberAcknowledgementId}; destinationCommitId={destinationCommitId}; exactlyOnceDelivery={exactlyOnceDelivery}; exactlyOnceDeliveryProofId={exactlyOnceDeliveryProofId}; exactlyOnceDeliveryStrategy={exactlyOnceDeliveryStrategy}; contextDispatchOutboxId={contextDispatchOutboxId}; contextDispatchLastOutcome={contextDispatchLastOutcome}; contextDispatchLastObservedAtUtc={contextDispatchLastObservedAtUtc}; consumerContextSubscriptionId={consumerContextSubscriptionId}; consumerContextLastOutcome={consumerContextLastOutcome}; consumerContextLastObservedAtUtc={consumerContextLastObservedAtUtc}; executablePropagation={executablePropagation}; executableValidation={executableValidation}; wolverineRequired=false");
        var nextGap = status == "claimed"
            ? "Add provider-specific context handoff adapters and compliance tests for each broker/provider before declaring provider-family parity."
            : hasCompleteContextDispatchProof
            ? "Attach successful consumer context extraction runtime proof to the same provider handoff before claiming full tenant and correlation ownership."
            : hasCrossNodeContextHandoff && hasDownstreamDeliveryCompletion && hasExactlyOnceDelivery
            ? "Attach successful consumer context extraction proof beside the provider handoff before claiming full tenant and correlation ownership."
            : hasCrossNodeContextHandoff && hasDownstreamDeliveryCompletion
            ? "Attach subscriber acknowledgement, destination commit, and exactly-once proof before claiming full tenant and correlation ownership."
            : hasCrossNodeContextHandoff
            ? "Attach downstream delivery completion, provider receipts, subscriber acknowledgement, and destination commit proof before claiming full tenant and correlation ownership."
            : hasConsumerContextExtraction
            ? hasProviderSideContextPersistence
                ? "Extend provider-persisted and consumer-extracted context into cross-node handoff before claiming full tenant and correlation ownership."
                : "Extend consumer-extracted context into provider-side persistence and cross-node handoff before claiming full tenant and correlation ownership."
            : hasProviderSideContextPersistence
            ? "Extend provider-persisted context into consumer extraction and cross-node handoff before claiming full tenant and correlation ownership."
            : hasProviderBrokerContextHeaderProjection
            ? "Extend projected provider/broker context headers into consumer extraction and cross-node handoff before claiming full tenant and correlation ownership."
            : hasDispatchReportContextMetadata
            ? "Extend dispatch-report context into provider/broker headers, consumer extraction, and cross-node handoff before claiming full tenant and correlation ownership."
            : hasOutboxContextHandoff && topology.HasDispatchStore
            ? "Extend dispatch-store-read context into durable dispatch reports, provider/broker headers, consumer extraction, and cross-node handoff before claiming full tenant and correlation ownership."
            : hasInProcessContextExecution || hasPublisherContextValidation || hasOutboxContextHandoff
            ? "Extend outbox-staged context into dispatch-store reads, provider/broker headers, consumer extraction, and cross-node handoff before claiming full tenant and correlation ownership."
            : policyCount > 0
            ? "Add executable tenant, correlation, causation, baggage, and message-header propagation plus validation before claiming full tenant and correlation ownership."
            : "Add code-first event context policy descriptors before claiming context propagation policy evidence.";

        return new TenantCorrelationProfile(status, evidence, nextGap);
    }

    private ScheduledDeliveryProfile ResolveScheduledDeliveryProfile()
    {
        var publicationPath = topology.HasPublishingPath ? "active" : "not-active";
        var publicationScheduling = options.EnablePublicationScheduling ? "configured" : "not-configured";
        var processLocalScheduleQueue = options.EnablePublicationScheduling ? "active" : "not-active";
        var schedulePolicy = options.EnablePublicationScheduling
            ? EventPublicationSchedulingPolicy.GetPolicyId(options)
            : "not-enabled";
        var maxDelayMilliseconds = options.EnablePublicationScheduling
            ? options.PublicationSchedulingMaxDelayMilliseconds.ToString(CultureInfo.InvariantCulture)
            : "not-configured";
        var maxPendingCount = options.EnablePublicationScheduling
            ? options.PublicationSchedulingMaxPendingCount.ToString(CultureInfo.InvariantCulture)
            : "not-configured";
        var fallbackScheduleDurability = options.EnablePublicationScheduling
            ? EventPublicationSchedulingPolicy.GetDurability(options)
            : "none";
        var fallbackScheduleScope = options.EnablePublicationScheduling
            ? EventPublicationSchedulingPolicy.GetScope(options)
            : "not-active";

        using var scope = scopeFactory.CreateScope();
        var dispatchRuntimeCatalog = scope.ServiceProvider.GetService<IEventDispatchRuntimeCatalog>();
        var scheduledDeliveryStates = dispatchRuntimeCatalog?.States
            .Where(static state =>
                state.Metadata.TryGetValue(EventDispatchRuntimeMetadataKeys.ScheduledDeliveryOwnership, out var value) &&
                string.Equals(value, "provider-reported", StringComparison.OrdinalIgnoreCase))
            .ToArray() ?? [];
        var scheduledDeliveryProvenCount = scheduledDeliveryStates.Count(IsSuccessfulScheduledDeliveryProof);
        var scheduledDeliveryState = SelectBestDispatchProof(
            scheduledDeliveryStates,
            IsSuccessfulScheduledDeliveryProof);

        if (scheduledDeliveryState is not null)
        {
            var metadata = scheduledDeliveryState.Metadata;
            var scheduledDeliveryOwnership = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.ScheduledDeliveryOwnership,
                "not-claimed");
            var scheduledDeliveryOwnershipSource = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.ScheduledDeliveryOwnershipSource,
                "not-reported");
            var scheduleDurability = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.ScheduleDurability,
                fallbackScheduleDurability);
            var scheduleScope = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.ScheduleScope,
                fallbackScheduleScope);
            var durableScheduledDelivery = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.DurableScheduledDelivery,
                "not-claimed");
            var durableScheduledDeliveryId = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.DurableScheduledDeliveryId,
                "not-reported");
            var providerDelayQueue = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.ProviderDelayQueue,
                "not-present");
            var providerDelayQueueId = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.ProviderDelayQueueId,
                "not-reported");
            var brokerScheduledDelivery = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.BrokerScheduledDelivery,
                "not-claimed");
            var brokerScheduledDeliveryId = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.BrokerScheduledDeliveryId,
                "not-reported");
            var crossNodeScheduleCoordination = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.CrossNodeScheduleCoordination,
                "not-claimed");
            var scheduleCoordinationId = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.ScheduleCoordinationId,
                "not-reported");
            var scheduleRecovery = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.ScheduleRecovery,
                "not-claimed");
            var scheduleRecoveryId = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.ScheduleRecoveryId,
                "not-reported");
            var status = EventDispatchScheduledDeliveryMetadata.IsScheduledDeliveryProven(metadata)
                ? "claimed"
                : "partial";
            var nextGap = status == "claimed"
                ? "Keep durable scheduled delivery, provider delay queue, broker scheduling, cross-node coordination, and recovery proof covered by provider integration tests."
                : "Complete durable scheduled delivery, provider delay queue, broker scheduling, cross-node coordination, and recovery evidence before claiming scheduled/delayed delivery ownership.";

            return new ScheduledDeliveryProfile(
                status,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"publicationPath={publicationPath}; publicationScheduling={publicationScheduling}; schedulePolicy={schedulePolicy}; processLocalScheduleQueue={processLocalScheduleQueue}; maxDelayMilliseconds={maxDelayMilliseconds}; maxPendingCount={maxPendingCount}; scheduleDurability={scheduleDurability}; scheduleScope={scheduleScope}; scheduledDeliveryProofSelection=latest-proven-dispatch-state; scheduledDeliveryStateCount={scheduledDeliveryStates.Length.ToString(CultureInfo.InvariantCulture)}; scheduledDeliveryProvenCount={scheduledDeliveryProvenCount.ToString(CultureInfo.InvariantCulture)}; scheduledDeliveryOwnership={scheduledDeliveryOwnership}; scheduledDeliveryOwnershipSource={scheduledDeliveryOwnershipSource}; durableScheduledDelivery={durableScheduledDelivery}; durableScheduledDeliveryId={durableScheduledDeliveryId}; providerDelayQueue={providerDelayQueue}; providerDelayQueueId={providerDelayQueueId}; brokerScheduledDelivery={brokerScheduledDelivery}; brokerScheduledDeliveryId={brokerScheduledDeliveryId}; crossNodeScheduleCoordination={crossNodeScheduleCoordination}; scheduleCoordinationId={scheduleCoordinationId}; scheduleRecovery={scheduleRecovery}; scheduleRecoveryId={scheduleRecoveryId}; outboxId={scheduledDeliveryState.OutboxId}; lastOutcome={scheduledDeliveryState.LastOutcome ?? "unknown"}; lastObservedAtUtc={FormatObservedAt(scheduledDeliveryState.LastObservedAtUtc)}; wolverineRequired=false"),
                nextGap);
        }

        if (!options.EnablePublicationScheduling)
        {
            return new ScheduledDeliveryProfile(
                "not-claimed",
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"publicationPath={publicationPath}; publicationScheduling={publicationScheduling}; processLocalScheduleQueue={processLocalScheduleQueue}; scheduleDurability={fallbackScheduleDurability}; scheduledDeliveryProofSelection=latest-proven-dispatch-state; scheduledDeliveryStateCount={scheduledDeliveryStates.Length.ToString(CultureInfo.InvariantCulture)}; scheduledDeliveryProvenCount={scheduledDeliveryProvenCount.ToString(CultureInfo.InvariantCulture)}; scheduledDeliveryOwnership=not-claimed; scheduledDeliveryOwnershipSource=not-reported; durableScheduledDelivery=not-claimed; durableScheduledDeliveryId=not-reported; providerDelayQueue=not-present; providerDelayQueueId=not-reported; brokerScheduledDelivery=not-claimed; brokerScheduledDeliveryId=not-reported; crossNodeScheduleCoordination=not-claimed; scheduleCoordinationId=not-reported; scheduleRecovery=not-claimed; scheduleRecoveryId=not-reported; wolverineRequired=false"),
                "Enable bounded process-local publication scheduling or supply complete provider scheduled-delivery proof before claiming scheduled/delayed delivery evidence.");
        }

        return new ScheduledDeliveryProfile(
            "partial",
            string.Create(
                CultureInfo.InvariantCulture,
                $"publicationPath={publicationPath}; publicationScheduling={publicationScheduling}; schedulePolicy={schedulePolicy}; processLocalScheduleQueue={processLocalScheduleQueue}; maxDelayMilliseconds={maxDelayMilliseconds}; maxPendingCount={maxPendingCount}; scheduleDurability={fallbackScheduleDurability}; scheduleScope={fallbackScheduleScope}; scheduledDeliveryProofSelection=latest-proven-dispatch-state; scheduledDeliveryStateCount={scheduledDeliveryStates.Length.ToString(CultureInfo.InvariantCulture)}; scheduledDeliveryProvenCount={scheduledDeliveryProvenCount.ToString(CultureInfo.InvariantCulture)}; scheduledDeliveryOwnership=not-claimed; scheduledDeliveryOwnershipSource=not-reported; durableScheduledDelivery=not-claimed; durableScheduledDeliveryId=not-reported; providerDelayQueue=not-present; providerDelayQueueId=not-reported; brokerScheduledDelivery=not-claimed; brokerScheduledDeliveryId=not-reported; crossNodeScheduleCoordination=not-claimed; scheduleCoordinationId=not-reported; scheduleRecovery=not-claimed; scheduleRecoveryId=not-reported; wolverineRequired=false"),
            "Add provider-reported durable scheduled delivery, provider delay queue, broker scheduling, recovery, and cross-node coordination evidence before claiming scheduled/delayed delivery ownership.");
    }

    private DurableRetryQueueProfile ResolveDurableRetryQueueProfile()
    {
        var publicationPath = topology.HasPublishingPath ? "active" : "not-active";
        var inProcessExecution = topology.HasInProcessSubscriptionExecutionPath ? "active" : "not-active";
        var inProcessRetryPolicy = InProcessEventingRetryPolicy.GetPolicyId(options);
        var inProcessRetryAttempts = InProcessEventingRetryPolicy.GetMaxAttempts(options).ToString(CultureInfo.InvariantCulture);
        var dispatchRuntime = topology.HasDispatchRuntimeContributors ? "reported" : "not-reported";
        var managedSubscriptionBindings = topology.HasManagedSubscriptionExecutionBindings ? "present" : "not-present";
        var externalManagedSubscriptionBindings = topology.HasExternalManagedSubscriptionExecutionBindings ? "present" : "not-present";

        using var scope = scopeFactory.CreateScope();
        var dispatchRuntimeCatalog = scope.ServiceProvider.GetService<IEventDispatchRuntimeCatalog>();
        var durableRetryStates = dispatchRuntimeCatalog?.States
            .Where(static state =>
                state.Metadata.TryGetValue(EventDispatchRuntimeMetadataKeys.DurableRetryQueue, out var value) &&
                string.Equals(value, "provider-reported", StringComparison.OrdinalIgnoreCase))
            .ToArray() ?? [];
        var durableRetryProvenCount = durableRetryStates.Count(IsDurableRetryQueueProof);
        var durableRetryState = SelectBestDispatchProof(
            durableRetryStates,
            IsDurableRetryQueueProof);

        if (!topology.HasPublishingPath)
        {
            return new DurableRetryQueueProfile(
                "not-claimed",
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"publicationPath={publicationPath}; inProcessExecution={inProcessExecution}; inProcessRetryPolicy={inProcessRetryPolicy}; inProcessRetryMaxAttempts={inProcessRetryAttempts}; dispatchRuntime={dispatchRuntime}; managedSubscriptionBindings={managedSubscriptionBindings}; externalManagedSubscriptionBindings={externalManagedSubscriptionBindings}; durableRetryProofSelection=latest-proven-dispatch-state; durableRetryStateCount={durableRetryStates.Length.ToString(CultureInfo.InvariantCulture)}; durableRetryProvenCount={durableRetryProvenCount.ToString(CultureInfo.InvariantCulture)}; retryDurability=none-or-provider-reported; durableRetryQueue=not-claimed; durableRetryQueueSource=not-reported; durableRetryQueueId=not-reported; retryPersistence=not-claimed; retryPersistenceId=not-reported; brokerErrorQueue=not-claimed; brokerErrorQueueId=not-reported; poisonQueueOwnership=not-claimed; poisonQueueId=not-reported; crossNodeRetryCoordination=not-claimed; retryCoordinationId=not-reported; retryLease=not-claimed; retryLeaseId=not-reported; wolverineRequired=false"),
                "Add a publishing path before claiming durable retry queue evidence.");
        }

        if (durableRetryState is not null)
        {
            var metadata = durableRetryState.Metadata;
            var source = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.DurableRetryQueueSource,
                "not-reported");
            var durableRetryQueue = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.DurableRetryQueue,
                "not-claimed");
            var durableRetryQueueId = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.DurableRetryQueueId,
                "not-reported");
            var retryDurability = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.RetryDurability,
                "none-or-provider-reported");
            var retryScope = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.RetryScope,
                "not-claimed");
            var retryPersistence = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.RetryPersistence,
                "not-claimed");
            var retryPersistenceId = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.RetryPersistenceId,
                "not-reported");
            var brokerErrorQueue = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.BrokerErrorQueue,
                "not-claimed");
            var brokerErrorQueueId = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.BrokerErrorQueueId,
                "not-reported");
            var poisonQueueOwnership = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.PoisonQueueOwnership,
                "not-claimed");
            var poisonQueueId = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.PoisonQueueId,
                "not-reported");
            var crossNodeRetryCoordination = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.CrossNodeRetryCoordination,
                "not-claimed");
            var retryCoordinationId = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.RetryCoordinationId,
                "not-reported");
            var retryLease = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.RetryLease,
                "not-claimed");
            var retryLeaseId = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.RetryLeaseId,
                "not-reported");
            var status = EventDispatchDurableRetryQueueMetadata.IsDurableRetryQueueProven(metadata)
                ? "claimed"
                : "partial";
            var nextGap = status == "claimed"
                ? "Keep provider durable retry queue, persistence, broker error queue, poison queue, coordination, and lease proof covered by provider integration tests."
                : "Complete provider durable retry queue, persistence, broker error queue, poison queue, coordination, and lease evidence before claiming durable retry queue ownership.";

            return new DurableRetryQueueProfile(
                status,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"publicationPath=active; inProcessExecution={inProcessExecution}; inProcessRetryPolicy={inProcessRetryPolicy}; inProcessRetryMaxAttempts={inProcessRetryAttempts}; dispatchRuntime={dispatchRuntime}; managedSubscriptionBindings={managedSubscriptionBindings}; externalManagedSubscriptionBindings={externalManagedSubscriptionBindings}; durableRetryProofSelection=latest-proven-dispatch-state; durableRetryStateCount={durableRetryStates.Length.ToString(CultureInfo.InvariantCulture)}; durableRetryProvenCount={durableRetryProvenCount.ToString(CultureInfo.InvariantCulture)}; retryDurability={retryDurability}; retryScope={retryScope}; durableRetryQueue={durableRetryQueue}; durableRetryQueueSource={source}; durableRetryQueueId={durableRetryQueueId}; retryPersistence={retryPersistence}; retryPersistenceId={retryPersistenceId}; brokerErrorQueue={brokerErrorQueue}; brokerErrorQueueId={brokerErrorQueueId}; poisonQueueOwnership={poisonQueueOwnership}; poisonQueueId={poisonQueueId}; crossNodeRetryCoordination={crossNodeRetryCoordination}; retryCoordinationId={retryCoordinationId}; retryLease={retryLease}; retryLeaseId={retryLeaseId}; outboxId={durableRetryState.OutboxId}; lastOutcome={durableRetryState.LastOutcome ?? "unknown"}; lastObservedAtUtc={FormatObservedAt(durableRetryState.LastObservedAtUtc)}; wolverineRequired=false"),
                nextGap);
        }

        return new DurableRetryQueueProfile(
            "not-claimed",
            string.Create(
                CultureInfo.InvariantCulture,
                $"publicationPath=active; inProcessExecution={inProcessExecution}; inProcessRetryPolicy={inProcessRetryPolicy}; inProcessRetryMaxAttempts={inProcessRetryAttempts}; dispatchRuntime={dispatchRuntime}; managedSubscriptionBindings={managedSubscriptionBindings}; externalManagedSubscriptionBindings={externalManagedSubscriptionBindings}; durableRetryProofSelection=latest-proven-dispatch-state; durableRetryStateCount={durableRetryStates.Length.ToString(CultureInfo.InvariantCulture)}; durableRetryProvenCount={durableRetryProvenCount.ToString(CultureInfo.InvariantCulture)}; retryDurability=none-or-provider-reported; durableRetryQueue=not-claimed; durableRetryQueueSource=not-reported; durableRetryQueueId=not-reported; retryPersistence=not-claimed; retryPersistenceId=not-reported; brokerErrorQueue=not-claimed; brokerErrorQueueId=not-reported; poisonQueueOwnership=not-claimed; poisonQueueId=not-reported; crossNodeRetryCoordination=not-claimed; retryCoordinationId=not-reported; retryLease=not-claimed; retryLeaseId=not-reported; wolverineRequired=false"),
            "Add a provider-owned durable retry queue descriptor plus retry persistence, broker error queue, poison queue, cross-node coordination, and lease evidence before claiming durable retry queue ownership.");
    }

    private IdempotencyOwnershipProfile ResolveIdempotencyOwnershipProfile()
    {
        var publicationPath = topology.HasPublishingPath ? "active" : "not-active";
        var inProcessExecution = topology.HasInProcessSubscriptionExecutionPath ? "active" : "not-active";
        var idempotencyPolicy = InProcessEventingIdempotencyPolicy.GetPolicyId(options);
        var idempotencyStore = InProcessEventingIdempotencyPolicy.GetStore(options);
        var idempotencyScope = InProcessEventingIdempotencyPolicy.GetScope(options);
        var idempotencyDurability = InProcessEventingIdempotencyPolicy.GetDurability(options);
        var idempotencyKeyShape = InProcessEventingIdempotencyPolicy.GetKeyShape(options);
        var idempotencyRetentionMinutes = InProcessEventingIdempotencyPolicy.IsEnabled(options)
            ? InProcessEventingIdempotencyPolicy.GetRetentionMinutes(options).ToString(CultureInfo.InvariantCulture)
            : "none";
        var completedExecutionDuplicateSuppression = InProcessEventingIdempotencyPolicy.IsEnabled(options) ? "active" : "not-active";
        var inboxPath = topology.HasInboxPath ? "present" : "not-present";
        var managedSubscriptionBindings = topology.HasManagedSubscriptionExecutionBindings ? "present" : "not-present";
        var externalManagedSubscriptionBindings = topology.HasExternalManagedSubscriptionExecutionBindings ? "present" : "not-present";

        using var scope = scopeFactory.CreateScope();
        var subscriptionRuntimeCatalog = scope.ServiceProvider.GetService<IEventSubscriptionRuntimeCatalog>();
        var providerIdempotencyStates = subscriptionRuntimeCatalog?.States
            .Where(static state =>
                state.Metadata.TryGetValue(EventSubscriptionRuntimeMetadataKeys.ProviderIdempotency, out var value) &&
                string.Equals(value, "provider-reported", StringComparison.OrdinalIgnoreCase))
            .ToArray() ?? [];
        var providerIdempotencyProvenCount = providerIdempotencyStates.Count(IsProviderIdempotencyProof);
        var providerIdempotencyState = SelectBestSubscriptionProof(
            providerIdempotencyStates,
            IsProviderIdempotencyProof);

        if (providerIdempotencyState is not null)
        {
            var metadata = providerIdempotencyState.Metadata;
            var messageDeduplication = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.MessageDeduplication,
                "not-claimed");
            var providerIdempotency = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.ProviderIdempotency,
                "not-claimed");
            var providerIdempotencySource = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.ProviderIdempotencySource,
                "not-reported");
            var providerIdempotencyKey = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.ProviderIdempotencyKey,
                "not-reported");
            var brokerDeduplication = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.BrokerDeduplication,
                "not-claimed");
            var brokerDeduplicationId = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.BrokerDeduplicationId,
                "not-reported");
            var exactlyOnceDelivery = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.ExactlyOnceDelivery,
                "not-claimed");
            var exactlyOnceProofId = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.ExactlyOnceDeliveryProofId,
                "not-reported");
            var durableInboxCommandOwnership = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.DurableInboxCommandOwnership,
                "not-claimed");
            var durableInboxCommandId = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.DurableInboxCommandId,
                "not-reported");
            var genericInboxCommandOwnership = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.GenericInboxCommandOwnership,
                "not-claimed");
            var genericInboxCommandId = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.GenericInboxCommandId,
                "not-reported");
            var crossNodeIdempotencyLease = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.CrossNodeIdempotencyLease,
                "not-claimed");
            var crossNodeIdempotencyLeaseId = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.CrossNodeIdempotencyLeaseId,
                "not-reported");
            var status = EventSubscriptionProviderIdempotencyMetadata.IsProviderIdempotencyProven(metadata)
                ? "claimed"
                : "partial";
            var nextGap = status == "claimed"
                ? "Keep provider idempotency, broker deduplication, exactly-once processing, inbox command, and cross-node lease proof covered by provider integration tests."
                : "Complete provider idempotency, broker deduplication, exactly-once processing, inbox command, and cross-node lease evidence before claiming idempotency ownership.";

            return new IdempotencyOwnershipProfile(
                status,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"publicationPath={publicationPath}; inProcessExecution={inProcessExecution}; idempotencyPolicy={idempotencyPolicy}; idempotencyStore={idempotencyStore}; idempotencyScope={idempotencyScope}; idempotencyDurability={idempotencyDurability}; idempotencyKeyShape={idempotencyKeyShape}; idempotencyRetentionMinutes={idempotencyRetentionMinutes}; inboxPath={inboxPath}; managedSubscriptionBindings={managedSubscriptionBindings}; externalManagedSubscriptionBindings={externalManagedSubscriptionBindings}; providerIdempotencyProofSelection=latest-proven-subscription-state; providerIdempotencyStateCount={providerIdempotencyStates.Length.ToString(CultureInfo.InvariantCulture)}; providerIdempotencyProvenCount={providerIdempotencyProvenCount.ToString(CultureInfo.InvariantCulture)}; completedExecutionDuplicateSuppression={completedExecutionDuplicateSuppression}; messageDeduplication={messageDeduplication}; brokerDeduplication={brokerDeduplication}; brokerDeduplicationId={brokerDeduplicationId}; exactlyOnceDelivery={exactlyOnceDelivery}; exactlyOnceDeliveryProofId={exactlyOnceProofId}; durableInboxCommandOwnership={durableInboxCommandOwnership}; durableInboxCommandId={durableInboxCommandId}; genericInboxCommandOwnership={genericInboxCommandOwnership}; genericInboxCommandId={genericInboxCommandId}; crossNodeIdempotencyLease={crossNodeIdempotencyLease}; crossNodeIdempotencyLeaseId={crossNodeIdempotencyLeaseId}; providerIdempotency={providerIdempotency}; providerIdempotencySource={providerIdempotencySource}; providerIdempotencyKey={providerIdempotencyKey}; subscriptionId={providerIdempotencyState.SubscriptionId}; lastOutcome={providerIdempotencyState.LastOutcome ?? "unknown"}; lastObservedAtUtc={FormatObservedAt(providerIdempotencyState.LastObservedAtUtc)}; wolverineRequired=false"),
                nextGap);
        }

        var statusWithoutProviderProof = options.EnableInProcessSubscriptionIdempotency ? "partial" : "not-claimed";
        var nextGapWithoutProviderProof = options.EnableInProcessSubscriptionIdempotency
            ? "Add provider idempotency, broker deduplication, exactly-once processing, durable inbox command, generic inbox command, and cross-node lease evidence before claiming full idempotency ownership."
            : "Enable completed-publication duplicate suppression or supply provider idempotency proof before claiming idempotency ownership evidence.";

        return new IdempotencyOwnershipProfile(
            statusWithoutProviderProof,
            string.Create(
                CultureInfo.InvariantCulture,
                $"publicationPath={publicationPath}; inProcessExecution={inProcessExecution}; idempotencyPolicy={idempotencyPolicy}; idempotencyStore={idempotencyStore}; idempotencyScope={idempotencyScope}; idempotencyDurability={idempotencyDurability}; idempotencyKeyShape={idempotencyKeyShape}; idempotencyRetentionMinutes={idempotencyRetentionMinutes}; inboxPath={inboxPath}; managedSubscriptionBindings={managedSubscriptionBindings}; externalManagedSubscriptionBindings={externalManagedSubscriptionBindings}; providerIdempotencyProofSelection=latest-proven-subscription-state; providerIdempotencyStateCount={providerIdempotencyStates.Length.ToString(CultureInfo.InvariantCulture)}; providerIdempotencyProvenCount={providerIdempotencyProvenCount.ToString(CultureInfo.InvariantCulture)}; completedExecutionDuplicateSuppression={completedExecutionDuplicateSuppression}; messageDeduplication=completed-execution-only; brokerDeduplication=not-claimed; brokerDeduplicationId=not-reported; exactlyOnceDelivery=not-claimed; exactlyOnceDeliveryProofId=not-reported; durableInboxCommandOwnership=not-claimed; durableInboxCommandId=not-reported; genericInboxCommandOwnership=not-claimed; genericInboxCommandId=not-reported; crossNodeIdempotencyLease=not-claimed; crossNodeIdempotencyLeaseId=not-reported; providerIdempotency=not-claimed; providerIdempotencySource=not-reported; providerIdempotencyKey=not-reported; wolverineRequired=false"),
            nextGapWithoutProviderProof);
    }

    private SubscriptionConcurrencyProfile ResolveSubscriptionConcurrencyProfile()
    {
        var declaredSubscriptions = topology.HasSubscriptionContributors ? "present" : "not-present";
        var inProcessExecution = topology.HasInProcessSubscriptionExecutionPath ? "active" : "not-active";
        var managedSubscriptionBindings = topology.HasManagedSubscriptionExecutionBindings ? "present" : "not-present";
        var externalManagedSubscriptionBindings = topology.HasExternalManagedSubscriptionExecutionBindings ? "present" : "not-present";
        var middlewareCount = topology.SubscriptionExecutionMiddlewareCount.ToString(CultureInfo.InvariantCulture);

        using var scope = scopeFactory.CreateScope();
        var subscriptionRuntimeCatalog = scope.ServiceProvider.GetService<IEventSubscriptionRuntimeCatalog>();
        var concurrencyStates = subscriptionRuntimeCatalog?.States
            .Where(static state =>
                state.Metadata.TryGetValue(EventSubscriptionRuntimeMetadataKeys.SubscriptionConcurrency, out var value) &&
                string.Equals(value, "provider-reported", StringComparison.OrdinalIgnoreCase))
            .ToArray() ?? [];
        var concurrencyProvenCount = concurrencyStates.Count(IsSubscriptionConcurrencyProof);
        var concurrencyState = SelectBestSubscriptionProof(
            concurrencyStates,
            IsSubscriptionConcurrencyProof);

        if (concurrencyState is not null)
        {
            var metadata = concurrencyState.Metadata;
            var subscriptionConcurrency = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.SubscriptionConcurrency,
                "not-claimed");
            var subscriptionConcurrencySource = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.SubscriptionConcurrencySource,
                "not-reported");
            var perSubscriptionConcurrencyLimit = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.PerSubscriptionConcurrencyLimit,
                "not-claimed");
            var parallelHandlerExecution = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.ParallelHandlerExecution,
                "not-claimed");
            var consumerPrefetch = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.ConsumerPrefetch,
                "not-claimed");
            var consumerPrefetchCount = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.ConsumerPrefetchCount,
                "not-reported");
            var backpressure = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.Backpressure,
                "not-claimed");
            var backpressureStrategy = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.BackpressureStrategy,
                "not-reported");
            var providerConcurrency = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.ProviderConcurrency,
                "not-present");
            var providerConcurrencyId = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.ProviderConcurrencyId,
                "not-reported");
            var consumerLease = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.ConsumerLease,
                "not-claimed");
            var consumerLeaseId = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.ConsumerLeaseId,
                "not-reported");
            var workStealing = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.WorkStealing,
                "not-claimed");
            var workStealingId = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.WorkStealingId,
                "not-reported");
            var distributedWorkSharing = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.DistributedWorkSharing,
                "not-claimed");
            var distributedWorkSharingId = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.DistributedWorkSharingId,
                "not-reported");
            var status = EventSubscriptionConcurrencyMetadata.IsConcurrencyProven(metadata)
                ? "claimed"
                : "partial";
            var nextGap = status == "claimed"
                ? "Keep per-subscription concurrency, prefetch, backpressure, provider concurrency, lease, work-stealing, and distributed work-sharing proof covered by provider integration tests."
                : "Complete per-subscription concurrency, prefetch, backpressure, provider concurrency, lease, work-stealing, and distributed work-sharing evidence before claiming subscription concurrency ownership.";

            return new SubscriptionConcurrencyProfile(
                status,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"declaredSubscriptions={declaredSubscriptions}; inProcessExecution={inProcessExecution}; subscriptionExecutionPipeline={topology.SubscriptionExecutionPipeline}; subscriptionExecutionMiddlewareCount={middlewareCount}; managedSubscriptionBindings={managedSubscriptionBindings}; externalManagedSubscriptionBindings={externalManagedSubscriptionBindings}; subscriptionConcurrencyProofSelection=latest-proven-subscription-state; subscriptionConcurrencyStateCount={concurrencyStates.Length.ToString(CultureInfo.InvariantCulture)}; subscriptionConcurrencyProvenCount={concurrencyProvenCount.ToString(CultureInfo.InvariantCulture)}; subscriptionConcurrency={subscriptionConcurrency}; subscriptionConcurrencySource={subscriptionConcurrencySource}; perSubscriptionConcurrencyLimit={perSubscriptionConcurrencyLimit}; parallelHandlerExecution={parallelHandlerExecution}; consumerPrefetch={consumerPrefetch}; consumerPrefetchCount={consumerPrefetchCount}; backpressure={backpressure}; backpressureStrategy={backpressureStrategy}; providerConcurrency={providerConcurrency}; providerConcurrencyId={providerConcurrencyId}; consumerLease={consumerLease}; consumerLeaseId={consumerLeaseId}; workStealing={workStealing}; workStealingId={workStealingId}; distributedWorkSharing={distributedWorkSharing}; distributedWorkSharingId={distributedWorkSharingId}; subscriptionId={concurrencyState.SubscriptionId}; lastOutcome={concurrencyState.LastOutcome ?? "unknown"}; lastObservedAtUtc={FormatObservedAt(concurrencyState.LastObservedAtUtc)}; wolverineRequired=false"),
                nextGap);
        }

        return new SubscriptionConcurrencyProfile(
            "not-claimed",
            string.Create(
                CultureInfo.InvariantCulture,
                $"declaredSubscriptions={declaredSubscriptions}; inProcessExecution={inProcessExecution}; subscriptionExecutionPipeline={topology.SubscriptionExecutionPipeline}; subscriptionExecutionMiddlewareCount={middlewareCount}; managedSubscriptionBindings={managedSubscriptionBindings}; externalManagedSubscriptionBindings={externalManagedSubscriptionBindings}; subscriptionConcurrencyProofSelection=latest-proven-subscription-state; subscriptionConcurrencyStateCount={concurrencyStates.Length.ToString(CultureInfo.InvariantCulture)}; subscriptionConcurrencyProvenCount={concurrencyProvenCount.ToString(CultureInfo.InvariantCulture)}; subscriptionConcurrency=not-claimed; subscriptionConcurrencySource=not-reported; perSubscriptionConcurrencyLimit=not-claimed; parallelHandlerExecution=not-claimed; consumerPrefetch=not-claimed; consumerPrefetchCount=not-reported; backpressure=not-claimed; backpressureStrategy=not-reported; providerConcurrency=not-present; providerConcurrencyId=not-reported; consumerLease=not-claimed; consumerLeaseId=not-reported; workStealing=not-claimed; workStealingId=not-reported; distributedWorkSharing=not-claimed; distributedWorkSharingId=not-reported; wolverineRequired=false"),
            "Add provider-neutral subscription concurrency proof metadata plus per-subscription limits, prefetch, backpressure, lease, and work-sharing evidence before claiming subscription concurrency ownership.");
    }

    private SubscriptionOrderingProfile ResolveSubscriptionOrderingProfile()
    {
        var declaredSubscriptions = topology.HasSubscriptionContributors ? "present" : "not-present";
        var inProcessExecution = topology.HasInProcessSubscriptionExecutionPath ? "active" : "not-active";
        var managedSubscriptionBindings = topology.HasManagedSubscriptionExecutionBindings ? "present" : "not-present";
        var externalManagedSubscriptionBindings = topology.HasExternalManagedSubscriptionExecutionBindings ? "present" : "not-present";
        var middlewareCount = topology.SubscriptionExecutionMiddlewareCount.ToString(CultureInfo.InvariantCulture);

        using var scope = scopeFactory.CreateScope();
        var subscriptionRuntimeCatalog = scope.ServiceProvider.GetService<IEventSubscriptionRuntimeCatalog>();
        var orderingStates = subscriptionRuntimeCatalog?.States
            .Where(static state =>
                state.Metadata.TryGetValue(EventSubscriptionRuntimeMetadataKeys.SubscriptionOrdering, out var value) &&
                string.Equals(value, "provider-reported", StringComparison.OrdinalIgnoreCase))
            .ToArray() ?? [];
        var orderingProvenCount = orderingStates.Count(IsSubscriptionOrderingProof);
        var orderingState = SelectBestSubscriptionProof(
            orderingStates,
            IsSubscriptionOrderingProof);

        if (orderingState is not null)
        {
            var metadata = orderingState.Metadata;
            var subscriptionOrdering = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.SubscriptionOrdering,
                "not-claimed");
            var subscriptionOrderingSource = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.SubscriptionOrderingSource,
                "not-reported");
            var handlerOrderingGuarantee = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.HandlerOrderingGuarantee,
                "not-claimed");
            var handlerOrderingGuaranteeId = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.HandlerOrderingGuaranteeId,
                "not-reported");
            var localFanOutOrdering = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.LocalFanOutOrdering,
                "not-claimed");
            var localFanOutOrderingId = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.LocalFanOutOrderingId,
                "not-reported");
            var perKeyOrdering = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.PerKeyOrdering,
                "not-claimed");
            var perKeyOrderingKey = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.PerKeyOrderingKey,
                "not-reported");
            var partitionOrdering = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.PartitionOrdering,
                "not-claimed");
            var partitionOrderingId = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.PartitionOrderingId,
                "not-reported");
            var causalOrdering = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.CausalOrdering,
                "not-claimed");
            var causalOrderingId = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.CausalOrderingId,
                "not-reported");
            var replayOrdering = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.ReplayOrdering,
                "not-claimed");
            var replayOrderingCursorId = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.ReplayOrderingCursorId,
                "not-reported");
            var crossNodeOrdering = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.CrossNodeOrdering,
                "not-claimed");
            var crossNodeOrderingId = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.CrossNodeOrderingId,
                "not-reported");
            var providerOrdering = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.ProviderOrdering,
                "not-present");
            var providerOrderingId = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.ProviderOrderingId,
                "not-reported");
            var status = EventSubscriptionOrderingMetadata.IsOrderingProven(metadata)
                ? "claimed"
                : "partial";
            var nextGap = status == "claimed"
                ? "Keep handler, local fan-out, per-key, partition, causal, replay, cross-node, and provider ordering proof covered by provider integration tests."
                : "Complete handler, local fan-out, per-key, partition, causal, replay, cross-node, and provider ordering evidence before claiming subscription ordering ownership.";

            return new SubscriptionOrderingProfile(
                status,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"declaredSubscriptions={declaredSubscriptions}; inProcessExecution={inProcessExecution}; subscriptionExecutionPipeline={topology.SubscriptionExecutionPipeline}; subscriptionExecutionMiddlewareCount={middlewareCount}; managedSubscriptionBindings={managedSubscriptionBindings}; externalManagedSubscriptionBindings={externalManagedSubscriptionBindings}; subscriptionOrderingProofSelection=latest-proven-subscription-state; subscriptionOrderingStateCount={orderingStates.Length.ToString(CultureInfo.InvariantCulture)}; subscriptionOrderingProvenCount={orderingProvenCount.ToString(CultureInfo.InvariantCulture)}; subscriptionOrdering={subscriptionOrdering}; subscriptionOrderingSource={subscriptionOrderingSource}; handlerOrderingGuarantee={handlerOrderingGuarantee}; handlerOrderingGuaranteeId={handlerOrderingGuaranteeId}; localFanOutOrdering={localFanOutOrdering}; localFanOutOrderingId={localFanOutOrderingId}; perKeyOrdering={perKeyOrdering}; perKeyOrderingKey={perKeyOrderingKey}; partitionOrdering={partitionOrdering}; partitionOrderingId={partitionOrderingId}; causalOrdering={causalOrdering}; causalOrderingId={causalOrderingId}; replayOrdering={replayOrdering}; replayOrderingCursorId={replayOrderingCursorId}; crossNodeOrdering={crossNodeOrdering}; crossNodeOrderingId={crossNodeOrderingId}; providerOrdering={providerOrdering}; providerOrderingId={providerOrderingId}; subscriptionId={orderingState.SubscriptionId}; lastOutcome={orderingState.LastOutcome ?? "unknown"}; lastObservedAtUtc={FormatObservedAt(orderingState.LastObservedAtUtc)}; wolverineRequired=false"),
                nextGap);
        }

        return new SubscriptionOrderingProfile(
            "not-claimed",
            string.Create(
                CultureInfo.InvariantCulture,
                $"declaredSubscriptions={declaredSubscriptions}; inProcessExecution={inProcessExecution}; subscriptionExecutionPipeline={topology.SubscriptionExecutionPipeline}; subscriptionExecutionMiddlewareCount={middlewareCount}; managedSubscriptionBindings={managedSubscriptionBindings}; externalManagedSubscriptionBindings={externalManagedSubscriptionBindings}; subscriptionOrderingProofSelection=latest-proven-subscription-state; subscriptionOrderingStateCount={orderingStates.Length.ToString(CultureInfo.InvariantCulture)}; subscriptionOrderingProvenCount={orderingProvenCount.ToString(CultureInfo.InvariantCulture)}; subscriptionOrdering=not-claimed; subscriptionOrderingSource=not-reported; handlerOrderingGuarantee=not-claimed; handlerOrderingGuaranteeId=not-reported; localFanOutOrdering=not-claimed; localFanOutOrderingId=not-reported; perKeyOrdering=not-claimed; perKeyOrderingKey=not-reported; partitionOrdering=not-claimed; partitionOrderingId=not-reported; causalOrdering=not-claimed; causalOrderingId=not-reported; replayOrdering=not-claimed; replayOrderingCursorId=not-reported; crossNodeOrdering=not-claimed; crossNodeOrderingId=not-reported; providerOrdering=not-present; providerOrderingId=not-reported; wolverineRequired=false"),
            "Add provider-neutral subscription ordering proof metadata plus local, per-key, partition, causal, replay, cross-node, and provider ordering evidence before claiming subscription ordering ownership.");
    }

    private ProcessManagerStateProfile ResolveProcessManagerStateProfile()
    {
        var publicationPath = topology.HasPublishingPath ? "active" : "not-active";
        var declaredSubscriptions = topology.HasSubscriptionContributors ? "present" : "not-present";
        var inProcessExecution = topology.HasInProcessSubscriptionExecutionPath ? "active" : "not-active";
        var managedSubscriptionBindings = topology.HasManagedSubscriptionExecutionBindings ? "present" : "not-present";
        var externalManagedSubscriptionBindings = topology.HasExternalManagedSubscriptionExecutionBindings ? "present" : "not-present";
        var outboxHandoff = topology.HasOutboxPublishingPath ? "available" : "not-active";
        var middlewareCount = topology.SubscriptionExecutionMiddlewareCount.ToString(CultureInfo.InvariantCulture);

        using var scope = scopeFactory.CreateScope();
        var subscriptionRuntimeCatalog = scope.ServiceProvider.GetService<IEventSubscriptionRuntimeCatalog>();
        var processManagerStates = subscriptionRuntimeCatalog?.States
            .Where(static state =>
                state.Metadata.TryGetValue(EventSubscriptionRuntimeMetadataKeys.ProcessManagerState, out var value) &&
                string.Equals(value, "provider-reported", StringComparison.OrdinalIgnoreCase))
            .ToArray() ?? [];
        var processManagerProvenCount = processManagerStates.Count(IsProcessManagerStateProof);
        var processManagerState = SelectBestSubscriptionProof(
            processManagerStates,
            IsProcessManagerStateProof);

        if (processManagerState is not null)
        {
            var metadata = processManagerState.Metadata;
            var state = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.ProcessManagerState,
                "not-claimed");
            var stateSource = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.ProcessManagerStateSource,
                "not-reported");
            var sagaStatePersistence = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.SagaStatePersistence,
                "not-claimed");
            var sagaStatePersistenceId = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.SagaStatePersistenceId,
                "not-reported");
            var sagaCorrelation = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.SagaCorrelation,
                "not-claimed");
            var sagaCorrelationId = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.SagaCorrelationId,
                "not-reported");
            var sagaTimeouts = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.SagaTimeouts,
                "not-claimed");
            var sagaTimeoutSchedulerId = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.SagaTimeoutSchedulerId,
                "not-reported");
            var compensationWorkflow = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.CompensationWorkflow,
                "not-claimed");
            var compensationWorkflowId = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.CompensationWorkflowId,
                "not-reported");
            var processManagerConcurrency = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.ProcessManagerConcurrency,
                "not-claimed");
            var processManagerConcurrencyId = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.ProcessManagerConcurrencyId,
                "not-reported");
            var processManagerRecovery = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.ProcessManagerRecovery,
                "not-claimed");
            var processManagerRecoveryId = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.ProcessManagerRecoveryId,
                "not-reported");
            var providerProcessManager = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.ProviderProcessManager,
                "not-present");
            var providerProcessManagerId = GetMetadataValue(
                metadata,
                EventSubscriptionRuntimeMetadataKeys.ProviderProcessManagerId,
                "not-reported");
            var status = EventSubscriptionProcessManagerStateMetadata.IsProcessManagerStateProven(metadata)
                ? "claimed"
                : "partial";
            var nextGap = status == "claimed"
                ? "Keep saga/process-manager state persistence, correlation, timeouts, compensation, concurrency, recovery, and provider process-manager proof covered by provider integration tests."
                : "Complete saga/process-manager state persistence, correlation, timeouts, compensation, concurrency, recovery, and provider process-manager evidence before claiming process-manager state ownership.";

            return new ProcessManagerStateProfile(
                status,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"publicationPath={publicationPath}; declaredSubscriptions={declaredSubscriptions}; inProcessExecution={inProcessExecution}; subscriptionExecutionPipeline={topology.SubscriptionExecutionPipeline}; subscriptionExecutionMiddlewareCount={middlewareCount}; managedSubscriptionBindings={managedSubscriptionBindings}; externalManagedSubscriptionBindings={externalManagedSubscriptionBindings}; outboxHandoff={outboxHandoff}; processManagerStateProofSelection=latest-proven-subscription-state; processManagerStateCount={processManagerStates.Length.ToString(CultureInfo.InvariantCulture)}; processManagerStateProvenCount={processManagerProvenCount.ToString(CultureInfo.InvariantCulture)}; processManagerState={state}; processManagerStateSource={stateSource}; sagaStatePersistence={sagaStatePersistence}; sagaStatePersistenceId={sagaStatePersistenceId}; sagaCorrelation={sagaCorrelation}; sagaCorrelationId={sagaCorrelationId}; sagaTimeouts={sagaTimeouts}; sagaTimeoutSchedulerId={sagaTimeoutSchedulerId}; compensationWorkflow={compensationWorkflow}; compensationWorkflowId={compensationWorkflowId}; processManagerConcurrency={processManagerConcurrency}; processManagerConcurrencyId={processManagerConcurrencyId}; processManagerRecovery={processManagerRecovery}; processManagerRecoveryId={processManagerRecoveryId}; providerProcessManager={providerProcessManager}; providerProcessManagerId={providerProcessManagerId}; subscriptionId={processManagerState.SubscriptionId}; lastOutcome={processManagerState.LastOutcome ?? "unknown"}; lastObservedAtUtc={FormatObservedAt(processManagerState.LastObservedAtUtc)}; wolverineRequired=false"),
                nextGap);
        }

        return new ProcessManagerStateProfile(
            "not-claimed",
            string.Create(
                CultureInfo.InvariantCulture,
                $"publicationPath={publicationPath}; declaredSubscriptions={declaredSubscriptions}; inProcessExecution={inProcessExecution}; subscriptionExecutionPipeline={topology.SubscriptionExecutionPipeline}; subscriptionExecutionMiddlewareCount={middlewareCount}; managedSubscriptionBindings={managedSubscriptionBindings}; externalManagedSubscriptionBindings={externalManagedSubscriptionBindings}; outboxHandoff={outboxHandoff}; processManagerStateProofSelection=latest-proven-subscription-state; processManagerStateCount={processManagerStates.Length.ToString(CultureInfo.InvariantCulture)}; processManagerStateProvenCount={processManagerProvenCount.ToString(CultureInfo.InvariantCulture)}; processManagerState=not-claimed; processManagerStateSource=not-reported; sagaStatePersistence=not-claimed; sagaStatePersistenceId=not-reported; sagaCorrelation=not-claimed; sagaCorrelationId=not-reported; sagaTimeouts=not-claimed; sagaTimeoutSchedulerId=not-reported; compensationWorkflow=not-claimed; compensationWorkflowId=not-reported; processManagerConcurrency=not-claimed; processManagerConcurrencyId=not-reported; processManagerRecovery=not-claimed; processManagerRecoveryId=not-reported; providerProcessManager=not-present; providerProcessManagerId=not-reported; wolverineRequired=false"),
            "Add provider-neutral process-manager state proof metadata plus state persistence, correlation, timeout, compensation, concurrency, recovery, and provider process-manager evidence before claiming process-manager state ownership.");
    }

    private BrokerDeadLetterReplayProfile ResolveBrokerDeadLetterReplayProfile()
    {
        var dispatchStoreDeadLetterIntent = topology.HasDispatchStore ? "available" : "not-active";
        var dispatchRuntime = topology.HasDispatchRuntimeContributors ? "reported" : "not-reported";

        if (!topology.HasOutboxPublishingPath)
        {
            return new BrokerDeadLetterReplayProfile(
                "not-claimed",
                "no outbox-backed dispatch reporting path is active; brokerDeadLetterQueueOwnership=not-claimed; brokerReplay=not-claimed; wolverineRequired=false",
                "Add an outbox-backed dispatch reporting path before claiming broker dead-letter or replay ownership.");
        }

        using var scope = scopeFactory.CreateScope();
        var dispatchRuntimeCatalog = scope.ServiceProvider.GetService<IEventDispatchRuntimeCatalog>();
        var brokerDeadLetterStates = dispatchRuntimeCatalog?.States
            .Where(static state =>
                state.Metadata.TryGetValue(EventDispatchRuntimeMetadataKeys.BrokerDeadLetterReplayOwnership, out var value) &&
                string.Equals(value, "provider-reported", StringComparison.OrdinalIgnoreCase))
            .ToArray() ?? [];
        var brokerDeadLetterProvenCount = brokerDeadLetterStates.Count(IsBrokerDeadLetterReplayProof);
        var brokerDeadLetterState = SelectBestDispatchProof(
            brokerDeadLetterStates,
            IsBrokerDeadLetterReplayProof);

        if (brokerDeadLetterState is not null)
        {
            var metadata = brokerDeadLetterState.Metadata;
            var brokerDeadLetterReplayOwnership = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.BrokerDeadLetterReplayOwnership,
                "not-claimed");
            var brokerDeadLetterReplayOwnershipSource = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.BrokerDeadLetterReplayOwnershipSource,
                "not-reported");
            var brokerDeadLetterQueueOwnership = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.BrokerDeadLetterQueueOwnership,
                "not-claimed");
            var brokerDeadLetterQueueId = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.BrokerDeadLetterQueueId,
                "not-reported");
            var brokerReplay = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.BrokerReplay,
                "not-claimed");
            var brokerReplayActionCatalog = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.BrokerReplayActionCatalog,
                "not-claimed");
            var brokerReplayActionCatalogId = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.BrokerReplayActionCatalogId,
                "not-reported");
            var brokerReplayCursor = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.BrokerReplayCursor,
                "not-claimed");
            var brokerReplayCursorId = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.BrokerReplayCursorId,
                "not-reported");
            var brokerPurgeQuarantine = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.BrokerPurgeQuarantine,
                "not-claimed");
            var brokerPurgeQuarantineId = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.BrokerPurgeQuarantineId,
                "not-reported");
            var brokerDeadLetterReplayProofId = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.BrokerDeadLetterReplayProofId,
                "not-reported");
            var deadLetterOutcome = GetMetadataValue(
                metadata,
                EventDispatchRuntimeMetadataKeys.DeadLetterOutcome,
                "not-reported");
            var status = IsBrokerDeadLetterReplayProof(brokerDeadLetterState)
                ? "claimed"
                : "partial";
            var nextGap = status == "claimed"
                ? "Keep broker dead-letter queue, replay action catalog, replay cursor, purge/quarantine, and provider proof covered by provider integration tests."
                : "Complete broker dead-letter queue, replay action catalog, replay cursor, purge/quarantine, and provider proof evidence before claiming broker replay ownership.";

            return new BrokerDeadLetterReplayProfile(
                status,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"dispatchStoreDeadLetterIntent={dispatchStoreDeadLetterIntent}; dispatchRuntime={dispatchRuntime}; brokerDeadLetterReplayProofSelection=latest-proven-dispatch-state; brokerDeadLetterReplayStateCount={brokerDeadLetterStates.Length.ToString(CultureInfo.InvariantCulture)}; brokerDeadLetterReplayProvenCount={brokerDeadLetterProvenCount.ToString(CultureInfo.InvariantCulture)}; brokerDeadLetterReplayOwnership={brokerDeadLetterReplayOwnership}; brokerDeadLetterReplayOwnershipSource={brokerDeadLetterReplayOwnershipSource}; brokerDeadLetterQueueOwnership={brokerDeadLetterQueueOwnership}; brokerDeadLetterQueueId={brokerDeadLetterQueueId}; brokerReplay={brokerReplay}; brokerReplayActionCatalog={brokerReplayActionCatalog}; brokerReplayActionCatalogId={brokerReplayActionCatalogId}; brokerReplayCursor={brokerReplayCursor}; brokerReplayCursorId={brokerReplayCursorId}; brokerPurgeQuarantine={brokerPurgeQuarantine}; brokerPurgeQuarantineId={brokerPurgeQuarantineId}; brokerDeadLetterReplayProofId={brokerDeadLetterReplayProofId}; deadLetterOutcome={deadLetterOutcome}; outboxId={brokerDeadLetterState.OutboxId}; lastOutcome={brokerDeadLetterState.LastOutcome ?? "unknown"}; lastObservedAtUtc={FormatObservedAt(brokerDeadLetterState.LastObservedAtUtc)}; providerOwnedBrokerPath=reported; wolverineRequired=false"),
                nextGap);
        }

        return new BrokerDeadLetterReplayProfile(
            "not-claimed",
            string.Create(
                CultureInfo.InvariantCulture,
                $"dispatchStoreDeadLetterIntent={dispatchStoreDeadLetterIntent}; dispatchRuntime={dispatchRuntime}; brokerDeadLetterReplayProofSelection=latest-proven-dispatch-state; brokerDeadLetterReplayStateCount={brokerDeadLetterStates.Length.ToString(CultureInfo.InvariantCulture)}; brokerDeadLetterReplayProvenCount={brokerDeadLetterProvenCount.ToString(CultureInfo.InvariantCulture)}; brokerDeadLetterReplayOwnership=not-claimed; brokerDeadLetterQueueOwnership=not-claimed; brokerDeadLetterQueueId=not-reported; brokerReplay=not-claimed; brokerReplayActionCatalog=not-claimed; brokerReplayActionCatalogId=not-reported; brokerReplayCursor=not-claimed; brokerReplayCursorId=not-reported; brokerPurgeQuarantine=not-claimed; brokerPurgeQuarantineId=not-reported; brokerDeadLetterReplayProofId=not-reported; providerOwnedBrokerPath=not-present; wolverineRequired=false"),
            "Add provider-owned broker dead-letter queue, replay action catalog, replay cursor, purge/quarantine, and provider proof metadata before claiming broker replay ownership.");
    }

    private static string ResolveDurableCommandJournalStatus(EventDispatchRemediationCommandJournalDescriptor? descriptor)
    {
        if (descriptor is null)
        {
            return "not-claimed";
        }

        return IsDurableCrossNodeAudit(descriptor) ? "claimed" : "partial";
    }

    private static string ResolveDurableCommandJournalEvidence(EventDispatchRemediationCommandJournalDescriptor? descriptor)
    {
        if (descriptor is null)
        {
            return "no remediation command journal is active.";
        }

        var replayCursor = descriptor.DurableReplayCursor ? "durable" : "not-claimed";
        return string.Create(
            CultureInfo.InvariantCulture,
            $"journalId={descriptor.JournalId}; provider={descriptor.Provider}; storage={descriptor.Storage}; durability={descriptor.Durability}; scope={descriptor.Scope}; crossNodeCommandAudit={ToMetadataValue(descriptor.CrossNodeCommandAudit)}; replayCursor={replayCursor}; wolverineRequired=false");
    }

    private static string ResolveDurableCommandJournalNextGap(EventDispatchRemediationCommandJournalDescriptor? descriptor)
    {
        if (descriptor is null)
        {
            return "Activate an outbox-backed command path before claiming remediation command audit evidence.";
        }

        if (!IsDurableCrossNodeAudit(descriptor))
        {
            return "Use a provider-backed durable cross-node journal before claiming durable remediation command audit.";
        }

        return descriptor.DurableReplayCursor
            ? "Attach broker dead-letter replay ownership to the same audit trail when a provider package owns that path."
            : "Add a durable replay cursor contract before claiming broker replay ownership.";
    }

    private static string ResolveDurableCommandJournalReplayStatus(EventDispatchRemediationCommandJournalDescriptor? descriptor)
    {
        if (descriptor is null)
        {
            return "not-claimed";
        }

        return descriptor.DurableReplayCursor ? "claimed" : "partial";
    }

    private static string ResolveDurableCommandJournalReplayEvidence(EventDispatchRemediationCommandJournalDescriptor? descriptor)
    {
        if (descriptor is null)
        {
            return "no remediation command journal is active.";
        }

        var replayCursor = descriptor.DurableReplayCursor ? "durable" : "not-claimed";
        return descriptor.DurableReplayCursor
            ? string.Create(
                CultureInfo.InvariantCulture,
                $"journalId={descriptor.JournalId}; provider={descriptor.Provider}; replayCursor={replayCursor}; order=oldest-first-observed-utc-command-id; scope=command-journal; brokerReplay=not-claimed; wolverineRequired=false")
            : string.Create(
                CultureInfo.InvariantCulture,
                $"journalId={descriptor.JournalId}; provider={descriptor.Provider}; replayCursor={replayCursor}; brokerReplay=not-claimed; wolverineRequired=false");
    }

    private static string ResolveDurableCommandJournalReplayNextGap(EventDispatchRemediationCommandJournalDescriptor? descriptor)
    {
        if (descriptor is null)
        {
            return "Activate a remediation command journal before claiming command-journal replay cursor evidence.";
        }

        return descriptor.DurableReplayCursor
            ? "Attach broker dead-letter replay ownership only when a provider package owns that broker path."
            : "Implement IEventDispatchRemediationCommandReplayCursorCatalog in a durable provider before claiming command-journal replay cursor support.";
    }

    private static TechnologyRuntimeEntry CreateEntry(
        string id,
        string displayName,
        string description,
        string status,
        string evidence,
        string advantage,
        string nextGap)
    {
        return new TechnologyRuntimeEntry(
            id: id,
            displayName: displayName,
            description: description,
            metadata: new Dictionary<string, string>
            {
                ["status"] = status,
                ["referenceFrameworks"] = ReferenceFrameworks,
                ["claimPolicy"] = ClaimPolicy,
                ["runtimeEvidence"] = evidence,
                ["cephalonAdvantage"] = advantage,
                ["nextGap"] = nextGap
            });
    }

    private static bool IsDurableCrossNodeAudit(EventDispatchRemediationCommandJournalDescriptor descriptor)
    {
        return string.Equals(descriptor.Durability, "durable", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(descriptor.Scope, "cross-node", StringComparison.OrdinalIgnoreCase) &&
            descriptor.CrossNodeCommandAudit;
    }

    private static string ToMetadataValue(bool value) => value ? "true" : "false";

    private sealed record BrokerTopologyProfile(string Status, string Evidence, string NextGap);

    private sealed record ProviderPartitionProfile(string Status, string Evidence, string NextGap);

    private sealed record ChoreographyHandoffProfile(string Status, string Evidence, string NextGap);

    private sealed record SerializationVersioningProfile(string Status, string Evidence, string NextGap);

    private sealed record TenantCorrelationProfile(string Status, string Evidence, string NextGap);

    private sealed record DownstreamDeliveryCompletionProfile(string Status, string Evidence, string NextGap);

    private sealed record BrokerInboundConsumptionProfile(string Status, string Evidence, string NextGap);

    private sealed record ScheduledDeliveryProfile(string Status, string Evidence, string NextGap);

    private sealed record DurableRetryQueueProfile(string Status, string Evidence, string NextGap);

    private sealed record IdempotencyOwnershipProfile(string Status, string Evidence, string NextGap);

    private sealed record SubscriptionConcurrencyProfile(string Status, string Evidence, string NextGap);

    private sealed record SubscriptionOrderingProfile(string Status, string Evidence, string NextGap);

    private sealed record ProcessManagerStateProfile(string Status, string Evidence, string NextGap);

    private sealed record BrokerDeadLetterReplayProfile(string Status, string Evidence, string NextGap);
}
