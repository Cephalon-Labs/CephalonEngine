using System.Reflection;
using Cephalon.Engine.Diagnostics;
using Cephalon.Observability.Configuration;
using Cephalon.Observability.HuaweiCloud.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Cephalon.Observability.HuaweiCloud.Hosting;

/// <summary>
/// Adds Huawei Cloud-hosted observability defaults and optional managed APM trace ingestion for Cephalon hosts.
/// </summary>
public static class HuaweiCloudHostApplicationBuilderExtensions
{
    /// <summary>
    /// Adds Huawei Cloud-aware OpenTelemetry registration for the Cephalon engine diagnostics surface.
    /// </summary>
    /// <typeparam name="TBuilder">The host-application builder type to extend.</typeparam>
    /// <param name="builder">The target host-application builder.</param>
    /// <param name="configure">
    /// An optional callback that can extend or override the configuration-driven Huawei Cloud telemetry export options.
    /// </param>
    /// <returns>The same builder instance for fluent host composition.</returns>
    /// <remarks>
    /// <para>
    /// This package keeps Huawei Cloud-specific resource defaults and managed APM trace-ingestion concerns
    /// outside <c>Cephalon.Engine</c> and the baseline observability package. It still uses the shared
    /// <c>Engine:Observability:Telemetry</c> contract so hosts can keep one explicit telemetry surface.
    /// </para>
    /// <para>
    /// When <c>Endpoint</c> or <c>UseSelfHostedDefaults</c> is configured, the package keeps using the shared
    /// collector-oriented OTLP path and layers Huawei Cloud resource defaults on top. When those shared
    /// endpoint settings are absent and <c>UseApmManagedTraceIngestion</c> is enabled, the package targets the
    /// configured Huawei Cloud APM OTLP/gRPC endpoint for traces only by sending the configured
    /// <c>Authentication</c> header. Logs and metrics stay on the shared collector path or another
    /// runtime-specific path instead of being redirected implicitly.
    /// </para>
    /// </remarks>
    public static TBuilder AddCephalonHuaweiCloud<TBuilder>(
        this TBuilder builder,
        Action<HuaweiCloudTelemetryExportOptions>? configure = null)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        var telemetry = ObservabilityOptions.FromConfiguration(builder.Configuration).Telemetry;
        var options = HuaweiCloudTelemetryExportOptions.FromConfiguration(builder.Configuration);
        configure?.Invoke(options);

        var endpointMode = ResolveEndpointMode(telemetry, options);
        if (!ShouldRegister(telemetry, endpointMode))
        {
            return builder;
        }

        EnsureProviderIsSupported(telemetry.Provider);

        var directManagedTraceIngestion = endpointMode == HuaweiCloudEndpointMode.ManagedApmTraceIngestion;
        var exporterProtocol = ResolveExporterProtocol(telemetry.Protocol, directManagedTraceIngestion);
        var serviceName = ResolveServiceName(builder.Environment.ApplicationName);
        var serviceVersion = ResolveServiceVersion();
        var platformAttributes = ResolveHostedPlatformAttributes(options.HostedPlatform, options.Region, builder.Environment.EnvironmentName);

        builder.Services.AddSingleton(telemetry);
        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton<IDiagnosticsConventionContributor, HuaweiCloudDiagnosticsConventionContributor>();
        builder.Services.AddHostedService<HuaweiCloudSummaryHostedService>();

        var openTelemetry = builder.Services
            .AddOpenTelemetry()
            .ConfigureResource(resource =>
            {
                resource.AddService(serviceName, serviceVersion: serviceVersion);
                ConfigureHuaweiResource(resource, platformAttributes);
            });

