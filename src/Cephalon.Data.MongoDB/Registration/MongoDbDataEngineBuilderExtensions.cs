using Cephalon.Data.MongoDB.Configuration;
using Cephalon.Data.MongoDB.Modules;
using Cephalon.Engine.Composition;

namespace Cephalon.Data.MongoDB.Registration;

/// <summary>
/// Registers the MongoDB data companion pack with an <see cref="EngineBuilder" />.
/// </summary>
public static class MongoDbDataEngineBuilderExtensions
{
    /// <summary>
    /// Adds the MongoDB data pack with the supplied connection string and database name.
    /// </summary>
    /// <param name="builder">The engine builder to extend.</param>
    /// <param name="connectionString">The MongoDB connection string.</param>
    /// <param name="databaseName">The target MongoDB database name.</param>
    /// <param name="configure">
    /// An optional callback that configures the host-owned MongoDB pack options.
    /// </param>
    /// <returns>The same engine builder for fluent composition.</returns>
    /// <remarks>
    /// Register outbox or inbox support by configuring <see cref="MongoDbDataOptions.RegisterOutbox" />
    /// or <see cref="MongoDbDataOptions.RegisterInbox" /> in the <paramref name="configure" /> callback.
    /// </remarks>
    public static EngineBuilder AddMongoDbData(
        this EngineBuilder builder,
        string connectionString,
        string databaseName,
        Action<MongoDbDataOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        var options = new MongoDbDataOptions
        {
            ConnectionString = connectionString,
            DatabaseName = databaseName
        };

        configure?.Invoke(options);

        return AddMongoDbData(builder, options);
    }

    /// <summary>
    /// Adds the MongoDB data pack using an options callback that can bind from configuration.
    /// </summary>
    /// <param name="builder">The engine builder to extend.</param>
    /// <param name="configure">
    /// The callback that configures the host-owned MongoDB pack options, including
    /// <see cref="MongoDbDataOptions.ConnectionStringName" />,
    /// <see cref="MongoDbDataOptions.ConnectionString" />, and <see cref="MongoDbDataOptions.DatabaseName" />.
    /// </param>
    /// <returns>The same engine builder for fluent composition.</returns>
    /// <remarks>
    /// Use either <see cref="MongoDbDataOptions.ConnectionStringName" /> or
    /// <see cref="MongoDbDataOptions.ConnectionString" />. Leaving both unset falls back to
    /// <c>mongodb://localhost:27017</c>.
    /// </remarks>
    public static EngineBuilder AddMongoDbData(
        this EngineBuilder builder,
        Action<MongoDbDataOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new MongoDbDataOptions();
        configure(options);

        return AddMongoDbData(builder, options);
    }

    private static EngineBuilder AddMongoDbData(
        EngineBuilder builder,
        MongoDbDataOptions options)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.DatabaseName);

        builder.AddModule(new MongoDbDataModule(options));
        return builder;
    }
}
