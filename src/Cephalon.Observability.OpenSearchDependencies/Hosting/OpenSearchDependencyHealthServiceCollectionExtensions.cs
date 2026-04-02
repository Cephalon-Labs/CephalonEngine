using Cephalon.Abstractions.Health;
using Cephalon.Engine.Diagnostics;
using Cephalon.Observability.OpenSearchDependencies.Configuration;
using Cephalon.Observability.OpenSearchDependencies.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

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

        services.TryAddSingleton(options);
        services.AddHttpClient(HttpClientName)
            .ConfigureHttpClient(client => client.Timeout = Timeout.InfiniteTimeSpan);
        services.TryAddSingleton<IOpenSearchDependencyProbeClient, OpenSearchDependencyProbeClient>();
        services.TryAddSingleton<OpenSearchDependencyHealthStore>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDiagnosticsConventionContributor, OpenSearchDependencyHealthDiagnosticsConventionContributor>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDependencyHealthContributor, OpenSearchDependencyHealthContributor>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, OpenSearchDependencyHealthProbeHostedService>());

        return services;
    }
}
