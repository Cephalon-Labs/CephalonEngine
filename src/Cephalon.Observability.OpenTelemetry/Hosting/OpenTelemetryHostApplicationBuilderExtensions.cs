using System.Reflection;
using Cephalon.Abstractions.Technologies;
using Cephalon.Diagnostics;
using Cephalon.Engine.Diagnostics;
using Cephalon.Observability.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Cephalon.Observability.OpenTelemetry.Hosting;

/// <summary>
/// Adds OpenTelemetry OTLP exporter wiring for Cephalon hosts.
/// </summary>
public static class OpenTelemetryHostApplicationBuilderExtensions
{
    /// <summary>
    /// Adds OpenTelemetry exporter registration for the Cephalon engine diagnostics surface.
    /// </summary>
    /// <typeparam name="TBuilder">The host-application builder type to extend.</typeparam>
    /// <param name="builder">The target host-application builder.</param>
    /// <param name="configure">
    /// An optional callback that can extend or override the configuration-driven telemetry export options.
    /// </param>
    /// <returns>The same builder instance for fluent host composition.</returns>
    /// <remarks>
    /// <para>
    /// This package keeps exporter wiring outside <c>Cephalon.Engine</c> and <c>Cephalon.Observability</c>.
    /// Hosts opt in explicitly when they want a supported OpenTelemetry path instead of guidance-only settings.
    /// </para>
    /// <para>
    /// Registration is skipped when every signal is disabled or when no export endpoint is configured
    /// and explicit self-hosted defaults are not enabled. When <c>UseSelfHostedDefaults</c> is enabled,
    /// the package falls back to the standard local OTLP collector ports and adds a
    /// <c>deployment.environment.name</c> resource attribute from the current host environment.
    /// The endpoint is interpreted as a base collector endpoint for HTTP/protobuf and the signal-specific
    /// OTLP paths are appended automatically.
    /// </para>
    /// </remarks>
    public static TBuilder AddCephalonOpenTelemetry<TBuilder>(
        this TBuilder builder,
        Action<TelemetryExportOptions>? configure = null)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        var telemetry = ObservabilityOptions.FromConfiguration(builder.Configuration).Telemetry;
        configure?.Invoke(telemetry);

        if (!ShouldRegister(telemetry))
        {
            return builder;
        }

        EnsureProviderIsSupported(telemetry.Provider);

        var serviceName = ResolveServiceName(builder.Environment.ApplicationName);
        var serviceVersion = ResolveServiceVersion();
        var exporterProtocol = ResolveExporterProtocol(telemetry.Protocol);

