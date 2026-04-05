using Cephalon.Engine.Composition;
using Cephalon.Ids.Sfid.Configuration;
using Cephalon.Ids.Sfid.Modules;

namespace Cephalon.Ids.Sfid.Registration;

/// <summary>
/// Registers the official <c>Sfid.Net</c>-backed identifier strategy pack with an <see cref="EngineBuilder" />.
/// </summary>
public static class SfidEngineBuilderExtensions
{
    /// <summary>
    /// Adds the Sfid id-strategy pack to the engine.
    /// </summary>
    /// <param name="builder">The engine builder to extend.</param>
    /// <param name="configure">
    /// An optional callback that configures the host-owned Sfid generator options.
    /// </param>
    /// <returns>The same engine builder for fluent composition.</returns>
    public static EngineBuilder AddSfidIds(
        this EngineBuilder builder,
        Action<SfidIdOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddModule(new SfidIdModule(configure));
        return builder;
    }
}
