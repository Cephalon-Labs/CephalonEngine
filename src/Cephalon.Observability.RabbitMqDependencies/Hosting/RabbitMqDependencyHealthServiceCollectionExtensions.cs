using Cephalon.Abstractions.Health;
using Cephalon.Observability.RabbitMqDependencies.Configuration;
using Cephalon.Observability.RabbitMqDependencies.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Cephalon.Observability.RabbitMqDependencies.Hosting;

/// <summary>
/// Adds RabbitMQ dependency-health services to a Cephalon host.
/// </summary>
public static class RabbitMqDependencyHealthServiceCollectionExtensions
{
    /// <summary>
    /// Adds RabbitMQ dependency-health services using configuration as the primary source of probe settings.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="configure">
    /// An optional callback that can extend or override the configuration-driven dependency-health setup.
    /// </param>
    /// <returns>The same service collection for further registration.</returns>
    public static IServiceCollection AddCephalonRabbitMqDependencyHealth(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<RabbitMqDependencyHealthOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = RabbitMqDependencyHealthOptions.FromConfiguration(configuration);
        configure?.Invoke(options);

        return services.AddCephalonRabbitMqDependencyHealth(options);
    }

    /// <summary>
    /// Adds RabbitMQ dependency-health services using code-first configuration.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="configure">An optional callback that configures dependency-health options.</param>
    /// <returns>The same service collection for further registration.</returns>
    public static IServiceCollection AddCephalonRabbitMqDependencyHealth(
        this IServiceCollection services,
        Action<RabbitMqDependencyHealthOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new RabbitMqDependencyHealthOptions();
        configure?.Invoke(options);

        return services.AddCephalonRabbitMqDependencyHealth(options);
    }

    private static IServiceCollection AddCephalonRabbitMqDependencyHealth(
        this IServiceCollection services,
        RabbitMqDependencyHealthOptions options)
    {
        if (options.Dependencies.Count == 0)
        {
            return services;
        }

        services.TryAddSingleton(options);
        services.TryAddSingleton<IRabbitMqDependencyProbeClient, RabbitMqDependencyProbeClient>();
        services.TryAddSingleton<RabbitMqDependencyHealthStore>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDependencyHealthContributor, RabbitMqDependencyHealthContributor>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, RabbitMqDependencyHealthProbeHostedService>());

        return services;
    }
}
