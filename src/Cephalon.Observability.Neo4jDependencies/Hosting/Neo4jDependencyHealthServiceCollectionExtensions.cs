using Cephalon.Observability.DependencyHealth.Core.Hosting;
using Cephalon.Observability.DependencyHealth.Core.Services;
using Cephalon.Observability.Neo4jDependencies.Configuration;
using Cephalon.Observability.Neo4jDependencies.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Cephalon.Observability.Neo4jDependencies.Hosting;

/// <summary>
/// Adds Neo4j dependency-health services to a Cephalon host.
/// </summary>
public static class Neo4jDependencyHealthServiceCollectionExtensions
{
    /// <summary>
    /// Adds Neo4j dependency-health services using configuration as the primary source of probe settings.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="configure">
    /// An optional callback that can extend or override the configuration-driven dependency-health setup.
    /// </param>
    /// <returns>The same service collection for further registration.</returns>
    public static IServiceCollection AddCephalonNeo4jDependencyHealth(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<Neo4jDependencyHealthOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = Neo4jDependencyHealthOptions.FromConfiguration(configuration);
        configure?.Invoke(options);

        return services.AddCephalonNeo4jDependencyHealth(options);
    }

    /// <summary>
    /// Adds Neo4j dependency-health services using code-first configuration.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="configure">An optional callback that configures dependency-health options.</param>
    /// <returns>The same service collection for further registration.</returns>
    public static IServiceCollection AddCephalonNeo4jDependencyHealth(
        this IServiceCollection services,
        Action<Neo4jDependencyHealthOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new Neo4jDependencyHealthOptions();
        configure?.Invoke(options);

        return services.AddCephalonNeo4jDependencyHealth(options);
    }

    private static IServiceCollection AddCephalonNeo4jDependencyHealth(
        this IServiceCollection services,
        Neo4jDependencyHealthOptions options)
    {
        if (options.Dependencies.Count == 0)
        {
            return services;
        }

        services.TryAddSingleton<INeo4jDependencyProbeClient, Neo4jDependencyProbeClient>();

        return DependencyHealthServiceRegistration.AddDependencyHealth<
            Neo4jDependencyHealthOptions,
            Neo4jDependencyDefinition,
            Neo4jDependencyHealthDiagnosticsConventionContributor>(
            services,
            options,
            (sp, store) => new Neo4jDependencyHealthProbeHostedService(
                options,
                sp.GetRequiredService<INeo4jDependencyProbeClient>(),
                store,
                sp.GetRequiredService<ILogger<Neo4jDependencyHealthProbeHostedService>>()));
    }
}
