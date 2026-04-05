using Cephalon.Abstractions.EventSourcing;
using Cephalon.Abstractions.Technologies;
using Cephalon.EventSourcing.Configuration;
using Cephalon.EventSourcing.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Cephalon.EventSourcing.Services;

internal static class EventSourcingServiceRegistration
{
    public static void Register(
        IServiceCollection services,
        EventSourcingOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        services.TryAddSingleton(options);
        services.TryAddSingleton<IOptions<EventSourcingOptions>>(static serviceProvider =>
            Options.Create(serviceProvider.GetRequiredService<EventSourcingOptions>()));
        services.TryAddSingleton<EventStreamRegistry>();
        services.TryAddSingleton<IEventStoreRegistry>(static serviceProvider =>
            serviceProvider.GetRequiredService<EventStreamRegistry>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IEventStoreContributor>(static serviceProvider =>
            serviceProvider.GetRequiredService<EventStreamRegistry>()));
        services.TryAddSingleton<IEventStoreCatalog>(static serviceProvider =>
            new EventStreamCatalog(serviceProvider.GetServices<IEventStoreContributor>()));
        services.TryAddSingleton(typeof(AggregateHydrator<,>));
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, EventSourcingRuntimeContributor>());
    }
}
