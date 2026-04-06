using Cephalon.Data.Elasticsearch.Configuration;
using Cephalon.Data.Elasticsearch.Modules;
using Cephalon.Engine.Composition;

namespace Cephalon.Data.Elasticsearch.Registration;

/// <summary>Registers the Elasticsearch data companion pack with an <see cref="EngineBuilder"/>.</summary>
public static class ElasticsearchDataEngineBuilderExtensions
{
    /// <summary>Adds the Elasticsearch data pack with the supplied node URI.</summary>
    /// <param name="builder">The engine builder to extend.</param>
    /// <param name="uri">The Elasticsearch node URI.</param>
    /// <param name="configure">An optional callback that configures the host-owned Elasticsearch pack options.</param>
    /// <returns>The same engine builder for fluent composition.</returns>
    public static EngineBuilder AddElasticsearchData(
        this EngineBuilder builder,
        string uri,
        Action<ElasticsearchDataOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(uri);
        var options = new ElasticsearchDataOptions { Uri = uri };
        configure?.Invoke(options);
        builder.AddModule(new ElasticsearchDataModule(options));
        return builder;
    }
}
