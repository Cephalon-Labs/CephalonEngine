using BenchmarkDotNet.Attributes;
using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Technologies;
using Cephalon.Benchmarks.Support;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Runtime;
using Cephalon.Eventing.Registration;
using Cephalon.Eventing.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;

namespace Cephalon.Benchmarks.HotPath;

/// <summary>
/// Measures provider-operated Eventing proof reports and aggregate runtime evidence readback without Wolverine.
/// </summary>
[MemoryDiagnoser]
[Config(typeof(BenchmarkInProcessShortRunConfig))]
public class EventProviderOperatedEventingBenchmarks
{
    private const int ProviderOperatedOperationsPerIteration = 256;
    private const string OutboxId = "benchmark-operated-outbox";
    private const string RetryOutboxId = "benchmark-operated-retry-outbox";
    private const string DeadLetterOutboxId = "benchmark-operated-dead-letter-outbox";
    private const string ChannelId = "benchmark-provider-operated-events";
    private const string SubscriptionId = "benchmark-provider-operated-consumer";
    private const string DispatchRuntimeId = "benchmark-provider-operated-dispatch-runtime";
    private const string SubscriptionRuntimeId = "benchmark-provider-operated-subscription-runtime";
    private const string SourceModuleId = "benchmark-provider-operated-eventing-module";
    private const string ProviderSource = "provider-operated-benchmark";

    private static readonly DateTimeOffset ObservedAtUtc = new(2026, 5, 13, 9, 0, 0, TimeSpan.Zero);

    private ServiceProvider provider = null!;
    private EngineRuntime runtime = null!;
    private IEventDispatchRuntimeReporter dispatchReporter = null!;
    private IEventSubscriptionRuntimeReporter subscriptionReporter = null!;
    private ITechnologyRuntimeCatalog technologyRuntimeCatalog = null!;

    /// <summary>
    /// Builds a provider-operated Eventing proof runtime using only Cephalon-owned contracts.
    /// </summary>
    [GlobalSetup]
    public async Task Setup()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IOutbox, InMemoryBenchmarkOutbox>();
        services.AddSingleton<IEventDispatchStore, BenchmarkProviderOperatedDispatchStore>();
        services.AddSingleton<IEventDispatchRuntimeContributor, BenchmarkProviderOperatedDispatchRuntimeContributor>();
        services.AddSingleton<IEventSubscriptionContributor, BenchmarkProviderOperatedSubscriptionContributor>();
        services.AddSingleton<IEventSubscriptionExecutionBindingContributor, BenchmarkProviderOperatedSubscriptionBindingContributor>();

        var builder = new EngineBuilder(services);
        builder.UseSettings(new EngineSettings(
            blueprint: "modular-vertical-slice",
            patterns: ["cqrs", "outbox"],
            transports: ["rest-api"],
            technologies: ["event-driven-integration"]));
        builder.AddModule(new BenchmarkProviderOperatedEventingModule());
        builder.AddEventing(options =>
        {
            options.Channels.Add(new EventChannelDescriptor(
                id: ChannelId,
                displayName: "Benchmark Provider-operated Events",
                description: "Provider-operated benchmark channel for aggregate Eventing proof reports."));
            options.ContextPolicies.Add(new EventContextPolicyDescriptor(
                id: "benchmark-provider-operated-context",
                displayName: "Benchmark Provider-operated Context",
                description: "Code-first context policy used by the provider-operated Eventing benchmark.",
                runtimeKind: "code-first",
                declaresTenantContext: true,
                declaresCorrelationId: true,
                declaresCausationId: true,
                declaresBaggage: true,
                validatesMessageHeaders: true,
                headerNames:
                [
                    EventContextHeaderNames.TenantId,
                    EventContextHeaderNames.CorrelationId,
                    EventContextHeaderNames.CausationId,
                    EventContextHeaderNames.Baggage
                ]));
        });

        runtime = builder.Build();
        services.AddSingleton<IRuntime>(runtime);
        provider = services.BuildServiceProvider();
        await runtime.InitializeAsync(provider).ConfigureAwait(false);

        dispatchReporter = provider.GetRequiredService<IEventDispatchRuntimeReporter>();
        subscriptionReporter = provider.GetRequiredService<IEventSubscriptionRuntimeReporter>();
        technologyRuntimeCatalog = provider.GetRequiredService<ITechnologyRuntimeCatalog>();

