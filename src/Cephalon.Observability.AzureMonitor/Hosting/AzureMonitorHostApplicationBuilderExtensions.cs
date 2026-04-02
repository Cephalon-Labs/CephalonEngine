using System.Reflection;
using Azure.Identity;
using Azure.Monitor.OpenTelemetry.Exporter;
using Cephalon.Engine.Diagnostics;
using Cephalon.Observability.AzureMonitor.Configuration;
using Cephalon.Observability.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Cephalon.Observability.AzureMonitor.Hosting;

/// <summary>
/// Adds Azure Monitor exporter wiring for Cephalon hosts.
/// </summary>
public static class AzureMonitorHostApplicationBuilderExtensions
{
    /// <summary>
    /// Adds Azure Monitor exporter registration for the Cephalon engine diagnostics surface.
    /// </summary>
    /// <typeparam name="TBuilder">The host-application builder type to extend.</typeparam>
    /// <param name="builder">The target host-application builder.</param>
    /// <param name="configure">
    /// An optional callback that can extend or override the configuration-driven Azure Monitor export options.
    /// </param>
    /// <returns>The same builder instance for fluent host composition.</returns>
    /// <remarks>
    /// <para>
    /// This package keeps Azure-specific exporter wiring outside <c>Cephalon.Engine</c> and
    /// <c>Cephalon.Observability</c>. Hosts opt in explicitly when they want Azure Monitor /
    /// Application Insights export over the same shared <c>ILogger</c> and OpenTelemetry baseline.
    /// </para>
    /// <para>
    /// Registration is skipped when every signal is disabled or when no Azure Monitor connection string
    /// is configured. When <c>HostedPlatform</c> is supplied, the package adds Azure-specific
    /// <c>cloud.provider</c>, <c>cloud.platform</c>, and <c>deployment.environment.name</c> resource
    /// attributes on top of the existing service-name and service-version defaults.
    /// </para>
    /// </remarks>
    public static TBuilder AddCephalonAzureMonitor<TBuilder>(
        this TBuilder builder,
        Action<AzureMonitorExportOptions>? configure = null)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        var telemetry = ObservabilityOptions.FromConfiguration(builder.Configuration).Telemetry;
        var options = AzureMonitorExportOptions.FromConfiguration(builder.Configuration);
        configure?.Invoke(options);

        if (!ShouldRegister(telemetry, options))
        {
            return builder;
        }

        EnsureProviderIsSupported(telemetry.Provider);

        builder.Services.AddSingleton(telemetry);
        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton<IDiagnosticsConventionContributor, AzureMonitorDiagnosticsConventionContributor>();
        builder.Services.AddHostedService<AzureMonitorSummaryHostedService>();

        var serviceName = ResolveServiceName(builder.Environment.ApplicationName);
        var serviceVersion = ResolveServiceVersion();
        var platformAttributes = ResolveHostedPlatformAttributes(options.HostedPlatform, builder.Environment.EnvironmentName);
        var credential = options.UseDefaultAzureCredential ? new DefaultAzureCredential() : null;

        var openTelemetry = builder.Services
            .AddOpenTelemetry()
            .ConfigureResource(resource =>
            {
                resource.AddService(serviceName, serviceVersion: serviceVersion);
                AddAzureResourceAttributes(resource, platformAttributes);
            });

