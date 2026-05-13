using BenchmarkDotNet.Attributes;
using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Modules;
using Cephalon.Benchmarks.Support;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Eventing.Registration;
using Cephalon.Eventing.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;

namespace Cephalon.Benchmarks.HotPath;

/// <summary>
/// Measures provider-managed Eventing proof reports and runtime evidence readback without Wolverine.
/// </summary>
[MemoryDiagnoser]
[Config(typeof(BenchmarkInProcessShortRunConfig))]
public class EventProviderManagedEventingBenchmarks
{
    private const int ProviderManagedOperationsPerIteration = 512;
    private const string OutboxId = "benchmark-outbox";
    private const string ChannelId = "benchmark-provider-managed-events";
    private const string SubscriptionId = "benchmark-provider-managed-consumer";
    private const string DispatchRuntimeId = "benchmark-provider-managed-dispatch-runtime";
    private const string SubscriptionRuntimeId = "benchmark-provider-managed-subscription-runtime";
    private const string SourceModuleId = "benchmark-provider-managed-eventing-module";
    private const string ProviderSource = "provider-managed-benchmark";

    private static readonly DateTimeOffset ObservedAtUtc = new(2026, 5, 13, 8, 0, 0, TimeSpan.Zero);

    private ServiceProvider provider = null!;
    private IEventDispatchRuntimeCatalog dispatchCatalog = null!;
    private IEventDispatchRuntimeReporter dispatchReporter = null!;
    private IEventSubscriptionRuntimeCatalog subscriptionCatalog = null!;
    private IEventSubscriptionRuntimeReporter subscriptionReporter = null!;

    /// <summary>
    /// Builds a provider-managed Eventing proof runtime using only Cephalon-owned contracts.
    /// </summary>
    [GlobalSetup]
    public async Task Setup()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IOutbox, InMemoryBenchmarkOutbox>();
        services.AddSingleton<IEventDispatchRuntimeContributor, BenchmarkProviderManagedDispatchRuntimeContributor>();
        services.AddSingleton<IEventSubscriptionContributor, BenchmarkProviderManagedSubscriptionContributor>();
        services.AddSingleton<IEventSubscriptionExecutionBindingContributor, BenchmarkProviderManagedSubscriptionBindingContributor>();

        var builder = new EngineBuilder(services);
        builder.UseSettings(new EngineSettings(
            blueprint: "modular-vertical-slice",
            patterns: ["cqrs", "outbox"],
            transports: ["rest-api"],
            technologies: ["event-driven-integration"]));
        builder.AddModule(new BenchmarkProviderManagedEventingModule());
        builder.AddEventing(options =>
        {
            options.Channels.Add(new EventChannelDescriptor(
                id: ChannelId,
                displayName: "Benchmark Provider-managed Events",
                description: "Provider-managed benchmark channel for Eventing proof reports."));
        });

        using var runtime = builder.Build();
        provider = services.BuildServiceProvider();
        await runtime.InitializeAsync(provider).ConfigureAwait(false);

        dispatchCatalog = provider.GetRequiredService<IEventDispatchRuntimeCatalog>();
        dispatchReporter = provider.GetRequiredService<IEventDispatchRuntimeReporter>();
        subscriptionCatalog = provider.GetRequiredService<IEventSubscriptionRuntimeCatalog>();
        subscriptionReporter = provider.GetRequiredService<IEventSubscriptionRuntimeReporter>();

