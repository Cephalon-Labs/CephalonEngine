using Cephalon.Abstractions.Modules;
using Cephalon.Engine.Composition;

namespace Cephalon.Behaviors.Http.Hosting;

/// <summary>
/// Extends <see cref="EngineBuilder" /> with low-code registration for module-owned REST behavior modules.
/// </summary>
public static class RestBehaviorEngineBuilderExtensions
{
    /// <summary>
    /// Adds a low-code behavior-backed REST module without requiring a dedicated
    /// <see cref="RestBehaviorModuleBase" /> subclass.
    /// </summary>
    /// <typeparam name="TMarker">
    /// A stable marker type from the module's behavior assembly. Cephalon uses this marker both to
    /// create a distinct module type for engine validation and to resolve generated REST profile
    /// hints from the correct assembly when <c>MapGeneratedProfiles(...)</c> is used.
    /// </typeparam>
    /// <param name="engine">The engine builder to extend.</param>
    /// <param name="descriptor">The descriptor that identifies the inline module.</param>
    /// <param name="configureRestBehaviors">
    /// The callback that declares owned behaviors and their public REST surface.
    /// </param>
    /// <returns>The same engine builder for fluent composition.</returns>
    /// <remarks>
    /// This helper remains explicit and module-owned. It does not publish public REST from
    /// <c>[AppBehavior]</c> alone. Use a dedicated <see cref="RestBehaviorModuleBase" /> subclass
    /// when a module needs richer lifecycle hooks, custom services, or additional manual endpoints.
    /// Use one stable marker type per inline module so engine module-type validation stays
    /// deterministic.
    /// </remarks>
    public static EngineBuilder AddRestBehaviorModule<TMarker>(
        this EngineBuilder engine,
        ModuleDescriptor descriptor,
        Action<IRestBehaviorModuleBuilder> configureRestBehaviors)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(configureRestBehaviors);

        return engine.AddModule(new InlineRestBehaviorModule<TMarker>(descriptor, configureRestBehaviors));
    }

    private sealed class InlineRestBehaviorModule<TMarker>(
        ModuleDescriptor descriptor,
        Action<IRestBehaviorModuleBuilder> configureRestBehaviors) : RestBehaviorModuleBase
    {
        public override ModuleDescriptor Descriptor { get; } = descriptor;

        protected override Type GetRestBehaviorProfileSourceType()
            => typeof(TMarker);

        public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
        {
            ArgumentNullException.ThrowIfNull(behaviors);
            configureRestBehaviors(behaviors);
        }
    }
}
