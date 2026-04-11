using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.AppModel;
using Cephalon.Abstractions.Resilience;
using Cephalon.Engine.Configuration;
using Polly;
using Polly.CircuitBreaker;
using Polly.RateLimiting;
using Polly.Retry;
using Polly.Timeout;

namespace Cephalon.Behaviors.Resilience;

internal static class BehaviorResiliencePolicyResolver
{
    internal const string PolicyId = "cephalon-behavior-execution";
    internal const string DisabledExecutionMode = "disabled";
    internal const string EnforcedExecutionMode = "behavior-dispatch-middleware";
    internal const string ContractOnlyExecutionMode = "contract-only";
    internal const string Scope = "all-behavior-executions";
    internal const int DefaultMaxRetryAttempts = 3;
    internal const string DefaultRetryBackoff = "Exponential";
    internal const int DefaultRetryBaseDelayMilliseconds = 200;
    internal const int DefaultRetryMaxDelayMilliseconds = 2000;
    internal const bool DefaultRetryUseJitter = true;
    internal const int DefaultTotalTimeoutSeconds = 30;
    internal const int DefaultAttemptTimeoutSeconds = 10;
    internal const int DefaultMaxConcurrentExecutions = 64;
    internal const int DefaultMaxQueuedActions = 0;
    internal const decimal DefaultCircuitBreakerFailureRatio = 0.1m;
    internal const int DefaultCircuitBreakerMinimumThroughput = 100;
    internal const int DefaultCircuitBreakerSamplingDurationSeconds = 30;
    internal const int DefaultCircuitBreakerBreakDurationSeconds = 5;

    internal static BehaviorResiliencePolicyCatalog ResolvePolicies(ResilienceSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var selection = new ResilienceSelection(
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
                maxQueuedActions: settings.Bulkhead.MaxQueuedActions),
            behaviorExecutionOverrides: settings.BehaviorExecutionOverrides
                .Select(static entry => new BehaviorExecutionResilienceOverrideSelection(
                    id: entry.Id,
                    behaviorIds: entry.BehaviorIds,
                    transportIds: entry.TransportIds,
                    retry: new RetrySelection(
                        enabled: entry.Retry.Enabled,
                        maxAttempts: entry.Retry.MaxAttempts,
                        backoff: entry.Retry.Backoff,
                        baseDelayMilliseconds: entry.Retry.BaseDelayMilliseconds,
                        maxDelayMilliseconds: entry.Retry.MaxDelayMilliseconds,
                        useJitter: entry.Retry.UseJitter),
                    timeout: new TimeoutSelection(
                        enabled: entry.Timeout.Enabled,
                        totalTimeoutSeconds: entry.Timeout.TotalTimeoutSeconds,
                        attemptTimeoutSeconds: entry.Timeout.AttemptTimeoutSeconds),
                    circuitBreaker: new CircuitBreakerSelection(
                        enabled: entry.CircuitBreaker.Enabled,
                        failureRatio: entry.CircuitBreaker.FailureRatio,
                        minimumThroughput: entry.CircuitBreaker.MinimumThroughput,
                        samplingDurationSeconds: entry.CircuitBreaker.SamplingDurationSeconds,
                        breakDurationSeconds: entry.CircuitBreaker.BreakDurationSeconds),
                    bulkhead: new BulkheadSelection(
                        enabled: entry.Bulkhead.Enabled,
                        maxConcurrentExecutions: entry.Bulkhead.MaxConcurrentExecutions,
                        maxQueuedActions: entry.Bulkhead.MaxQueuedActions)))
                .ToArray());

