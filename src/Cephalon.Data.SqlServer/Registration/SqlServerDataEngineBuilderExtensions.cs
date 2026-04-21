using Cephalon.Data.SqlServer.Configuration;
using Cephalon.Data.SqlServer.Modules;
using Cephalon.Engine.Composition;

namespace Cephalon.Data.SqlServer.Registration;

/// <summary>
/// Registers the SQL Server CDC companion pack with an <see cref="EngineBuilder" />.
/// </summary>
public static class SqlServerDataEngineBuilderExtensions
{
    /// <summary>
    /// Adds the SQL Server CDC pack with the supplied connection string and database name.
    /// </summary>
    /// <param name="builder">The engine builder to extend.</param>
    /// <param name="connectionString">The SQL Server connection string.</param>
    /// <param name="databaseName">The operator-facing database name.</param>
    /// <param name="configure">An optional callback that configures the host-owned SQL Server pack options.</param>
    /// <returns>The same engine builder for fluent composition.</returns>
    public static EngineBuilder AddSqlServerData(
        this EngineBuilder builder,
        string connectionString,
        string databaseName,
        Action<SqlServerDataOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        var options = new SqlServerDataOptions
        {
            ConnectionString = connectionString,
            DatabaseName = databaseName
        };

        configure?.Invoke(options);

        return AddSqlServerData(builder, options);
    }

    /// <summary>
    /// Adds the SQL Server CDC pack using an options callback that can bind from configuration.
    /// </summary>
    /// <param name="builder">The engine builder to extend.</param>
    /// <param name="configure">
    /// The callback that configures the host-owned SQL Server pack options, including
    /// <see cref="SqlServerDataOptions.ConnectionStringName" />,
    /// <see cref="SqlServerDataOptions.ConnectionString" />, and
    /// <see cref="SqlServerDataOptions.DatabaseName" />.
    /// </param>
    /// <returns>The same engine builder for fluent composition.</returns>
    public static EngineBuilder AddSqlServerData(
        this EngineBuilder builder,
        Action<SqlServerDataOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new SqlServerDataOptions();
        configure(options);

        return AddSqlServerData(builder, options);
    }

    private static EngineBuilder AddSqlServerData(
        EngineBuilder builder,
        SqlServerDataOptions options)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.DatabaseName);

        builder.AddModule(new SqlServerDataModule(options));
        return builder;
    }
}
