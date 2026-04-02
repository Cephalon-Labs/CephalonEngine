using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Cephalon.Observability.Serilog.Hosting;

/// <summary>
/// Adds Serilog provider wiring for Cephalon hosts without changing the shared <see cref="Microsoft.Extensions.Logging.ILogger" /> contract.
/// </summary>
public static class SerilogHostApplicationBuilderExtensions
{
    /// <summary>
    /// Adds Serilog as an <see cref="Microsoft.Extensions.Logging.ILogger" /> provider for the target host builder.
    /// </summary>
    /// <typeparam name="TBuilder">The host-application builder type to extend.</typeparam>
    /// <param name="builder">The target host-application builder.</param>
    /// <param name="configure">
    /// An optional callback that can extend or override the configuration-driven Serilog pipeline.
    /// </param>
    /// <returns>The same builder instance for fluent host composition.</returns>
    /// <remarks>
    /// <para>
    /// This package keeps provider-specific logging integration outside <c>Cephalon.Engine</c> and
    /// <c>Cephalon.Observability</c>. Hosts opt in explicitly when they want Serilog sinks, enrichers,
    /// or formatting while still logging through injected <c>ILogger&lt;T&gt;</c> services.
    /// </para>
    /// <para>
    /// The standard top-level <c>Serilog</c> configuration section is read automatically when present.
    /// If no <c>Serilog</c> section exists and no code-based configuration callback is supplied, registration
    /// is skipped so hosts do not accidentally replace their existing logging setup with an empty pipeline.
    /// </para>
    /// </remarks>
    public static TBuilder AddCephalonSerilog<TBuilder>(
        this TBuilder builder,
        Action<IServiceProvider, LoggerConfiguration>? configure = null)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        var hasConfiguration = HasSerilogConfiguration(builder.Configuration);
        if (!hasConfiguration && configure is null)
        {
            return builder;
        }

        builder.Services.AddSerilog((services, loggerConfiguration) =>
        {
            if (hasConfiguration)
            {
                loggerConfiguration.ReadFrom.Configuration(builder.Configuration);
            }

            loggerConfiguration.ReadFrom.Services(services);
            loggerConfiguration.Enrich.FromLogContext();
            configure?.Invoke(services, loggerConfiguration);
        });

        return builder;
    }

    private static bool HasSerilogConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return configuration.GetSection("Serilog").Exists();
    }
}
