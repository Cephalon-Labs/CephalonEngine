using System.Globalization;
using Cephalon.Abstractions.Technologies;

namespace Cephalon.AspNetCore.Transports.Streaming;

internal abstract class DirectStreamingModuleResilienceRuntimeContributor(
    string technologyId,
    string surfaceId,
    string entryDisplayName,
    string transportId,
    string scope,
    string protocolEnvelope,
    string codePrefix,
    DirectStreamingModuleResilienceOptions options,
    DirectStreamingModuleResilienceStateRegistry stateRegistry) : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        if (!options.HasEnforcedStrategies)
        {
            return new TechnologyRuntimeSurface(
                technologyId,
                surfaceId,
                entryDisplayName,
                $"Host-enforced resilience posture for direct {entryDisplayName} endpoints.",
                entries: []);
        }

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["executionMode"] = "aspnetcore-streaming-endpoint-filter",
            ["policySource"] = "Engine:Resilience",
            ["scope"] = scope,
            ["protocolEnvelope"] = protocolEnvelope,
            ["wolverineRequired"] = "false",
            ["consumerCodeRequired"] = "false",
            ["timeoutEnabled"] = options.TimeoutEnabled.ToString(CultureInfo.InvariantCulture),
            ["timeoutSeconds"] = options.Timeout?.TotalSeconds.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            ["timeoutStatusCode"] = "503",
            ["timeoutCephalonCode"] = $"{codePrefix}_execution_timeout",
            ["circuitBreakerEnabled"] = options.CircuitBreakerEnabled.ToString(CultureInfo.InvariantCulture),
            ["circuitBreakerFailureRatio"] = options.CircuitBreakerFailureRatio.ToString(CultureInfo.InvariantCulture),
            ["circuitBreakerMinimumThroughput"] = options.CircuitBreakerMinimumThroughput.ToString(CultureInfo.InvariantCulture),
            ["circuitBreakerSamplingDurationSeconds"] = options.CircuitBreakerSamplingDuration.TotalSeconds.ToString(CultureInfo.InvariantCulture),
            ["circuitBreakerBreakDurationSeconds"] = options.CircuitBreakerBreakDuration.TotalSeconds.ToString(CultureInfo.InvariantCulture),
            ["circuitBreakerOpenStatusCode"] = "503",
            ["circuitBreakerOpenCephalonCode"] = $"{codePrefix}_circuit_breaker_open",
            ["bulkheadEnabled"] = options.BulkheadEnabled.ToString(CultureInfo.InvariantCulture),
            ["bulkheadMaxConcurrentExecutions"] = options.BulkheadMaxConcurrentExecutions.ToString(CultureInfo.InvariantCulture),
            ["bulkheadMaxQueuedActions"] = options.BulkheadMaxQueuedActions.ToString(CultureInfo.InvariantCulture),
            ["bulkheadQueueingMode"] = options.BulkheadMaxQueuedActions > 0 ? "bounded-queue" : "disabled-reject-on-entry",
            ["bulkheadRejectedStatusCode"] = "429",
            ["bulkheadRejectedCephalonCode"] = $"{codePrefix}_bulkhead_rejected"
        };

        foreach (var entry in stateRegistry.GetCircuitBreaker(transportId).CreateMetadata())
        {
            metadata[entry.Key] = entry.Value;
        }

        foreach (var entry in stateRegistry.GetBulkhead(transportId).CreateMetadata())
        {
            metadata[entry.Key] = entry.Value;
        }

        foreach (var entry in stateRegistry.GetTimeout(transportId).CreateMetadata())
        {
            metadata[entry.Key] = entry.Value;
        }

        return new TechnologyRuntimeSurface(
            technologyId,
            surfaceId,
            entryDisplayName,
            $"Host-enforced timeout, circuit-breaker, and bulkhead posture for direct {entryDisplayName} endpoints.",
            entries:
            [
                new TechnologyRuntimeEntry(
                    id: surfaceId,
                    displayName: entryDisplayName,
                    description: $"Applies configured Engine:Resilience timeout, circuit-breaker, and bulkhead policy to direct {entryDisplayName} endpoints without Wolverine or consumer endpoint code.",
                    metadata: metadata)
            ]);
    }
}

internal sealed class ServerSentEventsDirectModuleResilienceRuntimeContributor(
    DirectStreamingModuleResilienceOptions options,
    DirectStreamingModuleResilienceStateRegistry stateRegistry) : DirectStreamingModuleResilienceRuntimeContributor(
        technologyId: "server-sent-events",
        surfaceId: "sse-direct-module-resilience",
        entryDisplayName: "SSE Direct Module Resilience",
        transportId: "server-sent-events",
        scope: "direct-sse-module-endpoints",
        protocolEnvelope: "sse-error-event",
        codePrefix: "sse",
        options,
        stateRegistry);

internal sealed class WebSocketDirectModuleResilienceRuntimeContributor(
    DirectStreamingModuleResilienceOptions options,
    DirectStreamingModuleResilienceStateRegistry stateRegistry) : DirectStreamingModuleResilienceRuntimeContributor(
        technologyId: "websocket",
        surfaceId: "websocket-direct-module-resilience",
        entryDisplayName: "WebSocket Direct Module Resilience",
        transportId: "websocket",
        scope: "direct-websocket-module-endpoints",
        protocolEnvelope: "websocket-error-frame",
        codePrefix: "websocket",
        options,
        stateRegistry);
