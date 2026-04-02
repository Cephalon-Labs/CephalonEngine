using Cephalon.Abstractions.Health;
using Cephalon.Engine.Diagnostics;
using Cephalon.Observability.MongoDbDependencies.Configuration;
using Cephalon.Observability.MongoDbDependencies.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Cephalon.Observability.MongoDbDependencies.Hosting;

/// <summary>
/// Adds MongoDB dependency-health services to a Cephalon host.
/// </summary>
public static class MongoDbDependencyHealthServiceCollectionExtensions
{
    /// <summary>
    /// Adds MongoDB dependency-health services using configuration as the primary source of probe settings.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="configure">
    /// An optional callback that can extend or override the configuration-driven dependency-health setup.
    /// </param>
    /// <returns>The same service collection for further registration.</returns>
    public static IServiceCollection AddCephalonMongoDbDependencyHealth(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<MongoDbDependencyHealthOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = MongoDbDependencyHealthOptions.FromConfiguration(configuration);
        configure?.Invoke(options);

        return services.AddCephalonMongoDbDependencyHealth(options);
    }

    /// <summary>
    /// Adds MongoDB dependency-health services using code-first configuration.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="configure">An optional callback that configures dependency-health options.</param>
    /// <returns>The same service collection for further registration.</returns>
    public static IServiceCollection AddCephalonMongoDbDependencyHealth(
        this IServiceCollection services,
        Action<MongoDbDependencyHealthOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new MongoDbDependencyHealthOptions();
        configure?.Invoke(options);

        return services.AddCephalonMongoDbDependencyHealth(options);
    }

    private static IServiceCollection AddCephalonMongoDbDependencyHealth(
        this IServiceCollection services,
        MongoDbDependencyHealthOptions options)
    {
        if (options.Dependencies.Count == 0)
        {
            return services;
        }

        services.TryAddSingleton(options);
        services.TryAddSingleton<IMongoDbDependencyProbeClient, MongoDbDependencyProbeClient>();
        services.TryAddSingleton<MongoDbDependencyHealthStore>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDiagnosticsConventionContributor, MongoDbDependencyHealthDiagnosticsConventionContributor>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDependencyHealthContributor, MongoDbDependencyHealthContributor>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, MongoDbDependencyHealthProbeHostedService>());

        return services;
    }
}
