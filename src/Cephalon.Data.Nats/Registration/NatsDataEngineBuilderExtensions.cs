using Cephalon.Data.Nats.Configuration;
using Cephalon.Data.Nats.Modules;
using Cephalon.Engine.Composition;

namespace Cephalon.Data.Nats.Registration;

/// <summary>
/// Registers the NATS JetStream ledger-store data companion pack with an <see cref="EngineBuilder" />.
/// </summary>
public static class NatsDataEngineBuilderExtensions
{
    /// <summary>
    /// Adds the NATS data pack with the supplied server URL.
    /// </summary>
    /// <param name="builder">The engine builder to extend.</param>
    /// <param name="uri">The NATS server URI (e.g. <c>"nats://localhost:4222"</c>).</param>
    /// <param name="configure">
    /// An optional callback that configures the host-owned NATS pack options.
    /// </param>
    /// <returns>The same engine builder for fluent composition.</returns>
    /// <remarks>
    /// Register outbox or inbox support by configuring <see cref="NatsDataOptions.RegisterOutbox" />
    /// or <see cref="NatsDataOptions.RegisterInbox" /> in the <paramref name="configure" /> callback.
    /// <para>
    /// <see cref="NATS.Client.Core.NatsConnection" /> does not connect on construction — the connection
    /// is deferred to the first operation. DI resolution does not require a live NATS server.
    /// </para>
    /// </remarks>
    public static EngineBuilder AddNatsData(
        this EngineBuilder builder,
        string uri,
        Action<NatsDataOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(uri);

        var options = new NatsDataOptions
        {
            Uri = uri
        };

        configure?.Invoke(options);

        return AddNatsData(builder, options);
    }

    /// <summary>
    /// Adds the NATS data pack using an options callback that can bind from configuration.
    /// </summary>
    /// <param name="builder">The engine builder to extend.</param>
    /// <param name="configure">
    /// The callback that configures the host-owned NATS pack options, including
    /// <see cref="NatsDataOptions.UriName" /> and
    /// <see cref="NatsDataOptions.Uri" />.
    /// </param>
    /// <returns>The same engine builder for fluent composition.</returns>
    /// <remarks>
    /// Use either <see cref="NatsDataOptions.UriName" /> or
    /// <see cref="NatsDataOptions.Uri" />. Leaving both unset falls back to
    /// <c>nats://localhost:4222</c>.
    /// </remarks>
    public static EngineBuilder AddNatsData(
        this EngineBuilder builder,
        Action<NatsDataOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new NatsDataOptions();
        configure(options);

        return AddNatsData(builder, options);
    }

    private static EngineBuilder AddNatsData(
        EngineBuilder builder,
        NatsDataOptions options)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(options);

        builder.AddModule(new NatsDataModule(options));
        return builder;
    }
}
