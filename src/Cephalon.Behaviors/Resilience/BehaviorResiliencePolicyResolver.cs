using Cephalon.Abstractions.AppModel;
using Cephalon.Abstractions.Resilience;
using Cephalon.Engine.Configuration;
using Polly;
using Polly.RateLimiting;
using Polly.Timeout;

namespace Cephalon.Behaviors.Resilience;

internal static class BehaviorResiliencePolicyResolver
{
    internal const string PolicyId = "cephalon-behavior-execution";
    internal const string EnforcedExecutionMode = "behavior-dispatch-middleware";
    internal const string ContractOnlyExecutionMode = "contract-only";
    internal const string Scope = "all-behavior-executions";
    internal const int DefaultTotalTimeoutSeconds = 30;
    internal const int DefaultAttemptTimeoutSeconds = 10;
    internal const int DefaultMaxConcurrentExecutions = 64;
    internal const int DefaultMaxQueuedActions = 0;

    internal static ResolvedBehaviorResiliencePolicy? Resolve(ResilienceSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var requested = new BehaviorExecutionResilienceSelection(
            retry: new RetrySelection(
                enabled: settings.Retry.Enabled,
                maxAttempts: settings.Retry.MaxAttempts,
                backoff: settings.Retry.Backoff,
                baseDelayMilliseconds: settings.Retry.BaseDelayMilliseconds,
                maxDelayMilliseconds: settings.Retry.MaxDelayMilliseconds,
                useJitter: settings.Retry.UseJitter),
            timeout: new TimeoutSelection(
                enabled: settings.Timeout.Enabled,
                totalTimeoutSeconds: settings.Timeout.TotalTimeoutSeconds,
                attemptTimeoutSeconds: settings.Timeout.AttemptTimeoutSeconds),
            circuitBreaker: new CircuitBreakerSelection(
                enabled: settings.CircuitBreaker.Enabled,
                failureRatio: settings.CircuitBreaker.FailureRatio,
                minimumThroughput: settings.CircuitBreaker.MinimumThroughput,
                samplingDurationSeconds: settings.CircuitBreaker.SamplingDurationSeconds,
                breakDurationSeconds: settings.CircuitBreaker.BreakDurationSeconds),
            bulkhead: new BulkheadSelection(
                enabled: settings.Bulkhead.Enabled,
                maxConcurrentExecutions: settings.Bulkhead.MaxConcurrentExecutions,
                maxQueuedActions: settings.Bulkhead.MaxQueuedActions));

        return Resolve(requested);
    }

    internal static ResolvedBehaviorResiliencePolicy? Resolve(ResilienceSelection selection)
    {
        ArgumentNullException.ThrowIfNull(selection);

        var requested = new BehaviorExecutionResilienceSelection(
            retry: selection.Retry,
            timeout: selection.Timeout,
            circuitBreaker: selection.CircuitBreaker,
            bulkhead: selection.Bulkhead);

        return Resolve(requested);
    }

    private static ResolvedBehaviorResiliencePolicy? Resolve(BehaviorExecutionResilienceSelection requested)
    {
        ArgumentNullException.ThrowIfNull(requested);

        if (!HasRequestedStrategies(requested))
        {
            return null;
        }

        var effectiveTimeout = ResolveTimeout(requested);
        var effectiveBulkhead = ResolveBulkhead(requested.Bulkhead);
        var effective = new BehaviorExecutionResilienceSelection(
            timeout: effectiveTimeout,
            bulkhead: effectiveBulkhead);
        var metadata = CreateMetadata(requested, effective);
        var executionMode = effective.HasValues
            ? EnforcedExecutionMode
            : ContractOnlyExecutionMode;
        var description = effective.HasValues
            ? "Behavior dispatch resilience enforced through the shared middleware pipeline."
            : "Behavior resilience requested by configuration but not yet enforced by the current runtime.";

        return new ResolvedBehaviorResiliencePolicy(
            Id: PolicyId,
            DisplayName: "Cephalon Behavior Execution Resilience",
            Description: description,
            ExecutionMode: executionMode,
            Scope: Scope,
            Requested: requested,
            Effective: effective,
            Metadata: metadata);
    }

    private static TimeoutSelection ResolveTimeout(BehaviorExecutionResilienceSelection requested)
    {
        ArgumentNullException.ThrowIfNull(requested);

        var settings = requested.Timeout;

        if (settings.Enabled == false)
        {
            return TimeoutSelection.Empty;
        }

        if (settings.Enabled != true &&
            !settings.TotalTimeoutSeconds.HasValue &&
            !settings.AttemptTimeoutSeconds.HasValue)
        {
            return TimeoutSelection.Empty;
        }

        var totalTimeoutSeconds = settings.TotalTimeoutSeconds;
        var attemptTimeoutSeconds = settings.AttemptTimeoutSeconds;
        if (!totalTimeoutSeconds.HasValue && !attemptTimeoutSeconds.HasValue)
        {
            totalTimeoutSeconds = DefaultTotalTimeoutSeconds;
            attemptTimeoutSeconds = DefaultAttemptTimeoutSeconds;
        }

        totalTimeoutSeconds ??= attemptTimeoutSeconds;
        attemptTimeoutSeconds = null;

        return new TimeoutSelection(
            enabled: true,
            totalTimeoutSeconds: totalTimeoutSeconds,
            attemptTimeoutSeconds: attemptTimeoutSeconds);
    }

