using Cephalon.Observability.DependencyHealth.Core.Hosting;
using Cephalon.Observability.DependencyHealth.Core.Services;
using Cephalon.Observability.SqlServerDependencies.Configuration;
using Cephalon.Observability.SqlServerDependencies.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Cephalon.Observability.SqlServerDependencies.Hosting;

/// <summary>
/// Adds SQL Server dependency-health services to a Cephalon host.
/// </summary>
public static class SqlServerDependencyHealthServiceCollectionExtensions
{
    /// <summary>
    /// Adds SQL Server dependency-health services using configuration as the primary source of probe settings.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="configure">
    /// An optional callback that can extend or override the configuration-driven dependency-health setup.
    /// </param>
    /// <returns>The same service collection for further registration.</returns>
    public static IServiceCollection AddCephalonSqlServerDependencyHealth(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<SqlServerDependencyHealthOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = SqlServerDependencyHealthOptions.FromConfiguration(configuration);
        configure?.Invoke(options);

        return services.AddCephalonSqlServerDependencyHealth(options);
    }

    /// <summary>
    /// Adds SQL Server dependency-health services using code-first configuration.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="configure">An optional callback that configures dependency-health options.</param>
    /// <returns>The same service collection for further registration.</returns>
    public static IServiceCollection AddCephalonSqlServerDependencyHealth(
        this IServiceCollection services,
        Action<SqlServerDependencyHealthOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new SqlServerDependencyHealthOptions();
        configure?.Invoke(options);

        return services.AddCephalonSqlServerDependencyHealth(options);
    }

    private static IServiceCollection AddCephalonSqlServerDependencyHealth(
        this IServiceCollection services,
        SqlServerDependencyHealthOptions options)
    {
        if (options.Dependencies.Count == 0)
        {
            return services;
        }

        services.TryAddSingleton<ISqlServerDependencyProbeClient, SqlServerDependencyProbeClient>();

        return DependencyHealthServiceRegistration.AddDependencyHealth<
            SqlServerDependencyHealthOptions,
            SqlServerDependencyDefinition,
            SqlServerDependencyHealthDiagnosticsConventionContributor>(
            services,
            options,
            (sp, store) => new SqlServerDependencyHealthProbeHostedService(
                options,
                sp.GetRequiredService<ISqlServerDependencyProbeClient>(),
                store,
                sp.GetRequiredService<ILogger<SqlServerDependencyHealthProbeHostedService>>()));
    }
}
