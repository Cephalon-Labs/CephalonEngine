using Cephalon.Abstractions.EventSourcing;
using Cephalon.EventSourcing.Hosting;
using Cephalon.EventSourcing.MongoDB.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;

namespace Cephalon.EventSourcing.MongoDB.Hosting;

/// <summary>
/// Registers the MongoDB event-store provider used by Cephalon hosts.
/// </summary>
public static class MongoDbEventSourcingServiceCollectionExtensions
{
    /// <summary>
    /// Adds the MongoDB event-store provider to the service collection.
    /// </summary>
    /// <param name="services">The service collection to extend.</param>
    /// <param name="connectionString">The MongoDB connection string.</param>
    /// <param name="databaseName">The target MongoDB database name.</param>
    /// <param name="collectionName">
    /// The MongoDB collection name used for event stream documents. Defaults to <c>event_streams</c>.
    /// </param>
    /// <returns>The same service collection for fluent registration.</returns>
    public static IServiceCollection AddCephalonMongoDbEventSourcing(
        this IServiceCollection services,
        string connectionString,
        string databaseName,
        string collectionName = "event_streams")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);
        ArgumentException.ThrowIfNullOrWhiteSpace(collectionName);

        services.TryAddSingleton<IMongoClient>(_ => new MongoClient(connectionString));
        services.TryAddSingleton(serviceProvider =>
            serviceProvider.GetRequiredService<IMongoClient>().GetDatabase(databaseName));

        services.TryAddSingleton(serviceProvider =>
            serviceProvider.GetRequiredService<IMongoDatabase>().GetCollection<MongoDbEventEntry>(collectionName));

        services.AddCephalonEventTypeRegistry();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IEventStoreContributor>(
            new MongoDbEventStoreContributor(connectionString, databaseName, collectionName)));
        services.TryAddSingleton<IEventStore, MongoDbEventStore>();

        return services;
    }
}
