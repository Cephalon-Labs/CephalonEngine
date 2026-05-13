using BenchmarkDotNet.Attributes;
using Cephalon.Abstractions.Data;
using Cephalon.Benchmarks.Support;
using Cephalon.Data.EntityFramework.Modeling;
using Cephalon.Data.EntityFramework.Registration;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Eventing.Registration;
using Cephalon.Eventing.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Benchmarks.HotPath;

/// <summary>
/// Measures the provider-backed durable remediation command journal and replay-cursor read path without Wolverine.
/// </summary>
[MemoryDiagnoser]
[Config(typeof(BenchmarkInProcessShortRunConfig))]
public class EventDispatchDurableJournalBenchmarks
{
    private const int SeededCommandCount = 128;
    private const int JournalOperationsPerIteration = 256;
    private const string OutboxId = "entity-framework-outbox";
    private const string ChannelId = "benchmark-durable-journal-events";

    private static readonly DateTimeOffset ObservedAtUtc = new(2026, 5, 13, 7, 0, 0, TimeSpan.Zero);

    private ServiceProvider provider = null!;
    private IServiceScope scope = null!;
    private IEventDispatchRemediationCommandJournal journal = null!;
    private IEventDispatchRemediationCommandReplayCursorCatalog replayCursorCatalog = null!;
    private EventDispatchRemediationCommandReplayCursor[] replayCursors = null!;

    /// <summary>
    /// Builds Eventing with the Entity Framework-backed outbox and durable remediation command journal.
    /// </summary>
    [GlobalSetup]
    public async Task Setup()
    {
        var databaseName = $"cephalon-benchmark-durable-journal-{Guid.NewGuid():N}";
        var databaseRoot = new InMemoryDatabaseRoot();
        var services = new ServiceCollection();

        var builder = new EngineBuilder(services);
        builder.UseSettings(new EngineSettings(
            blueprint: "modular-vertical-slice",
            patterns: ["cqrs", "outbox"],
            transports: ["rest-api"],
            technologies: ["event-driven-integration"]));
        builder.AddEventing(options =>
        {
            options.Channels.Add(new EventChannelDescriptor(
                id: ChannelId,
                displayName: "Benchmark Durable Journal Events",
                description: "Provider-backed benchmark channel for durable command-journal proof."));
        });
        builder.AddEntityFrameworkData<DurableJournalBenchmarkDbContext>(
            options => options.UseInMemoryDatabase(databaseName, databaseRoot),
            options => options.RegisterOutbox = true);

        using var runtime = builder.Build();
        provider = services.BuildServiceProvider();
        await runtime.InitializeAsync(provider).ConfigureAwait(false);

        scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DurableJournalBenchmarkDbContext>();
        await dbContext.Database.EnsureCreatedAsync().ConfigureAwait(false);

        journal = scope.ServiceProvider.GetRequiredService<IEventDispatchRemediationCommandJournal>();
        replayCursorCatalog = (IEventDispatchRemediationCommandReplayCursorCatalog)journal;

        for (var index = 0; index < SeededCommandCount; index++)
        {
            await journal.RecordAsync(CreateResult(index, ObservedAtUtc.AddMilliseconds(index))).ConfigureAwait(false);
        }

        replayCursors = Enumerable
            .Range(0, SeededCommandCount)
            .Select(index => new EventDispatchRemediationCommandReplayCursor(
                ObservedAtUtc.AddMilliseconds(index),
                CreateCommandId(index)))
            .ToArray();

        _ = await RecordAndReadDurableJournal().ConfigureAwait(false);
    }

    /// <summary>
    /// Releases all DI and DbContext resources.
    /// </summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        scope.Dispose();
        provider.Dispose();
    }

    /// <summary>
    /// Records durable command-journal results and reads replay-cursor windows from the provider-backed journal.
    /// </summary>
    [Benchmark(OperationsPerInvoke = JournalOperationsPerIteration)]
    public async Task<long> RecordAndReadDurableJournal()
    {
        long total = 0;
        for (var index = 0; index < JournalOperationsPerIteration; index++)
        {
            var commandIndex = index % SeededCommandCount;
            var observedAtUtc = ObservedAtUtc.AddMinutes(1).AddTicks(index);
            await journal.RecordAsync(CreateResult(commandIndex, observedAtUtc)).ConfigureAwait(false);

            var cursor = replayCursors[commandIndex];
            var replayPage = replayCursorCatalog.GetAfterReplayCursor(cursor, maxCount: 4);
            var latestReplayCursor = replayCursorCatalog.LatestReplayCursor;

            total += replayPage.Count;
            total += latestReplayCursor is null ? 0 : latestReplayCursor.CommandId.Length;
        }

        return total;
    }

    private static EventDispatchRemediationResult CreateResult(
        int index,
        DateTimeOffset observedAtUtc)
    {
        var normalizedIndex = index % SeededCommandCount;
        return new EventDispatchRemediationResult(
            CommandId: CreateCommandId(normalizedIndex),
            OutboxId: OutboxId,
            MessageId: $"journal-message-{normalizedIndex:D4}",
            ChannelId: ChannelId,
            OperationId: EventDispatchRemediationOperationIds.RetryNow,
            Outcome: normalizedIndex % 7 == 0
                ? EventDispatchRemediationOutcomes.Reserved
                : EventDispatchRemediationOutcomes.Accepted,
            DispatchOutcome: normalizedIndex % 5 == 0
                ? EventDispatchExecutionOutcomes.RetryScheduled
                : EventDispatchExecutionOutcomes.Succeeded,
            ObservedAtUtc: observedAtUtc,
            Error: null,
            Metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [EventDispatchRemediationMetadataKeys.OperatorActorId] = $"operator-{normalizedIndex % 8:D2}",
                [EventDispatchRemediationMetadataKeys.OperatorCorrelationId] = $"journal-correlation-{normalizedIndex % 16:D2}",
                [EventDispatchRemediationMetadataKeys.OperatorCommandReason] = "benchmark-durable-journal",
                ["journalDurability"] = "durable",
                ["journalScope"] = "cross-node",
                ["journalReplayCursor"] = "durable",
                ["wolverineRequired"] = "false"
            });
    }

    private static string CreateCommandId(int index)
    {
        return $"journal-command-{index:D4}";
    }

    private sealed class DurableJournalBenchmarkDbContext(DbContextOptions<DurableJournalBenchmarkDbContext> options) :
        DbContext(options),
        IEntityFrameworkOutboxContext,
        IEntityFrameworkEventDispatchRemediationCommandJournalContext
    {
        public DbSet<EntityFrameworkOutboxEntry> OutboxMessages => Set<EntityFrameworkOutboxEntry>();

        public DbSet<EntityFrameworkEventDispatchRemediationCommandEntry> EventDispatchRemediationCommandJournalEntries =>
            Set<EntityFrameworkEventDispatchRemediationCommandEntry>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ArgumentNullException.ThrowIfNull(modelBuilder);

            modelBuilder.ConfigureCephalonOutbox();
            modelBuilder.ConfigureCephalonEventDispatchRemediationCommandJournal();
        }
    }
}
