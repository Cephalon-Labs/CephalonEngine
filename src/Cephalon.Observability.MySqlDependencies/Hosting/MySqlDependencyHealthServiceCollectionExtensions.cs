using Cephalon.Abstractions.Health;
using Cephalon.Engine.Diagnostics;
using Cephalon.Observability.MySqlDependencies.Configuration;
using Cephalon.Observability.MySqlDependencies.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Cephalon.Observability.MySqlDependencies.Hosting;

/// <summary>
/// Adds MySQL dependency-health services to a Cephalon host.
/// </summary>
public static class MySqlDependencyHealthServiceCollectionExtensions
{
    /// <summary>
    /// Adds MySQL dependency-health services using configuration as the primary source of probe settings.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="configure">
    /// An optional callback that can extend or override the configuration-driven dependency-health setup.
    /// </param>
    /// <returns>The same service collection for further registration.</returns>
    public static IServiceCollection AddCephalonMySqlDependencyHealth(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<MySqlDependencyHealthOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = MySqlDependencyHealthOptions.FromConfiguration(configuration);
        configure?.Invoke(options);

        return services.AddCephalonMySqlDependencyHealth(options);
    }

    /// <summary>
    /// Adds MySQL dependency-health services using code-first configuration.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="configure">An optional callback that configures dependency-health options.</param>
    /// <returns>The same service collection for further registration.</returns>
    public static IServiceCollection AddCephalonMySqlDependencyHealth(
        this IServiceCollection services,
        Action<MySqlDependencyHealthOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new MySqlDependencyHealthOptions();
        configure?.Invoke(options);

        return services.AddCephalonMySqlDependencyHealth(options);
    }

    private static IServiceCollection AddCephalonMySqlDependencyHealth(
        this IServiceCollection services,
        MySqlDependencyHealthOptions options)
    {
        if (options.Dependencies.Count == 0)
        {
            return services;
        }

        services.TryAddSingleton(options);
        services.TryAddSingleton<IMySqlDependencyProbeClient, MySqlDependencyProbeClient>();
        services.TryAddSingleton<MySqlDependencyHealthStore>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDiagnosticsConventionContributor, MySqlDependencyHealthDiagnosticsConventionContributor>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDependencyHealthContributor, MySqlDependencyHealthContributor>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, MySqlDependencyHealthProbeHostedService>());

        return services;
    }
}
