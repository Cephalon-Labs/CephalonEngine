using Cephalon.Abstractions.Health;
using Cephalon.Engine.Diagnostics;
using Cephalon.Observability.ConsulDependencies.Configuration;
using Cephalon.Observability.ConsulDependencies.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Cephalon.Observability.ConsulDependencies.Hosting;

/// <summary>
/// Adds Consul dependency-health services to a Cephalon host.
/// </summary>
public static class ConsulDependencyHealthServiceCollectionExtensions
{
    internal const string HttpClientName = "Cephalon.Observability.ConsulDependencies";

    /// <summary>
    /// Adds Consul dependency-health services using configuration as the primary source of probe settings.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="configure">
    /// An optional callback that can extend or override the configuration-driven dependency-health setup.
    /// </param>
    /// <returns>The same service collection for further registration.</returns>
    public static IServiceCollection AddCephalonConsulDependencyHealth(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<ConsulDependencyHealthOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = ConsulDependencyHealthOptions.FromConfiguration(configuration);
        configure?.Invoke(options);

        return services.AddCephalonConsulDependencyHealth(options);
    }

    /// <summary>
    /// Adds Consul dependency-health services using code-first configuration.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="configure">An optional callback that configures dependency-health options.</param>
    /// <returns>The same service collection for further registration.</returns>
    public static IServiceCollection AddCephalonConsulDependencyHealth(
        this IServiceCollection services,
        Action<ConsulDependencyHealthOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new ConsulDependencyHealthOptions();
        configure?.Invoke(options);

        return services.AddCephalonConsulDependencyHealth(options);
    }

    private static IServiceCollection AddCephalonConsulDependencyHealth(
        this IServiceCollection services,
        ConsulDependencyHealthOptions options)
    {
        if (options.Dependencies.Count == 0)
        {
            return services;
        }

        services.TryAddSingleton(options);
        services.AddHttpClient(HttpClientName)
            .ConfigureHttpClient(client => client.Timeout = Timeout.InfiniteTimeSpan);
        services.TryAddSingleton<IConsulDependencyProbeClient, ConsulDependencyProbeClient>();
        services.TryAddSingleton<ConsulDependencyHealthStore>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDiagnosticsConventionContributor, ConsulDependencyHealthDiagnosticsConventionContributor>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDependencyHealthContributor, ConsulDependencyHealthContributor>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, ConsulDependencyHealthProbeHostedService>());

        return services;
    }
}
