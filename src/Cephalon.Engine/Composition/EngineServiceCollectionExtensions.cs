using Cephalon.Engine.Configuration;
using Cephalon.Engine.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Cephalon.Engine.Runtime;

namespace Cephalon.Engine.Composition;

/// <summary>
/// Adds the Cephalon runtime and its supporting services to an <see cref="IServiceCollection" />.
/// </summary>
public static class EngineServiceCollectionExtensions
{
    /// <summary>
    /// Adds Cephalon using configuration as the primary source of engine settings.
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
    public static IServiceCollection AddCephalon(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<EngineBuilder>? configure = null,
        string sectionPath = EngineSettings.SectionName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        return AddRuntime(services, builder =>
        {
            builder.UseConfiguration(configuration, sectionPath);
            configure?.Invoke(builder);
        });
    }

    /// <summary>
    /// Adds Cephalon using code-first configuration.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="configure">The callback that configures the engine builder.</param>
    /// <returns>The same service collection for further registration.</returns>
    public static IServiceCollection AddCephalon(
        this IServiceCollection services,
        Action<EngineBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        return AddRuntime(services, configure);
    }

    private static IServiceCollection AddRuntime(
        IServiceCollection services,
        Action<EngineBuilder> configure)
    {
        var builder = new EngineBuilder(services);
        configure(builder);

        var runtime = builder.Build();
        services.TryAddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.TryAddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.TryAddSingleton<TimeProvider>(TimeProvider.System);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDiagnosticsConventionContributor, EngineDiagnosticsConventionContributor>());
        services.AddSingleton<IRuntime>(runtime);
        services.AddSingleton(runtime);
        services.AddSingleton(runtime.Manifest);
        services.AddSingleton<RuntimeHealthEvaluator>();
        services.AddSingleton<RuntimeDiagnosticsCatalogSnapshot>(serviceProvider =>
            new RuntimeDiagnosticsCatalogSnapshot(serviceProvider.GetServices<IDiagnosticsConventionContributor>()));
        services.AddSingleton<IRuntimeDiagnosticsCatalog>(serviceProvider =>
            serviceProvider.GetRequiredService<RuntimeDiagnosticsCatalogSnapshot>());
        services.AddSingleton<IRuntimeIntrospectionSnapshotProvider, RuntimeIntrospectionSnapshotProvider>();

        return services;
    }
}