        builder.Services.AddSingleton(telemetry);
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, OpenTelemetryTelemetryRuntimeContributor>());

        var openTelemetry = builder.Services
            .AddOpenTelemetry()
            .ConfigureResource(resource =>
            {
                resource.AddService(serviceName, serviceVersion: serviceVersion);

                if (telemetry.UseSelfHostedDefaults &&
                    !string.IsNullOrWhiteSpace(builder.Environment.EnvironmentName))
                {
                    resource.AddAttributes(
                    [
                        new KeyValuePair<string, object>(
                            "deployment.environment.name",
                            builder.Environment.EnvironmentName.Trim())
                    ]);
                }
            });

        if (telemetry.ExportTraces)
        {
            openTelemetry.WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation();
                // Subscribe to every canonical Cephalon ActivitySource declared in
                // Cephalon.Diagnostics so engine-runtime, ASP.NET Core host adapter, and worker
                // host adapter spans all flow into the OTLP exporter. EngineDiagnostics
                // .ActivitySourceName is sourced from CephalonActivitySources.Engine so the value
                // is identical to the canonical constant; subscribing to both is a no-op
                // duplicate that future engine-internal renames cannot accidentally drop.
                tracing.AddSource(CephalonActivitySources.Engine);
                tracing.AddSource(CephalonActivitySources.AspNetCore);
                tracing.AddSource(CephalonActivitySources.Worker);
                tracing.AddSource(CephalonActivitySources.Eventing);
                tracing.AddSource(CephalonActivitySources.MultiTenancyGovernance);
                tracing.AddSource(CephalonActivitySources.Agentics);
                tracing.AddSource(CephalonActivitySources.Retrieval);
                tracing.AddOtlpExporter(exporter =>
                    ConfigureExporter(exporter, telemetry, exporterProtocol, TelemetrySignal.Traces));
            });
        }

        if (telemetry.ExportMetrics)
        {
            openTelemetry.WithMetrics(metrics =>
            {
                metrics.AddMeter(CephalonMeters.Engine);
                metrics.AddMeter(CephalonMeters.AspNetCore);
                metrics.AddMeter(CephalonMeters.Worker);
                metrics.AddMeter(CephalonMeters.Eventing);
                metrics.AddMeter(CephalonMeters.MultiTenancyGovernance);
                metrics.AddMeter(CephalonMeters.Agentics);
                metrics.AddMeter(CephalonMeters.Retrieval);
                metrics.AddOtlpExporter(exporter =>
                    ConfigureExporter(exporter, telemetry, exporterProtocol, TelemetrySignal.Metrics));
            });
        }

        if (telemetry.ExportLogs)
        {
            builder.Logging.AddOpenTelemetry(logging =>
            {
                logging.IncludeFormattedMessage = true;
                logging.IncludeScopes = true;
                logging.ParseStateValues = true;
                logging.SetResourceBuilder(BuildResourceBuilder(
                    serviceName,
                    serviceVersion,
                    builder.Environment.EnvironmentName,
                    telemetry.UseSelfHostedDefaults));
                logging.AddOtlpExporter(exporter =>
                    ConfigureExporter(exporter, telemetry, exporterProtocol, TelemetrySignal.Logs));
            });
        }

        return builder;
    }

    private static bool ShouldRegister(TelemetryExportOptions telemetry)
    {
        ArgumentNullException.ThrowIfNull(telemetry);

        return (!string.IsNullOrWhiteSpace(telemetry.Endpoint) || telemetry.UseSelfHostedDefaults) &&
            (telemetry.ExportLogs || telemetry.ExportMetrics || telemetry.ExportTraces);
    }

    private static void EnsureProviderIsSupported(string provider)
    {
        if (string.Equals(provider, "OpenTelemetry", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        throw new InvalidOperationException(
            $"Cephalon OpenTelemetry integration only supports telemetry provider 'OpenTelemetry'. Configured provider '{provider}' is not supported.");
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

    private static OtlpExportProtocol ResolveExporterProtocol(string protocol)
    {
        if (string.IsNullOrWhiteSpace(protocol))
        {
            return OtlpExportProtocol.Grpc;
        }

        return protocol.Trim().ToLowerInvariant() switch
        {
            "otlp" => OtlpExportProtocol.Grpc,
            "grpc" => OtlpExportProtocol.Grpc,
            "otlp/grpc" => OtlpExportProtocol.Grpc,
            "http" => OtlpExportProtocol.HttpProtobuf,
            "otlp/http" => OtlpExportProtocol.HttpProtobuf,
            "otlp-http" => OtlpExportProtocol.HttpProtobuf,
            "http/protobuf" => OtlpExportProtocol.HttpProtobuf,
            "httpprotobuf" => OtlpExportProtocol.HttpProtobuf,
            _ => throw new InvalidOperationException(
                $"Telemetry protocol '{protocol}' is not supported by Cephalon OpenTelemetry integration. Use 'otlp', 'otlp/grpc', or 'otlp/http'.")
        };
    }

    private static void ConfigureExporter(
        OtlpExporterOptions exporter,
        TelemetryExportOptions telemetry,
        OtlpExportProtocol exporterProtocol,
        TelemetrySignal signal)
    {
        ArgumentNullException.ThrowIfNull(exporter);
        ArgumentNullException.ThrowIfNull(telemetry);

        exporter.Protocol = exporterProtocol;

        var endpoint = ResolveCollectorEndpoint(telemetry, exporterProtocol);
        if (endpoint is null)
        {
            return;
        }

        exporter.Endpoint = exporterProtocol == OtlpExportProtocol.HttpProtobuf
            ? BuildSignalEndpoint(endpoint, signal)
            : endpoint;
    }

    private static ResourceBuilder BuildResourceBuilder(
        string serviceName,
        string? serviceVersion,
        string? environmentName,
        bool useSelfHostedDefaults)
    {
        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(serviceName, serviceVersion: serviceVersion);

        if (useSelfHostedDefaults && !string.IsNullOrWhiteSpace(environmentName))
        {
            resourceBuilder.AddAttributes(
            [
                new KeyValuePair<string, object>(
                    "deployment.environment.name",
                    environmentName.Trim())
            ]);
        }

        return resourceBuilder;
    }

    private static Uri? ResolveCollectorEndpoint(
        TelemetryExportOptions telemetry,
        OtlpExportProtocol exporterProtocol)
    {
        ArgumentNullException.ThrowIfNull(telemetry);

        if (!string.IsNullOrWhiteSpace(telemetry.Endpoint))
        {
            if (!Uri.TryCreate(telemetry.Endpoint, UriKind.Absolute, out var endpoint))
            {
                throw new InvalidOperationException(
                    $"Telemetry endpoint '{telemetry.Endpoint}' is not a valid absolute URI.");
            }

            return endpoint;
        }

        if (!telemetry.UseSelfHostedDefaults)
        {
            return null;
        }

        return exporterProtocol switch
        {
            OtlpExportProtocol.Grpc => new Uri("http://localhost:4317", UriKind.Absolute),
            OtlpExportProtocol.HttpProtobuf => new Uri("http://localhost:4318", UriKind.Absolute),
            _ => throw new ArgumentOutOfRangeException(nameof(exporterProtocol))
        };
    }

    private static Uri BuildSignalEndpoint(Uri endpoint, TelemetrySignal signal)
    {
        var signalPath = signal switch
        {
            TelemetrySignal.Logs => "/v1/logs",
            TelemetrySignal.Metrics => "/v1/metrics",
            TelemetrySignal.Traces => "/v1/traces",
            _ => throw new ArgumentOutOfRangeException(nameof(signal))
        };

        var builder = new UriBuilder(endpoint);
        var trimmedPath = builder.Path.TrimEnd('/');
        if (string.IsNullOrEmpty(trimmedPath))
        {
            builder.Path = signalPath;
            return builder.Uri;
        }

        if (IsSignalPath(trimmedPath))
        {
            builder.Path = signalPath;
            return builder.Uri;
        }

        builder.Path = $"{trimmedPath}{signalPath}";
        return builder.Uri;
    }

    private static bool IsSignalPath(string path)
    {
        return string.Equals(path, "/v1/logs", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(path, "/v1/metrics", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(path, "/v1/traces", StringComparison.OrdinalIgnoreCase);
    }

    private enum TelemetrySignal
    {
        Logs,
        Metrics,
        Traces
    }
}
