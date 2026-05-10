using BenchmarkDotNet.Attributes;
using Cephalon.Abstractions.Data;
using Cephalon.Benchmarks.Support;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Eventing.Registration;
using Cephalon.Eventing.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Benchmarks.HotPath;

/// <summary>
/// Measures Wolverine-free event-dispatch remediation filtered read paths over the public eventing composition surface.
/// </summary>
[MemoryDiagnoser]
[Config(typeof(BenchmarkInProcessShortRunConfig))]
public class EventDispatchRemediationCatalogBenchmarks
{
    private const int CommandCount = 512;
    private const int CatalogOperationsPerIteration = 2048;
    private const string TargetMessageId = "benchmark-message-07";
    private const string TargetCorrelationId = "benchmark-correlation-03";
    private const string TargetDispatchOutcome = EventDispatchExecutionOutcomes.RetryScheduled;

    private static readonly DateTimeOffset ObservedAtUtc = new(2026, 5, 11, 4, 0, 0, TimeSpan.Zero);

    private ServiceProvider provider = null!;
    private IEventDispatchRemediationRuntimeCatalog catalog = null!;

    /// <summary>
    /// Builds the native eventing runtime with a process-local command journal and warms filtered operator reads.
    /// </summary>
    [GlobalSetup]
    public async Task Setup()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IOutbox, InMemoryBenchmarkOutbox>();
        services.AddSingleton<IEventDispatchStore, BenchmarkEventDispatchStore>();

        var builder = new EngineBuilder(services);
        builder.UseSettings(new EngineSettings(
            blueprint: "modular-vertical-slice",
            patterns: ["cqrs"],
            transports: ["rest-api"],
            technologies: ["event-driven-integration"]));
        builder.AddEventing(options => options.RemediationCommandHistoryLimit = CommandCount);

        using var runtime = builder.Build();
        provider = services.BuildServiceProvider();
        await runtime.InitializeAsync(provider);

        await using var scope = provider.CreateAsyncScope();
        var journal = scope.ServiceProvider.GetRequiredService<IEventDispatchRemediationCommandJournal>();
        for (var index = 0; index < CommandCount; index++)
        {
            await journal.RecordAsync(CreateResult(index));
        }

        catalog = provider.GetRequiredService<IEventDispatchRemediationRuntimeCatalog>();
        _ = catalog.GetSummaryByMessageId(TargetMessageId);
        _ = catalog.GetRetentionByMessageId(TargetMessageId);
        _ = catalog.GetLatestByCorrelationId(TargetCorrelationId);
        _ = catalog.GetOldestByDispatchOutcome(TargetDispatchOutcome);
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
    /// Reads the retained command summary for a single message id without materializing the matching detail list.
    /// </summary>
    [Benchmark(OperationsPerInvoke = CatalogOperationsPerIteration)]
    public long FilterSummaryByMessageId()
    {
        long total = 0;
        for (var i = 0; i < CatalogOperationsPerIteration; i++)
        {
            var summary = catalog.GetSummaryByMessageId(TargetMessageId);
            total += summary.TotalCommandCount + summary.AcceptedCount + summary.ReservedCount;
        }

        return total;
    }

    /// <summary>
    /// Reads retained command-retention posture for a single message id without materializing the matching detail list.
    /// </summary>
    [Benchmark(OperationsPerInvoke = CatalogOperationsPerIteration)]
    public long FilterRetentionByMessageId()
    {
        long total = 0;
        for (var i = 0; i < CatalogOperationsPerIteration; i++)
        {
            var retention = catalog.GetRetentionByMessageId(TargetMessageId);
            total += retention.RetainedCommandCount + retention.TotalRecordedCommandCount + retention.DroppedCommandCount;
        }

        return total;
    }

