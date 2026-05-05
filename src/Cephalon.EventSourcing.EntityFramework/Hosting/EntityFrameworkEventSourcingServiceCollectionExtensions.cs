using Cephalon.Abstractions.EventSourcing;
using Cephalon.EventSourcing.Hosting;
using Cephalon.EventSourcing.EntityFramework.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cephalon.EventSourcing.EntityFramework.Hosting;

/// <summary>
/// Registers the Entity Framework event-store provider used by Cephalon hosts.
/// </summary>
public static class EntityFrameworkEventSourcingServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Entity Framework event-store provider to the service collection.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type that persists event rows.</typeparam>
    /// <param name="services">The service collection to extend.</param>
    /// <returns>The same service collection for fluent registration.</returns>
    public static IServiceCollection AddCephalonEntityFrameworkEventSourcing<TContext>(
        this IServiceCollection services)
        where TContext : DbContext, IEntityFrameworkEventContext
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddCephalonEventTypeRegistry();
        services.TryAddScoped<IEventStore, EntityFrameworkEventStore<TContext>>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IEventStoreContributor, EntityFrameworkEventStoreContributor<TContext>>());
        return services;
    }
}
