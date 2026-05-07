using Cephalon.Abstractions.EventSourcing;
using Cephalon.EventSourcing.Hosting;
using Cephalon.EventSourcing.Neo4j.Services;
using Cephalon.EventSourcing.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Neo4j.Driver;

namespace Cephalon.EventSourcing.Neo4j.Hosting;

/// <summary>
/// Registers the Neo4j event-store provider used by Cephalon hosts.
/// </summary>
public static class Neo4jEventSourcingServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Neo4j event-store provider to the service collection.
    /// </summary>
    /// <param name="services">The service collection to extend.</param>
    /// <param name="uri">The Neo4j Bolt URI (e.g. <c>bolt://localhost:7687</c>).</param>
    /// <param name="username">The Neo4j username.</param>
    /// <param name="password">The Neo4j password.</param>
    /// <param name="eventLabel">
    /// The node label used for event nodes. Defaults to <c>Event</c>.
    /// </param>
    /// <returns>The same service collection for fluent registration.</returns>
    public static IServiceCollection AddCephalonNeo4jEventSourcing(
        this IServiceCollection services,
        string uri,
        string username,
        string password,
        string eventLabel = "Event")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(uri);
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventLabel);

        services.TryAddSingleton<IDriver>(_ =>
            GraphDatabase.Driver(uri, AuthTokens.Basic(username, password)));

        services.AddCephalonEventTypeRegistry();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IEventStoreContributor>(
            new Neo4jEventStoreContributor(uri, username, password, eventLabel)));
        services.TryAddScoped<IEventStore>(serviceProvider =>
        {
            var driver = serviceProvider.GetRequiredService<IDriver>();
            var eventTypes = serviceProvider.GetRequiredService<IEventTypeRegistry>();
            return new Neo4jEventStore(driver, eventLabel, eventTypes);
        });

        return services;
    }
}
