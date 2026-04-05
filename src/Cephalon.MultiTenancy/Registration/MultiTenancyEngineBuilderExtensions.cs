using Cephalon.Engine.Composition;
using Cephalon.MultiTenancy.Configuration;
using Cephalon.MultiTenancy.Modules;

namespace Cephalon.MultiTenancy.Registration;

/// <summary>
/// Registers the host-agnostic Cephalon multi-tenancy companion pack with an <see cref="EngineBuilder" />.
/// </summary>
public static class MultiTenancyEngineBuilderExtensions
{
    /// <summary>
    /// Adds the Cephalon multi-tenancy companion pack to the engine.
    /// </summary>
    /// <param name="builder">The engine builder to extend.</param>
    /// <param name="configure">
    /// An optional callback that configures host-owned multi-tenancy runtime options.
    /// </param>
    /// <returns>The same engine builder for fluent composition.</returns>
    public static EngineBuilder AddMultiTenancy(
        this EngineBuilder builder,
        Action<MultiTenancyRuntimeOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddModule(new MultiTenancyModule(configure));
        return builder;
    }
}
