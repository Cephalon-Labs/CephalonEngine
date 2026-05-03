using Polly.CircuitBreaker;

namespace Cephalon.Resilience;

internal sealed class BehaviorCircuitBreakerStateRegistry
{
    private readonly Dictionary<string, BehaviorCircuitBreakerRuntimeState> states = new(StringComparer.OrdinalIgnoreCase);
    private readonly object gate = new();

    public BehaviorCircuitBreakerRuntimeState Ensure(string policyId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyId);

        lock (gate)
        {
            if (!states.TryGetValue(policyId.Trim(), out var state))
            {
                state = new BehaviorCircuitBreakerRuntimeState();
                states[policyId.Trim()] = state;
            }

            return state;
        }
    }

    public BehaviorCircuitBreakerRuntimeState? Get(string policyId)
    {
        if (string.IsNullOrWhiteSpace(policyId))
        {
            return null;
        }

        lock (gate)
        {
            return states.TryGetValue(policyId.Trim(), out var state)
                ? state
                : null;
        }
    }
}

internal sealed class BehaviorCircuitBreakerRuntimeState
{
    private readonly object gate = new();

    public CircuitBreakerStateProvider StateProvider { get; } = new();

    public DateTimeOffset? LastOpenedAtUtc { get; private set; }

    public DateTimeOffset? LastClosedAtUtc { get; private set; }

    public DateTimeOffset? LastHalfOpenedAtUtc { get; private set; }

    public TimeSpan? LastBreakDuration { get; private set; }

    public string? LastOpenedExceptionType { get; private set; }

    public string StateKey
    {
        get
        {
            return StateProvider.CircuitState switch
            {
                CircuitState.Open => "open",
                CircuitState.HalfOpen => "half-open",
                CircuitState.Isolated => "isolated",
                _ => "closed"
            };
        }
    }

    public void MarkOpened(TimeSpan breakDuration, Exception? exception)
    {
        lock (gate)
        {
            LastOpenedAtUtc = DateTimeOffset.UtcNow;
            LastBreakDuration = breakDuration;
            LastOpenedExceptionType = exception?.GetType().FullName;
        }
    }

    public void MarkClosed()
    {
        lock (gate)
        {
            LastClosedAtUtc = DateTimeOffset.UtcNow;
        }
    }

    public void MarkHalfOpened()
    {
        lock (gate)
        {
            LastHalfOpenedAtUtc = DateTimeOffset.UtcNow;
        }
    }
}
