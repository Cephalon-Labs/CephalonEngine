using System.Globalization;

namespace Cephalon.AspNetCore.JsonRpc.Hosting;

internal sealed class JsonRpcDirectModuleBulkheadState : IDisposable
{
    private readonly JsonRpcDirectModuleResilienceOptions options;
    private readonly SemaphoreSlim semaphore;
    private int activeCount;
    private int queuedCount;
    private int maxObservedConcurrency;
    private int maxObservedQueueLength;
    private long acceptedCount;
    private long rejectedCount;
    private long lastRejectedAtUtcTicks;

    public JsonRpcDirectModuleBulkheadState(JsonRpcDirectModuleResilienceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        this.options = options;
        semaphore = new SemaphoreSlim(options.BulkheadMaxConcurrentExecutions, options.BulkheadMaxConcurrentExecutions);
    }

    public async ValueTask<Lease?> TryEnterAsync(CancellationToken cancellationToken)
    {
        if (!options.BulkheadEnabled)
        {
            return default(Lease);
        }

        if (semaphore.Wait(0, CancellationToken.None))
        {
            return Accept();
        }

        if (options.BulkheadMaxQueuedActions == 0)
        {
            Reject();
            return null;
        }

        var queued = Interlocked.Increment(ref queuedCount);
        if (queued > options.BulkheadMaxQueuedActions)
        {
            Interlocked.Decrement(ref queuedCount);
            Reject();
            return null;
        }

        UpdateMaxObservedQueueLength(queued);

        try
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            return Accept();
        }
        finally
        {
            Interlocked.Decrement(ref queuedCount);
        }
    }

    public IReadOnlyDictionary<string, string> CreateMetadata()
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["bulkheadActiveCount"] = Volatile.Read(ref activeCount).ToString(CultureInfo.InvariantCulture),
            ["bulkheadAvailableCount"] = semaphore.CurrentCount.ToString(CultureInfo.InvariantCulture),
            ["bulkheadQueuedCount"] = Volatile.Read(ref queuedCount).ToString(CultureInfo.InvariantCulture),
            ["bulkheadAcceptedCount"] = Interlocked.Read(ref acceptedCount).ToString(CultureInfo.InvariantCulture),
            ["bulkheadRejectedCount"] = Interlocked.Read(ref rejectedCount).ToString(CultureInfo.InvariantCulture),
            ["bulkheadMaxObservedConcurrency"] = Volatile.Read(ref maxObservedConcurrency).ToString(CultureInfo.InvariantCulture),
            ["bulkheadMaxObservedQueueLength"] = Volatile.Read(ref maxObservedQueueLength).ToString(CultureInfo.InvariantCulture)
        };

        var lastRejectedTicks = Interlocked.Read(ref lastRejectedAtUtcTicks);
        if (lastRejectedTicks > 0)
        {
            metadata["bulkheadLastRejectedAtUtc"] = new DateTimeOffset(lastRejectedTicks, TimeSpan.Zero)
                .ToString("O", CultureInfo.InvariantCulture);
        }

        return metadata;
    }

    public void Dispose()
    {
        semaphore.Dispose();
    }

    private Lease Accept()
    {
        var active = Interlocked.Increment(ref activeCount);
        Interlocked.Increment(ref acceptedCount);
        UpdateMaxObservedConcurrency(active);
        return new Lease(this);
    }

    private void Reject()
    {
        Interlocked.Increment(ref rejectedCount);
        Interlocked.Exchange(ref lastRejectedAtUtcTicks, DateTimeOffset.UtcNow.UtcTicks);
    }

    private void Release()
    {
        Interlocked.Decrement(ref activeCount);
        semaphore.Release();
    }

    private void UpdateMaxObservedConcurrency(int value)
    {
        UpdateMax(ref maxObservedConcurrency, value);
    }

    private void UpdateMaxObservedQueueLength(int value)
    {
        UpdateMax(ref maxObservedQueueLength, value);
    }

    private static void UpdateMax(ref int target, int value)
    {
        while (true)
        {
            var current = Volatile.Read(ref target);
            if (value <= current)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref target, value, current) == current)
            {
                return;
            }
        }
    }

    public readonly struct Lease : IDisposable
    {
        private readonly JsonRpcDirectModuleBulkheadState? owner;

        internal Lease(JsonRpcDirectModuleBulkheadState owner)
        {
            this.owner = owner;
        }

        public void Dispose()
        {
            owner?.Release();
        }
    }
}
