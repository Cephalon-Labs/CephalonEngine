using Cephalon.Observability.DependencyHealth.Core.Hosting;
using Cephalon.Observability.DependencyHealth.Core.Services;
using Cephalon.Observability.RedisDependencies.Configuration;
using Cephalon.Observability.RedisDependencies.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cephalon.Observability.RedisDependencies.Hosting;

/// <summary>
/// Adds Redis dependency-health services to a Cephalon host.
/// </summary>
public static class RedisDependencyHealthServiceCollectionExtensions
{
    /// <summary>
    /// Adds Redis dependency-health services using configuration as the primary source of probe settings.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="configure">
    /// An optional callback that can extend or override the configuration-driven dependency-health setup.
    /// </param>
    /// <returns>The same service collection for further registration.</returns>
    public static IServiceCollection AddCephalonRedisDependencyHealth(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<RedisDependencyHealthOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = RedisDependencyHealthOptions.FromConfiguration(configuration);
        configure?.Invoke(options);

        return services.AddCephalonRedisDependencyHealth(options);
    }

    /// <summary>
    /// Adds Redis dependency-health services using code-first configuration.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="configure">An optional callback that configures dependency-health options.</param>
    /// <returns>The same service collection for further registration.</returns>
    public static IServiceCollection AddCephalonRedisDependencyHealth(
        this IServiceCollection services,
        Action<RedisDependencyHealthOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new RedisDependencyHealthOptions();
        configure?.Invoke(options);

        return services.AddCephalonRedisDependencyHealth(options);
    }

    private static IServiceCollection AddCephalonRedisDependencyHealth(
        this IServiceCollection services,
        RedisDependencyHealthOptions options)
    {
        if (options.Dependencies.Count == 0)
        {
            return services;
        }

        return DependencyHealthServiceRegistration.AddDependencyHealth<
            RedisDependencyHealthOptions,
            RedisDependencyDefinition,
            RedisDependencyHealthDiagnosticsConventionContributor>(
            services,
            options,
            (sp, store) => new RedisDependencyHealthProbeHostedService(
                options,
                store,
                sp.GetRequiredService<ILogger<RedisDependencyHealthProbeHostedService>>()));
    }
}
