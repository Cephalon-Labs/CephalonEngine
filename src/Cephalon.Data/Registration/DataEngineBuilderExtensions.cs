using Cephalon.Data.Configuration;
using Cephalon.Data.Modules;
using Cephalon.Engine.Composition;

namespace Cephalon.Data.Registration;

/// <summary>
/// Registers the runtime-neutral data pack with an <see cref="EngineBuilder" />.
/// </summary>
public static class DataEngineBuilderExtensions
{
    /// <summary>
    /// Adds the data runtime pack to the engine.
    /// </summary>
    /// <param name="builder">The engine builder to extend.</param>
    /// <param name="configure">An optional callback that configures the host-owned data runtime options.</param>
    /// <returns>The same engine builder for fluent composition.</returns>
    public static EngineBuilder AddData(
        this EngineBuilder builder,
        Action<DataRuntimeOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var options = new DataRuntimeOptions();
        configure?.Invoke(options);

        builder.AddModule(new DataModule(options));
        return builder;
    }
}
