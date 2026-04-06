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
    /// <param name="url">The NATS server URL (e.g. <c>"nats://localhost:4222"</c>).</param>
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
        string url,
        Action<NatsDataOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        var options = new NatsDataOptions
        {
            Url = url
        };

        configure?.Invoke(options);

        builder.AddModule(new NatsDataModule(options));
        return builder;
    }
}
