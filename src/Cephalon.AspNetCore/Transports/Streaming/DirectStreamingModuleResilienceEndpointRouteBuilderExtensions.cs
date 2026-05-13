using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.AspNetCore.Transports.Streaming;

internal static class DirectStreamingModuleResilienceEndpointRouteBuilderExtensions
{
    public static RouteGroupBuilder ApplyCephalonDirectStreamingModuleResilience(
        this RouteGroupBuilder builder,
        IServiceProvider services,
        string transportId,
        DirectStreamingModuleTransportKind transportKind)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(transportId);

        var options = services.GetRequiredService<DirectStreamingModuleResilienceOptions>();
        if (!options.HasEnforcedStrategies)
        {
            return builder;
        }

        var stateRegistry = services.GetRequiredService<DirectStreamingModuleResilienceStateRegistry>();
        builder.AddEndpointFilter(new DirectStreamingModuleResilienceFilter(
            transportKind,
            options,
            stateRegistry.GetCircuitBreaker(transportId),
            stateRegistry.GetBulkhead(transportId),
            stateRegistry.GetTimeout(transportId)));

        return builder;
    }
}
