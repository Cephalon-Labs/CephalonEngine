using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Cephalon.Worker.Hosting;

/// <summary>
/// Adds the Cephalon worker adapter and runtime services to an <see cref="IServiceCollection" />.
/// </summary>
public static class WorkerServiceCollectionExtensions
{
    /// <summary>
    /// Adds Cephalon worker hosting using configuration as the primary source of engine settings.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="configure">
    /// An optional callback that can extend or override the configuration-driven engine setup.
    /// </param>
    /// <param name="sectionPath">
    /// The configuration section path that contains the engine settings. The default is <c>Engine</c>.
    /// </param>
    /// <returns>The same service collection for further registration.</returns>
    public static IServiceCollection AddCephalonWorker(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<EngineBuilder>? configure = null,
        string sectionPath = EngineSettings.SectionName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, RuntimeHostedService>());
        services.AddCephalon(configuration, configure, sectionPath);

        return services;
    }

    /// <summary>
    /// Adds Cephalon worker hosting using code-first engine configuration.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="configure">The callback that configures the engine builder.</param>
    /// <returns>The same service collection for further registration.</returns>
    public static IServiceCollection AddCephalonWorker(
        this IServiceCollection services,
        Action<EngineBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, RuntimeHostedService>());
        services.AddCephalon(configure);

        return services;
    }
}
