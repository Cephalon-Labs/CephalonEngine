using Cephalon.Abstractions.EventSourcing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Qdrant.Client;

namespace Cephalon.EventSourcing.Qdrant.Hosting;

/// <summary>
/// Registers the Qdrant event-store provider used by Cephalon hosts.
/// </summary>
public static class QdrantEventSourcingServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Qdrant event-store provider to the service collection.
    /// </summary>
    /// <param name="services">The service collection to extend.</param>
    /// <param name="host">The Qdrant server hostname (e.g. <c>"localhost"</c>).</param>
    /// <param name="port">The Qdrant gRPC port. Defaults to <c>6334</c>.</param>
    /// <param name="collectionName">
    /// The Qdrant collection name used for event stream points. Defaults to <c>event-streams</c>.
    /// </param>
    /// <returns>The same service collection for fluent registration.</returns>
    public static IServiceCollection AddCephalonQdrantEventSourcing(
        this IServiceCollection services,
        string host,
        int port = 6334,
        string collectionName = "event-streams")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(host);
        ArgumentException.ThrowIfNullOrWhiteSpace(collectionName);

        services.TryAddSingleton<QdrantClient>(_ => new QdrantClient(host, port));

        services.TryAddSingleton<IEventStore>(serviceProvider =>
        {
            var client = serviceProvider.GetRequiredService<QdrantClient>();
            return new QdrantEventStore(client, collectionName);
        });

        return services;
    }
}
