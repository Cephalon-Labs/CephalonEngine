using Cephalon.Observability.DependencyHealth.Core.Hosting;
using Cephalon.Observability.DependencyHealth.Core.Services;
using Cephalon.Observability.MqttDependencies.Configuration;
using Cephalon.Observability.MqttDependencies.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Cephalon.Observability.MqttDependencies.Hosting;

/// <summary>
/// Adds MQTT dependency-health services to a Cephalon host.
/// </summary>
public static class MqttDependencyHealthServiceCollectionExtensions
{
    /// <summary>
    /// Adds MQTT dependency-health services using configuration as the primary source of probe settings.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="configure">
    /// An optional callback that can extend or override the configuration-driven dependency-health setup.
    /// </param>
    /// <returns>The same service collection for further registration.</returns>
    public static IServiceCollection AddCephalonMqttDependencyHealth(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<MqttDependencyHealthOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = MqttDependencyHealthOptions.FromConfiguration(configuration);
        configure?.Invoke(options);

        return services.AddCephalonMqttDependencyHealth(options);
    }

    /// <summary>
    /// Adds MQTT dependency-health services using code-first configuration.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="configure">An optional callback that configures dependency-health options.</param>
    /// <returns>The same service collection for further registration.</returns>
    public static IServiceCollection AddCephalonMqttDependencyHealth(
        this IServiceCollection services,
        Action<MqttDependencyHealthOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new MqttDependencyHealthOptions();
        configure?.Invoke(options);

        return services.AddCephalonMqttDependencyHealth(options);
    }

    private static IServiceCollection AddCephalonMqttDependencyHealth(
        this IServiceCollection services,
        MqttDependencyHealthOptions options)
    {
        if (options.Dependencies.Count == 0)
        {
            return services;
        }

        services.TryAddSingleton<IMqttDependencyProbeClient, MqttDependencyProbeClient>();

        return DependencyHealthServiceRegistration.AddDependencyHealth<
            MqttDependencyHealthOptions,
            MqttDependencyDefinition,
            MqttDependencyHealthDiagnosticsConventionContributor>(
            services,
            options,
            (sp, store) => new MqttDependencyHealthProbeHostedService(
                options,
                sp.GetRequiredService<IMqttDependencyProbeClient>(),
                store,
                sp.GetRequiredService<ILogger<MqttDependencyHealthProbeHostedService>>()));
    }
}
