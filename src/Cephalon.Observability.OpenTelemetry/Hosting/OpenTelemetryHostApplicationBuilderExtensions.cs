using System.Reflection;
using Cephalon.Engine.Diagnostics;
using Cephalon.Observability.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
    /// Registration is skipped when no export endpoint is configured or when every signal is disabled.
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

        var openTelemetry = builder.Services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName, serviceVersion: serviceVersion));

        if (telemetry.ExportTraces)
        {
            openTelemetry.WithTracing(tracing =>
            {
                tracing.AddSource(EngineDiagnostics.ActivitySourceName);
                tracing.AddOtlpExporter(exporter =>
                    ConfigureExporter(exporter, telemetry, exporterProtocol, TelemetrySignal.Traces));
            });
        }

        if (telemetry.ExportMetrics)
        {
            openTelemetry.WithMetrics(metrics =>
            {
                metrics.AddMeter(EngineDiagnostics.MeterName);
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
                logging.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName, serviceVersion: serviceVersion));
                logging.AddOtlpExporter(exporter =>
                    ConfigureExporter(exporter, telemetry, exporterProtocol, TelemetrySignal.Logs));
            });
        }

        return builder;
    }

    private static bool ShouldRegister(TelemetryExportOptions telemetry)
    {
        ArgumentNullException.ThrowIfNull(telemetry);

        return !string.IsNullOrWhiteSpace(telemetry.Endpoint) &&
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

        if (string.IsNullOrWhiteSpace(telemetry.Endpoint))
        {
            return;
        }

        if (!Uri.TryCreate(telemetry.Endpoint, UriKind.Absolute, out var endpoint))
        {
            throw new InvalidOperationException(
                $"Telemetry endpoint '{telemetry.Endpoint}' is not a valid absolute URI.");
        }

        exporter.Endpoint = exporterProtocol == OtlpExportProtocol.HttpProtobuf
            ? BuildSignalEndpoint(endpoint, signal)
            : endpoint;
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
