using Cephalon.Abstractions.EventSourcing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cephalon.EventSourcing.ClickHouse.Hosting;

/// <summary>
/// Registers the ClickHouse event-store provider used by Cephalon hosts.
/// </summary>
public static class ClickHouseEventSourcingServiceCollectionExtensions
{
    /// <summary>
    /// Adds the ClickHouse event-store provider to the service collection.
    /// </summary>
    /// <param name="services">The service collection to extend.</param>
    /// <param name="host">The ClickHouse host address (e.g. <c>"localhost"</c>).</param>
    /// <param name="database">The ClickHouse database that contains the event-streams table.</param>
    /// <param name="tableName">
    /// The ClickHouse table name used for event stream rows. Defaults to <c>cephalon_event_streams</c>.
    /// </param>
    /// <param name="username">The ClickHouse username. Defaults to <c>"default"</c>.</param>
    /// <param name="password">The ClickHouse password. Defaults to empty.</param>
    /// <returns>The same service collection for fluent registration.</returns>
    /// <remarks>
    /// <see cref="IEventStore" /> is registered using <c>TryAdd</c> semantics — a host that already
    /// registered a shared <see cref="IEventStore" /> keeps its own instance.
    /// <para>
    /// <strong>Connection behaviour</strong>: A new <c>ClickHouseConnection</c> is created per operation.
    /// No long-lived connection object is held. Service resolution is safe without a live ClickHouse server —
    /// connections are only opened when operations are actually invoked.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddCephalonClickHouseEventSourcing(
        this IServiceCollection services,
        string host,
        string database,
        string tableName = "cephalon_event_streams",
        string username = "default",
        string password = "")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(host);
        ArgumentException.ThrowIfNullOrWhiteSpace(database);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);

        var connectionString = $"Host={host};Port=8123;Database={database};Username={username};Password={password}";

        services.TryAddSingleton<IEventStore>(_ => new ClickHouseEventStore(connectionString, tableName));

        return services;
    }
}
