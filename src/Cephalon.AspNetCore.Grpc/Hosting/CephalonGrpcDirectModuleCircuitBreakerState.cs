using System.Globalization;

namespace Cephalon.AspNetCore.Grpc.Hosting;

internal sealed class CephalonGrpcDirectModuleCircuitBreakerState
{
    private readonly CephalonGrpcDirectModuleResilienceOptions options;
    private readonly Queue<CircuitSample> samples = new();
    private readonly object gate = new();
    private DateTimeOffset? openedUntilUtc;
    private bool halfOpenProbeInProgress;
    private long openedCount;
    private long rejectedWhileOpenCount;
    private DateTimeOffset? lastRejectedWhileOpenAtUtc;

    public CephalonGrpcDirectModuleCircuitBreakerState(CephalonGrpcDirectModuleResilienceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        this.options = options;
    }

    public string StateKey
    {
        get
        {
            lock (gate)
            {
                return ResolveStateKey(DateTimeOffset.UtcNow);
            }
        }
    }

    public DateTimeOffset? LastOpenedAtUtc { get; private set; }

    public TimeSpan? LastBreakDuration { get; private set; }

    public string? LastFailureExceptionType { get; private set; }

    public int SampleCount
    {
        get
        {
            lock (gate)
            {
                Prune(DateTimeOffset.UtcNow);
                return samples.Count;
            }
        }
    }

    public int FailedSampleCount
    {
        get
        {
            lock (gate)
            {
                Prune(DateTimeOffset.UtcNow);
                return samples.Count(static sample => !sample.Succeeded);
            }
        }
    }

    public int RetryAfterSeconds
    {
        get
        {
            lock (gate)
            {
                var now = DateTimeOffset.UtcNow;
                return ResolveRetryAfterSeconds(now);
            }
        }
    }

    public bool TryEnter(out int retryAfterSeconds)
    {
        retryAfterSeconds = 0;
        if (!options.CircuitBreakerEnabled)
        {
            return true;
        }

        lock (gate)
        {
            var now = DateTimeOffset.UtcNow;

            if (openedUntilUtc is not null && openedUntilUtc > now)
            {
                retryAfterSeconds = ResolveRetryAfterSeconds(now);
                RecordRejectedWhileOpen(now);
                return false;
            }

            if (openedUntilUtc is not null)
            {
                if (halfOpenProbeInProgress)
                {
                    retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(options.CircuitBreakerBreakDuration.TotalSeconds));
                    RecordRejectedWhileOpen(now);
                    return false;
                }

                halfOpenProbeInProgress = true;
            }

            return true;
        }
    }

    public void RecordSuccess()
    {
        if (!options.CircuitBreakerEnabled)
        {
            return;
        }

        lock (gate)
        {
            if (openedUntilUtc is not null)
            {
                Close();
                return;
            }

            RecordSample(DateTimeOffset.UtcNow, succeeded: true, exception: null);
        }
    }

    public void RecordFailure(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (!options.CircuitBreakerEnabled)
        {
            return;
        }

        lock (gate)
        {
            var now = DateTimeOffset.UtcNow;
            if (openedUntilUtc is not null)
            {
                Open(now, exception);
                return;
            }

            RecordSample(now, succeeded: false, exception);
            var sampleCount = samples.Count;
            if (sampleCount < options.CircuitBreakerMinimumThroughput)
            {
                return;
            }

            var failedCount = samples.Count(static sample => !sample.Succeeded);
            var failureRatio = sampleCount == 0
                ? 0m
                : failedCount / (decimal)sampleCount;
            if (failureRatio >= options.CircuitBreakerFailureRatio)
            {
                Open(now, exception);
            }
        }
    }

    public IReadOnlyDictionary<string, string> CreateMetadata()
    {
        lock (gate)
        {
            var now = DateTimeOffset.UtcNow;
            Prune(now);

            var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["circuitState"] = ResolveStateKey(now),
                ["circuitSampleCount"] = samples.Count.ToString(CultureInfo.InvariantCulture),
                ["circuitFailedSampleCount"] = samples.Count(static sample => !sample.Succeeded).ToString(CultureInfo.InvariantCulture),
                ["circuitRetryAfterSeconds"] = ResolveRetryAfterSeconds(now).ToString(CultureInfo.InvariantCulture),
                ["circuitOpenedCount"] = openedCount.ToString(CultureInfo.InvariantCulture),
                ["circuitRejectedWhileOpenCount"] = rejectedWhileOpenCount.ToString(CultureInfo.InvariantCulture)
            };

            if (LastOpenedAtUtc is not null)
            {
                metadata["circuitLastOpenedAtUtc"] = LastOpenedAtUtc.Value.ToString("O", CultureInfo.InvariantCulture);
            }

            if (LastBreakDuration is not null)
            {
                metadata["circuitLastBreakDurationSeconds"] = ((int)Math.Ceiling(LastBreakDuration.Value.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
            }

            if (!string.IsNullOrWhiteSpace(LastFailureExceptionType))
            {
                metadata["circuitLastFailureExceptionType"] = LastFailureExceptionType;
            }

            if (lastRejectedWhileOpenAtUtc is not null)
            {
                metadata["circuitLastRejectedWhileOpenAtUtc"] = lastRejectedWhileOpenAtUtc.Value.ToString("O", CultureInfo.InvariantCulture);
            }

            return metadata;
        }
    }

    private void RecordSample(DateTimeOffset observedAtUtc, bool succeeded, Exception? exception)
    {
        Prune(observedAtUtc);
        samples.Enqueue(new CircuitSample(observedAtUtc, succeeded));
        LastFailureExceptionType = succeeded ? LastFailureExceptionType : exception?.GetType().FullName;
    }

    private void Open(DateTimeOffset observedAtUtc, Exception exception)
    {
        LastOpenedAtUtc = observedAtUtc;
        LastBreakDuration = options.CircuitBreakerBreakDuration;
        LastFailureExceptionType = exception.GetType().FullName;
        openedUntilUtc = observedAtUtc + options.CircuitBreakerBreakDuration;
        halfOpenProbeInProgress = false;
        openedCount++;
    }

    private void RecordRejectedWhileOpen(DateTimeOffset observedAtUtc)
    {
        rejectedWhileOpenCount++;
        lastRejectedWhileOpenAtUtc = observedAtUtc;
    }

    private void Close()
    {
        openedUntilUtc = null;
        halfOpenProbeInProgress = false;
        samples.Clear();
    }

    private string ResolveStateKey(DateTimeOffset observedAtUtc)
    {
        if (openedUntilUtc is null)
        {
            return "closed";
        }

        return halfOpenProbeInProgress || openedUntilUtc <= observedAtUtc ? "half-open" : "open";
    }

    private int ResolveRetryAfterSeconds(DateTimeOffset observedAtUtc)
    {
        if (openedUntilUtc is null || openedUntilUtc <= observedAtUtc)
        {
            return 0;
        }

        return (int)Math.Ceiling((openedUntilUtc.Value - observedAtUtc).TotalSeconds);
    }

    private void Prune(DateTimeOffset observedAtUtc)
    {
        var cutoff = observedAtUtc - options.CircuitBreakerSamplingDuration;
        while (samples.TryPeek(out var sample) && sample.ObservedAtUtc < cutoff)
        {
            samples.Dequeue();
        }
    }

    private readonly record struct CircuitSample(DateTimeOffset ObservedAtUtc, bool Succeeded);
}
