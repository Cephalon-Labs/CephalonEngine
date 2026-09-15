using System.Collections.Concurrent;

namespace Cephalon.AspNetCore.Transports.Streaming;

internal sealed class DirectStreamingModuleResilienceStateRegistry : IDisposable
{
    private readonly DirectStreamingModuleResilienceOptions options;
    private readonly ConcurrentDictionary<string, DirectStreamingModuleCircuitBreakerState> circuitBreakers = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, DirectStreamingModuleBulkheadState> bulkheads = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, DirectStreamingModuleTimeoutState> timeouts = new(StringComparer.OrdinalIgnoreCase);

    public DirectStreamingModuleResilienceStateRegistry(DirectStreamingModuleResilienceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        this.options = options;
    }

    public DirectStreamingModuleCircuitBreakerState GetCircuitBreaker(string transportId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transportId);

        return circuitBreakers.GetOrAdd(transportId, _ => new DirectStreamingModuleCircuitBreakerState(options));
    }

    public DirectStreamingModuleBulkheadState GetBulkhead(string transportId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transportId);

        return bulkheads.GetOrAdd(transportId, _ => new DirectStreamingModuleBulkheadState(options));
    }

    public DirectStreamingModuleTimeoutState GetTimeout(string transportId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transportId);

        return timeouts.GetOrAdd(transportId, _ => new DirectStreamingModuleTimeoutState(options));
    }

    public void Dispose()
    {
        foreach (var bulkhead in bulkheads.Values)
        {
            bulkhead.Dispose();
        }
    }
}
