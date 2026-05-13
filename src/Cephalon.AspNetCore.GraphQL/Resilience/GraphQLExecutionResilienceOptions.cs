using Cephalon.Abstractions.AppModel;
using Cephalon.Engine.Manifest;

namespace Cephalon.AspNetCore.GraphQL.Resilience;

internal sealed class GraphQLExecutionResilienceOptions
{
    private const int DefaultTotalTimeoutSeconds = 30;
    private const decimal DefaultCircuitBreakerFailureRatio = 0.1m;
    private const int DefaultCircuitBreakerMinimumThroughput = 100;
    private const int DefaultCircuitBreakerSamplingDurationSeconds = 30;
    private const int DefaultCircuitBreakerBreakDurationSeconds = 5;
    private const int DefaultBulkheadMaxConcurrentExecutions = 64;
    private const int DefaultBulkheadMaxQueuedActions = 0;

    private GraphQLExecutionResilienceOptions(
        bool timeoutEnabled,
        TimeSpan? timeout,
        bool circuitBreakerEnabled,
        decimal circuitBreakerFailureRatio,
        int circuitBreakerMinimumThroughput,
        TimeSpan circuitBreakerSamplingDuration,
        TimeSpan circuitBreakerBreakDuration,
        bool bulkheadEnabled,
        int bulkheadMaxConcurrentExecutions,
        int bulkheadMaxQueuedActions)
    {
        TimeoutEnabled = timeoutEnabled;
        Timeout = timeout;
        CircuitBreakerEnabled = circuitBreakerEnabled;
        CircuitBreakerFailureRatio = circuitBreakerFailureRatio;
        CircuitBreakerMinimumThroughput = circuitBreakerMinimumThroughput;
        CircuitBreakerSamplingDuration = circuitBreakerSamplingDuration;
        CircuitBreakerBreakDuration = circuitBreakerBreakDuration;
        BulkheadEnabled = bulkheadEnabled;
        BulkheadMaxConcurrentExecutions = bulkheadMaxConcurrentExecutions;
        BulkheadMaxQueuedActions = bulkheadMaxQueuedActions;
    }

    public static GraphQLExecutionResilienceOptions Empty { get; } = new(
        timeoutEnabled: false,
        timeout: null,
        circuitBreakerEnabled: false,
        circuitBreakerFailureRatio: DefaultCircuitBreakerFailureRatio,
        circuitBreakerMinimumThroughput: DefaultCircuitBreakerMinimumThroughput,
        circuitBreakerSamplingDuration: TimeSpan.FromSeconds(DefaultCircuitBreakerSamplingDurationSeconds),
        circuitBreakerBreakDuration: TimeSpan.FromSeconds(DefaultCircuitBreakerBreakDurationSeconds),
        bulkheadEnabled: false,
        bulkheadMaxConcurrentExecutions: DefaultBulkheadMaxConcurrentExecutions,
        bulkheadMaxQueuedActions: DefaultBulkheadMaxQueuedActions);

    public bool TimeoutEnabled { get; }

    public TimeSpan? Timeout { get; }

    public bool CircuitBreakerEnabled { get; }

    public decimal CircuitBreakerFailureRatio { get; }

    public int CircuitBreakerMinimumThroughput { get; }

    public TimeSpan CircuitBreakerSamplingDuration { get; }

    public TimeSpan CircuitBreakerBreakDuration { get; }

    public bool BulkheadEnabled { get; }

    public int BulkheadMaxConcurrentExecutions { get; }

    public int BulkheadMaxQueuedActions { get; }

    public bool HasEnforcedStrategies => TimeoutEnabled || CircuitBreakerEnabled || BulkheadEnabled;

    public static GraphQLExecutionResilienceOptions FromManifest(RuntimeManifest? manifest)
    {
        if (manifest is null)
        {
            return Empty;
        }

        var timeout = ResolveTimeout(manifest.AppProfile.Resilience.Timeout);
        var circuitBreaker = ResolveCircuitBreaker(manifest.AppProfile.Resilience.CircuitBreaker);
        var bulkhead = ResolveBulkhead(manifest.AppProfile.Resilience.Bulkhead);

        return new GraphQLExecutionResilienceOptions(
            timeout.Enabled,
            timeout.Timeout,
            circuitBreaker.Enabled,
            circuitBreaker.FailureRatio,
            circuitBreaker.MinimumThroughput,
            circuitBreaker.SamplingDuration,
            circuitBreaker.BreakDuration,
            bulkhead.Enabled,
            bulkhead.MaxConcurrentExecutions,
            bulkhead.MaxQueuedActions);
    }

