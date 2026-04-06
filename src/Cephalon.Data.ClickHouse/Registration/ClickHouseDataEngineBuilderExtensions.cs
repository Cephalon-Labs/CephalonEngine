using Cephalon.Data.ClickHouse.Configuration;
using Cephalon.Data.ClickHouse.Modules;
using Cephalon.Engine.Composition;

namespace Cephalon.Data.ClickHouse.Registration;

/// <summary>
/// Registers the ClickHouse data companion pack with an <see cref="EngineBuilder" />.
/// </summary>
public static class ClickHouseDataEngineBuilderExtensions
{
    /// <summary>
    /// Adds the ClickHouse data pack with the supplied host and database.
    /// </summary>
    /// <param name="builder">The engine builder to extend.</param>
    /// <param name="host">The ClickHouse host address (e.g. <c>"localhost"</c>).</param>
    /// <param name="database">The ClickHouse database to connect to.</param>
    /// <param name="configure">
    /// An optional callback that configures the host-owned ClickHouse pack options.
    /// </param>
    /// <returns>The same engine builder for fluent composition.</returns>
    /// <remarks>
    /// Register outbox or inbox support by configuring <see cref="ClickHouseDataOptions.RegisterOutbox" />
    /// or <see cref="ClickHouseDataOptions.RegisterInbox" /> in the <paramref name="configure" /> callback.
    /// </remarks>
    public static EngineBuilder AddClickHouseData(
        this EngineBuilder builder,
        string host,
        string database,
        Action<ClickHouseDataOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(host);
        ArgumentException.ThrowIfNullOrWhiteSpace(database);

        var options = new ClickHouseDataOptions
        {
            Host = host,
            Database = database
        };

        configure?.Invoke(options);

        builder.AddModule(new ClickHouseDataModule(options));
        return builder;
    }
}
