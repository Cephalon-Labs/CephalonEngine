using System.Collections.Concurrent;

namespace Cephalon.AspNetCore.GraphQL.Resilience;

internal sealed class GraphQLExecutionResilienceStateRegistry : IDisposable
{
    private const string RuntimeKey = "graphql";
    private readonly GraphQLExecutionResilienceOptions options;
    private readonly ConcurrentDictionary<string, GraphQLExecutionCircuitBreakerState> circuitBreakers = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, GraphQLExecutionBulkheadState> bulkheads = new(StringComparer.OrdinalIgnoreCase);

    public GraphQLExecutionResilienceStateRegistry(GraphQLExecutionResilienceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        this.options = options;
    }

    public GraphQLExecutionCircuitBreakerState CircuitBreaker => GetCircuitBreaker(RuntimeKey);

    public GraphQLExecutionBulkheadState Bulkhead => GetBulkhead(RuntimeKey);

    public void Dispose()
    {
        foreach (var bulkhead in bulkheads.Values)
        {
            bulkhead.Dispose();
        }
    }

    private GraphQLExecutionCircuitBreakerState GetCircuitBreaker(string key)
    {
        return circuitBreakers.GetOrAdd(key, _ => new GraphQLExecutionCircuitBreakerState(options));
    }

    private GraphQLExecutionBulkheadState GetBulkhead(string key)
    {
        return bulkheads.GetOrAdd(key, _ => new GraphQLExecutionBulkheadState(options));
    }
}
