using Cephalon.Observability.DependencyHealth.Core.Hosting;
using Cephalon.Observability.DependencyHealth.Core.Services;
using Cephalon.Observability.ElasticsearchDependencies.Configuration;
using Cephalon.Observability.ElasticsearchDependencies.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Cephalon.Observability.ElasticsearchDependencies.Hosting;

/// <summary>
/// Adds Elasticsearch dependency-health services to a Cephalon host.
/// </summary>
public static class ElasticsearchDependencyHealthServiceCollectionExtensions
{
    internal const string HttpClientName = "Cephalon.Observability.ElasticsearchDependencies";

    /// <summary>
    /// Adds Elasticsearch dependency-health services using configuration as the primary source of probe settings.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="configure">
    /// An optional callback that can extend or override the configuration-driven dependency-health setup.
    /// </param>
    /// <returns>The same service collection for further registration.</returns>
    public static IServiceCollection AddCephalonElasticsearchDependencyHealth(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<ElasticsearchDependencyHealthOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = ElasticsearchDependencyHealthOptions.FromConfiguration(configuration);
        configure?.Invoke(options);

        return services.AddCephalonElasticsearchDependencyHealth(options);
    }

    /// <summary>
    /// Adds Elasticsearch dependency-health services using code-first configuration.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="configure">An optional callback that configures dependency-health options.</param>
    /// <returns>The same service collection for further registration.</returns>
    public static IServiceCollection AddCephalonElasticsearchDependencyHealth(
        this IServiceCollection services,
        Action<ElasticsearchDependencyHealthOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new ElasticsearchDependencyHealthOptions();
        configure?.Invoke(options);

        return services.AddCephalonElasticsearchDependencyHealth(options);
    }

    private static IServiceCollection AddCephalonElasticsearchDependencyHealth(
        this IServiceCollection services,
        ElasticsearchDependencyHealthOptions options)
    {
        if (options.Dependencies.Count == 0)
        {
            return services;
        }

        services.AddHttpClient(HttpClientName)
            .ConfigureHttpClient(client => client.Timeout = Timeout.InfiniteTimeSpan);
        services.TryAddSingleton<IElasticsearchDependencyProbeClient, ElasticsearchDependencyProbeClient>();

        return DependencyHealthServiceRegistration.AddDependencyHealth<
            ElasticsearchDependencyHealthOptions,
            ElasticsearchDependencyDefinition,
            ElasticsearchDependencyHealthDiagnosticsConventionContributor>(
            services,
            options,
            (sp, store) => new ElasticsearchDependencyHealthProbeHostedService(
                options,
                sp.GetRequiredService<IElasticsearchDependencyProbeClient>(),
                store,
                sp.GetRequiredService<ILogger<ElasticsearchDependencyHealthProbeHostedService>>()));
    }
}
