using System.Globalization;
using Cephalon.Abstractions.Technologies;

namespace Cephalon.AspNetCore.GraphQL.Resilience;

internal sealed class GraphQLExecutionResilienceRuntimeContributor(
    GraphQLExecutionResilienceOptions options,
    GraphQLExecutionResilienceStateRegistry stateRegistry) : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        if (!options.HasEnforcedStrategies)
        {
            return new TechnologyRuntimeSurface(
                technologyId: "graphql",
                surfaceId: "graphql-execution-resilience",
                displayName: "GraphQL Execution Resilience",
                description: "Host-enforced resilience posture for built-in GraphQL query, mutation, and subscription root-field execution.",
                entries: []);
        }

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["executionMode"] = "hotchocolate-field-middleware",
            ["policySource"] = "Engine:Resilience",
            ["scope"] = "built-in-graphql-root-operation-fields",
            ["operationTypes"] = "query,mutation,subscription",
            ["protocolEnvelope"] = "graphql-errors-extensions",
            ["wolverineRequired"] = "false",
            ["consumerCodeRequired"] = "false",
            ["timeoutEnabled"] = options.TimeoutEnabled.ToString(CultureInfo.InvariantCulture),
            ["timeoutSeconds"] = options.Timeout?.TotalSeconds.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            ["timeoutStatusCode"] = "503",
            ["timeoutCephalonCode"] = "graphql_execution_timeout",
            ["circuitBreakerEnabled"] = options.CircuitBreakerEnabled.ToString(CultureInfo.InvariantCulture),
            ["circuitBreakerFailureRatio"] = options.CircuitBreakerFailureRatio.ToString(CultureInfo.InvariantCulture),
            ["circuitBreakerMinimumThroughput"] = options.CircuitBreakerMinimumThroughput.ToString(CultureInfo.InvariantCulture),
            ["circuitBreakerSamplingDurationSeconds"] = options.CircuitBreakerSamplingDuration.TotalSeconds.ToString(CultureInfo.InvariantCulture),
            ["circuitBreakerBreakDurationSeconds"] = options.CircuitBreakerBreakDuration.TotalSeconds.ToString(CultureInfo.InvariantCulture),
            ["circuitBreakerOpenStatusCode"] = "503",
            ["circuitBreakerOpenCephalonCode"] = "graphql_circuit_breaker_open",
            ["bulkheadEnabled"] = options.BulkheadEnabled.ToString(CultureInfo.InvariantCulture),
            ["bulkheadMaxConcurrentExecutions"] = options.BulkheadMaxConcurrentExecutions.ToString(CultureInfo.InvariantCulture),
            ["bulkheadMaxQueuedActions"] = options.BulkheadMaxQueuedActions.ToString(CultureInfo.InvariantCulture),
            ["bulkheadQueueingMode"] = options.BulkheadMaxQueuedActions > 0 ? "bounded-queue" : "disabled-reject-on-entry",
            ["bulkheadRejectedStatusCode"] = "429",
            ["bulkheadRejectedCephalonCode"] = "graphql_bulkhead_rejected"
        };

        foreach (var entry in stateRegistry.CircuitBreaker.CreateMetadata())
        {
            metadata[entry.Key] = entry.Value;
        }

        foreach (var entry in stateRegistry.Bulkhead.CreateMetadata())
        {
            metadata[entry.Key] = entry.Value;
        }

        return new TechnologyRuntimeSurface(
            technologyId: "graphql",
            surfaceId: "graphql-execution-resilience",
            displayName: "GraphQL Execution Resilience",
            description: "Host-enforced timeout, circuit-breaker, and bulkhead posture for built-in GraphQL query, mutation, and subscription root-field execution.",
            entries:
            [
                new TechnologyRuntimeEntry(
                    id: "graphql-execution-resilience",
                    displayName: "GraphQL Execution Resilience",
                    description: "Applies configured Engine:Resilience timeout, circuit-breaker, and bulkhead policy to built-in GraphQL query, mutation, and subscription root fields without Wolverine or consumer field middleware.",
                    metadata: metadata)
            ]);
    }
}
