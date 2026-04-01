using Cephalon.Abstractions.Health;
using Cephalon.Observability.PostgresDependencies.Configuration;
using Cephalon.Observability.PostgresDependencies.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Cephalon.Observability.PostgresDependencies.Hosting;

/// <summary>
/// Adds Postgres dependency-health services to a Cephalon host.
/// </summary>
public static class PostgresDependencyHealthServiceCollectionExtensions
{
    /// <summary>
    /// Adds Postgres dependency-health services using configuration as the primary source of probe settings.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="configure">
    /// An optional callback that can extend or override the configuration-driven dependency-health setup.
    /// </param>
    /// <returns>The same service collection for further registration.</returns>
    public static IServiceCollection AddCephalonPostgresDependencyHealth(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<PostgresDependencyHealthOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = PostgresDependencyHealthOptions.FromConfiguration(configuration);
        configure?.Invoke(options);

        return services.AddCephalonPostgresDependencyHealth(options);
    }

    /// <summary>
    /// Adds Postgres dependency-health services using code-first configuration.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="configure">An optional callback that configures dependency-health options.</param>
    /// <returns>The same service collection for further registration.</returns>
    public static IServiceCollection AddCephalonPostgresDependencyHealth(
        this IServiceCollection services,
        Action<PostgresDependencyHealthOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new PostgresDependencyHealthOptions();
        configure?.Invoke(options);

        return services.AddCephalonPostgresDependencyHealth(options);
    }

    private static IServiceCollection AddCephalonPostgresDependencyHealth(
        this IServiceCollection services,
        PostgresDependencyHealthOptions options)
    {
        if (options.Dependencies.Count == 0)
        {
            return services;
        }

        services.TryAddSingleton(options);
        services.TryAddSingleton<IPostgresDependencyProbeClient, NpgsqlPostgresDependencyProbeClient>();
        services.TryAddSingleton<PostgresDependencyHealthStore>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDependencyHealthContributor, PostgresDependencyHealthContributor>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, PostgresDependencyHealthProbeHostedService>());

        return services;
    }
}
