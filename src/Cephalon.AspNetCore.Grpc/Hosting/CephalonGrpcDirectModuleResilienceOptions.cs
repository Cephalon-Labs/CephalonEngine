using Cephalon.Abstractions.AppModel;
using Cephalon.Engine.Manifest;

namespace Cephalon.AspNetCore.Grpc.Hosting;

internal sealed class CephalonGrpcDirectModuleResilienceOptions
{
    private const int DefaultTotalTimeoutSeconds = 30;
    private const decimal DefaultCircuitBreakerFailureRatio = 0.1m;
    private const int DefaultCircuitBreakerMinimumThroughput = 100;
    private const int DefaultCircuitBreakerSamplingDurationSeconds = 30;
    private const int DefaultCircuitBreakerBreakDurationSeconds = 5;

    private CephalonGrpcDirectModuleResilienceOptions(
        bool timeoutEnabled,
        TimeSpan? timeout,
        bool circuitBreakerEnabled,
        decimal circuitBreakerFailureRatio,
        int circuitBreakerMinimumThroughput,
        TimeSpan circuitBreakerSamplingDuration,
        TimeSpan circuitBreakerBreakDuration)
    {
        TimeoutEnabled = timeoutEnabled;
        Timeout = timeout;
        CircuitBreakerEnabled = circuitBreakerEnabled;
        CircuitBreakerFailureRatio = circuitBreakerFailureRatio;
        CircuitBreakerMinimumThroughput = circuitBreakerMinimumThroughput;
        CircuitBreakerSamplingDuration = circuitBreakerSamplingDuration;
        CircuitBreakerBreakDuration = circuitBreakerBreakDuration;
    }

    public static CephalonGrpcDirectModuleResilienceOptions Empty { get; } = new(
        timeoutEnabled: false,
        timeout: null,
        circuitBreakerEnabled: false,
        circuitBreakerFailureRatio: DefaultCircuitBreakerFailureRatio,
        circuitBreakerMinimumThroughput: DefaultCircuitBreakerMinimumThroughput,
        circuitBreakerSamplingDuration: TimeSpan.FromSeconds(DefaultCircuitBreakerSamplingDurationSeconds),
        circuitBreakerBreakDuration: TimeSpan.FromSeconds(DefaultCircuitBreakerBreakDurationSeconds));

    public bool TimeoutEnabled { get; }

    public TimeSpan? Timeout { get; }

    public bool CircuitBreakerEnabled { get; }

    public decimal CircuitBreakerFailureRatio { get; }

    public int CircuitBreakerMinimumThroughput { get; }

    public TimeSpan CircuitBreakerSamplingDuration { get; }

    public TimeSpan CircuitBreakerBreakDuration { get; }

    public bool HasEnforcedStrategies => TimeoutEnabled || CircuitBreakerEnabled;

    public static CephalonGrpcDirectModuleResilienceOptions FromManifest(RuntimeManifest? manifest)
    {
        if (manifest is null)
        {
            return Empty;
        }

        var timeout = ResolveTimeout(manifest.AppProfile.Resilience.Timeout);
        var circuitBreaker = ResolveCircuitBreaker(manifest.AppProfile.Resilience.CircuitBreaker);

        return new CephalonGrpcDirectModuleResilienceOptions(
            timeout.Enabled,
            timeout.Timeout,
            circuitBreaker.Enabled,
            circuitBreaker.FailureRatio,
            circuitBreaker.MinimumThroughput,
            circuitBreaker.SamplingDuration,
            circuitBreaker.BreakDuration);
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
}
