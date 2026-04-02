using Cephalon.Abstractions.Health;
using Cephalon.Engine.Diagnostics;
using Cephalon.Observability.OracleDependencies.Configuration;
using Cephalon.Observability.OracleDependencies.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Cephalon.Observability.OracleDependencies.Hosting;

/// <summary>
/// Adds Oracle dependency-health services to a Cephalon host.
/// </summary>
public static class OracleDependencyHealthServiceCollectionExtensions
{
    /// <summary>
    /// Adds Oracle dependency-health services using configuration as the primary source of probe settings.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="configure">
    /// An optional callback that can extend or override the configuration-driven dependency-health setup.
    /// </param>
    /// <returns>The same service collection for further registration.</returns>
    public static IServiceCollection AddCephalonOracleDependencyHealth(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<OracleDependencyHealthOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = OracleDependencyHealthOptions.FromConfiguration(configuration);
        configure?.Invoke(options);

        return services.AddCephalonOracleDependencyHealth(options);
    }

    /// <summary>
    /// Adds Oracle dependency-health services using code-first configuration.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="configure">An optional callback that configures dependency-health options.</param>
    /// <returns>The same service collection for further registration.</returns>
    public static IServiceCollection AddCephalonOracleDependencyHealth(
        this IServiceCollection services,
        Action<OracleDependencyHealthOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new OracleDependencyHealthOptions();
        configure?.Invoke(options);

        return services.AddCephalonOracleDependencyHealth(options);
    }

    private static IServiceCollection AddCephalonOracleDependencyHealth(
        this IServiceCollection services,
        OracleDependencyHealthOptions options)
    {
        if (options.Dependencies.Count == 0)
        {
            return services;
        }

        services.TryAddSingleton(options);
        services.TryAddSingleton<IOracleDependencyProbeClient, OracleDependencyProbeClient>();
        services.TryAddSingleton<OracleDependencyHealthStore>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDiagnosticsConventionContributor, OracleDependencyHealthDiagnosticsConventionContributor>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDependencyHealthContributor, OracleDependencyHealthContributor>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, OracleDependencyHealthProbeHostedService>());

        return services;
    }
}
