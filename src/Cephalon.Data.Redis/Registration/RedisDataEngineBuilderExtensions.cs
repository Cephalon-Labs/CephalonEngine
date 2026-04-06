using Cephalon.Data.Redis.Configuration;
using Cephalon.Data.Redis.Modules;
using Cephalon.Engine.Composition;

namespace Cephalon.Data.Redis.Registration;

/// <summary>
/// Registers the Redis data companion pack with an <see cref="EngineBuilder" />.
/// </summary>
public static class RedisDataEngineBuilderExtensions
{
    /// <summary>
    /// Adds the Redis data pack with the supplied connection string and optional configuration callback.
    /// </summary>
    /// <param name="builder">The engine builder to extend.</param>
    /// <param name="configuration">
    /// The StackExchange.Redis connection string or configuration (e.g. <c>"localhost:6379"</c> or a full
    /// StackExchange.Redis configuration string with options such as <c>"localhost:6379,abortConnect=false"</c>).
    /// </param>
    /// <param name="configure">
    /// An optional callback that configures the host-owned Redis pack options.
    /// </param>
    /// <returns>The same engine builder for fluent composition.</returns>
    /// <remarks>
    /// Register outbox or inbox support by configuring <see cref="RedisDataOptions.RegisterOutbox" />
    /// or <see cref="RedisDataOptions.RegisterInbox" /> in the <paramref name="configure" /> callback.
    /// </remarks>
    public static EngineBuilder AddRedisData(
        this EngineBuilder builder,
        string configuration,
        Action<RedisDataOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(configuration);

        var options = new RedisDataOptions
        {
            Configuration = configuration
        };

        configure?.Invoke(options);

        builder.AddModule(new RedisDataModule(options));
        return builder;
    }
}
