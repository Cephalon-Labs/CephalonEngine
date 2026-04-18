using Cephalon.Abstractions.Resilience;
using Cephalon.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Cephalon.Behaviors.Http.Hosting;

internal static class BehaviorTransportResilienceMapper
{
    internal const int JsonRpcTooManyRequestsCode = -32029;

    public static BehaviorTransportFaultDescriptor CreateRateLimitingFault(
        IServiceProvider services,
        string behaviorId,
        string transportId,
        TimeSpan? retryAfter = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(behaviorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(transportId);

        var rejection = ResolveRateLimitingRejection(services, behaviorId, transportId);
        return new BehaviorTransportFaultDescriptor(
            rejection.Message,
            rejection.Code,
            StatusCodes.Status429TooManyRequests,
            ResolveRetryAfterSeconds(retryAfter));
    }

    public static object CreateGraphqlErrorResponse(BehaviorTransportFaultDescriptor fault)
    {
        ArgumentNullException.ThrowIfNull(fault);

        return new
        {
            errors = new[] { CreateGraphqlError(fault) }
        };
    }

    public static object[] CreateGraphqlWsErrorPayload(BehaviorTransportFaultDescriptor fault)
    {
        ArgumentNullException.ThrowIfNull(fault);

        return [CreateGraphqlError(fault)];
    }

    public static object CreateStreamingErrorPayload(BehaviorTransportFaultDescriptor fault)
    {
        ArgumentNullException.ThrowIfNull(fault);

        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["error"] = fault.Message,
            ["code"] = fault.Code,
            ["statusCode"] = fault.StatusCode
        };

        if (fault.RetryAfterSeconds.HasValue)
        {
            payload["retryAfterSeconds"] = fault.RetryAfterSeconds.Value;
        }

        return payload;
    }

    public static (int Code, string Message, string? Data) CreateJsonRpcRateLimitingError(
        BehaviorTransportFaultDescriptor fault)
    {
        ArgumentNullException.ThrowIfNull(fault);

        var data = fault.RetryAfterSeconds.HasValue
            ? $"{fault.Code}: {fault.Message} Retry after {fault.RetryAfterSeconds.Value} seconds."
            : $"{fault.Code}: {fault.Message}";

        return (JsonRpcTooManyRequestsCode, "Too many requests", data);
    }

    public static int? ResolveRetryAfterSeconds(TimeSpan? retryAfter)
    {
        if (retryAfter is null)
        {
            return null;
        }

        return retryAfter.Value <= TimeSpan.Zero
            ? 0
            : (int)Math.Ceiling(retryAfter.Value.TotalSeconds);
    }

    private static object CreateGraphqlError(BehaviorTransportFaultDescriptor fault)
    {
        var extensions = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["code"] = fault.Code.ToUpperInvariant(),
            ["cephalonCode"] = fault.Code,
            ["statusCode"] = fault.StatusCode
        };

        if (fault.RetryAfterSeconds.HasValue)
        {
            extensions["retryAfterSeconds"] = fault.RetryAfterSeconds.Value;
        }

        return new
        {
            message = fault.Message,
            extensions
        };
    }

    private static BehaviorRateLimitingRejection ResolveRateLimitingRejection(
        IServiceProvider services,
        string behaviorId,
        string transportId)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(behaviorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(transportId);

        var behaviorResilienceCatalog = services.GetService(typeof(IBehaviorResilienceRuntimeCatalog))
            as IBehaviorResilienceRuntimeCatalog;
        var policy = behaviorResilienceCatalog?.Resolve(behaviorId, transportId);
        var rateLimitingEnabled = policy?.Effective.RateLimiting.Enabled == true &&
            policy.Effective.RateLimiting.HasValues;
        var bulkheadEnabled = policy?.Effective.Bulkhead.Enabled == true &&
            policy.Effective.Bulkhead.HasValues;

        return (rateLimitingEnabled, bulkheadEnabled) switch
        {
            (true, false) => new BehaviorRateLimitingRejection(
                "The request exceeded the configured Cephalon behavior execution rate limit.",
                "behavior_execution_rate_limited"),
            (false, true) => new BehaviorRateLimitingRejection(
                "The request exceeded the configured Cephalon behavior concurrency limit.",
                "behavior_execution_rejected"),
            (true, true) => new BehaviorRateLimitingRejection(
                "The request exceeded the configured Cephalon behavior execution rate or concurrency limit.",
                "behavior_execution_limited"),
            _ => new BehaviorRateLimitingRejection(
                "The request exceeded the configured Cephalon behavior execution limit.",
                "behavior_execution_limited")
        };
    }

    private sealed record BehaviorRateLimitingRejection(string Message, string Code);
}

internal sealed record BehaviorTransportFaultDescriptor(
    string Message,
    string Code,
    int StatusCode,
    int? RetryAfterSeconds);
