using Cephalon.Engine.Composition;
using Cephalon.Eventing.Configuration;
using Cephalon.Eventing.Modules;

namespace Cephalon.Eventing.Registration;

/// <summary>
/// Registers the built-in eventing runtime pack with an <see cref="EngineBuilder" />.
/// </summary>
public static class EventingEngineBuilderExtensions
{
    /// <summary>
    /// Adds the eventing runtime pack to the engine.
    /// </summary>
    /// <param name="builder">The engine builder to extend.</param>
    /// <param name="configure">
    /// An optional callback that configures the host-owned eventing options.
    /// </param>
    /// <returns>The same engine builder for fluent composition.</returns>
    public static EngineBuilder AddEventing(
        this EngineBuilder builder,
        Action<EventingOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var options = new EventingOptions();
        configure?.Invoke(options);

        builder.AddModule(new EventingModule(options));
        return builder;
    }
}