        _ = await ReportProviderManagedEventingProofs().ConfigureAwait(false);
    }

    /// <summary>
    /// Releases all DI resources.
    /// </summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        provider.Dispose();
    }

    /// <summary>
    /// Records provider-managed dispatch and subscription proofs, then reads runtime evidence back.
    /// </summary>
    [Benchmark(OperationsPerInvoke = ProviderManagedOperationsPerIteration)]
    public async Task<long> ReportProviderManagedEventingProofs()
    {
        long total = 0;
        for (var index = 0; index < ProviderManagedOperationsPerIteration; index++)
        {
            var suffix = (index % 128).ToString("D4", CultureInfo.InvariantCulture);
            var dispatchReport = CreateProviderManagedDispatchReport(index, suffix);
            await dispatchReporter.ReportAsync(dispatchReport).ConfigureAwait(false);

            var subscriptionReport = CreateProviderManagedSubscriptionReport(index, suffix);
            await subscriptionReporter.ReportAsync(subscriptionReport).ConfigureAwait(false);

            var dispatchState = dispatchCatalog.GetByOutboxId(OutboxId);
            var subscriptionState = subscriptionCatalog.GetById(SubscriptionId);

            total += dispatchState?.Metadata.Count ?? 0;
            total += dispatchState?.LastAttempt ?? 0;
            total += subscriptionState?.Metadata.Count ?? 0;
            total += subscriptionState?.LastAttempt ?? 0;
        }

        return total;
    }

    private static EventDispatchExecutionReport CreateProviderManagedDispatchReport(int index, string suffix)
    {
        var baseReport = new EventDispatchExecutionReport(
            outboxId: OutboxId,
            channelId: ChannelId,
            outcome: EventDispatchExecutionOutcomes.Succeeded,
            observedAtUtc: ObservedAtUtc.AddTicks(index),
            messageId: $"provider-managed-message-{suffix}",
            attempt: (index % 3) + 1,
            metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["dispatchRuntimeId"] = DispatchRuntimeId,
                ["dispatchOwnership"] = "provider-managed",
                ["providerManagedEventing"] = "true",
                ["wolverineRequired"] = "false"
            });

        var topologyReport = EventDispatchBrokerTopologyMetadata.CreateReport(
            baseReport,
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

        return EventDispatchExactlyOnceDeliveryProofMetadata.CreateReport(
            partitionReport,
            ProviderSource,
            providerReceiptId: $"provider-receipt-{suffix}",
            subscriberAcknowledgementId: $"subscriber-ack-{suffix}",
            destinationCommitId: $"destination-commit-{suffix}",
            exactlyOnceProofId: $"exactly-once-{suffix}",
            strategy: "provider-transactional-ack");
    }

    private static EventSubscriptionExecutionReport CreateProviderManagedSubscriptionReport(int index, string suffix)
    {
        var baseReport = new EventSubscriptionExecutionReport(
            subscriptionId: SubscriptionId,
            outcome: EventSubscriptionExecutionOutcomes.Succeeded,
            observedAtUtc: ObservedAtUtc.AddTicks(index),
            messageId: $"provider-managed-message-{suffix}",
            attempt: (index % 4) + 1,
            metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["subscriptionRuntimeId"] = SubscriptionRuntimeId,
                ["subscriptionOwnership"] = "provider-managed",
                ["providerManagedEventing"] = "true",
                ["wolverineRequired"] = "false"
            });

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

    private sealed class BenchmarkProviderManagedEventingModule : ModuleBase, IOutboxContributor
    {
        private static readonly ModuleDescriptor DescriptorInstance = new(
            id: SourceModuleId,
            displayName: "Benchmark Provider-managed Eventing Module",
            description: "Benchmark module that owns provider-managed Eventing proof descriptors.",
            tags: ["benchmark", "eventing", "provider-managed"],
            version: "1.0.0");

        public override ModuleDescriptor Descriptor => DescriptorInstance;

        public void RegisterOutboxes(IOutboxRegistry outboxes)
        {
            outboxes.Add(new OutboxDescriptor(
                id: OutboxId,
                displayName: "Benchmark Provider-managed Outbox",
                description: "In-memory benchmark outbox boundary used by provider-managed Eventing guardrails.",
                sourceModuleId: SourceModuleId,
                provider: "benchmark-provider",
                mode: "in-memory",
                channelIds: [ChannelId],
                tags: ["benchmark", "provider-managed"],
                dispatchPolicy: new OutboxDispatchPolicyDescriptor(
                    outboxId: OutboxId,
                    policyId: "benchmark-provider-managed-dispatch",
                    displayName: "Benchmark Provider-managed Dispatch",
                    description: "Provider-managed benchmark dispatch policy.",
                    executionMode: "runtime-managed",
                    runtimeId: DispatchRuntimeId,
                    metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["dispatchRuntime"] = "configured",
                        ["dispatchRuntimeId"] = DispatchRuntimeId,
                        ["dispatchOwnership"] = "provider-managed",
                        ["wolverineRequired"] = "false"
                    })));
        }
    }

    private sealed class BenchmarkProviderManagedDispatchRuntimeContributor : IEventDispatchRuntimeContributor
    {
        public void RegisterDispatchRuntimes(IEventDispatchRuntimeRegistry dispatchRuntimes)
        {
            dispatchRuntimes.Add(new EventDispatchRuntimeDescriptor(
                id: DispatchRuntimeId,
                displayName: "Benchmark Provider-managed Dispatch Runtime",
                description: "Provider-managed benchmark runtime that reports broker and delivery proofs through Cephalon.",
                metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["runtimeKind"] = "provider-managed-eventing",
                    ["dispatchOwnership"] = "provider-managed",
                    ["wolverineRequired"] = "false"
                },
                outboxIds: [OutboxId]));
        }
    }

    private sealed class BenchmarkProviderManagedSubscriptionContributor : IEventSubscriptionContributor
    {
        public void RegisterSubscriptions(IEventSubscriptionRegistry subscriptions)
        {
            subscriptions.Add(new EventSubscriptionDescriptor(
                id: SubscriptionId,
                displayName: "Benchmark Provider-managed Consumer",
                description: "Provider-managed benchmark subscription for runtime proof reports.",
                channelId: ChannelId,
                handlerId: "benchmark-provider-managed-handler",
                deliveryMode: "broker-consumer",
                tags: ["benchmark", "provider-managed"],
                metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["subscriptionOwnership"] = "provider-managed",
                    ["wolverineRequired"] = "false"
                }));
        }
    }

    private sealed class BenchmarkProviderManagedSubscriptionBindingContributor : IEventSubscriptionExecutionBindingContributor
    {
        public IReadOnlyList<EventSubscriptionExecutionBindingDescriptor> GetExecutionBindings()
        {
            return
            [
                new EventSubscriptionExecutionBindingDescriptor(
                    subscriptionId: SubscriptionId,
                    executionRuntimeId: SubscriptionRuntimeId,
                    executionOwnership: "provider-managed",
                    executionMode: "broker-consumer-loop",
                    metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["runtimeKind"] = "provider-managed-eventing",
                        ["brokerConsumerLoop"] = "provider-managed",
                        ["wolverineRequired"] = "false"
                    })
            ];
        }
    }
}
