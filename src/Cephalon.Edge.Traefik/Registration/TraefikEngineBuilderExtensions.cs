using Cephalon.Edge.Traefik.Configuration;
using Cephalon.Edge.Traefik.Modules;
using Cephalon.Engine.Composition;

namespace Cephalon.Edge.Traefik.Registration;

/// <summary>
/// Registers the Traefik traffic materializer companion pack with an <see cref="EngineBuilder" />.
/// </summary>
public static class TraefikEngineBuilderExtensions
{
    /// <summary>
    /// Adds the Traefik IngressRoute traffic materializer companion pack to the engine.
    /// </summary>
    /// <param name="builder">The engine builder to extend.</param>
    /// <param name="configure">
    /// An optional callback that configures how provider-managed cell traffic automation should project into
    /// Traefik IngressRoute intent.
    /// </param>
    /// <returns>The same engine builder for fluent composition.</returns>
    /// <remarks>
    /// This pack keeps Traefik-specific control-plane projection outside <c>Cephalon.Engine</c> while still reporting
    /// provider materialization truth back through the shared
    /// <c>/engine/cell-traffic-automations*</c>, <c>/engine/technology-surfaces</c>, and <c>snapshot</c> surfaces.
    /// </remarks>
    public static EngineBuilder AddTraefikTrafficMaterializer(
        this EngineBuilder builder,
        Action<TraefikTrafficMaterializerOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var options = new TraefikTrafficMaterializerOptions();
        configure?.Invoke(options);

        builder.AddModule(new TraefikTrafficMaterializerModule(options));
        return builder;
    }
}
