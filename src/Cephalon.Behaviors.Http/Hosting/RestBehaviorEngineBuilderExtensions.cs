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
    /// <see cref="RestBehaviorModuleBase" /> subclass or a manually constructed
    /// <see cref="ModuleDescriptor" />.
    /// </summary>
    /// <typeparam name="TMarker">
    /// A stable marker type from the module's behavior assembly. Cephalon uses this marker both to
    /// create a distinct module type for engine validation and to resolve generated REST profile
    /// hints from the correct assembly when <c>MapGeneratedProfiles(...)</c> is used.
    /// </typeparam>
    /// <param name="engine">The engine builder to extend.</param>
    /// <param name="moduleId">The stable module identifier.</param>
    /// <param name="displayName">The human-readable module name.</param>
    /// <param name="description">The module description.</param>
    /// <param name="configureRestBehaviors">
    /// The callback that declares owned behaviors and their public REST surface.
    /// </param>
    /// <param name="version">The declared module version, when one is available.</param>
    /// <returns>The same engine builder for fluent composition.</returns>
    /// <remarks>
    /// This helper keeps the common inline module path low-ceremony while still materializing a
    /// real module descriptor and using the same projection, precedence, governance, and runtime
    /// catalog pipeline as the descriptor-based overload. When a module needs explicit dependency,
    /// tag, or metadata declarations, use the
    /// <see cref="AddRestBehaviorModule{TMarker}(EngineBuilder, ModuleDescriptor, Action{IRestBehaviorModuleBuilder})" />
    /// overload explicitly.
    /// </remarks>
    public static EngineBuilder AddRestBehaviorModule<TMarker>(
        this EngineBuilder engine,
        string moduleId,
        string displayName,
        string description,
        Action<IRestBehaviorModuleBuilder> configureRestBehaviors,
        string? version = null)
        => engine.AddRestBehaviorModule<TMarker>(
            CreateInlineModuleDescriptor(
                moduleId,
                displayName,
                description,
                version),
            configureRestBehaviors);

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
    /// Adds a low-code generated REST module whose generated behavior-id prefix is the inline
    /// module descriptor id.
    /// </summary>
    /// <typeparam name="TMarker">
    /// A stable marker type from the module's behavior assembly. Cephalon uses this marker both to
    /// create a distinct module type for engine validation and to resolve generated REST profile
    /// hints from the correct assembly.
    /// </typeparam>
    /// <param name="engine">The engine builder to extend.</param>
    /// <param name="descriptor">
    /// The descriptor that identifies the inline module. <see cref="ModuleDescriptor.Id" /> must
    /// also be a valid dot-separated generated behavior-id prefix.
    /// </param>
    /// <param name="configureGroup">
    /// An optional callback that applies group-level conventions such as <c>ApiVersion(...)</c> or
    /// <c>WithTagName(...)</c> before the generated profiles are mapped.
    /// </param>
    /// <returns>The same engine builder for fluent composition.</returns>
    /// <remarks>
    /// Use this overload when the inline module id already matches the generated behavior-id
    /// prefix the module should own. When the module id and generated prefix should differ, use
    /// <see cref="AddGeneratedRestBehaviorModule{TMarker}(EngineBuilder, ModuleDescriptor, string, Action{IRestBehaviorEndpointGroupBuilder}?)" />
    /// explicitly. This helper still creates a real module and still never publishes public REST
    /// from <c>[AppBehavior]</c> alone.
    /// </remarks>
    public static EngineBuilder AddGeneratedRestBehaviorModule<TMarker>(
        this EngineBuilder engine,
        ModuleDescriptor descriptor,
        Action<IRestBehaviorEndpointGroupBuilder>? configureGroup = null)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(descriptor);

        return engine.AddGeneratedRestBehaviorModule<TMarker>(
            descriptor,
            ResolveGeneratedBehaviorIdPrefix(descriptor),
            configureGroup);
    }

    /// <summary>
    /// Adds a low-code generated REST module whose generated behavior-id prefix is the inline
    /// module id.
    /// </summary>
    /// <typeparam name="TMarker">
    /// A stable marker type from the module's behavior assembly. Cephalon uses this marker both to
    /// create a distinct module type for engine validation and to resolve generated REST profile
    /// hints from the correct assembly.
    /// </typeparam>
    /// <param name="engine">The engine builder to extend.</param>
    /// <param name="moduleId">The stable module identifier.</param>
    /// <param name="displayName">The human-readable module name.</param>
    /// <param name="description">The module description.</param>
    /// <param name="configureGroup">
    /// An optional callback that applies group-level conventions such as <c>ApiVersion(...)</c> or
    /// <c>WithTagName(...)</c> before the generated profiles are mapped.
    /// </param>
    /// <param name="version">The declared module version, when one is available.</param>
    /// <returns>The same engine builder for fluent composition.</returns>
    /// <remarks>
    /// Use this overload when the module id already matches the generated behavior-id prefix the
    /// inline module should own. When the module needs explicit dependency, tag, or metadata
    /// declarations, use the descriptor-based overload explicitly.
    /// </remarks>
    public static EngineBuilder AddGeneratedRestBehaviorModule<TMarker>(
        this EngineBuilder engine,
        string moduleId,
        string displayName,
        string description,
        Action<IRestBehaviorEndpointGroupBuilder>? configureGroup = null,
        string? version = null)
        => engine.AddGeneratedRestBehaviorModule<TMarker>(
            CreateInlineModuleDescriptor(
                moduleId,
                displayName,
                description,
                version),
            configureGroup);

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

    /// <summary>
    /// Adds a low-code generated REST module whose public route-group path is derived from a
    /// dot-separated behavior-id prefix without requiring a manually constructed
    /// <see cref="ModuleDescriptor" />.
    /// </summary>
    /// <typeparam name="TMarker">
    /// A stable marker type from the module's behavior assembly. Cephalon uses this marker both to
    /// create a distinct module type for engine validation and to resolve generated REST profile
    /// hints from the correct assembly.
    /// </typeparam>
    /// <param name="engine">The engine builder to extend.</param>
    /// <param name="moduleId">The stable module identifier.</param>
    /// <param name="displayName">The human-readable module name.</param>
    /// <param name="description">The module description.</param>
    /// <param name="behaviorIdPrefix">
    /// The dot-separated behavior-id prefix whose segments become the route-group path. For
    /// example, <c>showcase.cart</c> becomes <c>/showcase/cart</c>.
    /// </param>
    /// <param name="configureGroup">
    /// An optional callback that applies group-level conventions such as <c>ApiVersion(...)</c> or
    /// <c>WithTagName(...)</c> before the generated profiles are mapped.
    /// </param>
    /// <param name="version">The declared module version, when one is available.</param>
    /// <returns>The same engine builder for fluent composition.</returns>
    /// <remarks>
    /// Use this overload when the inline module should keep the low-ceremony string-based module
    /// descriptor path but its module id and generated behavior-id prefix should differ. When the
    /// module needs explicit dependency, tag, or metadata declarations, use the descriptor-based
    /// overload explicitly.
    /// </remarks>
    public static EngineBuilder AddGeneratedRestBehaviorModule<TMarker>(
        this EngineBuilder engine,
        string moduleId,
        string displayName,
        string description,
        string behaviorIdPrefix,
        Action<IRestBehaviorEndpointGroupBuilder>? configureGroup = null,
        string? version = null)
        => engine.AddGeneratedRestBehaviorModule<TMarker>(
            CreateInlineModuleDescriptor(
                moduleId,
                displayName,
                description,
                version),
            behaviorIdPrefix,
            configureGroup);

    /// <summary>
    /// Adds a low-code generated REST module that fans one generated behavior-id root prefix out
    /// into several derived route groups while reusing the descriptor id as the generated prefix.
    /// </summary>
    /// <typeparam name="TMarker">
    /// A stable marker type from the module's behavior assembly. Cephalon uses this marker both to
    /// create a distinct module type for engine validation and to resolve generated REST profile
    /// hints from the correct assembly.
    /// </typeparam>
    /// <param name="engine">The engine builder to extend.</param>
    /// <param name="descriptor">
    /// The descriptor that identifies the inline module. <see cref="ModuleDescriptor.Id" /> must
    /// also be a valid dot-separated generated behavior-id prefix.
    /// </param>
    /// <param name="configureGroup">
    /// An optional callback that applies shared group-level conventions such as
    /// <c>ApiVersion(...)</c>, <c>WithTagName(...)</c>, or <c>WithHostGovernanceScope(...)</c> to
    /// each derived route group before the generated profiles are mapped.
    /// </param>
    /// <returns>The same engine builder for fluent composition.</returns>
    /// <remarks>
    /// Use this overload when the inline module id already matches the generated root prefix whose
    /// parent segments should become several owned public route groups. When module identity and
    /// generated ownership prefix should differ, use
    /// <see cref="AddGeneratedRestBehaviorModuleGroups{TMarker}(EngineBuilder, ModuleDescriptor, string, Action{IRestBehaviorEndpointGroupBuilder}?)" />
    /// explicitly. This helper still creates a real module and still never publishes public REST
    /// from <c>[AppBehavior]</c> alone.
    /// </remarks>
    public static EngineBuilder AddGeneratedRestBehaviorModuleGroups<TMarker>(
        this EngineBuilder engine,
        ModuleDescriptor descriptor,
        Action<IRestBehaviorEndpointGroupBuilder>? configureGroup = null)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(descriptor);

        return engine.AddGeneratedRestBehaviorModuleGroups<TMarker>(
            descriptor,
            ResolveGeneratedBehaviorIdPrefix(descriptor),
            configureGroup);
    }

    /// <summary>
    /// Adds a low-code generated REST module that fans one generated behavior-id root prefix out
    /// into several derived route groups while reusing the inline module id as the generated
    /// prefix.
    /// </summary>
    /// <typeparam name="TMarker">
    /// A stable marker type from the module's behavior assembly. Cephalon uses this marker both to
    /// create a distinct module type for engine validation and to resolve generated REST profile
    /// hints from the correct assembly.
    /// </typeparam>
    /// <param name="engine">The engine builder to extend.</param>
    /// <param name="moduleId">The stable module identifier.</param>
    /// <param name="displayName">The human-readable module name.</param>
    /// <param name="description">The module description.</param>
    /// <param name="configureGroup">
    /// An optional callback that applies shared group-level conventions such as
    /// <c>ApiVersion(...)</c>, <c>WithTagName(...)</c>, or <c>WithHostGovernanceScope(...)</c> to
    /// each derived route group before the generated profiles are mapped.
    /// </param>
    /// <param name="version">The declared module version, when one is available.</param>
    /// <returns>The same engine builder for fluent composition.</returns>
    /// <remarks>
    /// Use this overload when the module id already matches the generated root prefix the inline
    /// module should own. When the module needs explicit dependency, tag, or metadata declarations,
    /// use the descriptor-based overload explicitly.
    /// </remarks>
    public static EngineBuilder AddGeneratedRestBehaviorModuleGroups<TMarker>(
        this EngineBuilder engine,
        string moduleId,
        string displayName,
        string description,
        Action<IRestBehaviorEndpointGroupBuilder>? configureGroup = null,
        string? version = null)
        => engine.AddGeneratedRestBehaviorModuleGroups<TMarker>(
            CreateInlineModuleDescriptor(
                moduleId,
                displayName,
                description,
                version),
            configureGroup);

    /// <summary>
    /// Adds a low-code generated REST module that fans one generated behavior-id root prefix out
    /// into several derived route groups.
    /// </summary>
    /// <typeparam name="TMarker">
    /// A stable marker type from the module's behavior assembly. Cephalon uses this marker both to
    /// create a distinct module type for engine validation and to resolve generated REST profile
    /// hints from the correct assembly.
    /// </typeparam>
    /// <param name="engine">The engine builder to extend.</param>
    /// <param name="descriptor">The descriptor that identifies the inline module.</param>
    /// <param name="behaviorIdPrefix">
    /// The root dot-separated behavior-id prefix whose child parent prefixes become the derived
    /// public route groups.
    /// </param>
    /// <param name="configureGroup">
    /// An optional callback that applies shared group-level conventions such as
    /// <c>ApiVersion(...)</c>, <c>WithTagName(...)</c>, or <c>WithHostGovernanceScope(...)</c> to
    /// each derived route group before the generated profiles are mapped.
    /// </param>
    /// <returns>The same engine builder for fluent composition.</returns>
    /// <remarks>
    /// This helper still creates a real module and still maps through the same generated
    /// profile-group projection, precedence, governance, and runtime-catalog pipeline as
    /// <see cref="IRestBehaviorModuleBuilder.MapGeneratedProfileGroups(string)" />. It does not
    /// publish public REST from <c>[AppBehavior]</c> alone.
    /// </remarks>
    public static EngineBuilder AddGeneratedRestBehaviorModuleGroups<TMarker>(
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
                if (configureGroup is null)
                {
                    behaviors.MapGeneratedProfileGroups(behaviorIdPrefix);
                }
                else
                {
                    behaviors.MapGeneratedProfileGroups(behaviorIdPrefix, configureGroup);
                }
            });
    }

    /// <summary>
    /// Adds a low-code generated REST module that fans one generated behavior-id root prefix out
    /// into several derived route groups while applying per-derived-group conventions with
    /// awareness of the derived generated behavior-id prefix.
    /// </summary>
    /// <typeparam name="TMarker">
    /// A stable marker type from the module's behavior assembly. Cephalon uses this marker both to
    /// create a distinct module type for engine validation and to resolve generated REST profile
    /// hints from the correct assembly.
    /// </typeparam>
    /// <param name="engine">The engine builder to extend.</param>
    /// <param name="descriptor">The descriptor that identifies the inline module.</param>
    /// <param name="configureGroup">
    /// The callback applied to each derived route group before the generated profiles are mapped.
    /// The first argument is the derived behavior-id prefix for that route group.
    /// </param>
    /// <returns>The same engine builder for fluent composition.</returns>
    public static EngineBuilder AddGeneratedRestBehaviorModuleGroups<TMarker>(
        this EngineBuilder engine,
        ModuleDescriptor descriptor,
        Action<string, IRestBehaviorEndpointGroupBuilder> configureGroup)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(configureGroup);

        return engine.AddGeneratedRestBehaviorModuleGroups<TMarker>(
            descriptor,
            ResolveGeneratedBehaviorIdPrefix(descriptor),
            configureGroup);
    }

    /// <summary>
    /// Adds a low-code generated REST module that fans one generated behavior-id root prefix out
    /// into several derived route groups while applying per-derived-group conventions with
    /// awareness of the derived generated behavior-id prefix.
    /// </summary>
    /// <typeparam name="TMarker">
    /// A stable marker type from the module's behavior assembly. Cephalon uses this marker both to
    /// create a distinct module type for engine validation and to resolve generated REST profile
    /// hints from the correct assembly.
    /// </typeparam>
    /// <param name="engine">The engine builder to extend.</param>
    /// <param name="moduleId">The stable module identifier.</param>
    /// <param name="displayName">The human-readable module name.</param>
    /// <param name="description">The module description.</param>
    /// <param name="configureGroup">
    /// The callback applied to each derived route group before the generated profiles are mapped.
    /// The first argument is the derived behavior-id prefix for that route group.
    /// </param>
    /// <param name="version">The declared module version, when one is available.</param>
    /// <returns>The same engine builder for fluent composition.</returns>
    public static EngineBuilder AddGeneratedRestBehaviorModuleGroups<TMarker>(
        this EngineBuilder engine,
        string moduleId,
        string displayName,
        string description,
        Action<string, IRestBehaviorEndpointGroupBuilder> configureGroup,
        string? version = null)
        => engine.AddGeneratedRestBehaviorModuleGroups<TMarker>(
            CreateInlineModuleDescriptor(
                moduleId,
                displayName,
                description,
                version),
            configureGroup);

    /// <summary>
    /// Adds a low-code generated REST module that fans one generated behavior-id root prefix out
    /// into several derived route groups while applying per-derived-group conventions with
    /// awareness of the derived generated behavior-id prefix.
    /// </summary>
    /// <typeparam name="TMarker">
    /// A stable marker type from the module's behavior assembly. Cephalon uses this marker both to
    /// create a distinct module type for engine validation and to resolve generated REST profile
    /// hints from the correct assembly.
    /// </typeparam>
    /// <param name="engine">The engine builder to extend.</param>
    /// <param name="descriptor">The descriptor that identifies the inline module.</param>
    /// <param name="behaviorIdPrefix">
    /// The root dot-separated behavior-id prefix whose child parent prefixes become the derived
    /// public route groups.
    /// </param>
    /// <param name="configureGroup">
    /// The callback applied to each derived route group before the generated profiles are mapped.
    /// The first argument is the derived behavior-id prefix for that route group.
    /// </param>
    /// <returns>The same engine builder for fluent composition.</returns>
    public static EngineBuilder AddGeneratedRestBehaviorModuleGroups<TMarker>(
        this EngineBuilder engine,
        ModuleDescriptor descriptor,
        string behaviorIdPrefix,
        Action<string, IRestBehaviorEndpointGroupBuilder> configureGroup)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentException.ThrowIfNullOrWhiteSpace(behaviorIdPrefix);
        ArgumentNullException.ThrowIfNull(configureGroup);

        return engine.AddRestBehaviorModule<TMarker>(
            descriptor,
            behaviors => behaviors.MapGeneratedProfileGroups(behaviorIdPrefix, configureGroup));
    }

    /// <summary>
    /// Adds a low-code generated REST module that fans one generated behavior-id root prefix out
    /// into several derived route groups without requiring a manually constructed
    /// <see cref="ModuleDescriptor" />, while applying per-derived-group conventions with
    /// awareness of the derived generated behavior-id prefix.
    /// </summary>
    /// <typeparam name="TMarker">
    /// A stable marker type from the module's behavior assembly. Cephalon uses this marker both to
    /// create a distinct module type for engine validation and to resolve generated REST profile
    /// hints from the correct assembly.
    /// </typeparam>
    /// <param name="engine">The engine builder to extend.</param>
    /// <param name="moduleId">The stable module identifier.</param>
    /// <param name="displayName">The human-readable module name.</param>
    /// <param name="description">The module description.</param>
    /// <param name="behaviorIdPrefix">
    /// The root dot-separated behavior-id prefix whose child parent prefixes become the derived
    /// public route groups.
    /// </param>
    /// <param name="configureGroup">
    /// The callback applied to each derived route group before the generated profiles are mapped.
    /// The first argument is the derived behavior-id prefix for that route group.
    /// </param>
    /// <param name="version">The declared module version, when one is available.</param>
    /// <returns>The same engine builder for fluent composition.</returns>
    public static EngineBuilder AddGeneratedRestBehaviorModuleGroups<TMarker>(
        this EngineBuilder engine,
        string moduleId,
        string displayName,
        string description,
        string behaviorIdPrefix,
        Action<string, IRestBehaviorEndpointGroupBuilder> configureGroup,
        string? version = null)
        => engine.AddGeneratedRestBehaviorModuleGroups<TMarker>(
            CreateInlineModuleDescriptor(
                moduleId,
                displayName,
                description,
                version),
            behaviorIdPrefix,
            configureGroup);

    /// <summary>
    /// Adds a low-code generated REST module that fans one generated behavior-id root prefix out
    /// into several derived route groups without requiring a manually constructed
    /// <see cref="ModuleDescriptor" />.
    /// </summary>
    /// <typeparam name="TMarker">
    /// A stable marker type from the module's behavior assembly. Cephalon uses this marker both to
    /// create a distinct module type for engine validation and to resolve generated REST profile
    /// hints from the correct assembly.
    /// </typeparam>
    /// <param name="engine">The engine builder to extend.</param>
    /// <param name="moduleId">The stable module identifier.</param>
    /// <param name="displayName">The human-readable module name.</param>
    /// <param name="description">The module description.</param>
    /// <param name="behaviorIdPrefix">
    /// The root dot-separated behavior-id prefix whose child parent prefixes become the derived
    /// public route groups.
    /// </param>
    /// <param name="configureGroup">
    /// An optional callback that applies shared group-level conventions such as
    /// <c>ApiVersion(...)</c>, <c>WithTagName(...)</c>, or <c>WithHostGovernanceScope(...)</c> to
    /// each derived route group before the generated profiles are mapped.
    /// </param>
    /// <param name="version">The declared module version, when one is available.</param>
    /// <returns>The same engine builder for fluent composition.</returns>
    /// <remarks>
    /// Use this overload when the inline module should keep the low-ceremony string-based module
    /// descriptor path but its module id and generated root prefix should differ. When the module
    /// needs explicit dependency, tag, or metadata declarations, use the descriptor-based overload
    /// explicitly.
    /// </remarks>
    public static EngineBuilder AddGeneratedRestBehaviorModuleGroups<TMarker>(
        this EngineBuilder engine,
        string moduleId,
        string displayName,
        string description,
        string behaviorIdPrefix,
        Action<IRestBehaviorEndpointGroupBuilder>? configureGroup = null,
        string? version = null)
        => engine.AddGeneratedRestBehaviorModuleGroups<TMarker>(
            CreateInlineModuleDescriptor(
                moduleId,
                displayName,
                description,
                version),
            behaviorIdPrefix,
            configureGroup);

    private static ModuleDescriptor CreateInlineModuleDescriptor(
        string moduleId,
        string displayName,
        string description,
        string? version)
        => new(
            moduleId,
            displayName,
            description,
            version: version);

    private static string ResolveGeneratedBehaviorIdPrefix(ModuleDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        try
        {
            _ = RestBehaviorAuthoringPathConventions.DeriveRouteGroupPrefixFromBehaviorIdPrefix(descriptor.Id);
            return descriptor.Id;
        }
        catch (ArgumentException exception)
        {
            throw new ArgumentException(
                $"Module descriptor id '{descriptor.Id}' cannot be used as an inline generated REST behavior-id prefix. Use non-empty dot-separated segments so Cephalon can derive a deterministic route-group prefix, or call AddGeneratedRestBehaviorModule<TMarker>(descriptor, behaviorIdPrefix, ...) explicitly when the module id and generated prefix should differ.",
                nameof(descriptor),
                exception);
        }
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
