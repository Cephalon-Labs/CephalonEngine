using Cephalon.Engine.Composition;
using Cephalon.Eventing.Behaviors.Modules;

namespace Cephalon.Eventing.Behaviors.Registration;

/// <summary>
/// Registers the optional behavior-to-eventing choreography bridge with an <see cref="EngineBuilder" />.
/// </summary>
public static class BehaviorEventingEngineBuilderExtensions
{
    /// <summary>
    /// Adds the explicit saga-choreography bridge that stages behavior publications through the shared
    /// <c>Cephalon.Eventing</c> publication path.
    /// </summary>
    /// <param name="builder">The engine builder to extend.</param>
    /// <returns>The same engine builder for fluent composition.</returns>
    public static EngineBuilder AddBehaviorEventingBridge(this EngineBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddModule(new BehaviorEventingModule());
        return builder;
    }
}
