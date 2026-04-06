using Cephalon.Observability.DependencyHealth.Core.Hosting;
using Cephalon.Observability.DependencyHealth.Core.Services;
using Cephalon.Observability.OpenSearchDependencies.Configuration;
using Cephalon.Observability.OpenSearchDependencies.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Cephalon.Observability.OpenSearchDependencies.Hosting;

/// <summary>
/// Adds OpenSearch dependency-health services to a Cephalon host.
/// </summary>
public static class OpenSearchDependencyHealthServiceCollectionExtensions
{
    internal const string HttpClientName = "Cephalon.Observability.OpenSearchDependencies";

    /// <summary>
    /// Adds OpenSearch dependency-health services using configuration as the primary source of probe settings.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="configure">
    /// An optional callback that can extend or override the configuration-driven dependency-health setup.
    /// </param>
    /// <returns>The same service collection for further registration.</returns>
    public static IServiceCollection AddCephalonOpenSearchDependencyHealth(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<OpenSearchDependencyHealthOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = OpenSearchDependencyHealthOptions.FromConfiguration(configuration);
        configure?.Invoke(options);

        return services.AddCephalonOpenSearchDependencyHealth(options);
    }

    /// <summary>
    /// Adds OpenSearch dependency-health services using code-first configuration.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="configure">An optional callback that configures dependency-health options.</param>
    /// <returns>The same service collection for further registration.</returns>
    public static IServiceCollection AddCephalonOpenSearchDependencyHealth(
        this IServiceCollection services,
        Action<OpenSearchDependencyHealthOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new OpenSearchDependencyHealthOptions();
        configure?.Invoke(options);

        return services.AddCephalonOpenSearchDependencyHealth(options);
    }

    private static IServiceCollection AddCephalonOpenSearchDependencyHealth(
        this IServiceCollection services,
        OpenSearchDependencyHealthOptions options)
    {
        if (options.Dependencies.Count == 0)
        {
            return services;
        }

        services.AddHttpClient(HttpClientName)
            .ConfigureHttpClient(client => client.Timeout = Timeout.InfiniteTimeSpan);
        services.TryAddSingleton<IOpenSearchDependencyProbeClient, OpenSearchDependencyProbeClient>();

        return DependencyHealthServiceRegistration.AddDependencyHealth<
            OpenSearchDependencyHealthOptions,
            OpenSearchDependencyDefinition,
            OpenSearchDependencyHealthDiagnosticsConventionContributor>(
            services,
            options,
            (sp, store) => new OpenSearchDependencyHealthProbeHostedService(
                options,
                sp.GetRequiredService<IOpenSearchDependencyProbeClient>(),
                store,
                sp.GetRequiredService<ILogger<OpenSearchDependencyHealthProbeHostedService>>()));
    }
}
