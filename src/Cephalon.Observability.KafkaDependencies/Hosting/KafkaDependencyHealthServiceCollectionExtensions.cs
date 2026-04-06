using Cephalon.Observability.DependencyHealth.Core.Hosting;
using Cephalon.Observability.DependencyHealth.Core.Services;
using Cephalon.Observability.KafkaDependencies.Configuration;
using Cephalon.Observability.KafkaDependencies.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Cephalon.Observability.KafkaDependencies.Hosting;

/// <summary>
/// Adds Kafka dependency-health services to a Cephalon host.
/// </summary>
public static class KafkaDependencyHealthServiceCollectionExtensions
{
    /// <summary>
    /// Adds Kafka dependency-health services using configuration as the primary source of probe settings.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="configure">
    /// An optional callback that can extend or override the configuration-driven dependency-health setup.
    /// </param>
    /// <returns>The same service collection for further registration.</returns>
    public static IServiceCollection AddCephalonKafkaDependencyHealth(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<KafkaDependencyHealthOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = KafkaDependencyHealthOptions.FromConfiguration(configuration);
        configure?.Invoke(options);

        return services.AddCephalonKafkaDependencyHealth(options);
    }

    /// <summary>
    /// Adds Kafka dependency-health services using code-first configuration.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="configure">An optional callback that configures dependency-health options.</param>
    /// <returns>The same service collection for further registration.</returns>
    public static IServiceCollection AddCephalonKafkaDependencyHealth(
        this IServiceCollection services,
        Action<KafkaDependencyHealthOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new KafkaDependencyHealthOptions();
        configure?.Invoke(options);

        return services.AddCephalonKafkaDependencyHealth(options);
    }

    private static IServiceCollection AddCephalonKafkaDependencyHealth(
        this IServiceCollection services,
        KafkaDependencyHealthOptions options)
    {
        if (options.Dependencies.Count == 0)
        {
            return services;
        }

        services.TryAddSingleton<IKafkaDependencyProbeClient, KafkaDependencyProbeClient>();

        return DependencyHealthServiceRegistration.AddDependencyHealth<
            KafkaDependencyHealthOptions,
            KafkaDependencyDefinition,
            KafkaDependencyHealthDiagnosticsConventionContributor>(
            services,
            options,
            (sp, store) => new KafkaDependencyHealthProbeHostedService(
                options,
                sp.GetRequiredService<IKafkaDependencyProbeClient>(),
                store,
                sp.GetRequiredService<ILogger<KafkaDependencyHealthProbeHostedService>>()));
    }
}
