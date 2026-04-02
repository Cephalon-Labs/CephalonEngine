using Cephalon.Abstractions.Health;
using Cephalon.Engine.Diagnostics;
using Cephalon.Observability.HttpDependencies.Configuration;
using Cephalon.Observability.HttpDependencies.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Cephalon.Observability.HttpDependencies.Hosting;

/// <summary>
/// Adds HTTP and external API dependency-health services to a Cephalon host.
/// </summary>
public static class HttpDependencyHealthServiceCollectionExtensions
{
    internal const string HttpClientName = "Cephalon.Observability.HttpDependencies";

    /// <summary>
    /// Adds HTTP dependency-health services using configuration as the primary source of probe settings.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="configure">
    /// An optional callback that can extend or override the configuration-driven dependency-health setup.
    /// </param>
    /// <returns>The same service collection for further registration.</returns>
    public static IServiceCollection AddCephalonHttpDependencyHealth(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<HttpDependencyHealthOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = HttpDependencyHealthOptions.FromConfiguration(configuration);
        configure?.Invoke(options);

        return services.AddCephalonHttpDependencyHealth(options);
    }

    /// <summary>
    /// Adds HTTP dependency-health services using code-first configuration.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="configure">An optional callback that configures dependency-health options.</param>
    /// <returns>The same service collection for further registration.</returns>
    public static IServiceCollection AddCephalonHttpDependencyHealth(
        this IServiceCollection services,
        Action<HttpDependencyHealthOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new HttpDependencyHealthOptions();
        configure?.Invoke(options);

        return services.AddCephalonHttpDependencyHealth(options);
    }

    private static IServiceCollection AddCephalonHttpDependencyHealth(
        this IServiceCollection services,
        HttpDependencyHealthOptions options)
    {
        if (options.Dependencies.Count == 0)
        {
            return services;
        }

        services.TryAddSingleton(options);
        services.AddHttpClient(HttpClientName)
            .ConfigureHttpClient(client => client.Timeout = Timeout.InfiniteTimeSpan);
        services.TryAddSingleton<HttpDependencyHealthStore>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDiagnosticsConventionContributor, HttpDependencyHealthDiagnosticsConventionContributor>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDependencyHealthContributor, HttpDependencyHealthContributor>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, HttpDependencyHealthProbeHostedService>());

        return services;
    }
}
