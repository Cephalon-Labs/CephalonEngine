using Cephalon.Observability.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Cephalon.Observability.Hosting;

/// <summary>
/// Adds the Cephalon observability package to an <see cref="IServiceCollection" />.
/// </summary>
public static class ObservabilityServiceCollectionExtensions
{
    /// <summary>
    /// Adds observability services using configuration as the primary source of observability options.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="configure">
    /// An optional callback that can extend or override the configuration-driven observability setup.
    /// </param>
    /// <returns>The same service collection for further registration.</returns>
    public static IServiceCollection AddCephalonObservability(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<ObservabilityOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = ObservabilityOptions.FromConfiguration(configuration);
        configure?.Invoke(options);

        return services.AddCephalonObservability(options);
    }

    /// <summary>
    /// Adds observability services using code-first configuration.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="configure">An optional callback that configures observability options.</param>
    /// <returns>The same service collection for further registration.</returns>
    public static IServiceCollection AddCephalonObservability(
        this IServiceCollection services,
        Action<ObservabilityOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new ObservabilityOptions();
        configure?.Invoke(options);

        return services.AddCephalonObservability(options);
    }

    private static IServiceCollection AddCephalonObservability(
        this IServiceCollection services,
        ObservabilityOptions options)
    {
        services.TryAddSingleton(options);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, ManifestSummaryHostedService>());

        return services;
    }
}