    private static BulkheadSelection ResolveBulkhead(BulkheadSelection settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (settings.Enabled == false)
        {
            return BulkheadSelection.Empty;
        }

        if (settings.Enabled != true &&
            !settings.MaxConcurrentExecutions.HasValue &&
            !settings.MaxQueuedActions.HasValue)
        {
            return BulkheadSelection.Empty;
        }

        return new BulkheadSelection(
            enabled: true,
            maxConcurrentExecutions: settings.MaxConcurrentExecutions ?? DefaultMaxConcurrentExecutions,
            maxQueuedActions: settings.MaxQueuedActions ?? DefaultMaxQueuedActions);
    }

    private static Dictionary<string, string> CreateMetadata(
        BehaviorExecutionResilienceSelection requested,
        BehaviorExecutionResilienceSelection effective)
    {
        ArgumentNullException.ThrowIfNull(requested);
        ArgumentNullException.ThrowIfNull(effective);

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["retryMode"] = IsRequested(requested.Retry) ? "contract-only" : "disabled",
            ["timeoutMode"] = effective.Timeout.HasValues ? "enforced" : "disabled",
            ["circuitBreakerMode"] = IsRequested(requested.CircuitBreaker) ? "contract-only" : "disabled",
            ["bulkheadMode"] = effective.Bulkhead.HasValues ? "enforced" : "disabled"
        };

        var requestedStrategies = new List<string>();
        if (IsRequested(requested.Retry))
        {
            requestedStrategies.Add("retry");
        }

        if (IsRequested(requested.Timeout))
        {
            requestedStrategies.Add("timeout");
        }

        if (IsRequested(requested.CircuitBreaker))
        {
            requestedStrategies.Add("circuit-breaker");
        }

        if (IsRequested(requested.Bulkhead))
        {
            requestedStrategies.Add("bulkhead");
        }

        var effectiveStrategies = new List<string>();
        if (effective.Timeout.HasValues && effective.Timeout.Enabled == true)
        {
            effectiveStrategies.Add("timeout");
        }

        if (effective.Bulkhead.HasValues && effective.Bulkhead.Enabled == true)
        {
            effectiveStrategies.Add("bulkhead");
        }

        metadata["requestedStrategies"] = requestedStrategies.Count == 0
            ? "none"
            : string.Join(",", requestedStrategies);
        metadata["effectiveStrategies"] = effectiveStrategies.Count == 0
            ? "none"
            : string.Join(",", effectiveStrategies);

        if (effective.Timeout.TotalTimeoutSeconds.HasValue)
        {
            metadata["totalTimeoutSeconds"] = effective.Timeout.TotalTimeoutSeconds.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        if (effective.Timeout.AttemptTimeoutSeconds.HasValue)
        {
            metadata["attemptTimeoutSeconds"] = effective.Timeout.AttemptTimeoutSeconds.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        if (effective.Bulkhead.MaxConcurrentExecutions.HasValue)
        {
            metadata["maxConcurrentExecutions"] = effective.Bulkhead.MaxConcurrentExecutions.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        if (effective.Bulkhead.MaxQueuedActions.HasValue)
        {
            metadata["maxQueuedActions"] = effective.Bulkhead.MaxQueuedActions.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        return metadata;
    }

    private static bool HasRequestedStrategies(BehaviorExecutionResilienceSelection requested)
    {
        ArgumentNullException.ThrowIfNull(requested);

        return IsRequested(requested.Retry) ||
            IsRequested(requested.Timeout) ||
            IsRequested(requested.CircuitBreaker) ||
            IsRequested(requested.Bulkhead);
    }

    private static bool IsRequested(RetrySelection selection)
    {
        ArgumentNullException.ThrowIfNull(selection);
        return selection.HasValues && selection.Enabled != false;
    }

    private static bool IsRequested(TimeoutSelection selection)
    {
        ArgumentNullException.ThrowIfNull(selection);
        return selection.HasValues && selection.Enabled != false;
    }

    private static bool IsRequested(CircuitBreakerSelection selection)
    {
        ArgumentNullException.ThrowIfNull(selection);
        return selection.HasValues && selection.Enabled != false;
    }

    private static bool IsRequested(BulkheadSelection selection)
    {
        ArgumentNullException.ThrowIfNull(selection);
        return selection.HasValues && selection.Enabled != false;
    }
}

internal sealed record ResolvedBehaviorResiliencePolicy(
    string Id,
    string DisplayName,
    string Description,
    string ExecutionMode,
    string Scope,
    BehaviorExecutionResilienceSelection Requested,
    BehaviorExecutionResilienceSelection Effective,
    IReadOnlyDictionary<string, string> Metadata)
{
    public bool HasEnforcedStrategies => string.Equals(
        ExecutionMode,
        BehaviorResiliencePolicyResolver.EnforcedExecutionMode,
        StringComparison.OrdinalIgnoreCase);

    public void Configure(ResiliencePipelineBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (Effective.Timeout.TotalTimeoutSeconds.HasValue)
        {
            builder.AddTimeout(TimeSpan.FromSeconds(Effective.Timeout.TotalTimeoutSeconds.Value));
        }

        if (Effective.Bulkhead.HasValues && Effective.Bulkhead.Enabled == true)
        {
            builder.AddConcurrencyLimiter(
                permitLimit: Effective.Bulkhead.MaxConcurrentExecutions ?? BehaviorResiliencePolicyResolver.DefaultMaxConcurrentExecutions,
                queueLimit: Effective.Bulkhead.MaxQueuedActions ?? BehaviorResiliencePolicyResolver.DefaultMaxQueuedActions);
        }
    }

    public BehaviorResilienceRuntimeDescriptor ToDescriptor()
    {
        return new BehaviorResilienceRuntimeDescriptor(
            Id,
            DisplayName,
            Description,
            ExecutionMode,
            Scope,
            Requested,
            Effective,
            Metadata);
    }
}