    /// <summary>
    /// Reads the latest retained command for one correlation id without sorting the matching detail list.
    /// </summary>
    [Benchmark(OperationsPerInvoke = CatalogOperationsPerIteration)]
    public long FilterLatestByCorrelationId()
    {
        long total = 0;
        for (var i = 0; i < CatalogOperationsPerIteration; i++)
        {
            var state = catalog.GetLatestByCorrelationId(TargetCorrelationId);
            total += state?.ObservedAtUtc.UtcTicks ?? 0;
        }

        return total;
    }

    /// <summary>
    /// Reads the oldest retained command for one dispatch outcome without sorting the matching detail list.
    /// </summary>
    [Benchmark(OperationsPerInvoke = CatalogOperationsPerIteration)]
    public long FilterOldestByDispatchOutcome()
    {
        long total = 0;
        for (var i = 0; i < CatalogOperationsPerIteration; i++)
        {
            var state = catalog.GetOldestByDispatchOutcome(TargetDispatchOutcome);
            total += state?.CommandId.Length ?? 0;
        }

        return total;
    }

    /// <summary>
    /// Exercises the common operator dashboard selector set over summary, retention, latest, and oldest filtered reads.
    /// </summary>
    [Benchmark(OperationsPerInvoke = CatalogOperationsPerIteration)]
    public long FilterOperatorDashboardSelectors()
    {
        long total = 0;
        for (var i = 0; i < CatalogOperationsPerIteration; i++)
        {
            total += catalog.GetSummaryByMessageId(TargetMessageId).TotalCommandCount;
            total += catalog.GetRetentionByMessageId(TargetMessageId).RetainedCommandCount;
            total += catalog.GetLatestByCorrelationId(TargetCorrelationId)?.CommandId.Length ?? 0;
            total += catalog.GetOldestByDispatchOutcome(TargetDispatchOutcome)?.CommandId.Length ?? 0;
        }

        return total;
    }

    private static EventDispatchRemediationResult CreateResult(int index)
    {
        var outcome = index % 11 == 0
            ? EventDispatchRemediationOutcomes.Reserved
            : index % 5 == 0
                ? EventDispatchRemediationOutcomes.Rejected
                : EventDispatchRemediationOutcomes.Accepted;
        var dispatchOutcome = (index % 4) switch
        {
            0 => EventDispatchExecutionOutcomes.Succeeded,
            1 => EventDispatchExecutionOutcomes.RetryScheduled,
            2 => EventDispatchExecutionOutcomes.Skipped,
            _ => EventDispatchExecutionOutcomes.Failed
        };
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [EventDispatchRemediationMetadataKeys.OperatorActorId] = $"benchmark-actor-{index % 6:D2}",
            [EventDispatchRemediationMetadataKeys.OperatorCorrelationId] = $"benchmark-correlation-{index % 8:D2}",
            [EventDispatchRemediationMetadataKeys.OperatorCommandReason] = $"benchmark-reason-{index % 5:D2}"
        };

        if (index % 17 == 0)
        {
            metadata[EventDispatchRemediationMetadataKeys.DuplicateCommand] = "true";
        }

        return new EventDispatchRemediationResult(
            CommandId: $"benchmark-command-{index:D4}",
            OutboxId: "benchmark-outbox",
            MessageId: $"benchmark-message-{index % 16:D2}",
            ChannelId: $"benchmark-channel-{index % 4:D2}",
            OperationId: index % 3 == 0 ? EventDispatchRemediationOperationIds.RetryNow : EventDispatchRemediationOperationIds.Skip,
            Outcome: outcome,
            DispatchOutcome: dispatchOutcome,
            ObservedAtUtc: ObservedAtUtc.AddMilliseconds(index),
            Error: string.Equals(outcome, EventDispatchRemediationOutcomes.Rejected, StringComparison.OrdinalIgnoreCase)
                ? "benchmark rejected command"
                : null,
            Metadata: metadata);
    }

    private sealed class BenchmarkEventDispatchStore : IEventDispatchStore
    {
        public IReadOnlyList<string> OutboxIds { get; } = ["benchmark-outbox"];

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
}
