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

        builder.AddModule(new MongoDbDataModule(options));
        return builder;
    }
}
