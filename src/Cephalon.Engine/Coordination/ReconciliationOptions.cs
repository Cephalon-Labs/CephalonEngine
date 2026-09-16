namespace Cephalon.Engine.Coordination;

/// <summary>Bounds process-local execution and retention. Capacity never evicts an idempotency reservation.</summary>
public sealed class ReconciliationOptions
{
    /// <summary>Creates immutable local execution limits.</summary>
    /// <param name="maxAttempts">The total attempt limit, from one through 32.</param>
    /// <param name="capacity">The maximum retained tenant/operation reservations, from one through 100,000.</param>
    /// <param name="retryDelay">The initial retry delay; default 100 milliseconds.</param>
    /// <param name="maxRetryDelay">The retry delay ceiling; default five seconds.</param>
    public ReconciliationOptions(int maxAttempts = 3, int capacity = 1024,
        TimeSpan? retryDelay = null, TimeSpan? maxRetryDelay = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxAttempts, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maxAttempts, 32);
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(capacity, 100_000);
        MaxAttempts = maxAttempts;
        Capacity = capacity;
        RetryDelay = retryDelay ?? TimeSpan.FromMilliseconds(100);
        MaxRetryDelay = maxRetryDelay ?? TimeSpan.FromSeconds(5);
        if (RetryDelay <= TimeSpan.Zero || MaxRetryDelay < RetryDelay || MaxRetryDelay > TimeSpan.FromHours(1))
        {
            throw new ArgumentOutOfRangeException(nameof(retryDelay), "Retry delays must be positive, ordered, and at most one hour.");
        }
    }

    /// <summary>Gets the total attempt limit.</summary>
    public int MaxAttempts { get; }
    /// <summary>Gets the retained reservation limit.</summary>
    public int Capacity { get; }
    /// <summary>Gets the initial retry delay.</summary>
    public TimeSpan RetryDelay { get; }
    /// <summary>Gets the retry delay ceiling.</summary>
    public TimeSpan MaxRetryDelay { get; }
}
