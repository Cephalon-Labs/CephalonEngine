using System.Globalization;
using Cephalon.Abstractions.Technologies;

namespace Cephalon.AspNetCore.Grpc.Hosting;

internal sealed class CephalonGrpcDirectModuleResilienceRuntimeContributor(
    CephalonGrpcDirectModuleResilienceOptions options,
    CephalonGrpcDirectModuleCircuitBreakerState circuitBreakerState) : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        if (!options.HasEnforcedStrategies)
        {
            return new TechnologyRuntimeSurface(
                technologyId: "grpc",
                surfaceId: "grpc-direct-module-resilience",
                displayName: "gRPC Direct Module Resilience",
                description: "Host-enforced resilience posture for direct gRPC module endpoints.",
                entries: []);
        }

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["executionMode"] = "aspnetcore-grpc-interceptor",
            ["policySource"] = "Engine:Resilience",
            ["scope"] = "direct-grpc-module-endpoints",
            ["wolverineRequired"] = "false",
            ["consumerCodeRequired"] = "false",
            ["timeoutEnabled"] = options.TimeoutEnabled.ToString(CultureInfo.InvariantCulture),
            ["timeoutSeconds"] = options.Timeout?.TotalSeconds.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            ["timeoutStatusCode"] = "DeadlineExceeded",
            ["circuitBreakerEnabled"] = options.CircuitBreakerEnabled.ToString(CultureInfo.InvariantCulture),
            ["circuitBreakerFailureRatio"] = options.CircuitBreakerFailureRatio.ToString(CultureInfo.InvariantCulture),
            ["circuitBreakerMinimumThroughput"] = options.CircuitBreakerMinimumThroughput.ToString(CultureInfo.InvariantCulture),
            ["circuitBreakerSamplingDurationSeconds"] = options.CircuitBreakerSamplingDuration.TotalSeconds.ToString(CultureInfo.InvariantCulture),
            ["circuitBreakerBreakDurationSeconds"] = options.CircuitBreakerBreakDuration.TotalSeconds.ToString(CultureInfo.InvariantCulture),
            ["circuitBreakerOpenStatusCode"] = "Unavailable"
        };

        foreach (var entry in circuitBreakerState.CreateMetadata())
        {
            metadata[entry.Key] = entry.Value;
        }

        return new TechnologyRuntimeSurface(
            technologyId: "grpc",
            surfaceId: "grpc-direct-module-resilience",
            displayName: "gRPC Direct Module Resilience",
            description: "Host-enforced timeout and circuit-breaker posture for direct gRPC module endpoints.",
            entries:
            [
                new TechnologyRuntimeEntry(
                    id: "grpc-direct-module-resilience",
                    displayName: "gRPC Direct Module Resilience",
                    description: "Applies configured Engine:Resilience timeout and circuit-breaker policy to direct gRPC module calls without Wolverine or consumer interceptor code.",
                    metadata: metadata)
            ]);
    }
}
