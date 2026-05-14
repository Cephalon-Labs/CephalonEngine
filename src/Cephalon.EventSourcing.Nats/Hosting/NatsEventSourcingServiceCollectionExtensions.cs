using Cephalon.Abstractions.EventSourcing;
using Cephalon.EventSourcing.Hosting;
using Cephalon.EventSourcing.Nats.Services;
using Cephalon.EventSourcing.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NATS.Client.Core;

namespace Cephalon.EventSourcing.Nats.Hosting;

/// <summary>
/// Registers the NATS JetStream KV event-store provider used by Cephalon hosts.
/// </summary>
public static class NatsEventSourcingServiceCollectionExtensions
{
    /// <summary>
    /// Adds the NATS event-store provider to the service collection.
    /// </summary>
    /// <param name="services">The service collection to extend.</param>
    /// <param name="url">The NATS server URL (e.g. <c>"nats://localhost:4222"</c>).</param>
    /// <param name="bucketName">
    /// The JetStream KV bucket name used for event stream entries. Defaults to <c>cephalon-events</c>.
    /// </param>
    /// <returns>The same service collection for fluent registration.</returns>
    /// <remarks>
    /// <see cref="NatsConnection" /> does not connect on construction — the connection is deferred
    /// to the first operation. DI resolution does not require a live NATS server.
    /// </remarks>
    public static IServiceCollection AddCephalonNatsEventSourcing(
        this IServiceCollection services,
        string url,
        string bucketName = "cephalon-events")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        ArgumentException.ThrowIfNullOrWhiteSpace(bucketName);

        services.TryAddSingleton<INatsConnection>(_ => new NatsConnection(new NatsOpts { Url = url }));

        services.AddCephalonEventTypeRegistry();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IEventStoreContributor>(
            new NatsEventStoreContributor(url, bucketName)));
        services.AddScoped<ISnapshotStore>(serviceProvider =>
            new NatsSnapshotStore(
                serviceProvider.GetRequiredService<INatsConnection>(),
                bucketName));
        services.TryAddSingleton<IEventStore>(serviceProvider =>
        {
            var nats = serviceProvider.GetRequiredService<INatsConnection>();
            var eventTypes = serviceProvider.GetRequiredService<IEventTypeRegistry>();
            return new NatsEventStore(nats, bucketName, eventTypes);
        });

        return services;
    }
}
