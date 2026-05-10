using Cephalon.Engine.Composition;
using Cephalon.Eventing.Configuration;
using Cephalon.Eventing.Modules;
using Microsoft.Extensions.Configuration;

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

    /// <summary>
    /// Adds the eventing runtime pack to the engine and reads host-owned native eventing descriptors and settings from configuration.
    /// </summary>
    /// <param name="builder">The engine builder to extend.</param>
    /// <param name="configuration">
    /// The host configuration that contains the <c>Engine:Messaging</c> section, including optional
    /// <c>Channels</c>, <c>Subscriptions</c>, <c>SubscriptionHandlers</c>,
    /// <c>InProcessSubscriptions</c>, and publication settings.
    /// </param>
    /// <returns>The same engine builder for fluent composition.</returns>
    public static EngineBuilder AddEventingFromConfiguration(
        this EngineBuilder builder,
        IConfiguration configuration)
    {
        return AddEventingCore(builder, configuration, configure: null);
    }

    /// <summary>
    /// Adds the eventing runtime pack to the engine and reads host-owned native eventing descriptors and settings from configuration.
    /// </summary>
    /// <param name="builder">The engine builder to extend.</param>
    /// <param name="configuration">
    /// The host configuration that contains the <c>Engine:Messaging</c> section, including optional
    /// <c>Channels</c>, <c>Subscriptions</c>, <c>SubscriptionHandlers</c>,
    /// <c>InProcessSubscriptions</c>, and publication settings.
    /// </param>
    /// <param name="configure">
    /// A callback that can add channels, subscriptions, or deliberate overrides after configuration is read.
    /// </param>
    /// <returns>The same engine builder for fluent composition.</returns>
    public static EngineBuilder AddEventingFromConfiguration(
        this EngineBuilder builder,
        IConfiguration configuration,
        Action<EventingOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        return AddEventingCore(builder, configuration, configure);
    }

    private static EngineBuilder AddEventingCore(
        EngineBuilder builder,
        IConfiguration configuration,
        Action<EventingOptions>? configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = EventingOptionsConfigurationReader.Read(configuration);
        configure?.Invoke(options);

        builder.AddModule(new EventingModule(options));
        return builder;
    }
}
