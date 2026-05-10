using Cephalon.Eventing.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cephalon.Eventing.Hosting;

/// <summary>
/// Registers code-first native eventing services used by Cephalon hosts and modules.
/// </summary>
public static class EventingServiceCollectionExtensions
{
    /// <summary>
    /// Registers a direct in-process event subscription executor with the native eventing pack.
    /// </summary>
    /// <typeparam name="TExecutor">The concrete executor implementation type.</typeparam>
    /// <param name="services">The service collection to extend.</param>
    /// <returns>The same service collection for fluent registration.</returns>
    /// <remarks>
    /// The executor is registered as a singleton <see cref="IEventSubscriptionExecutor" /> contribution.
    /// If the implementation also implements <see cref="IEventSubscriptionDescriptorProvider" />, the native
    /// in-process lane can discover the matching subscription descriptor from the same code-owned type.
    /// </remarks>
    public static IServiceCollection AddCephalonEventSubscriptionExecutor<TExecutor>(
        this IServiceCollection services)
        where TExecutor : class, IEventSubscriptionExecutor
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IEventSubscriptionExecutor, TExecutor>());
        return services;
    }

    /// <summary>
    /// Registers a direct in-process subscription execution middleware step with the native eventing pack.
    /// </summary>
    /// <typeparam name="TMiddleware">The concrete middleware implementation type.</typeparam>
    /// <param name="services">The service collection to extend.</param>
    /// <returns>The same service collection for fluent registration.</returns>
    /// <remarks>
    /// Middleware steps are registered as singleton <see cref="IEventSubscriptionExecutionMiddleware" />
    /// contributions and run in dependency-injection registration order before the final subscription executor.
    /// This keeps cross-cutting subscription policy typed and code-owned instead of binding it from configuration.
    /// </remarks>
    public static IServiceCollection AddCephalonEventSubscriptionExecutionMiddleware<TMiddleware>(
        this IServiceCollection services)
        where TMiddleware : class, IEventSubscriptionExecutionMiddleware
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IEventSubscriptionExecutionMiddleware, TMiddleware>());
        return services;
    }
}
