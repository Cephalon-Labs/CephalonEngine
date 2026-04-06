using Cephalon.Observability.DependencyHealth.Core.Hosting;
using Cephalon.Observability.DependencyHealth.Core.Services;
using Cephalon.Observability.MemcachedDependencies.Configuration;
using Cephalon.Observability.MemcachedDependencies.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cephalon.Observability.MemcachedDependencies.Hosting;

/// <summary>
/// Adds Memcached dependency-health services to a Cephalon host.
/// </summary>
public static class MemcachedDependencyHealthServiceCollectionExtensions
{
    /// <summary>
    /// Adds Memcached dependency-health services using configuration as the primary source of probe settings.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="configure">
    /// An optional callback that can extend or override the configuration-driven dependency-health setup.
    /// </param>
    /// <returns>The same service collection for further registration.</returns>
    public static IServiceCollection AddCephalonMemcachedDependencyHealth(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<MemcachedDependencyHealthOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = MemcachedDependencyHealthOptions.FromConfiguration(configuration);
        configure?.Invoke(options);

        return services.AddCephalonMemcachedDependencyHealth(options);
    }

    /// <summary>
    /// Adds Memcached dependency-health services using code-first configuration.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="configure">An optional callback that configures dependency-health options.</param>
    /// <returns>The same service collection for further registration.</returns>
    public static IServiceCollection AddCephalonMemcachedDependencyHealth(
        this IServiceCollection services,
        Action<MemcachedDependencyHealthOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new MemcachedDependencyHealthOptions();
        configure?.Invoke(options);

        return services.AddCephalonMemcachedDependencyHealth(options);
    }

    private static IServiceCollection AddCephalonMemcachedDependencyHealth(
        this IServiceCollection services,
        MemcachedDependencyHealthOptions options)
    {
        if (options.Dependencies.Count == 0)
        {
            return services;
        }

        return DependencyHealthServiceRegistration.AddDependencyHealth<
            MemcachedDependencyHealthOptions,
            MemcachedDependencyDefinition,
            MemcachedDependencyHealthDiagnosticsConventionContributor>(
            services,
            options,
            (sp, store) => new MemcachedDependencyHealthProbeHostedService(
                options,
                store,
                sp.GetRequiredService<ILogger<MemcachedDependencyHealthProbeHostedService>>()));
    }
}