        if (telemetry.ExportTraces)
        {
            openTelemetry.WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation();
                tracing.AddSource(EngineDiagnostics.ActivitySourceName);
                tracing.AddOtlpExporter(exporter =>
                    ConfigureExporter(exporter, telemetry, options, exporterProtocol, endpointMode, TelemetrySignal.Traces));
            });
        }

        if (ShouldExportMetrics(telemetry, endpointMode))
        {
            openTelemetry.WithMetrics(metrics =>
            {
                metrics.AddMeter(EngineDiagnostics.MeterName);
                metrics.AddOtlpExporter(exporter =>
                    ConfigureExporter(exporter, telemetry, options, exporterProtocol, endpointMode, TelemetrySignal.Metrics));
            });
        }

        if (ShouldExportLogs(telemetry, endpointMode))
        {
            builder.Logging.AddOpenTelemetry(logging =>
            {
                logging.IncludeFormattedMessage = true;
                logging.IncludeScopes = true;
                logging.ParseStateValues = true;
                logging.SetResourceBuilder(BuildResourceBuilder(serviceName, serviceVersion, platformAttributes));
                logging.AddOtlpExporter(exporter =>
                    ConfigureExporter(exporter, telemetry, options, exporterProtocol, endpointMode, TelemetrySignal.Logs));
            });
        }

        return builder;
    }

    internal static string? NormalizeHostedPlatform(string hostedPlatform)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hostedPlatform);

        return hostedPlatform.Trim().ToLowerInvariant() switch
        {
            "ecs" => "huawei_cloud_ecs",
            "elasticcloudserver" => "huawei_cloud_ecs",
            "elastic-cloud-server" => "huawei_cloud_ecs",
            "huawei_cloud_ecs" => "huawei_cloud_ecs",
            "cce" => "huawei_cloud_cce",
            "cloudcontainerengine" => "huawei_cloud_cce",
            "cloud-container-engine" => "huawei_cloud_cce",
            "huawei_cloud_cce" => "huawei_cloud_cce",
            "functiongraph" => "huawei_cloud_functiongraph",
            "function-graph" => "huawei_cloud_functiongraph",
            "huawei_cloud_functiongraph" => "huawei_cloud_functiongraph",
            _ => null
        };
    }

    private static bool ShouldRegister(TelemetryExportOptions telemetry, HuaweiCloudEndpointMode endpointMode)
    {
        ArgumentNullException.ThrowIfNull(telemetry);

        return endpointMode switch
        {
            HuaweiCloudEndpointMode.None => false,
            HuaweiCloudEndpointMode.ManagedApmTraceIngestion => telemetry.ExportTraces,
            _ => telemetry.ExportLogs || telemetry.ExportMetrics || telemetry.ExportTraces
        };
    }

    private static bool ShouldExportLogs(TelemetryExportOptions telemetry, HuaweiCloudEndpointMode endpointMode)
    {
        ArgumentNullException.ThrowIfNull(telemetry);

        return telemetry.ExportLogs && endpointMode != HuaweiCloudEndpointMode.ManagedApmTraceIngestion;
    }

    private static bool ShouldExportMetrics(TelemetryExportOptions telemetry, HuaweiCloudEndpointMode endpointMode)
    {
        ArgumentNullException.ThrowIfNull(telemetry);

        return telemetry.ExportMetrics && endpointMode != HuaweiCloudEndpointMode.ManagedApmTraceIngestion;
    }

    private static HuaweiCloudEndpointMode ResolveEndpointMode(
        TelemetryExportOptions telemetry,
        HuaweiCloudTelemetryExportOptions options)
    {
        ArgumentNullException.ThrowIfNull(telemetry);
        ArgumentNullException.ThrowIfNull(options);

        if (!string.IsNullOrWhiteSpace(telemetry.Endpoint))
        {
            return HuaweiCloudEndpointMode.ConfiguredEndpoint;
        }

        if (telemetry.UseSelfHostedDefaults)
        {
            return HuaweiCloudEndpointMode.SelfHostedDefaults;
        }

        return options.UseApmManagedTraceIngestion
            ? HuaweiCloudEndpointMode.ManagedApmTraceIngestion
            : HuaweiCloudEndpointMode.None;
    }

    private static void EnsureProviderIsSupported(string provider)
    {
        if (string.Equals(provider, "OpenTelemetry", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        throw new InvalidOperationException(
            $"Cephalon Huawei Cloud observability integration only supports telemetry provider 'OpenTelemetry'. Configured provider '{provider}' is not supported.");
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

    private static OtlpExportProtocol ResolveExporterProtocol(string protocol, bool directManagedTraceIngestion)
    {
        if (directManagedTraceIngestion)
        {
            return NormalizeGrpcProtocol(protocol)
                ? OtlpExportProtocol.Grpc
                : throw new InvalidOperationException(
                    $"Cephalon Huawei Cloud managed APM trace ingestion requires telemetry protocol 'otlp' or 'otlp/grpc'. Configured protocol '{protocol}' is not supported.");
        }

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
                $"Telemetry protocol '{protocol}' is not supported by Cephalon Huawei Cloud observability integration. Use 'otlp', 'otlp/grpc', or 'otlp/http'.")
        };
    }

    private static bool NormalizeGrpcProtocol(string protocol)
    {
        if (string.IsNullOrWhiteSpace(protocol))
        {
            return true;
        }

        return protocol.Trim().ToLowerInvariant() is "otlp" or "grpc" or "otlp/grpc";
    }

    private static void ConfigureExporter(
        OtlpExporterOptions exporter,
        TelemetryExportOptions telemetry,
        HuaweiCloudTelemetryExportOptions options,
        OtlpExportProtocol exporterProtocol,
        HuaweiCloudEndpointMode endpointMode,
        TelemetrySignal signal)
    {
        ArgumentNullException.ThrowIfNull(exporter);
        ArgumentNullException.ThrowIfNull(telemetry);
        ArgumentNullException.ThrowIfNull(options);

        exporter.Protocol = exporterProtocol;

        if (endpointMode == HuaweiCloudEndpointMode.ManagedApmTraceIngestion)
        {
            exporter.Headers = BuildManagedTraceHeaders(options.AuthenticationToken);
        }

        var endpoint = ResolveEndpoint(telemetry, options, exporterProtocol, endpointMode);
        if (endpoint is null)
        {
            return;
        }

        exporter.Endpoint = exporterProtocol == OtlpExportProtocol.HttpProtobuf
            ? BuildSignalEndpoint(endpoint, signal)
            : endpoint;
    }

    private static string BuildManagedTraceHeaders(string? authenticationToken)
    {
        if (string.IsNullOrWhiteSpace(authenticationToken))
        {
            throw new InvalidOperationException(
                "Cephalon Huawei Cloud managed APM trace ingestion requires Engine:Observability:Telemetry:HuaweiCloud:AuthenticationToken to be configured.");
        }

        return $"Authentication={authenticationToken.Trim()}";
    }

    private static Uri? ResolveEndpoint(
        TelemetryExportOptions telemetry,
        HuaweiCloudTelemetryExportOptions options,
        OtlpExportProtocol exporterProtocol,
        HuaweiCloudEndpointMode endpointMode)
    {
        ArgumentNullException.ThrowIfNull(telemetry);
        ArgumentNullException.ThrowIfNull(options);

        return endpointMode switch
        {
            HuaweiCloudEndpointMode.ConfiguredEndpoint => ResolveAbsoluteEndpoint(
                telemetry.Endpoint,
                "Telemetry endpoint"),
            HuaweiCloudEndpointMode.SelfHostedDefaults => exporterProtocol switch
            {
                OtlpExportProtocol.Grpc => new Uri("http://localhost:4317", UriKind.Absolute),
                OtlpExportProtocol.HttpProtobuf => new Uri("http://localhost:4318", UriKind.Absolute),
                _ => throw new ArgumentOutOfRangeException(nameof(exporterProtocol))
            },
            HuaweiCloudEndpointMode.ManagedApmTraceIngestion => ResolveAbsoluteEndpoint(
                options.ApmEndpoint,
                "Huawei Cloud APM endpoint"),
            _ => null
        };
    }

    private static Uri ResolveAbsoluteEndpoint(string? value, string description)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Cephalon Huawei Cloud observability integration requires {description.ToLowerInvariant()} to be configured.");
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var endpoint))
        {
            throw new InvalidOperationException(
                $"{description} '{value}' is not a valid absolute URI.");
        }

        return endpoint;
    }

    private static ResourceBuilder BuildResourceBuilder(
        string serviceName,
        string? serviceVersion,
        IReadOnlyList<KeyValuePair<string, object>> platformAttributes)
    {
        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(serviceName, serviceVersion: serviceVersion);
        ConfigureHuaweiResource(resourceBuilder, platformAttributes);
        return resourceBuilder;
    }

    private static void ConfigureHuaweiResource(
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

    private static List<KeyValuePair<string, object>> ResolveHostedPlatformAttributes(
        string? hostedPlatform,
        string? configuredRegion,
        string? environmentName)
    {
        var attributes = new List<KeyValuePair<string, object>>();

        if (!string.IsNullOrWhiteSpace(hostedPlatform))
        {
            var normalizedPlatform = NormalizeHostedPlatform(hostedPlatform);
            if (normalizedPlatform is null)
            {
                throw new InvalidOperationException(
                    $"Huawei Cloud hosted platform '{hostedPlatform}' is not supported. Use 'ecs', 'cce', or 'functiongraph'.");
            }

            attributes.Add(new KeyValuePair<string, object>("cloud.provider", "huawei_cloud"));
            attributes.Add(new KeyValuePair<string, object>("cloud.platform", normalizedPlatform));
        }

        if (!string.IsNullOrWhiteSpace(configuredRegion))
        {
            attributes.Add(new KeyValuePair<string, object>("cloud.region", configuredRegion.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(environmentName))
        {
            attributes.Add(new KeyValuePair<string, object>(
                "deployment.environment.name",
                environmentName.Trim()));
        }

        return attributes;
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

    private enum HuaweiCloudEndpointMode
    {
        None,
        ConfiguredEndpoint,
        SelfHostedDefaults,
        ManagedApmTraceIngestion
    }

    private enum TelemetrySignal
    {
        Logs,
        Metrics,
        Traces
    }
}
