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
        return AddOpenSearchData(builder, options);
    }

    /// <summary>
    /// Adds the OpenSearch data pack using an options callback that can bind from configuration.
    /// </summary>
    /// <param name="builder">The engine builder to extend.</param>
    /// <param name="configure">
    /// The callback that configures the host-owned OpenSearch pack options, including
    /// <see cref="OpenSearchDataOptions.UriName" /> and
    /// <see cref="OpenSearchDataOptions.Uri" />.
    /// </param>
    /// <returns>The same engine builder for fluent composition.</returns>
    /// <remarks>
    /// Use either <see cref="OpenSearchDataOptions.UriName" /> or
    /// <see cref="OpenSearchDataOptions.Uri" />. Leaving both unset falls back to
    /// <c>http://localhost:9200</c>.
    /// </remarks>
    public static EngineBuilder AddOpenSearchData(
        this EngineBuilder builder,
        Action<OpenSearchDataOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new OpenSearchDataOptions();
        configure(options);

        return AddOpenSearchData(builder, options);
    }

    private static EngineBuilder AddOpenSearchData(
        EngineBuilder builder,
        OpenSearchDataOptions options)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(options);

        builder.AddModule(new OpenSearchDataModule(options));
        return builder;
    }
}
