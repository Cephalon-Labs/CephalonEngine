using Cephalon.Data.MySql.Configuration;
using Cephalon.Data.MySql.Modules;
using Cephalon.Engine.Composition;

namespace Cephalon.Data.MySql.Registration;

/// <summary>
/// Registers the MySQL binlog CDC companion pack with an <see cref="EngineBuilder" />.
/// </summary>
public static class MySqlDataEngineBuilderExtensions
{
    /// <summary>
    /// Adds the MySQL binlog CDC pack with the supplied connection string and database name.
    /// </summary>
    /// <param name="builder">The engine builder to extend.</param>
    /// <param name="connectionString">The MySQL connection string.</param>
    /// <param name="databaseName">The operator-facing database name.</param>
    /// <param name="configure">An optional callback that configures the host-owned MySQL pack options.</param>
    /// <returns>The same engine builder for fluent composition.</returns>
    public static EngineBuilder AddMySqlData(
        this EngineBuilder builder,
        string connectionString,
        string databaseName,
        Action<MySqlDataOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        var options = new MySqlDataOptions
        {
            ConnectionString = connectionString,
            DatabaseName = databaseName
        };

        configure?.Invoke(options);

        return AddMySqlData(builder, options);
    }

    /// <summary>
    /// Adds the MySQL binlog CDC pack using an options callback that can bind from configuration.
    /// </summary>
    /// <param name="builder">The engine builder to extend.</param>
    /// <param name="configure">
    /// The callback that configures the host-owned MySQL pack options, including
    /// <see cref="MySqlDataOptions.ConnectionStringName" />,
    /// <see cref="MySqlDataOptions.ConnectionString" />, and
    /// <see cref="MySqlDataOptions.DatabaseName" />.
    /// </param>
    /// <returns>The same engine builder for fluent composition.</returns>
    public static EngineBuilder AddMySqlData(
        this EngineBuilder builder,
        Action<MySqlDataOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new MySqlDataOptions();
        configure(options);

        return AddMySqlData(builder, options);
    }

    private static EngineBuilder AddMySqlData(
        EngineBuilder builder,
        MySqlDataOptions options)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.DatabaseName);

        builder.AddModule(new MySqlDataModule(options));
        return builder;
    }
}
