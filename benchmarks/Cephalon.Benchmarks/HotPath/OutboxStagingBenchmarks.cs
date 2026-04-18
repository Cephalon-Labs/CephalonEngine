using BenchmarkDotNet.Attributes;
using Cephalon.Abstractions.Data;
using Cephalon.Benchmarks.Support;

namespace Cephalon.Benchmarks.HotPath;

/// <summary>
/// Measures the per-call overhead of outbox message staging against an in-memory
/// <see cref="IOutbox" /> implementation. The in-memory outbox isolates the message
/// construction and staging overhead from I/O latency, making it suitable for guardrail
/// enforcement on the framework's outbox hot path.
/// </summary>
[MemoryDiagnoser]
[Config(typeof(BenchmarkInProcessShortRunConfig))]
public class OutboxStagingBenchmarks
{
    private const int StagingsPerIteration = 4096;

    private InMemoryBenchmarkOutbox outbox = null!;

    /// <summary>
    /// Prepares a fresh in-memory outbox for the current iteration.
    /// </summary>
    [IterationSetup]
    public void PrepareIteration()
    {
        outbox = new InMemoryBenchmarkOutbox();
    }

    /// <summary>
    /// Releases the outbox for the current iteration.
    /// </summary>
    [IterationCleanup]
    public void CleanupIteration()
    {
        outbox.Clear();
    }

    /// <summary>
    /// Stages a single outbox message per operation, measuring message construction and enqueue overhead.
    /// </summary>
    [Benchmark(OperationsPerInvoke = StagingsPerIteration)]
    public async Task<int> StageOutboxMessage()
    {
        for (var i = 0; i < StagingsPerIteration; i++)
        {
            var message = new OutboxMessage(
                id: $"msg-{i}",
                channelId: "benchmark-channel",
                messageType: "BenchmarkEvent",
                payload: "{\"value\":42}",
                occurredAtUtc: new DateTimeOffset(2042, 4, 2, 10, 0, 0, TimeSpan.Zero),
                correlationId: "bench-correlation",
                tenantId: "tenant-alpha");

            await outbox.EnqueueAsync(message);
        }

        return outbox.Count;
    }
}
