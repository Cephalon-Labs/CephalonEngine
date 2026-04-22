using Cephalon.Data.Oracle.Configuration;
using Cephalon.Data.Oracle.Modules;
using Cephalon.Engine.Composition;

namespace Cephalon.Data.Oracle.Registration;

/// <summary>
/// Registers the Oracle LogMiner CDC companion pack with an <see cref="EngineBuilder" />.
/// </summary>
public static class OracleDataEngineBuilderExtensions
{
    /// <summary>
    /// Adds the Oracle LogMiner CDC pack with the supplied connection string and database name.
    /// </summary>
    /// <param name="builder">The engine builder to extend.</param>
    /// <param name="connectionString">The Oracle connection string.</param>
    /// <param name="databaseName">The operator-facing Oracle database name.</param>
    /// <param name="configure">An optional callback that configures the host-owned Oracle pack options.</param>
    /// <returns>The same engine builder for fluent composition.</returns>
    public static EngineBuilder AddOracleData(
        this EngineBuilder builder,
        string connectionString,
        string databaseName,
        Action<OracleDataOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        var options = new OracleDataOptions
        {
            ConnectionString = connectionString,
            DatabaseName = databaseName
        };

        configure?.Invoke(options);

        return AddOracleData(builder, options);
    }

    /// <summary>
    /// Adds the Oracle LogMiner CDC pack using an options callback that can bind from configuration.
    /// </summary>
    /// <param name="builder">The engine builder to extend.</param>
    /// <param name="configure">
    /// The callback that configures the host-owned Oracle pack options, including
    /// <see cref="OracleDataOptions.ConnectionStringName" />,
    /// <see cref="OracleDataOptions.ConnectionString" />, and
    /// <see cref="OracleDataOptions.DatabaseName" />.
    /// </param>
    /// <returns>The same engine builder for fluent composition.</returns>
    public static EngineBuilder AddOracleData(
        this EngineBuilder builder,
        Action<OracleDataOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new OracleDataOptions();
        configure(options);

        return AddOracleData(builder, options);
    }

    private static EngineBuilder AddOracleData(
        EngineBuilder builder,
        OracleDataOptions options)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.DatabaseName);

        builder.AddModule(new OracleDataModule(options));
        return builder;
    }
}
