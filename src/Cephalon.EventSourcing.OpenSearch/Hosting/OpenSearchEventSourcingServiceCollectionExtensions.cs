using Cephalon.Abstractions.EventSourcing;
using Cephalon.EventSourcing.Hosting;
using Cephalon.EventSourcing.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenSearch.Client;

namespace Cephalon.EventSourcing.OpenSearch.Hosting;

/// <summary>Extension methods for registering the OpenSearch event store.</summary>
public static class OpenSearchEventSourcingServiceCollectionExtensions
{
    /// <summary>Registers <see cref="OpenSearchEventStore"/> as <see cref="IEventStore"/> in the service collection.</summary>
    /// <param name="services">The service collection to extend.</param>
    /// <param name="uri">The OpenSearch node URI.</param>
    /// <param name="indexName">The target OpenSearch index name for event stream documents.</param>
    /// <returns>The same service collection for fluent composition.</returns>
    public static IServiceCollection AddCephalonOpenSearchEventSourcing(
        this IServiceCollection services,
        string uri,
        string indexName = "event-streams")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(uri);
        ArgumentException.ThrowIfNullOrWhiteSpace(indexName);

        services.TryAddSingleton<OpenSearchClient>(_ =>
            new OpenSearchClient(new ConnectionSettings(new Uri(uri))));
        services.AddCephalonEventTypeRegistry();
        services.TryAddSingleton<IEventStore>(sp =>
            new OpenSearchEventStore(
                sp.GetRequiredService<OpenSearchClient>(),
                sp.GetRequiredService<IEventTypeRegistry>(),
                indexName));
        return services;
    }
}
