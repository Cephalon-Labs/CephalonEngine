using Cephalon.Abstractions.Data;
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

    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        var routeCount = options.PublicationRoutes.Count.ToString(CultureInfo.InvariantCulture);
        var routingStatus = options.EnablePublicationRouting && options.PublicationRoutes.Count > 0
            ? "claimed"
            : options.EnablePublicationRouting ? "partial" : "not-claimed";
        var routingEvidence = options.EnablePublicationRouting
            ? $"policy={EventPublicationRoutingPolicy.GetPolicyId(options)} routes={routeCount} autoChannel={EventPublicationRoutingPolicy.GetAutoChannelId(options)}"
            : "publication routing is not enabled.";
        var brokerTopologyEvidence = ResolveBrokerTopologyEvidence(options, routeCount);
        var providerPartitionEvidence = ResolveProviderPartitionEvidence(options, routeCount);
        var downstreamDeliveryCompletionEvidence = ResolveDownstreamDeliveryCompletionEvidence(topology);
        var brokerInboundConsumptionEvidence = ResolveBrokerInboundConsumptionEvidence(topology);
        var remediationReadPerformanceStatus = topology.HasOutboxPublishingPath ? "claimed" : "partial";
        var remediationReadPerformanceEvidence = topology.HasOutboxPublishingPath
            ? $"benchmarks={RemediationFilteredReadBenchmarks}; readPolicy=single-pass-retained-catalog; materialization=not-required; wolverineRequired=false"
            : $"benchmark guardrails exist for {RemediationFilteredReadBenchmarks}; no outbox-backed command path is active.";
        var brokerDeadLetterReplayEvidence = ResolveBrokerDeadLetterReplayEvidence(topology);
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
                    status: "not-claimed",
                    evidence: brokerTopologyEvidence,
                    advantage: "Teams can use Cephalon channel routing without assuming the engine silently provisions provider-specific topology or binds application code to a broker API.",
                    nextGap: "Add a provider-owned broker topology descriptor and validation/provisioning catalog before claiming topology materialization."),
                CreateEntry(
                    id: "provider-partition-ownership",
                    displayName: "Provider Partition Ownership",
                    description: "Makes provider-specific partition assignment, affinity, rebalancing, and ordering guarantees explicit instead of inferring them from Cephalon route or broker-topology metadata.",
                    status: "not-claimed",
                    evidence: providerPartitionEvidence,
                    advantage: "Teams can route events through Cephalon without assuming the engine silently owns provider partition placement or per-partition ordering semantics.",
                    nextGap: "Add a provider-owned partition descriptor plus assignment, rebalancing, affinity, and ordering-evidence catalog before claiming partition ownership."),
                CreateEntry(
                    id: "downstream-delivery-completion-ownership",
                    displayName: "Downstream Delivery Completion Ownership",
                    description: "Makes provider destination delivery completion, subscriber acknowledgements, and exactly-once completion explicit instead of inferring them from outbox accepted handoff or dispatch reports.",
                    status: "not-claimed",
                    evidence: downstreamDeliveryCompletionEvidence,
                    advantage: "Teams can read Cephalon publication and dispatch truth without assuming the engine silently proves broker/provider destination delivery or subscriber acknowledgement.",
                    nextGap: "Add a provider-owned delivery completion descriptor plus acknowledgement, receipt, and completion-evidence catalog before claiming downstream delivery completion."),
                CreateEntry(
                    id: "broker-inbound-consumption-ownership",
                    displayName: "Broker Inbound Consumption Ownership",
                    description: "Makes provider-owned inbound broker consumption, acknowledgements, and offset checkpoints explicit instead of inferring them from declared subscriptions or in-process execution.",
                    status: "not-claimed",
                    evidence: brokerInboundConsumptionEvidence,
                    advantage: "Teams can use Cephalon subscription descriptors, direct in-process execution, and optional provider bindings without assuming the core pack silently owns a generic broker consumer loop.",
                    nextGap: "Add a provider-owned inbound consumption descriptor plus acknowledgement, retry, lease, and offset-checkpoint evidence before claiming broker inbound consumption."),
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
                    status: "not-claimed",
                    evidence: brokerDeadLetterReplayEvidence,
                    advantage: "Operators can see that Cephalon-owned command journals and dispatch-store remediation do not silently promise broker DLQ mutation, broker replay, or a Wolverine dependency.",
                    nextGap: "Add a provider-owned broker dead-letter descriptor and replay action catalog before claiming broker replay ownership."),
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

    private static string ResolveBrokerTopologyEvidence(EventingOptions options, string routeCount)
    {
        if (!options.EnablePublicationRouting)
        {
            return "publication routing is not enabled; brokerTopologyMaterialization=not-claimed; providerOwnedTopology=not-present; wolverineRequired=false";
        }

        return string.Create(
            CultureInfo.InvariantCulture,
            $"routingPolicy={EventPublicationRoutingPolicy.GetPolicyId(options)}; routes={routeCount}; autoChannel={EventPublicationRoutingPolicy.GetAutoChannelId(options)}; brokerTopologyMaterialization=not-claimed; exchangeProvisioning=not-claimed; queueProvisioning=not-claimed; topicProvisioning=not-claimed; partitionOwnership=not-claimed; providerOwnedTopology=not-present; wolverineRequired=false");
    }

    private static string ResolveProviderPartitionEvidence(EventingOptions options, string routeCount)
    {
        if (!options.EnablePublicationRouting)
        {
            return "publication routing is not enabled; providerPartitionOwnership=not-claimed; providerOwnedPartitioning=not-present; wolverineRequired=false";
        }

        return string.Create(
            CultureInfo.InvariantCulture,
            $"routingPolicy={EventPublicationRoutingPolicy.GetPolicyId(options)}; routes={routeCount}; autoChannel={EventPublicationRoutingPolicy.GetAutoChannelId(options)}; providerPartitionOwnership=not-claimed; partitionAssignment=not-claimed; partitionAffinity=not-claimed; partitionRebalancing=not-claimed; partitionOrderingGuarantee=not-claimed; providerOwnedPartitioning=not-present; wolverineRequired=false");
    }

    private static string ResolveDownstreamDeliveryCompletionEvidence(EventingRuntimeTopology topology)
    {
        if (!topology.HasPublishingPath)
        {
            return "no publication path is active; downstreamDeliveryCompletion=not-claimed; providerDeliveryReceipt=not-present; subscriberAcknowledgement=not-claimed; destinationCommit=not-claimed; exactlyOnceDelivery=not-claimed; wolverineRequired=false";
        }

        var handoff = topology.HasOutboxPublishingPath ? "outbox-accepted" : "direct-or-provider-publisher";
        var dispatchRuntime = topology.HasDispatchRuntimeContributors ? "reported" : "not-reported";

        return string.Create(
            CultureInfo.InvariantCulture,
            $"publicationPath=active; handoff={handoff}; dispatchRuntime={dispatchRuntime}; downstreamDeliveryCompletion=not-claimed; providerDeliveryReceipt=not-present; subscriberAcknowledgement=not-claimed; destinationCommit=not-claimed; exactlyOnceDelivery=not-claimed; wolverineRequired=false");
    }

    private static string ResolveBrokerInboundConsumptionEvidence(EventingRuntimeTopology topology)
    {
        var declaredSubscriptions = topology.HasSubscriptionContributors ? "present" : "not-present";
        var inProcessExecution = topology.HasInProcessSubscriptionExecutionPath ? "active" : "not-active";
        var managedSubscriptionBindings = topology.HasManagedSubscriptionExecutionBindings ? "present" : "not-present";
        var externalManagedSubscriptionBindings = topology.HasExternalManagedSubscriptionExecutionBindings ? "present" : "not-present";
        var inboxPath = topology.HasInboxPath ? "present" : "not-present";

        return string.Create(
            CultureInfo.InvariantCulture,
            $"declaredSubscriptions={declaredSubscriptions}; inProcessExecution={inProcessExecution}; managedSubscriptionBindings={managedSubscriptionBindings}; externalManagedSubscriptionBindings={externalManagedSubscriptionBindings}; inboxPath={inboxPath}; brokerInboundConsumption=not-claimed; brokerConsumerLoop=not-present; providerOwnedConsumer=not-present; inboundAcknowledgement=not-claimed; consumerOffsetCheckpoint=not-claimed; wolverineRequired=false");
    }

    private static string ResolveBrokerDeadLetterReplayEvidence(EventingRuntimeTopology topology)
    {
        if (!topology.HasOutboxPublishingPath)
        {
            return "no outbox-backed dispatch reporting path is active; brokerDeadLetterQueueOwnership=not-claimed; brokerReplay=not-claimed; wolverineRequired=false";
        }

        var dispatchStoreDeadLetterIntent = topology.HasDispatchStore ? "available" : "not-active";
        var dispatchRuntime = topology.HasDispatchRuntimeContributors ? "reported" : "not-reported";
        return string.Create(
            CultureInfo.InvariantCulture,
            $"dispatchStoreDeadLetterIntent={dispatchStoreDeadLetterIntent}; dispatchRuntime={dispatchRuntime}; brokerDeadLetterQueueOwnership=not-claimed; brokerReplay=not-claimed; providerOwnedBrokerPath=not-present; wolverineRequired=false");
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
}
