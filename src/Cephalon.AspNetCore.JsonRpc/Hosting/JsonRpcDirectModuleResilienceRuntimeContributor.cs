using System.Globalization;
using Cephalon.Abstractions.Technologies;

namespace Cephalon.AspNetCore.JsonRpc.Hosting;

internal sealed class JsonRpcDirectModuleResilienceRuntimeContributor(
    JsonRpcDirectModuleResilienceOptions options,
    JsonRpcDirectModuleCircuitBreakerState circuitBreakerState,
    JsonRpcDirectModuleBulkheadState bulkheadState) : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        if (!options.HasEnforcedStrategies)
        {
            return new TechnologyRuntimeSurface(
                technologyId: "json-rpc",
                surfaceId: "json-rpc-direct-module-resilience",
                displayName: "JSON-RPC Direct Module Resilience",
                description: "Host-enforced resilience posture for direct JSON-RPC module endpoints.",
                entries: []);
        }

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["executionMode"] = "aspnetcore-jsonrpc-endpoint-filter",
            ["policySource"] = "Engine:Resilience",
            ["scope"] = "direct-json-rpc-module-endpoints",
            ["wolverineRequired"] = "false",
            ["consumerCodeRequired"] = "false",
            ["timeoutEnabled"] = options.TimeoutEnabled.ToString(CultureInfo.InvariantCulture),
            ["timeoutSeconds"] = options.Timeout?.TotalSeconds.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            ["timeoutStatusCode"] = "503",
            ["timeoutJsonRpcErrorCode"] = "-32053",
            ["timeoutCephalonCode"] = "jsonrpc_execution_timeout",
            ["circuitBreakerEnabled"] = options.CircuitBreakerEnabled.ToString(CultureInfo.InvariantCulture),
            ["circuitBreakerFailureRatio"] = options.CircuitBreakerFailureRatio.ToString(CultureInfo.InvariantCulture),
            ["circuitBreakerMinimumThroughput"] = options.CircuitBreakerMinimumThroughput.ToString(CultureInfo.InvariantCulture),
            ["circuitBreakerSamplingDurationSeconds"] = options.CircuitBreakerSamplingDuration.TotalSeconds.ToString(CultureInfo.InvariantCulture),
            ["circuitBreakerBreakDurationSeconds"] = options.CircuitBreakerBreakDuration.TotalSeconds.ToString(CultureInfo.InvariantCulture),
            ["circuitBreakerOpenStatusCode"] = "503",
            ["circuitBreakerOpenJsonRpcErrorCode"] = "-32053",
            ["circuitBreakerOpenCephalonCode"] = "jsonrpc_circuit_breaker_open",
            ["bulkheadEnabled"] = options.BulkheadEnabled.ToString(CultureInfo.InvariantCulture),
            ["bulkheadMaxConcurrentExecutions"] = options.BulkheadMaxConcurrentExecutions.ToString(CultureInfo.InvariantCulture),
            ["bulkheadMaxQueuedActions"] = options.BulkheadMaxQueuedActions.ToString(CultureInfo.InvariantCulture),
            ["bulkheadQueueingMode"] = options.BulkheadMaxQueuedActions > 0 ? "bounded-queue" : "disabled-reject-on-entry",
            ["bulkheadRejectedStatusCode"] = "429",
            ["bulkheadRejectedJsonRpcErrorCode"] = "-32029",
            ["bulkheadRejectedCephalonCode"] = "jsonrpc_bulkhead_rejected"
        };

        foreach (var entry in circuitBreakerState.CreateMetadata())
        {
            metadata[entry.Key] = entry.Value;
        }

        foreach (var entry in bulkheadState.CreateMetadata())
        {
            metadata[entry.Key] = entry.Value;
        }

        return new TechnologyRuntimeSurface(
            technologyId: "json-rpc",
            surfaceId: "json-rpc-direct-module-resilience",
            displayName: "JSON-RPC Direct Module Resilience",
            description: "Host-enforced timeout, circuit-breaker, and bulkhead posture for direct JSON-RPC module endpoints.",
            entries:
            [
                new TechnologyRuntimeEntry(
                    id: "json-rpc-direct-module-resilience",
                    displayName: "JSON-RPC Direct Module Resilience",
                    description: "Applies configured Engine:Resilience timeout, circuit-breaker, and bulkhead policy to direct JSON-RPC module calls without Wolverine or consumer endpoint code.",
                    metadata: metadata)
            ]);
    }
}
