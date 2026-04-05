using Cephalon.EventSourcing.Configuration;
using Cephalon.EventSourcing.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cephalon.EventSourcing.Hosting;

/// <summary>
/// Registers the runtime-neutral event-sourcing services used by Cephalon hosts.
/// </summary>
public static class EventSourcingServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Cephalon event-sourcing baseline services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to extend.</param>
    /// <param name="configure">An optional callback that configures the host-owned event-sourcing options.</param>
    /// <returns>The same service collection for fluent registration.</returns>
    public static IServiceCollection AddCephalonEventSourcing(
        this IServiceCollection services,
        Action<EventSourcingOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new EventSourcingOptions();
        configure?.Invoke(options);

        EventSourcingServiceRegistration.Register(services, options);
        return services;
    }
}
