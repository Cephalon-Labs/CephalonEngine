using Cephalon.Data.Debezium.Configuration;
using Cephalon.Data.Debezium.Modules;
using Cephalon.Engine.Composition;

namespace Cephalon.Data.Debezium.Registration;

/// <summary>
/// Registers the Debezium-managed external CDC companion pack with an <see cref="EngineBuilder" />.
/// </summary>
public static class DebeziumDataEngineBuilderExtensions
{
    /// <summary>
    /// Adds the Debezium-managed external CDC pack using an options callback that can bind from configuration.
    /// </summary>
    /// <param name="builder">The engine builder to extend.</param>
    /// <param name="configure">The callback that configures the host-owned Debezium pack options.</param>
    /// <returns>The same engine builder for fluent composition.</returns>
    public static EngineBuilder AddDebeziumData(
        this EngineBuilder builder,
        Action<DebeziumDataOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new DebeziumDataOptions();
        configure(options);

        return AddDebeziumData(builder, options);
    }

    private static EngineBuilder AddDebeziumData(
        EngineBuilder builder,
        DebeziumDataOptions options)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(options);

        builder.AddModule(new DebeziumDataModule(options));
        return builder;
    }
}
