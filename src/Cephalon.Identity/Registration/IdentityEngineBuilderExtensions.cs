using Cephalon.Engine.Composition;
using Cephalon.Identity.Configuration;
using Cephalon.Identity.Modules;

namespace Cephalon.Identity.Registration;

/// <summary>
/// Registers the host-agnostic Cephalon identity companion pack with an <see cref="EngineBuilder" />.
/// </summary>
public static class IdentityEngineBuilderExtensions
{
    /// <summary>
    /// Adds the Cephalon identity companion pack to the engine.
    /// </summary>
    /// <param name="builder">The engine builder to extend.</param>
    /// <param name="configure">
    /// An optional callback that configures host-owned identity runtime options.
    /// </param>
    /// <returns>The same engine builder for fluent composition.</returns>
    public static EngineBuilder AddIdentityAccess(
        this EngineBuilder builder,
        Action<IdentityRuntimeOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddModule(new IdentityModule(configure));
        return builder;
    }
}
