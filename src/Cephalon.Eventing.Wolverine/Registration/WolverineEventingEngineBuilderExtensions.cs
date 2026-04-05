using Cephalon.Engine.Composition;
using Cephalon.Eventing.Wolverine.Configuration;
using Cephalon.Eventing.Wolverine.Modules;

namespace Cephalon.Eventing.Wolverine.Registration;

/// <summary>
/// Registers the official Wolverine eventing companion pack with an <see cref="EngineBuilder" />.
/// </summary>
public static class WolverineEventingEngineBuilderExtensions
{
    /// <summary>
    /// Adds the Wolverine eventing companion pack to the engine.
    /// </summary>
    /// <param name="builder">The engine builder to extend.</param>
    /// <param name="configure">
    /// An optional callback that configures the host-owned Wolverine eventing options, including the opt-in managed dispatch loop.
    /// </param>
    /// <returns>The same engine builder for fluent composition.</returns>
    public static EngineBuilder AddWolverineEventing(
        this EngineBuilder builder,
        Action<WolverineEventingOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddModule(new WolverineEventingModule(configure));
        return builder;
    }
}
