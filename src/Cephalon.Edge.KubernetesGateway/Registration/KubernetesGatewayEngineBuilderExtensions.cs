using Cephalon.Edge.KubernetesGateway.Configuration;
using Cephalon.Edge.KubernetesGateway.Modules;
using Cephalon.Engine.Composition;

namespace Cephalon.Edge.KubernetesGateway.Registration;

/// <summary>
/// Registers the Kubernetes Gateway API traffic materializer companion pack with an <see cref="EngineBuilder" />.
/// </summary>
public static class KubernetesGatewayEngineBuilderExtensions
{
    /// <summary>
    /// Adds the Kubernetes Gateway API traffic materializer companion pack to the engine.
    /// </summary>
    /// <param name="builder">The engine builder to extend.</param>
    /// <param name="configure">
    /// An optional callback that configures how provider-managed cell traffic automation should project into
    /// Kubernetes Gateway API intent.
    /// </param>
    /// <returns>The same engine builder for fluent composition.</returns>
    /// <remarks>
    /// This pack keeps Kubernetes Gateway API control-plane projection outside <c>Cephalon.Engine</c> while still
    /// reporting provider materialization truth back through the shared
    /// <c>/engine/cell-traffic-automations*</c>, <c>/engine/technology-surfaces</c>, and <c>snapshot</c> surfaces.
    /// </remarks>
    public static EngineBuilder AddKubernetesGatewayTrafficMaterializer(
        this EngineBuilder builder,
        Action<KubernetesGatewayTrafficMaterializerOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var options = new KubernetesGatewayTrafficMaterializerOptions();
        configure?.Invoke(options);

        builder.AddModule(new KubernetesGatewayTrafficMaterializerModule(options));
        return builder;
    }
}
