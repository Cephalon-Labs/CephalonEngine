using Cephalon.Data.Qdrant.Configuration;
using Cephalon.Data.Qdrant.Modules;
using Cephalon.Engine.Composition;

namespace Cephalon.Data.Qdrant.Registration;

/// <summary>
/// Registers the Qdrant vector-store data companion pack with an <see cref="EngineBuilder" />.
/// </summary>
public static class QdrantDataEngineBuilderExtensions
{
    /// <summary>
    /// Adds the Qdrant data pack with the supplied host and optional port.
    /// </summary>
    /// <param name="builder">The engine builder to extend.</param>
    /// <param name="host">The Qdrant server hostname (e.g. <c>"localhost"</c>).</param>
    /// <param name="port">The Qdrant gRPC port. Defaults to <c>6334</c>.</param>
    /// <param name="configure">
    /// An optional callback that configures the host-owned Qdrant pack options.
    /// </param>
    /// <returns>The same engine builder for fluent composition.</returns>
    /// <remarks>
    /// Register outbox or inbox support by configuring <see cref="QdrantDataOptions.RegisterOutbox" />
    /// or <see cref="QdrantDataOptions.RegisterInbox" /> in the <paramref name="configure" /> callback.
    /// </remarks>
    public static EngineBuilder AddQdrantData(
        this EngineBuilder builder,
        string host,
        int port = 6334,
        Action<QdrantDataOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(host);

        var options = new QdrantDataOptions
        {
            Host = host,
            Port = port
        };

        configure?.Invoke(options);

        builder.AddModule(new QdrantDataModule(options));
        return builder;
    }
}
