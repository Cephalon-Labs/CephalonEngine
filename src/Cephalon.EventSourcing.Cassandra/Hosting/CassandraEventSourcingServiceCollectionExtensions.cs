using Cassandra;
using Cephalon.Abstractions.EventSourcing;
using Cephalon.EventSourcing.Hosting;
using Cephalon.EventSourcing.Cassandra.Services;
using Cephalon.EventSourcing.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cephalon.EventSourcing.Cassandra.Hosting;

/// <summary>
/// Registers the Cassandra event-store provider used by Cephalon hosts.
/// </summary>
public static class CassandraEventSourcingServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Cassandra event-store provider to the service collection.
    /// </summary>
    /// <param name="services">The service collection to extend.</param>
    /// <param name="contactPoints">
    /// One or more Cassandra contact-point host addresses, separated by commas
    /// (e.g. <c>"localhost"</c> or <c>"node1,node2,node3"</c>).
    /// </param>
    /// <param name="keyspace">The Cassandra keyspace that contains the event-streams table.</param>
    /// <param name="tableName">
    /// The Cassandra table name used for event stream rows. Defaults to <c>cephalon_event_streams</c>.
    /// </param>
    /// <returns>The same service collection for fluent registration.</returns>
    public static IServiceCollection AddCephalonCassandraEventSourcing(
        this IServiceCollection services,
        string contactPoints,
        string keyspace,
        string tableName = "cephalon_event_streams")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(contactPoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(keyspace);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);

        var hosts = contactPoints
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        services.AddCephalonEventTypeRegistry();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IEventStoreContributor>(
            new CassandraEventStoreContributor(hosts, keyspace, tableName)));
        services.TryAddSingleton<ICluster>(_ =>
            Cluster.Builder()
                .AddContactPoints(hosts)
                .WithPort(9042)
                .Build());

        services.TryAddSingleton<IEventStore>(serviceProvider =>
        {
            var cluster = serviceProvider.GetRequiredService<ICluster>();
            var eventTypes = serviceProvider.GetRequiredService<IEventTypeRegistry>();
            return new CassandraEventStore(cluster, keyspace, tableName, eventTypes);
        });

        return services;
    }
}
