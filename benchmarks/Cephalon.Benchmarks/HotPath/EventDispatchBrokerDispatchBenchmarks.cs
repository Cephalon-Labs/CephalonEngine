using BenchmarkDotNet.Attributes;
using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Modules;
using Cephalon.Benchmarks.Support;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Eventing.Registration;
using Cephalon.Eventing.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Benchmarks.HotPath;

/// <summary>
/// Measures native provider-neutral broker-dispatch report projection and runtime-catalog readback without Wolverine.
/// </summary>
[MemoryDiagnoser]
[Config(typeof(BenchmarkInProcessShortRunConfig))]
public class EventDispatchBrokerDispatchBenchmarks
{
    private const int DispatchItemCount = 128;
    private const int DispatchOperationsPerIteration = 2048;
    private const string OutboxId = "benchmark-outbox";
    private const string ChannelId = "benchmark-broker-events";
    private const string RuntimeId = "benchmark-broker-dispatch-runtime";
    private const string SourceModuleId = "benchmark-eventing-module";

    private static readonly DateTimeOffset ObservedAtUtc = new(2026, 5, 13, 6, 0, 0, TimeSpan.Zero);

    private ServiceProvider provider = null!;
    private IEventDispatchRuntimeCatalog catalog = null!;
    private IEventDispatchRuntimeReporter reporter = null!;
    private EventDispatchItem[] dispatchItems = null!;

    /// <summary>
    /// Builds a native Eventing runtime with one provider-neutral dispatch runtime and warms the report path.
    /// </summary>
    [GlobalSetup]
    public async Task Setup()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IOutbox, InMemoryBenchmarkOutbox>();
        services.AddSingleton<IEventDispatchRuntimeContributor, BenchmarkBrokerDispatchRuntimeContributor>();

        var builder = new EngineBuilder(services);
        builder.UseSettings(new EngineSettings(
            blueprint: "modular-vertical-slice",
            patterns: ["cqrs", "outbox"],
            transports: ["rest-api"],
            technologies: ["event-driven-integration"]));
        builder.AddModule(new BenchmarkBrokerDispatchOutboxModule());
        builder.AddEventing(options =>
        {
            options.Channels.Add(new EventChannelDescriptor(
                id: ChannelId,
                displayName: "Benchmark Broker Events",
                description: "Provider-neutral benchmark channel for broker-dispatch report projection."));
        });

        using var runtime = builder.Build();
        provider = services.BuildServiceProvider();
        await runtime.InitializeAsync(provider);

        catalog = provider.GetRequiredService<IEventDispatchRuntimeCatalog>();
        reporter = provider.GetRequiredService<IEventDispatchRuntimeReporter>();
        dispatchItems = Enumerable
            .Range(0, DispatchItemCount)
            .Select(CreateDispatchItem)
            .ToArray();

        _ = await ReportProjectedBrokerDispatches();
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
    /// Projects broker handoff headers, records successful dispatch reports, and reads the latest runtime state.
    /// </summary>
    [Benchmark(OperationsPerInvoke = DispatchOperationsPerIteration)]
    public async Task<long> ReportProjectedBrokerDispatches()
    {
        long total = 0;
        for (var index = 0; index < DispatchOperationsPerIteration; index++)
        {
            var item = dispatchItems[index % dispatchItems.Length];
            var providerBrokerHeaders = EventDispatchProviderBrokerContextHeaders.Create(item);
            var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["dispatchRuntimeId"] = RuntimeId,
                ["brokerDispatchReport"] = "provider-neutral-success",
                ["brokerDispatchTransport"] = "provider-neutral-broker"
            };
            EventDispatchProviderBrokerContextHeaders.ApplyReportMetadata(metadata, providerBrokerHeaders);

            var report = new EventDispatchExecutionReport(
                outboxId: item.OutboxId,
                channelId: item.ChannelId,
                outcome: EventDispatchExecutionOutcomes.Succeeded,
                observedAtUtc: ObservedAtUtc.AddTicks(index),
                messageId: item.MessageId,
                attempt: item.DispatchAttemptCount + 1,
                metadata: metadata);

            await reporter.ReportAsync(report).ConfigureAwait(false);
            var state = catalog.GetByOutboxId(item.OutboxId);
            total += state?.LastAttempt ?? 0;
            total += state?.Metadata.Count ?? 0;
        }

