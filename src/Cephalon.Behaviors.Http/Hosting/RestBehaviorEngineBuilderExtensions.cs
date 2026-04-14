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

    /// <summary>
    /// Adds a low-code generated REST module whose public route-group path is derived from a
    /// dot-separated behavior-id prefix.
    /// </summary>
    /// <typeparam name="TMarker">
    /// A stable marker type from the module's behavior assembly. Cephalon uses this marker both to
    /// create a distinct module type for engine validation and to resolve generated REST profile
    /// hints from the correct assembly.
    /// </typeparam>
    /// <param name="engine">The engine builder to extend.</param>
    /// <param name="descriptor">The descriptor that identifies the inline module.</param>
    /// <param name="behaviorIdPrefix">
    /// The dot-separated behavior-id prefix whose segments become the route-group path. For
    /// example, <c>showcase.cart</c> becomes <c>/showcase/cart</c>.
    /// </param>
    /// <param name="configureGroup">
    /// An optional callback that applies group-level conventions such as <c>ApiVersion(...)</c> or
    /// <c>WithTagName(...)</c> before the generated profiles are mapped.
    /// </param>
    /// <returns>The same engine builder for fluent composition.</returns>
    /// <remarks>
    /// This helper still creates a real module and still maps through the same generated-profile
    /// projection, precedence, governance, and runtime-catalog pipeline as
    /// <see cref="AddRestBehaviorModule{TMarker}(EngineBuilder, ModuleDescriptor, Action{IRestBehaviorModuleBuilder})" />.
    /// It does not publish public REST from <c>[AppBehavior]</c> alone.
    /// </remarks>
    public static EngineBuilder AddGeneratedRestBehaviorModule<TMarker>(
        this EngineBuilder engine,
        ModuleDescriptor descriptor,
        string behaviorIdPrefix,
        Action<IRestBehaviorEndpointGroupBuilder>? configureGroup = null)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentException.ThrowIfNullOrWhiteSpace(behaviorIdPrefix);

        return engine.AddRestBehaviorModule<TMarker>(
            descriptor,
            behaviors =>
            {
                var group = behaviors.GroupFromBehaviorIdPrefix(behaviorIdPrefix);
                configureGroup?.Invoke(group);
                group.MapGeneratedProfiles();
            });
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
