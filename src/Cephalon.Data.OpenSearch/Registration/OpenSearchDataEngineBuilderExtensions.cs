using Cephalon.Data.OpenSearch.Configuration;
using Cephalon.Data.OpenSearch.Modules;
using Cephalon.Engine.Composition;

namespace Cephalon.Data.OpenSearch.Registration;

/// <summary>Registers the OpenSearch data companion pack with an <see cref="EngineBuilder"/>.</summary>
public static class OpenSearchDataEngineBuilderExtensions
{
    /// <summary>Adds the OpenSearch data pack with the supplied node URI.</summary>
    /// <param name="builder">The engine builder to extend.</param>
    /// <param name="uri">The OpenSearch node URI.</param>
    /// <param name="configure">An optional callback that configures the host-owned OpenSearch pack options.</param>
    /// <returns>The same engine builder for fluent composition.</returns>
    public static EngineBuilder AddOpenSearchData(
        this EngineBuilder builder,
        string uri,
        Action<OpenSearchDataOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(uri);
        var options = new OpenSearchDataOptions { Uri = uri };
        configure?.Invoke(options);
        builder.AddModule(new OpenSearchDataModule(options));
        return builder;
    }
}
