using Cephalon.Data.Postgres.Configuration;
using Cephalon.Data.Postgres.Modules;
using Cephalon.Engine.Composition;

namespace Cephalon.Data.Postgres.Registration;

/// <summary>
/// Registers the PostgreSQL logical-replication CDC companion pack with an <see cref="EngineBuilder" />.
/// </summary>
public static class PostgresDataEngineBuilderExtensions
{
    /// <summary>
    /// Adds the PostgreSQL logical-replication CDC pack with the supplied connection string and database name.
    /// </summary>
    /// <param name="builder">The engine builder to extend.</param>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
    /// <param name="databaseName">The operator-facing database name.</param>
    /// <param name="configure">An optional callback that configures the host-owned PostgreSQL pack options.</param>
    /// <returns>The same engine builder for fluent composition.</returns>
    public static EngineBuilder AddPostgresData(
        this EngineBuilder builder,
        string connectionString,
        string databaseName,
        Action<PostgresDataOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        var options = new PostgresDataOptions
        {
            ConnectionString = connectionString,
            DatabaseName = databaseName
        };

        configure?.Invoke(options);

        return AddPostgresData(builder, options);
    }

    /// <summary>
    /// Adds the PostgreSQL logical-replication CDC pack using an options callback that can bind from configuration.
    /// </summary>
    /// <param name="builder">The engine builder to extend.</param>
    /// <param name="configure">
    /// The callback that configures the host-owned PostgreSQL pack options, including
    /// <see cref="PostgresDataOptions.ConnectionStringName" />,
    /// <see cref="PostgresDataOptions.ConnectionString" />, and
    /// <see cref="PostgresDataOptions.DatabaseName" />.
    /// </param>
    /// <returns>The same engine builder for fluent composition.</returns>
    public static EngineBuilder AddPostgresData(
        this EngineBuilder builder,
        Action<PostgresDataOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new PostgresDataOptions();
        configure(options);

        return AddPostgresData(builder, options);
    }

    private static EngineBuilder AddPostgresData(
        EngineBuilder builder,
        PostgresDataOptions options)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.DatabaseName);

        builder.AddModule(new PostgresDataModule(options));
        return builder;
    }
}