        _ = await ReportProviderOperatedEventingProofs().ConfigureAwait(false);
    }

    /// <summary>
    /// Releases all DI resources.
    /// </summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        provider.Dispose();
        runtime.Dispose();
    }

    /// <summary>
    /// Records provider-operated dispatch, retry, replay, wire-contract, context, and subscription proofs, then reads the aggregate profile.
    /// </summary>
    [Benchmark(OperationsPerInvoke = ProviderOperatedOperationsPerIteration)]
    public async Task<long> ReportProviderOperatedEventingProofs()
    {
        long total = 0;
        for (var index = 0; index < ProviderOperatedOperationsPerIteration; index++)
        {
            var suffix = (index % 128).ToString("D4", CultureInfo.InvariantCulture);
            await dispatchReporter.ReportAsync(CreateProviderOperatedDispatchReport(index, suffix)).ConfigureAwait(false);
            await dispatchReporter.ReportAsync(CreateProviderOperatedRetryReport(index, suffix)).ConfigureAwait(false);
            await dispatchReporter.ReportAsync(CreateProviderOperatedDeadLetterReport(index, suffix)).ConfigureAwait(false);
            await subscriptionReporter.ReportAsync(CreateProviderOperatedSubscriptionReport(index, suffix)).ConfigureAwait(false);
        }

        return total + ReadProviderOperatedAggregateProof();
    }

    private long ReadProviderOperatedAggregateProof()
    {
        var eventingSurfaces = technologyRuntimeCatalog.GetByTechnology("event-driven-integration");
        var profile = eventingSurfaces.First(static surface => surface.SurfaceId == "eventing-superiority-profile");
        var aggregate = profile.Entries.First(static entry => entry.Id == "provider-operated-runtime-proof-coverage");

        long total = aggregate.Metadata.Count;
        total += aggregate.Metadata.TryGetValue("status", out var status)
            ? status.Length
            : 0;
        total += aggregate.Metadata.TryGetValue("runtimeEvidence", out var evidence)
            ? evidence.Length
            : 0;
        return total;
    }

    private static EventDispatchExecutionReport CreateProviderOperatedDispatchReport(int index, string suffix)
    {
        var consumerContextMetadata = CreateConsumerContextMetadata(suffix);
        var providerHeaders = CreateProviderHeaders(suffix);
        var contextMetadata = CreateContextMetadata(providerHeaders);
        var baseReport = new EventDispatchExecutionReport(
            outboxId: OutboxId,
            channelId: ChannelId,
            outcome: EventDispatchExecutionOutcomes.Succeeded,
            observedAtUtc: ObservedAtUtc.AddTicks(index),
            messageId: $"provider-operated-message-{suffix}",
            attempt: (index % 3) + 1,
            metadata: contextMetadata);

        var contextPersistedReport = EventDispatchProviderContextPersistenceMetadata.CreateReport(
            baseReport,
            source: ProviderSource);
        var contextHandoffReport = EventDispatchCrossNodeContextHandoffMetadata.CreateReport(
            contextPersistedReport,
            consumerContextMetadata,
            source: ProviderSource,
            producerNodeId: $"producer-node-{suffix}",
            consumerNodeId: $"consumer-node-{suffix}");
        var topologyReport = EventDispatchBrokerTopologyMetadata.CreateReport(
            contextHandoffReport,
            ProviderSource,
            exchangeProvisioningId: $"exchange-{suffix}",
            queueProvisioningId: $"queue-{suffix}",
            topicProvisioningId: $"topic-{suffix}",
            partitionProvisioningId: $"partition-{suffix}",
            topologyVerificationId: $"topology-verification-{suffix}",
            providerTopologyId: $"provider-topology-{suffix}");
        var partitionReport = EventDispatchProviderPartitionMetadata.CreateReport(
            topologyReport,
            ProviderSource,
            partitionAssignmentId: $"partition-assignment-{suffix}",
            partitionAffinityId: $"partition-affinity-{suffix}",
            partitionRebalancingId: $"partition-rebalance-{suffix}",
            partitionOrderingGuaranteeId: $"partition-ordering-{suffix}",
            providerPartitioningId: $"provider-partitioning-{suffix}");
        var deliveryReport = EventDispatchExactlyOnceDeliveryProofMetadata.CreateReport(
            partitionReport,
            ProviderSource,
            providerReceiptId: $"provider-receipt-{suffix}",
            subscriberAcknowledgementId: $"subscriber-ack-{suffix}",
            destinationCommitId: $"destination-commit-{suffix}",
            exactlyOnceProofId: $"exactly-once-{suffix}",
            strategy: "provider-transactional-ack");
        var scheduledReport = EventDispatchScheduledDeliveryMetadata.CreateReport(
            deliveryReport,
            ProviderSource,
            durableScheduledDeliveryId: $"durable-schedule-{suffix}",
            providerDelayQueueId: $"provider-delay-{suffix}",
            brokerScheduledDeliveryId: $"broker-schedule-{suffix}",
            scheduleCoordinationId: $"schedule-coordination-{suffix}",
            scheduleRecoveryId: $"schedule-recovery-{suffix}");

        return EventDispatchWireContractMetadata.CreateReport(
            scheduledReport,
            ProviderSource,
            payloadSerializationExecutionId: $"payload-serialization-{suffix}",
            wireEnvelopeSchemaExecutionId: $"wire-envelope-schema-{suffix}",
            schemaLookupExecutionId: $"schema-lookup-{suffix}",
            contractVersionNegotiationExecutionId: $"contract-version-negotiation-{suffix}",
            upcasterExecutionId: $"upcaster-execution-{suffix}",
            compatibilityValidationExecutionId: $"compatibility-validation-{suffix}",
            providerSerializationId: $"provider-serialization-{suffix}",
            wireContractProofId: $"wire-contract-proof-{suffix}");
    }

    private static EventDispatchExecutionReport CreateProviderOperatedRetryReport(int index, string suffix)
    {
        return EventDispatchDurableRetryQueueMetadata.CreateReport(
            new EventDispatchExecutionReport(
                outboxId: RetryOutboxId,
                channelId: ChannelId,
                outcome: EventDispatchExecutionOutcomes.RetryScheduled,
                observedAtUtc: ObservedAtUtc.AddMinutes(1).AddTicks(index),
                messageId: $"provider-operated-retry-message-{suffix}",
                attempt: (index % 4) + 2,
                error: "Provider accepted retry into a durable retry queue."),
            ProviderSource,
            durableRetryQueueId: $"durable-retry-queue-{suffix}",
            retryPersistenceId: $"retry-persistence-{suffix}",
            brokerErrorQueueId: $"broker-error-queue-{suffix}",
            poisonQueueId: $"poison-queue-{suffix}",
            retryCoordinationId: $"retry-coordination-{suffix}",
            retryLeaseId: $"retry-lease-{suffix}");
    }

    private static EventDispatchExecutionReport CreateProviderOperatedDeadLetterReport(int index, string suffix)
    {
        return EventDispatchBrokerDeadLetterReplayMetadata.CreateReport(
            new EventDispatchExecutionReport(
                outboxId: DeadLetterOutboxId,
                channelId: ChannelId,
                outcome: EventDispatchExecutionOutcomes.Failed,
                observedAtUtc: ObservedAtUtc.AddMinutes(2).AddTicks(index),
                messageId: $"provider-operated-dead-letter-message-{suffix}",
                attempt: (index % 5) + 3,
                error: "Provider moved the event into a broker dead-letter queue."),
            ProviderSource,
            brokerDeadLetterQueueId: $"broker-dead-letter-queue-{suffix}",
            brokerReplayActionCatalogId: $"broker-replay-actions-{suffix}",
            brokerReplayCursorId: $"broker-replay-cursor-{suffix}",
            brokerPurgeQuarantineId: $"broker-purge-quarantine-{suffix}",
            providerProofId: $"dlq-proof-{suffix}");
    }

    private static EventSubscriptionExecutionReport CreateProviderOperatedSubscriptionReport(int index, string suffix)
    {
        var baseReport = new EventSubscriptionExecutionReport(
            subscriptionId: SubscriptionId,
            outcome: EventSubscriptionExecutionOutcomes.Succeeded,
            observedAtUtc: ObservedAtUtc.AddMinutes(3).AddTicks(index),
            messageId: $"provider-operated-message-{suffix}",
            attempt: (index % 4) + 1,
            metadata: CreateConsumerContextMetadata(suffix));
        var inboundReport = EventSubscriptionBrokerInboundConsumptionMetadata.CreateReport(
            baseReport,
            ProviderSource,
            consumerLoopId: $"consumer-loop-{suffix}",
            acknowledgementId: $"ack-{suffix}",
            leaseId: $"consumer-lease-{suffix}",
            retryPolicy: "provider-bounded-retry",
            poisonMessageHandling: "dead-letter-and-replay",
            offsetCheckpointId: $"offset-checkpoint-{suffix}");
        var idempotencyReport = EventSubscriptionProviderIdempotencyMetadata.CreateReport(
            inboundReport,
            ProviderSource,
            providerIdempotencyKey: $"{SubscriptionId}:{suffix}",
            brokerDeduplicationId: $"broker-dedup-{suffix}",
            exactlyOnceProofId: $"consumer-exactly-once-{suffix}",
            durableInboxCommandId: $"durable-inbox-command-{suffix}",
            genericInboxCommandId: $"generic-inbox-command-{suffix}",
            idempotencyLeaseId: $"idempotency-lease-{suffix}");
        var concurrencyReport = EventSubscriptionConcurrencyMetadata.CreateReport(
            idempotencyReport,
            ProviderSource,
            perSubscriptionConcurrencyLimit: 16,
            consumerPrefetchCount: 64,
            backpressureStrategy: "bounded-channel",
            providerConcurrencyId: $"provider-concurrency-{suffix}",
            consumerLeaseId: $"concurrency-lease-{suffix}",
            workStealingId: $"work-stealing-{suffix}",
            distributedWorkSharingId: $"distributed-work-sharing-{suffix}");
        var orderingReport = EventSubscriptionOrderingMetadata.CreateReport(
            concurrencyReport,
            ProviderSource,
            handlerOrderingGuaranteeId: $"handler-ordering-{suffix}",
            localFanOutOrderingId: $"local-fanout-ordering-{suffix}",
            perKeyOrderingKey: $"tenant:{suffix}",
            partitionOrderingId: $"partition-ordering-{suffix}",
            causalOrderingId: $"causal-ordering-{suffix}",
            replayOrderingCursorId: $"replay-cursor-{suffix}",
            crossNodeOrderingId: $"cross-node-ordering-{suffix}",
            providerOrderingId: $"provider-ordering-{suffix}");

        return EventSubscriptionProcessManagerStateMetadata.CreateReport(
            orderingReport,
            ProviderSource,
            sagaStatePersistenceId: $"saga-state-{suffix}",
            sagaCorrelationId: $"saga-correlation-{suffix}",
            sagaTimeoutSchedulerId: $"saga-timeout-{suffix}",
            compensationWorkflowId: $"compensation-workflow-{suffix}",
            processManagerConcurrencyId: $"process-manager-concurrency-{suffix}",
            processManagerRecoveryId: $"process-manager-recovery-{suffix}",
            providerProcessManagerId: $"provider-process-manager-{suffix}");
    }

    private static Dictionary<string, string> CreateContextMetadata(Dictionary<string, string> providerHeaders)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [EventDispatchRuntimeMetadataKeys.DurableDispatchContextPropagation] = "dispatch-report-metadata",
            [EventDispatchRuntimeMetadataKeys.DispatchContextMetadata] = "reported",
            [EventDispatchRuntimeMetadataKeys.DispatchContextHeaderCount] = providerHeaders.Count.ToString(CultureInfo.InvariantCulture),
            [EventDispatchRuntimeMetadataKeys.DispatchContextMetadataCount] = "4",
            ["dispatchRuntimeId"] = DispatchRuntimeId,
            ["dispatchOwnership"] = "provider-operated",
            ["providerOperatedEventing"] = "true",
            ["wolverineRequired"] = "false"
        };
        EventDispatchProviderBrokerContextHeaders.ApplyReportMetadata(metadata, providerHeaders);
        return metadata;
    }

    private static Dictionary<string, string> CreateProviderHeaders(string suffix) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            [EventContextHeaderNames.TenantId] = $"tenant-{suffix}",
            [EventContextHeaderNames.CorrelationId] = $"corr-{suffix}",
            [EventContextHeaderNames.CausationId] = $"cause-{suffix}",
            [EventContextHeaderNames.Baggage] = $"tier=operated-{suffix}",
            [EventContextHeaderNames.MessageId] = $"msg-{suffix}"
        };

    private static Dictionary<string, string> CreateConsumerContextMetadata(string suffix)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        EventConsumerContextExtractor.ApplyMetadata(metadata, CreateProviderHeaders(suffix));
        return metadata;
    }

    private sealed class BenchmarkProviderOperatedEventingModule : ModuleBase, IOutboxContributor
    {
        private static readonly ModuleDescriptor DescriptorInstance = new(
            id: SourceModuleId,
            displayName: "Benchmark Provider-operated Eventing Module",
            description: "Benchmark module that owns provider-operated Eventing proof descriptors.",
            tags: ["benchmark", "eventing", "provider-operated"],
            version: "1.0.0");

        public override ModuleDescriptor Descriptor => DescriptorInstance;

        public void RegisterOutboxes(IOutboxRegistry outboxes)
        {
            outboxes.Add(new OutboxDescriptor(
                id: OutboxId,
                displayName: "Benchmark Provider-operated Outbox",
                description: "In-memory benchmark outbox boundary used by provider-operated Eventing guardrails.",
                sourceModuleId: SourceModuleId,
                provider: "benchmark-provider",
                mode: "in-memory",
                channelIds: [ChannelId],
                tags: ["benchmark", "provider-operated"],
                dispatchPolicy: new OutboxDispatchPolicyDescriptor(
                    outboxId: OutboxId,
                    policyId: "benchmark-provider-operated-dispatch",
                    displayName: "Benchmark Provider-operated Dispatch",
                    description: "Provider-operated benchmark dispatch policy.",
                    executionMode: "runtime-managed",
                    runtimeId: DispatchRuntimeId,
                    metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["dispatchRuntime"] = "configured",
                        ["dispatchRuntimeId"] = DispatchRuntimeId,
                        ["dispatchOwnership"] = "provider-operated",
                        ["wolverineRequired"] = "false"
                    })));
            outboxes.Add(new OutboxDescriptor(
                id: RetryOutboxId,
                displayName: "Benchmark Provider-operated Retry Outbox",
                description: "In-memory benchmark outbox boundary used by provider-operated retry guardrails.",
                sourceModuleId: SourceModuleId,
                provider: "benchmark-provider",
                mode: "in-memory",
                channelIds: [ChannelId],
                tags: ["benchmark", "provider-operated", "retry"]));
            outboxes.Add(new OutboxDescriptor(
                id: DeadLetterOutboxId,
                displayName: "Benchmark Provider-operated Dead-letter Outbox",
                description: "In-memory benchmark outbox boundary used by provider-operated dead-letter guardrails.",
                sourceModuleId: SourceModuleId,
                provider: "benchmark-provider",
                mode: "in-memory",
                channelIds: [ChannelId],
                tags: ["benchmark", "provider-operated", "dead-letter"]));
        }
    }

    private sealed class BenchmarkProviderOperatedDispatchRuntimeContributor : IEventDispatchRuntimeContributor
    {
        public void RegisterDispatchRuntimes(IEventDispatchRuntimeRegistry dispatchRuntimes)
        {
            dispatchRuntimes.Add(new EventDispatchRuntimeDescriptor(
                id: DispatchRuntimeId,
                displayName: "Benchmark Provider-operated Dispatch Runtime",
                description: "Provider-operated benchmark runtime that reports resilience, wire-contract, and context proofs through Cephalon.",
                metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["runtimeKind"] = "provider-operated-eventing",
                    ["dispatchOwnership"] = "provider-operated",
                    ["wolverineRequired"] = "false"
                },
                outboxIds: [OutboxId, RetryOutboxId, DeadLetterOutboxId]));
        }
    }

    private sealed class BenchmarkProviderOperatedDispatchStore : IEventDispatchStore
    {
        public IReadOnlyList<string> OutboxIds { get; } = [OutboxId, RetryOutboxId, DeadLetterOutboxId];

        public ValueTask<IReadOnlyList<EventDispatchItem>> ReadPendingAsync(
            int maximumCount,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult<IReadOnlyList<EventDispatchItem>>([]);
        }

        public ValueTask ApplyReportAsync(
            EventDispatchExecutionReport report,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }
    }

    private sealed class BenchmarkProviderOperatedSubscriptionContributor : IEventSubscriptionContributor
    {
        public void RegisterSubscriptions(IEventSubscriptionRegistry subscriptions)
        {
            subscriptions.Add(new EventSubscriptionDescriptor(
                id: SubscriptionId,
                displayName: "Benchmark Provider-operated Consumer",
                description: "Provider-operated benchmark subscription for aggregate runtime proof reports.",
                channelId: ChannelId,
                handlerId: "benchmark-provider-operated-handler",
                deliveryMode: "broker-consumer",
                tags: ["benchmark", "provider-operated"],
                metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["subscriptionOwnership"] = "provider-operated",
                    ["wolverineRequired"] = "false"
                }));
        }
    }

    private sealed class BenchmarkProviderOperatedSubscriptionBindingContributor : IEventSubscriptionExecutionBindingContributor
    {
        public IReadOnlyList<EventSubscriptionExecutionBindingDescriptor> GetExecutionBindings()
        {
            return
            [
                new EventSubscriptionExecutionBindingDescriptor(
                    subscriptionId: SubscriptionId,
                    executionRuntimeId: SubscriptionRuntimeId,
                    executionOwnership: "provider-operated",
                    executionMode: "broker-consumer-loop",
                    metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["runtimeKind"] = "provider-operated-eventing",
                        ["brokerConsumerLoop"] = "provider-operated",
                        ["wolverineRequired"] = "false"
                    })
            ];
        }
    }
}