        return ResolvePolicies(selection);
    }

    internal static BehaviorResiliencePolicyCatalog ResolvePolicies(ResilienceSelection selection)
    {
        ArgumentNullException.ThrowIfNull(selection);

        var policies = new List<ResolvedBehaviorResiliencePolicy>();
        var defaultRequested = new BehaviorExecutionResilienceSelection(
            retry: selection.Retry,
            timeout: selection.Timeout,
            circuitBreaker: selection.CircuitBreaker,
            bulkhead: selection.Bulkhead);
        var defaultPolicy = ResolveDefaultPolicy(defaultRequested);
        if (defaultPolicy is not null)
        {
            policies.Add(defaultPolicy);
        }

        for (var index = 0; index < selection.BehaviorExecutionOverrides.Count; index++)
        {
            var policy = ResolveOverridePolicy(
                defaultRequested,
                selection.BehaviorExecutionOverrides[index],
                order: index + 1);
            if (policy is not null)
            {
                policies.Add(policy);
            }
        }

        return new BehaviorResiliencePolicyCatalog(policies);
    }

    private static ResolvedBehaviorResiliencePolicy? ResolveDefaultPolicy(BehaviorExecutionResilienceSelection requested)
    {
        ArgumentNullException.ThrowIfNull(requested);

        if (!HasRequestedStrategies(requested))
        {
            return null;
        }

        var effective = ResolveEffective(requested);
        var metadata = CreateMetadata(
            isOverride: false,
            overrideId: null,
            behaviorIds: [],
            transportIds: [],
            explicitSelection: requested,
            requested: requested,
            effective: effective);
        metadata["reason"] = "configured";
        metadata["scope"] = Scope;
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
            BehaviorIds: [],
            TransportIds: [],
            Requested: requested,
            Effective: effective,
            Metadata: metadata,
            Order: 0,
            IsOverride: false);
    }

    private static ResolvedBehaviorResiliencePolicy? ResolveOverridePolicy(
        BehaviorExecutionResilienceSelection defaultRequested,
        BehaviorExecutionResilienceOverrideSelection selection,
        int order)
    {
        ArgumentNullException.ThrowIfNull(defaultRequested);
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentOutOfRangeException.ThrowIfNegative(order);

        var behaviorIds = NormalizeList(selection.BehaviorIds);
        var transportIds = NormalizeList(selection.TransportIds);
        if (behaviorIds.Length == 0 && transportIds.Length == 0)
        {
            return null;
        }

        var explicitRequested = new BehaviorExecutionResilienceSelection(
            retry: selection.Retry,
            timeout: selection.Timeout,
            circuitBreaker: selection.CircuitBreaker,
            bulkhead: selection.Bulkhead);
        var requested = MergeRequested(defaultRequested, explicitRequested);
        var hasRequestedStrategies = HasRequestedStrategies(requested);
        var effective = hasRequestedStrategies
            ? ResolveEffective(requested)
            : BehaviorExecutionResilienceSelection.Empty;
        var executionMode = !hasRequestedStrategies
            ? DisabledExecutionMode
            : effective.HasValues
                ? EnforcedExecutionMode
                : ContractOnlyExecutionMode;
        var scope = ResolveOverrideScope(behaviorIds, transportIds);
        var metadata = CreateMetadata(
            isOverride: true,
            overrideId: selection.Id,
            behaviorIds: behaviorIds,
            transportIds: transportIds,
            explicitSelection: explicitRequested,
            requested: requested,
            effective: effective);
        metadata["reason"] = hasRequestedStrategies
            ? "configured"
            : "disabled-by-override";
        metadata["scope"] = scope;

        return new ResolvedBehaviorResiliencePolicy(
            Id: BuildOverridePolicyId(selection.Id),
            DisplayName: $"Behavior Resilience Override ({selection.Id})",
            Description: BuildOverrideDescription(selection.Id, behaviorIds, transportIds, hasRequestedStrategies),
            ExecutionMode: executionMode,
            Scope: scope,
            BehaviorIds: behaviorIds,
            TransportIds: transportIds,
            Requested: requested,
            Effective: effective,
            Metadata: metadata,
            Order: order,
            IsOverride: true);
    }

    private static BehaviorExecutionResilienceSelection ResolveEffective(BehaviorExecutionResilienceSelection requested)
    {
        ArgumentNullException.ThrowIfNull(requested);

        var retry = ResolveRetry(requested.Retry);

        return new BehaviorExecutionResilienceSelection(
            retry: retry,
            circuitBreaker: ResolveCircuitBreaker(requested.CircuitBreaker),
            timeout: ResolveTimeout(requested, retry),
            bulkhead: ResolveBulkhead(requested.Bulkhead));
    }

    private static RetrySelection ResolveRetry(RetrySelection settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (settings.Enabled == false)
        {
            return RetrySelection.Empty;
        }

        if (settings.Enabled != true &&
            !settings.MaxAttempts.HasValue &&
            settings.Backoff is null &&
            !settings.BaseDelayMilliseconds.HasValue &&
            !settings.MaxDelayMilliseconds.HasValue &&
            !settings.UseJitter.HasValue)
        {
            return RetrySelection.Empty;
        }

        var baseDelayMilliseconds = settings.BaseDelayMilliseconds ?? DefaultRetryBaseDelayMilliseconds;
        var maxDelayMilliseconds = settings.MaxDelayMilliseconds ?? DefaultRetryMaxDelayMilliseconds;
        if (maxDelayMilliseconds < baseDelayMilliseconds)
        {
            maxDelayMilliseconds = baseDelayMilliseconds;
        }

        return new RetrySelection(
            enabled: true,
            maxAttempts: settings.MaxAttempts ?? DefaultMaxRetryAttempts,
            backoff: NormalizeRetryBackoff(settings.Backoff),
            baseDelayMilliseconds: baseDelayMilliseconds,
            maxDelayMilliseconds: maxDelayMilliseconds,
            useJitter: settings.UseJitter ?? DefaultRetryUseJitter);
    }

    private static CircuitBreakerSelection ResolveCircuitBreaker(CircuitBreakerSelection settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (settings.Enabled == false)
        {
            return CircuitBreakerSelection.Empty;
        }

        if (settings.Enabled != true &&
            !settings.FailureRatio.HasValue &&
            !settings.MinimumThroughput.HasValue &&
            !settings.SamplingDurationSeconds.HasValue &&
            !settings.BreakDurationSeconds.HasValue)
        {
            return CircuitBreakerSelection.Empty;
        }

        return new CircuitBreakerSelection(
            enabled: true,
            failureRatio: settings.FailureRatio ?? DefaultCircuitBreakerFailureRatio,
            minimumThroughput: settings.MinimumThroughput ?? DefaultCircuitBreakerMinimumThroughput,
            samplingDurationSeconds: settings.SamplingDurationSeconds ?? DefaultCircuitBreakerSamplingDurationSeconds,
            breakDurationSeconds: settings.BreakDurationSeconds ?? DefaultCircuitBreakerBreakDurationSeconds);
    }

    private static BehaviorExecutionResilienceSelection MergeRequested(
        BehaviorExecutionResilienceSelection baseline,
        BehaviorExecutionResilienceSelection overrideSelection)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(overrideSelection);

        return new BehaviorExecutionResilienceSelection(
            retry: MergeRetry(baseline.Retry, overrideSelection.Retry),
            timeout: MergeTimeout(baseline.Timeout, overrideSelection.Timeout),
            circuitBreaker: MergeCircuitBreaker(baseline.CircuitBreaker, overrideSelection.CircuitBreaker),
            bulkhead: MergeBulkhead(baseline.Bulkhead, overrideSelection.Bulkhead));
    }

    private static TimeoutSelection ResolveTimeout(
        BehaviorExecutionResilienceSelection requested,
        RetrySelection effectiveRetry)
    {
        ArgumentNullException.ThrowIfNull(requested);
        ArgumentNullException.ThrowIfNull(effectiveRetry);

        var settings = requested.Timeout;
        var retryEnabled = effectiveRetry.HasValues && effectiveRetry.Enabled == true;

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
            attemptTimeoutSeconds = retryEnabled
                ? DefaultAttemptTimeoutSeconds
                : null;
        }

        totalTimeoutSeconds ??= attemptTimeoutSeconds;
        if (retryEnabled)
        {
            attemptTimeoutSeconds ??= Math.Min(
                totalTimeoutSeconds ?? DefaultAttemptTimeoutSeconds,
                DefaultAttemptTimeoutSeconds);
            if (attemptTimeoutSeconds.HasValue &&
                totalTimeoutSeconds.HasValue &&
                attemptTimeoutSeconds.Value > totalTimeoutSeconds.Value)
            {
                attemptTimeoutSeconds = totalTimeoutSeconds;
            }
        }
        else
        {
            attemptTimeoutSeconds = null;
        }

        return new TimeoutSelection(
            enabled: true,
            totalTimeoutSeconds: totalTimeoutSeconds,
            attemptTimeoutSeconds: attemptTimeoutSeconds);
    }

    private static RetrySelection MergeRetry(
        RetrySelection baseline,
        RetrySelection overrideSelection)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(overrideSelection);

        if (!overrideSelection.HasValues)
        {
            return baseline;
        }

        return new RetrySelection(
            enabled: overrideSelection.Enabled ?? baseline.Enabled,
            maxAttempts: overrideSelection.MaxAttempts ?? baseline.MaxAttempts,
            backoff: overrideSelection.Backoff ?? baseline.Backoff,
            baseDelayMilliseconds: overrideSelection.BaseDelayMilliseconds ?? baseline.BaseDelayMilliseconds,
            maxDelayMilliseconds: overrideSelection.MaxDelayMilliseconds ?? baseline.MaxDelayMilliseconds,
            useJitter: overrideSelection.UseJitter ?? baseline.UseJitter);
    }

    private static TimeoutSelection MergeTimeout(
        TimeoutSelection baseline,
        TimeoutSelection overrideSelection)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(overrideSelection);

        if (!overrideSelection.HasValues)
        {
            return baseline;
        }

        return new TimeoutSelection(
            enabled: overrideSelection.Enabled ?? baseline.Enabled,
            totalTimeoutSeconds: overrideSelection.TotalTimeoutSeconds ?? baseline.TotalTimeoutSeconds,
            attemptTimeoutSeconds: overrideSelection.AttemptTimeoutSeconds ?? baseline.AttemptTimeoutSeconds);
    }

    private static CircuitBreakerSelection MergeCircuitBreaker(
        CircuitBreakerSelection baseline,
        CircuitBreakerSelection overrideSelection)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(overrideSelection);

        if (!overrideSelection.HasValues)
        {
            return baseline;
        }

        return new CircuitBreakerSelection(
            enabled: overrideSelection.Enabled ?? baseline.Enabled,
            failureRatio: overrideSelection.FailureRatio ?? baseline.FailureRatio,
            minimumThroughput: overrideSelection.MinimumThroughput ?? baseline.MinimumThroughput,
            samplingDurationSeconds: overrideSelection.SamplingDurationSeconds ?? baseline.SamplingDurationSeconds,
            breakDurationSeconds: overrideSelection.BreakDurationSeconds ?? baseline.BreakDurationSeconds);
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

    private static BulkheadSelection MergeBulkhead(
        BulkheadSelection baseline,
        BulkheadSelection overrideSelection)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(overrideSelection);

        if (!overrideSelection.HasValues)
        {
            return baseline;
        }

        return new BulkheadSelection(
            enabled: overrideSelection.Enabled ?? baseline.Enabled,
            maxConcurrentExecutions: overrideSelection.MaxConcurrentExecutions ?? baseline.MaxConcurrentExecutions,
            maxQueuedActions: overrideSelection.MaxQueuedActions ?? baseline.MaxQueuedActions);
    }

    private static Dictionary<string, string> CreateMetadata(
        bool isOverride,
        string? overrideId,
        string[] behaviorIds,
        string[] transportIds,
        BehaviorExecutionResilienceSelection explicitSelection,
        BehaviorExecutionResilienceSelection requested,
        BehaviorExecutionResilienceSelection effective)
    {
        ArgumentNullException.ThrowIfNull(behaviorIds);
        ArgumentNullException.ThrowIfNull(transportIds);
        ArgumentNullException.ThrowIfNull(explicitSelection);
        ArgumentNullException.ThrowIfNull(requested);
        ArgumentNullException.ThrowIfNull(effective);

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["isOverride"] = isOverride ? "true" : "false",
            ["retryMode"] = effective.Retry.HasValues ? "enforced" : IsRequested(requested.Retry) ? "contract-only" : "disabled",
            ["retryEligibilityMode"] = IsRequested(requested.Retry) ? "behavior-dependent" : "disabled",
            ["timeoutMode"] = effective.Timeout.HasValues ? "enforced" : "disabled",
            ["circuitBreakerMode"] = effective.CircuitBreaker.HasValues ? "enforced" : IsRequested(requested.CircuitBreaker) ? "contract-only" : "disabled",
            ["bulkheadMode"] = effective.Bulkhead.HasValues ? "enforced" : "disabled"
        };
        if (!string.IsNullOrWhiteSpace(overrideId))
        {
            metadata["overrideId"] = overrideId.Trim();
        }

        if (behaviorIds.Length > 0)
        {
            metadata["behaviorIds"] = string.Join(",", behaviorIds);
        }

        if (transportIds.Length > 0)
        {
            metadata["transportIds"] = string.Join(",", transportIds);
        }

        var explicitStrategies = ResolveSuppliedStrategyNames(explicitSelection);
        var requestedStrategies = ResolveRequestedStrategyNames(requested);
        var effectiveStrategies = ResolveEffectiveStrategyNames(effective);
        var inheritedStrategies = requestedStrategies
            .Except(explicitStrategies, StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        metadata["explicitStrategies"] = explicitStrategies.Length == 0
            ? "none"
            : string.Join(",", explicitStrategies);
        metadata["requestedStrategies"] = requestedStrategies.Length == 0
            ? "none"
            : string.Join(",", requestedStrategies);
        metadata["effectiveStrategies"] = effectiveStrategies.Length == 0
            ? "none"
            : string.Join(",", effectiveStrategies);
        metadata["inheritedStrategies"] = inheritedStrategies.Length == 0
            ? "none"
            : string.Join(",", inheritedStrategies);

        if (effective.Timeout.TotalTimeoutSeconds.HasValue)
        {
            metadata["totalTimeoutSeconds"] = effective.Timeout.TotalTimeoutSeconds.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        if (effective.Retry.MaxAttempts.HasValue)
        {
            metadata["retryMaxAttempts"] = effective.Retry.MaxAttempts.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        if (!string.IsNullOrWhiteSpace(effective.Retry.Backoff))
        {
            metadata["retryBackoff"] = effective.Retry.Backoff;
        }

        if (effective.Retry.BaseDelayMilliseconds.HasValue)
        {
            metadata["retryBaseDelayMilliseconds"] = effective.Retry.BaseDelayMilliseconds.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        if (effective.Retry.MaxDelayMilliseconds.HasValue)
        {
            metadata["retryMaxDelayMilliseconds"] = effective.Retry.MaxDelayMilliseconds.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        if (effective.Retry.UseJitter.HasValue)
        {
            metadata["retryUseJitter"] = effective.Retry.UseJitter.Value ? "true" : "false";
        }

        if (effective.Timeout.AttemptTimeoutSeconds.HasValue)
        {
            metadata["attemptTimeoutSeconds"] = effective.Timeout.AttemptTimeoutSeconds.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        if (effective.CircuitBreaker.FailureRatio.HasValue)
        {
            metadata["failureRatio"] = effective.CircuitBreaker.FailureRatio.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        if (effective.CircuitBreaker.MinimumThroughput.HasValue)
        {
            metadata["minimumThroughput"] = effective.CircuitBreaker.MinimumThroughput.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        if (effective.CircuitBreaker.SamplingDurationSeconds.HasValue)
        {
            metadata["samplingDurationSeconds"] = effective.CircuitBreaker.SamplingDurationSeconds.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        if (effective.CircuitBreaker.BreakDurationSeconds.HasValue)
        {
            metadata["breakDurationSeconds"] = effective.CircuitBreaker.BreakDurationSeconds.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
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

    private static string[] ResolveSuppliedStrategyNames(BehaviorExecutionResilienceSelection selection)
    {
        ArgumentNullException.ThrowIfNull(selection);

        var strategies = new List<string>();
        if (selection.Retry.HasValues)
        {
            strategies.Add("retry");
        }

        if (selection.Timeout.HasValues)
        {
            strategies.Add("timeout");
        }

        if (selection.CircuitBreaker.HasValues)
        {
            strategies.Add("circuit-breaker");
        }

        if (selection.Bulkhead.HasValues)
        {
            strategies.Add("bulkhead");
        }

        return strategies.ToArray();
    }

    private static string[] ResolveRequestedStrategyNames(BehaviorExecutionResilienceSelection selection)
    {
        ArgumentNullException.ThrowIfNull(selection);

        var strategies = new List<string>();
        if (IsRequested(selection.Retry))
        {
            strategies.Add("retry");
        }

        if (IsRequested(selection.Timeout))
        {
            strategies.Add("timeout");
        }

        if (IsRequested(selection.CircuitBreaker))
        {
            strategies.Add("circuit-breaker");
        }

        if (IsRequested(selection.Bulkhead))
        {
            strategies.Add("bulkhead");
        }

        return strategies.ToArray();
    }

    private static string[] ResolveEffectiveStrategyNames(BehaviorExecutionResilienceSelection selection)
    {
        ArgumentNullException.ThrowIfNull(selection);

        var strategies = new List<string>();
        if (selection.Retry.HasValues && selection.Retry.Enabled == true)
        {
            strategies.Add("retry");
        }

        if (selection.Timeout.HasValues && selection.Timeout.Enabled == true)
        {
            strategies.Add("timeout");
        }

        if (selection.CircuitBreaker.HasValues && selection.CircuitBreaker.Enabled == true)
        {
            strategies.Add("circuit-breaker");
        }

        if (selection.Bulkhead.HasValues && selection.Bulkhead.Enabled == true)
        {
            strategies.Add("bulkhead");
        }

        return strategies.ToArray();
    }

    private static string[] NormalizeList(IReadOnlyList<string> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        return values
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string ResolveOverrideScope(
        string[] behaviorIds,
        string[] transportIds)
    {
        ArgumentNullException.ThrowIfNull(behaviorIds);
        ArgumentNullException.ThrowIfNull(transportIds);

        return (behaviorIds.Length > 0, transportIds.Length > 0) switch
        {
            (true, true) => "behavior-executions-by-behavior-and-transport",
            (true, false) => "behavior-executions-by-behavior",
            (false, true) => "behavior-executions-by-transport",
            _ => Scope
        };
    }

    private static string BuildOverridePolicyId(string overrideId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(overrideId);

        var normalized = string.Concat(overrideId.Trim().Select(static ch =>
            char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : '-'));
        return $"{PolicyId}-{normalized.Trim('-')}";
    }

    private static string BuildOverrideDescription(
        string overrideId,
        string[] behaviorIds,
        string[] transportIds,
        bool enabled)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(overrideId);
        ArgumentNullException.ThrowIfNull(behaviorIds);
        ArgumentNullException.ThrowIfNull(transportIds);

        var targets = new List<string>();
        if (behaviorIds.Length > 0)
        {
            targets.Add($"behaviors [{string.Join(", ", behaviorIds)}]");
        }

        if (transportIds.Length > 0)
        {
            targets.Add($"transports [{string.Join(", ", transportIds)}]");
        }

        var targetDescription = targets.Count == 0
            ? "the targeted behavior executions"
            : string.Join(" and ", targets);
        return enabled
            ? $"Behavior execution resilience override '{overrideId}' applies scoped resilience to {targetDescription}."
            : $"Behavior execution resilience override '{overrideId}' disables inherited/default resilience for {targetDescription}.";
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

    private static string NormalizeRetryBackoff(string? backoff)
    {
        return NormalizeRetryBackoffKey(backoff) switch
        {
            "constant" => "Constant",
            "linear" => "Linear",
            _ => DefaultRetryBackoff
        };
    }

    internal static DelayBackoffType ResolveRetryBackoffType(string? backoff)
    {
        return NormalizeRetryBackoffKey(backoff) switch
        {
            "constant" => DelayBackoffType.Constant,
            "linear" => DelayBackoffType.Linear,
            _ => DelayBackoffType.Exponential
        };
    }

    private static string NormalizeRetryBackoffKey(string? backoff)
        => string.IsNullOrWhiteSpace(backoff)
            ? string.Empty
            : string.Concat(backoff.Trim().Where(static ch => !char.IsWhiteSpace(ch) && ch != '-' && ch != '_')).ToLowerInvariant();

}

internal sealed class BehaviorResiliencePolicyCatalog
{
    private readonly ResolvedBehaviorResiliencePolicy[] policies;
    private readonly ResolvedBehaviorResiliencePolicy[] enforcedPolicies;
    private readonly Dictionary<string, ResolvedBehaviorResiliencePolicy> policiesById;

    public BehaviorResiliencePolicyCatalog(IReadOnlyList<ResolvedBehaviorResiliencePolicy> policies)
    {
        ArgumentNullException.ThrowIfNull(policies);

        this.policies = policies
            .OrderBy(static policy => policy.Order)
            .ToArray();
        enforcedPolicies = this.policies
            .Where(static policy => policy.HasEnforcedStrategies)
            .ToArray();
        policiesById = this.policies.ToDictionary(
            static policy => policy.Id,
            StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<ResolvedBehaviorResiliencePolicy> Policies => policies;

    public IReadOnlyList<ResolvedBehaviorResiliencePolicy> EnforcedPolicies => enforcedPolicies;

    public bool HasEnforcedPolicies => enforcedPolicies.Length > 0;

    public ResolvedBehaviorResiliencePolicy? GetById(string policyId)
    {
        if (string.IsNullOrWhiteSpace(policyId))
        {
            return null;
        }

        return policiesById.TryGetValue(policyId.Trim(), out var policy)
            ? policy
            : null;
    }

    public BehaviorResiliencePolicyResolution Resolve(string behaviorId, string? transportId = null)
    {
        if (string.IsNullOrWhiteSpace(behaviorId))
        {
            return BehaviorResiliencePolicyResolution.None;
        }

        var candidates = policies
            .Where(policy => policy.Matches(behaviorId, transportId))
            .OrderBy(static policy => policy.Specificity)
            .ThenBy(static policy => policy.Order)
            .ToArray();
        if (candidates.Length == 0)
        {
            return BehaviorResiliencePolicyResolution.None;
        }

        var selected = candidates[^1];
        return selected.IsDisabled
            ? BehaviorResiliencePolicyResolution.Disable(selected)
            : BehaviorResiliencePolicyResolution.Active(selected);
    }
}

internal enum BehaviorResiliencePolicyMode
{
    None,
    Active,
    Disable
}

internal sealed record BehaviorResiliencePolicyResolution(
    BehaviorResiliencePolicyMode Mode,
    ResolvedBehaviorResiliencePolicy? Policy)
{
    public static BehaviorResiliencePolicyResolution None { get; } = new(BehaviorResiliencePolicyMode.None, null);

    public static BehaviorResiliencePolicyResolution Active(ResolvedBehaviorResiliencePolicy policy)
        => new(BehaviorResiliencePolicyMode.Active, policy);

    public static BehaviorResiliencePolicyResolution Disable(ResolvedBehaviorResiliencePolicy policy)
        => new(BehaviorResiliencePolicyMode.Disable, policy);
}

internal sealed record ResolvedBehaviorResiliencePolicy(
    string Id,
    string DisplayName,
    string Description,
    string ExecutionMode,
    string Scope,
    IReadOnlyList<string> BehaviorIds,
    IReadOnlyList<string> TransportIds,
    BehaviorExecutionResilienceSelection Requested,
    BehaviorExecutionResilienceSelection Effective,
    IReadOnlyDictionary<string, string> Metadata,
    int Order,
    bool IsOverride)
{
    public bool HasEnforcedStrategies => string.Equals(
        ExecutionMode,
        BehaviorResiliencePolicyResolver.EnforcedExecutionMode,
        StringComparison.OrdinalIgnoreCase);

    public bool IsDisabled => string.Equals(
        ExecutionMode,
        BehaviorResiliencePolicyResolver.DisabledExecutionMode,
        StringComparison.OrdinalIgnoreCase);

    public int Specificity => (BehaviorIds.Count > 0, TransportIds.Count > 0) switch
    {
        (true, true) => 3,
        (true, false) => 2,
        (false, true) => 1,
        _ => 0
    };

    public bool Matches(string behaviorId, string? transportId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(behaviorId);

        if (BehaviorIds.Count > 0 &&
            !BehaviorIds.Contains(behaviorId.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        if (TransportIds.Count == 0)
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(transportId) &&
            TransportIds.Contains(transportId.Trim(), StringComparer.OrdinalIgnoreCase);
    }

    public void Configure(
        ResiliencePipelineBuilder builder,
        Cephalon.Abstractions.Resilience.IBehaviorResilienceExceptionClassifier exceptionClassifier,
        BehaviorCircuitBreakerRuntimeState? circuitBreakerRuntimeState)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(exceptionClassifier);

        if (Effective.Timeout.TotalTimeoutSeconds.HasValue &&
            Effective.Retry.HasValues &&
            Effective.Retry.Enabled == true)
        {
            builder.AddTimeout(TimeSpan.FromSeconds(Effective.Timeout.TotalTimeoutSeconds.Value));
        }

        if (Effective.Retry.HasValues && Effective.Retry.Enabled == true)
        {
            builder.AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = Effective.Retry.MaxAttempts ?? BehaviorResiliencePolicyResolver.DefaultMaxRetryAttempts,
                BackoffType = BehaviorResiliencePolicyResolver.ResolveRetryBackoffType(Effective.Retry.Backoff),
                UseJitter = Effective.Retry.UseJitter ?? BehaviorResiliencePolicyResolver.DefaultRetryUseJitter,
                Delay = TimeSpan.FromMilliseconds(Effective.Retry.BaseDelayMilliseconds ?? BehaviorResiliencePolicyResolver.DefaultRetryBaseDelayMilliseconds),
                MaxDelay = TimeSpan.FromMilliseconds(Effective.Retry.MaxDelayMilliseconds ?? BehaviorResiliencePolicyResolver.DefaultRetryMaxDelayMilliseconds),
                ShouldHandle = args =>
                {
                    if (args.Outcome.Exception is not Exception exception)
                    {
                        return PredicateResult.False();
                    }

                    var handling = ClassifyException(args.Context, exception, exceptionClassifier);
                    return handling == Cephalon.Abstractions.Resilience.BehaviorResilienceExceptionHandling.RetryAndTrip
                        ? PredicateResult.True()
                        : PredicateResult.False();
                }
            });
        }

        if (Effective.CircuitBreaker.HasValues && Effective.CircuitBreaker.Enabled == true)
        {
            builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = (double)(Effective.CircuitBreaker.FailureRatio ?? BehaviorResiliencePolicyResolver.DefaultCircuitBreakerFailureRatio),
                MinimumThroughput = Effective.CircuitBreaker.MinimumThroughput ?? BehaviorResiliencePolicyResolver.DefaultCircuitBreakerMinimumThroughput,
                SamplingDuration = TimeSpan.FromSeconds(Effective.CircuitBreaker.SamplingDurationSeconds ?? BehaviorResiliencePolicyResolver.DefaultCircuitBreakerSamplingDurationSeconds),
                BreakDuration = TimeSpan.FromSeconds(Effective.CircuitBreaker.BreakDurationSeconds ?? BehaviorResiliencePolicyResolver.DefaultCircuitBreakerBreakDurationSeconds),
                ShouldHandle = args =>
                {
                    if (args.Outcome.Exception is not Exception exception)
                    {
                        return PredicateResult.False();
                    }

                    var handling = ClassifyException(args.Context, exception, exceptionClassifier);
                    return handling == Cephalon.Abstractions.Resilience.BehaviorResilienceExceptionHandling.Ignore
                        ? PredicateResult.False()
                        : PredicateResult.True();
                },
                StateProvider = circuitBreakerRuntimeState?.StateProvider,
                OnOpened = args =>
                {
                    circuitBreakerRuntimeState?.MarkOpened(args.BreakDuration, args.Outcome.Exception);
                    return default;
                },
                OnClosed = args =>
                {
                    circuitBreakerRuntimeState?.MarkClosed();
                    return default;
                },
                OnHalfOpened = args =>
                {
                    circuitBreakerRuntimeState?.MarkHalfOpened();
                    return default;
                }
            });
        }

        if (Effective.Timeout.AttemptTimeoutSeconds.HasValue)
        {
            builder.AddTimeout(TimeSpan.FromSeconds(Effective.Timeout.AttemptTimeoutSeconds.Value));
        }
        else if (Effective.Timeout.TotalTimeoutSeconds.HasValue)
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

    private Cephalon.Abstractions.Resilience.BehaviorResilienceExceptionHandling ClassifyException(
        ResilienceContext context,
        Exception exception,
        Cephalon.Abstractions.Resilience.IBehaviorResilienceExceptionClassifier exceptionClassifier)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(exceptionClassifier);

        var behaviorId = context.Properties.TryGetValue(BehaviorResilienceExecutionContextKeys.BehaviorId, out var activeBehaviorId) &&
            !string.IsNullOrWhiteSpace(activeBehaviorId)
                ? activeBehaviorId
                : BehaviorIds.Count > 0 ? BehaviorIds[0] : Id;
        var transportId = context.Properties.TryGetValue(BehaviorResilienceExecutionContextKeys.TransportId, out var activeTransportId) &&
            !string.IsNullOrWhiteSpace(activeTransportId)
                ? activeTransportId
                : null;
        var behaviorIdempotency = context.Properties.TryGetValue(BehaviorResilienceExecutionContextKeys.IdempotencyMode, out var activeBehaviorIdempotency)
            ? activeBehaviorIdempotency
            : BehaviorIdempotencyMode.Unknown;

        return exceptionClassifier.Classify(new Cephalon.Abstractions.Resilience.BehaviorResilienceExceptionContext(
            policyId: Id,
            behaviorId: behaviorId,
            transportId: transportId,
            targetedBehaviorIds: BehaviorIds,
            targetedTransportIds: TransportIds,
            exception: exception,
            behaviorIdempotency: behaviorIdempotency));
    }

    public BehaviorResilienceRuntimeDescriptor ToDescriptor(
        BehaviorCircuitBreakerRuntimeState? circuitBreakerRuntimeState = null,
        BehaviorIdempotencyMode? behaviorIdempotency = null)
    {
        var metadata = new Dictionary<string, string>(Metadata, StringComparer.OrdinalIgnoreCase);
        if (circuitBreakerRuntimeState is not null && Effective.CircuitBreaker.HasValues && Effective.CircuitBreaker.Enabled == true)
        {
            metadata["circuitState"] = circuitBreakerRuntimeState.StateKey;
            metadata["circuitStateInitialized"] = "true";
            var stateChangedAtUtc = circuitBreakerRuntimeState.LastHalfOpenedAtUtc ??
                circuitBreakerRuntimeState.LastClosedAtUtc ??
                circuitBreakerRuntimeState.LastOpenedAtUtc;
            if (stateChangedAtUtc is not null)
            {
                metadata["circuitStateChangedAtUtc"] = stateChangedAtUtc.Value.ToString("O", System.Globalization.CultureInfo.InvariantCulture);
            }

            if (circuitBreakerRuntimeState.LastOpenedAtUtc is { } lastOpenedAtUtc &&
                circuitBreakerRuntimeState.LastBreakDuration is { } lastBreakDuration &&
                string.Equals(circuitBreakerRuntimeState.StateKey, "open", StringComparison.OrdinalIgnoreCase))
            {
                var remaining = (lastOpenedAtUtc + lastBreakDuration) - DateTimeOffset.UtcNow;
                var retryAfterSeconds = remaining <= TimeSpan.Zero
                    ? 0
                    : (int)Math.Ceiling(remaining.TotalSeconds);
                metadata["circuitRetryAfterSeconds"] = retryAfterSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            if (!string.IsNullOrWhiteSpace(circuitBreakerRuntimeState.LastOpenedExceptionType))
            {
                metadata["circuitLastOpenedExceptionType"] = circuitBreakerRuntimeState.LastOpenedExceptionType;
            }
        }

        if (behaviorIdempotency.HasValue)
        {
            metadata["behaviorIdempotency"] = ToMetadataValue(behaviorIdempotency.Value);
            metadata["retryEligibilityMode"] = ResolveRetryEligibilityMode(behaviorIdempotency.Value);
        }

        return new BehaviorResilienceRuntimeDescriptor(
            Id,
            DisplayName,
            Description,
            ExecutionMode,
            Scope,
            BehaviorIds,
            TransportIds,
            Requested,
            Effective,
            metadata);
    }

    private string ResolveRetryEligibilityMode(BehaviorIdempotencyMode behaviorIdempotency)
    {
        var retryRequested = Requested.Retry.HasValues && Requested.Retry.Enabled != false;
        if (!retryRequested)
        {
            return "not-requested";
        }

        return behaviorIdempotency switch
        {
            BehaviorIdempotencyMode.Idempotent => "eligible",
            BehaviorIdempotencyMode.NonIdempotent => "ineligible",
            _ => "unknown"
        };
    }

    private static string ToMetadataValue(BehaviorIdempotencyMode behaviorIdempotency)
    {
        return behaviorIdempotency switch
        {
            BehaviorIdempotencyMode.Idempotent => "idempotent",
            BehaviorIdempotencyMode.NonIdempotent => "non-idempotent",
            _ => "unknown"
        };
    }

}
