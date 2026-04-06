using Cephalon.Observability.DependencyHealth.Core.Hosting;
using Cephalon.Observability.DependencyHealth.Core.Services;
using Cephalon.Observability.NatsDependencies.Configuration;
using Cephalon.Observability.NatsDependencies.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Cephalon.Observability.NatsDependencies.Hosting;

/// <summary>
/// Adds NATS dependency-health services to a Cephalon host.
/// </summary>
public static class NatsDependencyHealthServiceCollectionExtensions
{
    /// <summary>
    /// Adds NATS dependency-health services using configuration as the primary source of probe settings.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="configure">
    /// An optional callback that can extend or override the configuration-driven dependency-health setup.
    /// </param>
    /// <returns>The same service collection for further registration.</returns>
    public static IServiceCollection AddCephalonNatsDependencyHealth(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<NatsDependencyHealthOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = NatsDependencyHealthOptions.FromConfiguration(configuration);
        configure?.Invoke(options);

        return services.AddCephalonNatsDependencyHealth(options);
    }

    /// <summary>
    /// Adds NATS dependency-health services using code-first configuration.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="configure">An optional callback that configures dependency-health options.</param>
    /// <returns>The same service collection for further registration.</returns>
    public static IServiceCollection AddCephalonNatsDependencyHealth(
        this IServiceCollection services,
        Action<NatsDependencyHealthOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new NatsDependencyHealthOptions();
        configure?.Invoke(options);

        return services.AddCephalonNatsDependencyHealth(options);
    }

    private static IServiceCollection AddCephalonNatsDependencyHealth(
        this IServiceCollection services,
        NatsDependencyHealthOptions options)
    {
        if (options.Dependencies.Count == 0)
        {
            return services;
        }

        services.TryAddSingleton<INatsDependencyProbeClient, NatsDependencyProbeClient>();

        return DependencyHealthServiceRegistration.AddDependencyHealth<
            NatsDependencyHealthOptions,
            NatsDependencyDefinition,
            NatsDependencyHealthDiagnosticsConventionContributor>(
            services,
            options,
            (sp, store) => new NatsDependencyHealthProbeHostedService(
                options,
                sp.GetRequiredService<INatsDependencyProbeClient>(),
                store,
                sp.GetRequiredService<ILogger<NatsDependencyHealthProbeHostedService>>()));
    }
}