        return total;
    }

    private static EventDispatchItem CreateDispatchItem(int index)
    {
        var messageId = $"benchmark-message-{index:D4}";
        return new EventDispatchItem(
            outboxId: OutboxId,
            messageId: messageId,
            channelId: ChannelId,
            eventType: "Benchmark.EventDispatched",
            payload: """{"event":"benchmark"}""",
            occurredAtUtc: ObservedAtUtc.AddMilliseconds(index),
            createdAtUtc: ObservedAtUtc.AddMilliseconds(index).AddTicks(1),
            dispatchAttemptCount: index % 3,
            contentType: "application/json",
            correlationId: $"benchmark-correlation-{index % 16:D2}",
            tenantId: $"benchmark-tenant-{index % 8:D2}",
            headers: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [EventContextHeaderNames.TenantId] = $"benchmark-tenant-{index % 8:D2}",
                [EventContextHeaderNames.CorrelationId] = $"benchmark-correlation-{index % 16:D2}",
                [EventContextHeaderNames.CausationId] = $"benchmark-causation-{index:D4}",
                [EventContextHeaderNames.Baggage] = $"segment={index % 4};priority=normal",
                [EventContextHeaderNames.MessageId] = messageId
            });
    }

    private sealed class BenchmarkBrokerDispatchOutboxModule : ModuleBase, IOutboxContributor
    {
        private static readonly ModuleDescriptor DescriptorInstance = new(
            id: SourceModuleId,
            displayName: "Benchmark Eventing Module",
            description: "Benchmark module that owns the provider-neutral broker-dispatch outbox descriptor.",
            tags: ["benchmark", "eventing", "broker-dispatch"],
            version: "1.0.0");

        public override ModuleDescriptor Descriptor => DescriptorInstance;

        public void RegisterOutboxes(IOutboxRegistry outboxes)
        {
            outboxes.Add(new OutboxDescriptor(
                id: OutboxId,
                displayName: "Benchmark Broker Outbox",
                description: "In-memory benchmark outbox boundary used by broker-dispatch guardrails.",
                sourceModuleId: SourceModuleId,
                provider: "benchmark",
                mode: "in-memory",
                channelIds: [ChannelId],
                tags: ["benchmark", "broker-dispatch"],
                dispatchPolicy: new OutboxDispatchPolicyDescriptor(
                    outboxId: OutboxId,
                    policyId: "benchmark-broker-dispatch",
                    displayName: "Benchmark Broker Dispatch",
                    description: "Provider-neutral broker-dispatch benchmark policy.",
                    executionMode: "runtime-managed",
                    runtimeId: RuntimeId,
                    metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["dispatchStore"] = "available",
                        ["dispatchRuntime"] = "configured",
                        ["dispatchRuntimeId"] = RuntimeId,
                        ["dispatchOwnership"] = "provider-neutral-benchmark"
                    })));
        }
    }

    private sealed class BenchmarkBrokerDispatchRuntimeContributor : IEventDispatchRuntimeContributor
    {
        public void RegisterDispatchRuntimes(IEventDispatchRuntimeRegistry dispatchRuntimes)
        {
            dispatchRuntimes.Add(new EventDispatchRuntimeDescriptor(
                id: RuntimeId,
                displayName: "Benchmark Broker Dispatch Runtime",
                description: "Provider-neutral benchmark runtime that reports broker handoff outcomes through Cephalon.",
                metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["runtimeKind"] = "provider-neutral-broker-dispatch",
                    ["wolverineRequired"] = "false"
                },
                outboxIds: [OutboxId]));
        }
    }
}
