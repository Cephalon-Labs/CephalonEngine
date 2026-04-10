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
    /// <param name="connectionString">
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
        string connectionString,
        Action<RedisDataOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var options = new RedisDataOptions
        {
            ConnectionString = connectionString
        };

        configure?.Invoke(options);

        return AddRedisData(builder, options);
    }

    /// <summary>
    /// Adds the Redis data pack using an options callback that can bind from configuration.
    /// </summary>
    /// <param name="builder">The engine builder to extend.</param>
    /// <param name="configure">
    /// The callback that configures the host-owned Redis pack options, including
    /// <see cref="RedisDataOptions.ConnectionStringName" /> and
    /// <see cref="RedisDataOptions.ConnectionString" />.
    /// </param>
    /// <returns>The same engine builder for fluent composition.</returns>
    /// <remarks>
    /// Use either <see cref="RedisDataOptions.ConnectionStringName" /> or
    /// <see cref="RedisDataOptions.ConnectionString" />. Leaving both unset falls back to
    /// <c>localhost:6379</c>.
    /// </remarks>
    public static EngineBuilder AddRedisData(
        this EngineBuilder builder,
        Action<RedisDataOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new RedisDataOptions();
        configure(options);

        return AddRedisData(builder, options);
    }

    private static EngineBuilder AddRedisData(
        EngineBuilder builder,
        RedisDataOptions options)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(options);

        builder.AddModule(new RedisDataModule(options));
        return builder;
    }
}
