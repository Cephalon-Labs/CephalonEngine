using Cephalon.Abstractions.EventSourcing;
using Cephalon.EventSourcing.Hosting;
using Cephalon.EventSourcing.Redis.Services;
using Cephalon.EventSourcing.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace Cephalon.EventSourcing.Redis.Hosting;

/// <summary>
/// Registers the Redis Streams event-store provider used by Cephalon hosts.
/// </summary>
public static class RedisEventSourcingServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Redis Streams event-store provider to the service collection.
    /// </summary>
    /// <param name="services">The service collection to extend.</param>
    /// <param name="configuration">
    /// The StackExchange.Redis connection string or configuration (e.g. <c>"localhost:6379"</c>).
    /// </param>
    /// <param name="keyPrefix">
    /// The key prefix applied to all stream keys. Defaults to <c>"cephalon:"</c>.
    /// Stream keys follow the pattern <c>{keyPrefix}stream:{streamId}</c>.
    /// </param>
    /// <returns>The same service collection for fluent registration.</returns>
    public static IServiceCollection AddCephalonRedisEventSourcing(
        this IServiceCollection services,
        string configuration,
        string keyPrefix = "cephalon:")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(keyPrefix);

        services.TryAddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(configuration));

        services.AddCephalonEventTypeRegistry();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IEventStoreContributor>(
            new RedisEventStoreContributor(configuration, keyPrefix)));
        services.TryAddSingleton<IEventStore>(serviceProvider =>
        {
            var multiplexer = serviceProvider.GetRequiredService<IConnectionMultiplexer>();
            var eventTypes = serviceProvider.GetRequiredService<IEventTypeRegistry>();
            return new RedisEventStore(multiplexer, eventTypes, keyPrefix);
        });

        return services;
    }
}
