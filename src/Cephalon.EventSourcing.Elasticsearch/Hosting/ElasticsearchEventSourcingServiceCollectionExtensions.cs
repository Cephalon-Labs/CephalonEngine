using Cephalon.Abstractions.EventSourcing;
using Cephalon.EventSourcing.Hosting;
using Cephalon.EventSourcing.Services;
using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cephalon.EventSourcing.Elasticsearch.Hosting;

/// <summary>Extension methods for registering the Elasticsearch event store.</summary>
public static class ElasticsearchEventSourcingServiceCollectionExtensions
{
    /// <summary>Registers <see cref="ElasticsearchEventStore"/> as <see cref="IEventStore"/> in the service collection.</summary>
    /// <param name="services">The service collection to extend.</param>
    /// <param name="uri">The Elasticsearch node URI.</param>
    /// <param name="indexName">The target Elasticsearch index name for event stream documents.</param>
    /// <returns>The same service collection for fluent composition.</returns>
    public static IServiceCollection AddCephalonElasticsearchEventSourcing(
        this IServiceCollection services,
        string uri,
        string indexName = "event-streams")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(uri);
        ArgumentException.ThrowIfNullOrWhiteSpace(indexName);

        services.TryAddSingleton<ElasticsearchClient>(_ =>
            new ElasticsearchClient(new ElasticsearchClientSettings(new Uri(uri))));
        services.AddCephalonEventTypeRegistry();
        services.TryAddSingleton<IEventStore>(sp =>
            new ElasticsearchEventStore(
                sp.GetRequiredService<ElasticsearchClient>(),
                sp.GetRequiredService<IEventTypeRegistry>(),
                indexName));
        return services;
    }
}
