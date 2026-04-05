using Cephalon.Engine.Composition;
using Cephalon.EventSourcing.Configuration;
using Cephalon.EventSourcing.Modules;

namespace Cephalon.EventSourcing.Registration;

/// <summary>
/// Registers the runtime-neutral Cephalon event-sourcing companion pack with an <see cref="EngineBuilder" />.
/// </summary>
public static class EventSourcingEngineBuilderExtensions
{
    /// <summary>
    /// Adds the Cephalon event-sourcing companion pack to the engine.
    /// </summary>
    /// <param name="builder">The engine builder to extend.</param>
    /// <param name="configure">An optional callback that configures host-owned event-sourcing options.</param>
    /// <returns>The same engine builder for fluent composition.</returns>
    public static EngineBuilder AddEventSourcing(
        this EngineBuilder builder,
        Action<EventSourcingOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddModule(new EventSourcingModule(configure));
        return builder;
    }
}