        if (telemetry.ExportTraces)
        {
            openTelemetry.WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation();
                tracing.AddSource(EngineDiagnostics.ActivitySourceName);
                tracing.AddAzureMonitorTraceExporter(exporter => ConfigureExporter(exporter, options), credential);
            });
        }

        if (telemetry.ExportMetrics)
        {
            openTelemetry.WithMetrics(metrics =>
            {
                metrics.AddMeter(EngineDiagnostics.MeterName);
                metrics.AddAzureMonitorMetricExporter(exporter => ConfigureExporter(exporter, options), credential);
            });
        }

        if (telemetry.ExportLogs)
        {
            builder.Logging.AddOpenTelemetry(logging =>
            {
                logging.IncludeFormattedMessage = true;
                logging.IncludeScopes = true;
                logging.ParseStateValues = true;
                logging.SetResourceBuilder(BuildResourceBuilder(serviceName, serviceVersion, platformAttributes));
                logging.AddAzureMonitorLogExporter(exporter => ConfigureExporter(exporter, options), credential);
            });
        }

        return builder;
    }

    internal static string? NormalizeHostedPlatform(string hostedPlatform)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hostedPlatform);

        return hostedPlatform.Trim().ToLowerInvariant() switch
        {
            "appservice" => "azure.app_service",
            "app-service" => "azure.app_service",
            "azure.app_service" => "azure.app_service",
            "functions" => "azure.functions",
            "azure.functions" => "azure.functions",
            "aks" => "azure.aks",
            "azure.aks" => "azure.aks",
            "containerapps" => "azure.container_apps",
            "container-apps" => "azure.container_apps",
            "azure.container_apps" => "azure.container_apps",
            "vm" => "azure.vm",
            "azure.vm" => "azure.vm",
            _ => null
        };
    }

    private static bool ShouldRegister(TelemetryExportOptions telemetry, AzureMonitorExportOptions options)
    {
        ArgumentNullException.ThrowIfNull(telemetry);
        ArgumentNullException.ThrowIfNull(options);

        return !string.IsNullOrWhiteSpace(options.ConnectionString) &&
            (telemetry.ExportLogs || telemetry.ExportMetrics || telemetry.ExportTraces);
    }

    private static void EnsureProviderIsSupported(string provider)
    {
        if (string.Equals(provider, "OpenTelemetry", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        throw new InvalidOperationException(
            $"Cephalon Azure Monitor integration only supports telemetry provider 'OpenTelemetry'. Configured provider '{provider}' is not supported.");
    }

    private static string ResolveServiceName(string? applicationName)
    {
        return string.IsNullOrWhiteSpace(applicationName)
            ? "Cephalon.Host"
            : applicationName.Trim();
    }

    private static string? ResolveServiceVersion()
    {
        var entryAssembly = Assembly.GetEntryAssembly();
        return entryAssembly?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion
            ?? entryAssembly?.GetName().Version?.ToString();
    }

    private static void ConfigureExporter(AzureMonitorExporterOptions exporter, AzureMonitorExportOptions options)
    {
        ArgumentNullException.ThrowIfNull(exporter);
        ArgumentNullException.ThrowIfNull(options);

        exporter.ConnectionString = options.ConnectionString;
    }

    private static ResourceBuilder BuildResourceBuilder(
        string serviceName,
        string? serviceVersion,
        IReadOnlyList<KeyValuePair<string, object>> platformAttributes)
    {
        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(serviceName, serviceVersion: serviceVersion);
        AddAzureResourceAttributes(resourceBuilder, platformAttributes);
        return resourceBuilder;
    }

    private static IReadOnlyList<KeyValuePair<string, object>> ResolveHostedPlatformAttributes(
        string? hostedPlatform,
        string? environmentName)
    {
        if (string.IsNullOrWhiteSpace(hostedPlatform))
        {
            return Array.Empty<KeyValuePair<string, object>>();
        }

        var normalizedPlatform = NormalizeHostedPlatform(hostedPlatform);
        if (normalizedPlatform is null)
        {
            throw new InvalidOperationException(
                $"Azure hosted platform '{hostedPlatform}' is not supported. Use 'appservice', 'functions', 'aks', 'containerapps', or 'vm'.");
        }

        var attributes = new List<KeyValuePair<string, object>>
        {
            new("cloud.provider", "azure"),
            new("cloud.platform", normalizedPlatform)
        };

        if (!string.IsNullOrWhiteSpace(environmentName))
        {
            attributes.Add(new KeyValuePair<string, object>(
                "deployment.environment.name",
                environmentName.Trim()));
        }

        return attributes;
    }

    private static void AddAzureResourceAttributes(
        ResourceBuilder resourceBuilder,
        IReadOnlyList<KeyValuePair<string, object>> platformAttributes)
    {
        ArgumentNullException.ThrowIfNull(resourceBuilder);
        ArgumentNullException.ThrowIfNull(platformAttributes);

        if (platformAttributes.Count > 0)
        {
            resourceBuilder.AddAttributes(platformAttributes);
        }
    }
}
