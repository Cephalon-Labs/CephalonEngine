using Cephalon.Observability.CassandraDependencies.Configuration;
using Cephalon.Observability.CassandraDependencies.Services;
using Cephalon.Observability.DependencyHealth.Core.Hosting;
using Cephalon.Observability.DependencyHealth.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Cephalon.Observability.CassandraDependencies.Hosting;

/// <summary>
/// Adds Cassandra dependency-health services to a Cephalon host.
/// </summary>
public static class CassandraDependencyHealthServiceCollectionExtensions
{
    /// <summary>
    /// Adds Cassandra dependency-health services using configuration as the primary source of probe settings.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="configure">
    /// An optional callback that can extend or override the configuration-driven dependency-health setup.
    /// </param>
    /// <returns>The same service collection for further registration.</returns>
    public static IServiceCollection AddCephalonCassandraDependencyHealth(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<CassandraDependencyHealthOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = CassandraDependencyHealthOptions.FromConfiguration(configuration);
        configure?.Invoke(options);

        return services.AddCephalonCassandraDependencyHealth(options);
    }

    /// <summary>
    /// Adds Cassandra dependency-health services using code-first configuration.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="configure">An optional callback that configures dependency-health options.</param>
    /// <returns>The same service collection for further registration.</returns>
    public static IServiceCollection AddCephalonCassandraDependencyHealth(
        this IServiceCollection services,
        Action<CassandraDependencyHealthOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new CassandraDependencyHealthOptions();
        configure?.Invoke(options);

        return services.AddCephalonCassandraDependencyHealth(options);
    }

    private static IServiceCollection AddCephalonCassandraDependencyHealth(
        this IServiceCollection services,
        CassandraDependencyHealthOptions options)
    {
        if (options.Dependencies.Count == 0)
        {
            return services;
        }

        services.TryAddSingleton<ICassandraDependencyProbeClient, CassandraDependencyProbeClient>();

        return DependencyHealthServiceRegistration.AddDependencyHealth<
            CassandraDependencyHealthOptions,
            CassandraDependencyDefinition,
            CassandraDependencyHealthDiagnosticsConventionContributor>(
            services,
            options,
            (sp, store) => new CassandraDependencyHealthProbeHostedService(
                options,
                sp.GetRequiredService<ICassandraDependencyProbeClient>(),
                store,
                sp.GetRequiredService<ILogger<CassandraDependencyHealthProbeHostedService>>()));
    }
}