    private static ResolvedTimeout ResolveTimeout(TimeoutSelection selection)
    {
        ArgumentNullException.ThrowIfNull(selection);

        if (selection.Enabled == false)
        {
            return new ResolvedTimeout(false, null);
        }

        if (selection.Enabled != true &&
            !selection.TotalTimeoutSeconds.HasValue &&
            !selection.AttemptTimeoutSeconds.HasValue)
        {
            return new ResolvedTimeout(false, null);
        }

        var timeoutSeconds = selection.TotalTimeoutSeconds ??
            selection.AttemptTimeoutSeconds ??
            DefaultTotalTimeoutSeconds;

        return new ResolvedTimeout(
            true,
            TimeSpan.FromSeconds(Math.Max(1, timeoutSeconds)));
    }

    private static ResolvedCircuitBreaker ResolveCircuitBreaker(CircuitBreakerSelection selection)
    {
        ArgumentNullException.ThrowIfNull(selection);

        if (selection.Enabled == false)
        {
            return ResolvedCircuitBreaker.Disabled;
        }

        if (selection.Enabled != true &&
            !selection.FailureRatio.HasValue &&
            !selection.MinimumThroughput.HasValue &&
            !selection.SamplingDurationSeconds.HasValue &&
            !selection.BreakDurationSeconds.HasValue)
        {
            return ResolvedCircuitBreaker.Disabled;
        }

        var failureRatio = selection.FailureRatio ?? DefaultCircuitBreakerFailureRatio;
        var minimumThroughput = selection.MinimumThroughput ?? DefaultCircuitBreakerMinimumThroughput;
        var samplingDurationSeconds = selection.SamplingDurationSeconds ?? DefaultCircuitBreakerSamplingDurationSeconds;
        var breakDurationSeconds = selection.BreakDurationSeconds ?? DefaultCircuitBreakerBreakDurationSeconds;

        return new ResolvedCircuitBreaker(
            true,
            Math.Clamp(failureRatio, 0m, 1m),
            Math.Max(1, minimumThroughput),
            TimeSpan.FromSeconds(Math.Max(1, samplingDurationSeconds)),
            TimeSpan.FromSeconds(Math.Max(1, breakDurationSeconds)));
    }

    private static ResolvedBulkhead ResolveBulkhead(BulkheadSelection selection)
    {
        ArgumentNullException.ThrowIfNull(selection);

        if (selection.Enabled == false)
        {
            return ResolvedBulkhead.Disabled;
        }

        if (selection.Enabled != true &&
            !selection.MaxConcurrentExecutions.HasValue &&
            !selection.MaxQueuedActions.HasValue)
        {
            return ResolvedBulkhead.Disabled;
        }

        return new ResolvedBulkhead(
            true,
            Math.Max(1, selection.MaxConcurrentExecutions ?? DefaultBulkheadMaxConcurrentExecutions),
            Math.Max(0, selection.MaxQueuedActions ?? DefaultBulkheadMaxQueuedActions));
    }

    private readonly record struct ResolvedTimeout(bool Enabled, TimeSpan? Timeout);

    private readonly record struct ResolvedCircuitBreaker(
        bool Enabled,
        decimal FailureRatio,
        int MinimumThroughput,
        TimeSpan SamplingDuration,
        TimeSpan BreakDuration)
    {
        public static ResolvedCircuitBreaker Disabled { get; } = new(
            false,
            DefaultCircuitBreakerFailureRatio,
            DefaultCircuitBreakerMinimumThroughput,
            TimeSpan.FromSeconds(DefaultCircuitBreakerSamplingDurationSeconds),
            TimeSpan.FromSeconds(DefaultCircuitBreakerBreakDurationSeconds));
    }

    private readonly record struct ResolvedBulkhead(
        bool Enabled,
        int MaxConcurrentExecutions,
        int MaxQueuedActions)
    {
        public static ResolvedBulkhead Disabled { get; } = new(
            false,
            DefaultBulkheadMaxConcurrentExecutions,
            DefaultBulkheadMaxQueuedActions);
    }
}
