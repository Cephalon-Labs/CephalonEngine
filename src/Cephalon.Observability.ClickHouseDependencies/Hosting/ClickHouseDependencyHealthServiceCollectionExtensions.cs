using Cephalon.Observability.ClickHouseDependencies.Configuration;
using Cephalon.Observability.ClickHouseDependencies.Services;
using Cephalon.Observability.DependencyHealth.Core.Hosting;
using Cephalon.Observability.DependencyHealth.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Cephalon.Observability.ClickHouseDependencies.Hosting;

/// <summary>
/// Adds ClickHouse dependency-health services to a Cephalon host.
/// </summary>
public static class ClickHouseDependencyHealthServiceCollectionExtensions
{
    /// <summary>
    /// Adds ClickHouse dependency-health services using configuration as the primary source of probe settings.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="configure">
    /// An optional callback that can extend or override the configuration-driven dependency-health setup.
    /// </param>
    /// <returns>The same service collection for further registration.</returns>
    public static IServiceCollection AddCephalonClickHouseDependencyHealth(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<ClickHouseDependencyHealthOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = ClickHouseDependencyHealthOptions.FromConfiguration(configuration);
        configure?.Invoke(options);

        return services.AddCephalonClickHouseDependencyHealth(options);
    }

    /// <summary>
    /// Adds ClickHouse dependency-health services using code-first configuration.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="configure">An optional callback that configures dependency-health options.</param>
    /// <returns>The same service collection for further registration.</returns>
    public static IServiceCollection AddCephalonClickHouseDependencyHealth(
        this IServiceCollection services,
        Action<ClickHouseDependencyHealthOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new ClickHouseDependencyHealthOptions();
        configure?.Invoke(options);

        return services.AddCephalonClickHouseDependencyHealth(options);
    }

    private static IServiceCollection AddCephalonClickHouseDependencyHealth(
        this IServiceCollection services,
        ClickHouseDependencyHealthOptions options)
    {
        if (options.Dependencies.Count == 0)
        {
            return services;
        }

        services.TryAddSingleton<IClickHouseDependencyProbeClient, ClickHouseDependencyProbeClient>();

        return DependencyHealthServiceRegistration.AddDependencyHealth<
            ClickHouseDependencyHealthOptions,
            ClickHouseDependencyDefinition,
            ClickHouseDependencyHealthDiagnosticsConventionContributor>(
            services,
            options,
            (sp, store) => new ClickHouseDependencyHealthProbeHostedService(
                options,
                sp.GetRequiredService<IClickHouseDependencyProbeClient>(),
                store,
                sp.GetRequiredService<ILogger<ClickHouseDependencyHealthProbeHostedService>>()));
    }
}
