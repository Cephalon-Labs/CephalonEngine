using System.Text.Json.Serialization.Metadata;
using Cephalon.Abstractions.EventSourcing;
using Cephalon.EventSourcing.Configuration;
using Cephalon.EventSourcing.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.EventSourcing.Hosting;

/// <summary>
/// Registers the runtime-neutral event-sourcing services used by Cephalon hosts.
/// </summary>
public static class EventSourcingServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Cephalon event-sourcing baseline services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to extend.</param>
    /// <param name="configure">An optional callback that configures the host-owned event-sourcing options.</param>
    /// <returns>The same service collection for fluent registration.</returns>
    public static IServiceCollection AddCephalonEventSourcing(
        this IServiceCollection services,
        Action<EventSourcingOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new EventSourcingOptions();
        configure?.Invoke(options);

        EventSourcingServiceRegistration.Register(services, options);
        return services;
    }

    /// <summary>
    /// Adds the shared Cephalon event-type registry if it has not already been registered.
    /// </summary>
    /// <param name="services">The service collection to extend.</param>
    /// <returns>The same service collection for fluent registration.</returns>
    public static IServiceCollection AddCephalonEventTypeRegistry(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        EventSourcingServiceRegistration.RegisterEventTypeRegistry(services);
        return services;
    }

    /// <summary>
    /// Registers a domain-event type with the Cephalon event-type registry.
    /// </summary>
    /// <typeparam name="TEvent">The concrete domain-event type.</typeparam>
    /// <param name="services">The service collection to extend.</param>
    /// <param name="name">An optional stable persisted name. Defaults to the event type's full name.</param>
    /// <param name="aliases">Optional legacy names that should resolve to this event type.</param>
    /// <returns>The same service collection for fluent registration.</returns>
    public static IServiceCollection AddCephalonEventType<TEvent>(
        this IServiceCollection services,
        string? name = null,
        params string[] aliases)
        where TEvent : IDomainEvent
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddCephalonEventTypeRegistry();
        services.AddSingleton<IEventTypeContributor>(
            new EventTypeRegistrationContributor(EventTypeDescriptor.Create<TEvent>(name, aliases)));
        return services;
    }

    /// <summary>
    /// Registers a domain-event type with the Cephalon event-type registry using source-generated JSON metadata.
    /// </summary>
    /// <typeparam name="TEvent">The concrete domain-event type.</typeparam>
    /// <param name="services">The service collection to extend.</param>
    /// <param name="jsonTypeInfo">The source-generated JSON type information for the event payload.</param>
    /// <param name="name">An optional stable persisted name. Defaults to the event type's full name.</param>
    /// <param name="aliases">Optional legacy names that should resolve to this event type.</param>
    /// <returns>The same service collection for fluent registration.</returns>
    public static IServiceCollection AddCephalonEventTypeWithJsonTypeInfo<TEvent>(
        this IServiceCollection services,
        JsonTypeInfo<TEvent> jsonTypeInfo,
        string? name = null,
        params string[] aliases)
        where TEvent : IDomainEvent
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(jsonTypeInfo);

        services.AddCephalonEventTypeRegistry();
        services.AddSingleton<IEventTypeContributor>(
            new EventTypeRegistrationContributor(EventTypeDescriptor.CreateWithJsonTypeInfo(jsonTypeInfo, name, aliases)));
        return services;
    }
}
