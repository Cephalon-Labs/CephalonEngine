using Cephalon.Edge.Configuration;
using Cephalon.Edge.Modules;
using Cephalon.Engine.Composition;

namespace Cephalon.Edge.Registration;

/// <summary>
/// Registers the built-in edge runtime pack with an <see cref="EngineBuilder" />.
/// </summary>
public static class EdgeEngineBuilderExtensions
{
    /// <summary>
    /// Adds the edge runtime pack to the engine.
    /// </summary>
    /// <param name="builder">The engine builder to extend.</param>
    /// <param name="configure">
    /// An optional callback that configures the host-owned edge runtime options.
    /// </param>
    /// <returns>The same engine builder for fluent composition.</returns>
    public static EngineBuilder AddEdge(
        this EngineBuilder builder,
        Action<EdgeRuntimeOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var options = new EdgeRuntimeOptions();
        configure?.Invoke(options);

        builder.AddModule(new EdgeRuntimeModule(options));
        return builder;
    }
}
