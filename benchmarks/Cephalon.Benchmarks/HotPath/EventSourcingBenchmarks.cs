using BenchmarkDotNet.Attributes;
using Cephalon.Abstractions.EventSourcing;
using Cephalon.Benchmarks.Support;

namespace Cephalon.Benchmarks.HotPath;

/// <summary>
/// Measures the per-call overhead of core event-sourcing operations against an in-memory
/// <see cref="IEventStore" /> implementation. The in-memory store isolates dispatch and
/// data-structure overhead from I/O latency, making it suitable for guardrail enforcement
/// on the framework's append, read, and version-check hot paths.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class EventSourcingBenchmarks
{
    private const int OperationsPerIteration = 4096;

    private InMemoryBenchmarkEventStore eventStore = null!;
    private IDomainEvent[] singleEvent = null!;

    /// <summary>
    /// Prepares the in-memory event store and pre-populates a stream for read/version benchmarks.
    /// </summary>
    [IterationSetup]
    public void PrepareIteration()
    {
        eventStore = new InMemoryBenchmarkEventStore();
        singleEvent = [new BenchmarkDomainEvent("setup-stream", 0)];

        // Pre-populate a stream with events so read and version benchmarks have data to work with.
        for (var i = 0; i < 100; i++)
        {
            var evt = new BenchmarkDomainEvent("read-stream", i);
            eventStore.AppendAsync("read-stream", [evt], i - 1).GetAwaiter().GetResult();
        }
    }

    /// <summary>
    /// Releases the event store for the current iteration.
    /// </summary>
    [IterationCleanup]
    public void CleanupIteration()
    {
        eventStore.Clear();
    }

    /// <summary>
    /// Appends a single event to a unique stream per operation, measuring append path overhead
    /// including optimistic concurrency check on a new stream (<c>expectedVersion = -1</c>).
    /// </summary>
    [Benchmark(OperationsPerInvoke = OperationsPerIteration)]
    public async Task AppendSingleEvent()
    {
        for (var i = 0; i < OperationsPerIteration; i++)
        {
            var streamId = $"bench-append-{i}";
            var evt = new BenchmarkDomainEvent(streamId, 0);
            await eventStore.AppendAsync(streamId, [evt], -1);
        }
    }

    /// <summary>
    /// Reads all events from a pre-populated 100-event stream, measuring async enumeration overhead.
    /// </summary>
    [Benchmark(OperationsPerInvoke = OperationsPerIteration)]
    public async Task<int> ReadStream()
    {
        var totalCount = 0;
        for (var i = 0; i < OperationsPerIteration; i++)
        {
            await foreach (var _ in eventStore.ReadStreamAsync("read-stream"))
            {
                totalCount++;
            }
        }

        return totalCount;
    }

    /// <summary>
    /// Retrieves the current version of a pre-populated stream, measuring version lookup overhead.
    /// </summary>
    [Benchmark(OperationsPerInvoke = OperationsPerIteration)]
    public async Task<long> GetStreamVersion()
    {
        var versionSum = 0L;
        for (var i = 0; i < OperationsPerIteration; i++)
        {
            versionSum += await eventStore.GetVersionAsync("read-stream");
        }

        return versionSum;
    }
}
