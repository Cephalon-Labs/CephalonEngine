using System.Reflection;
using Cephalon.Abstractions.Technologies;
using Cephalon.Engine.Diagnostics;
using Cephalon.Observability.AlibabaCloud.Configuration;
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

namespace Cephalon.Observability.AlibabaCloud.Hosting;

/// <summary>
/// Adds Alibaba Cloud-hosted observability defaults and optional managed OpenTelemetry ingestion for Cephalon hosts.
/// </summary>
public static class AlibabaCloudHostApplicationBuilderExtensions
{
    /// <summary>
    /// Adds Alibaba Cloud-aware OpenTelemetry registration for the Cephalon engine diagnostics surface.
    /// </summary>
    /// <typeparam name="TBuilder">The host-application builder type to extend.</typeparam>
    /// <param name="builder">The target host-application builder.</param>
    /// <param name="configure">
    /// An optional callback that can extend or override the configuration-driven Alibaba Cloud telemetry export options.
    /// </param>
    /// <returns>The same builder instance for fluent host composition.</returns>
    /// <remarks>
    /// <para>
    /// This package keeps Alibaba Cloud-specific resource defaults and managed OpenTelemetry-ingestion concerns
    /// outside <c>Cephalon.Engine</c> and the baseline observability package. It still uses the shared
    /// <c>Engine:Observability:Telemetry</c> contract so hosts can keep one explicit telemetry surface.
    /// </para>
    /// <para>
    /// When <c>Endpoint</c> or <c>UseSelfHostedDefaults</c> is configured, the package keeps using the shared
    /// collector-oriented OTLP path and layers Alibaba Cloud resource defaults on top. When those shared
    /// endpoint settings are absent and <c>UseManagedOpenTelemetryIngestion</c> is enabled, the package targets
    /// the configured Alibaba Cloud Managed Service for OpenTelemetry path for traces and metrics only. Logs
    /// stay on the shared collector path, SLS, or another runtime-specific route instead of being redirected
    /// implicitly.
    /// </para>
    /// </remarks>
    public static TBuilder AddCephalonAlibabaCloud<TBuilder>(
        this TBuilder builder,
        Action<AlibabaCloudTelemetryExportOptions>? configure = null)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        var telemetry = ObservabilityOptions.FromConfiguration(builder.Configuration).Telemetry;
        var options = AlibabaCloudTelemetryExportOptions.FromConfiguration(builder.Configuration);
        configure?.Invoke(options);

        var endpointMode = ResolveEndpointMode(telemetry, options);
        if (!ShouldRegister(telemetry, endpointMode))
        {
            return builder;
        }

        EnsureProviderIsSupported(telemetry.Provider);

        var directManagedIngestion = endpointMode == AlibabaCloudEndpointMode.ManagedOpenTelemetryIngestion;
        var exporterProtocol = ResolveExporterProtocol(telemetry.Protocol, directManagedIngestion);
        var serviceName = ResolveServiceName(builder.Environment.ApplicationName);
        var serviceVersion = ResolveServiceVersion();
        var platformAttributes = ResolveHostedPlatformAttributes(options.HostedPlatform, options.Region, builder.Environment.EnvironmentName);

        builder.Services.AddSingleton(telemetry);
        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton<IDiagnosticsConventionContributor, AlibabaCloudDiagnosticsConventionContributor>();
        builder.Services.AddHostedService<AlibabaCloudSummaryHostedService>();
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, AlibabaCloudTelemetryRuntimeContributor>());

        var openTelemetry = builder.Services
            .AddOpenTelemetry()
            .ConfigureResource(resource =>
            {
                resource.AddService(serviceName, serviceVersion: serviceVersion);
                ConfigureAlibabaCloudResource(resource, platformAttributes);
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
            "ecs" => "alibaba_cloud_ecs",
            "elasticcomputeservice" => "alibaba_cloud_ecs",
            "elastic-compute-service" => "alibaba_cloud_ecs",
            "alibaba_cloud_ecs" => "alibaba_cloud_ecs",
            "fc" => "alibaba_cloud_fc",
            "functioncompute" => "alibaba_cloud_fc",
            "function-compute" => "alibaba_cloud_fc",
            "alibaba_cloud_fc" => "alibaba_cloud_fc",
            "openshift" => "alibaba_cloud_openshift",
            "alibaba_cloud_openshift" => "alibaba_cloud_openshift",
            _ => null
        };
    }

    internal static OtlpExportProtocol ResolveManagedExporterProtocol(string protocol)
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
                $"Cephalon Alibaba Cloud managed OpenTelemetry ingestion requires telemetry protocol 'otlp', 'otlp/grpc', or 'otlp/http'. Configured protocol '{protocol}' is not supported.")
        };
    }

    private static bool ShouldRegister(TelemetryExportOptions telemetry, AlibabaCloudEndpointMode endpointMode)
    {
        ArgumentNullException.ThrowIfNull(telemetry);

        return endpointMode switch
        {
            AlibabaCloudEndpointMode.None => false,
            AlibabaCloudEndpointMode.ManagedOpenTelemetryIngestion => telemetry.ExportMetrics || telemetry.ExportTraces,
            _ => telemetry.ExportLogs || telemetry.ExportMetrics || telemetry.ExportTraces
        };
    }

    private static bool ShouldExportLogs(TelemetryExportOptions telemetry, AlibabaCloudEndpointMode endpointMode)
    {
        ArgumentNullException.ThrowIfNull(telemetry);

        return telemetry.ExportLogs && endpointMode != AlibabaCloudEndpointMode.ManagedOpenTelemetryIngestion;
    }

    private static bool ShouldExportMetrics(TelemetryExportOptions telemetry, AlibabaCloudEndpointMode endpointMode)
    {
        ArgumentNullException.ThrowIfNull(telemetry);

        return telemetry.ExportMetrics;
    }

    private static AlibabaCloudEndpointMode ResolveEndpointMode(
        TelemetryExportOptions telemetry,
        AlibabaCloudTelemetryExportOptions options)
    {
        ArgumentNullException.ThrowIfNull(telemetry);
        ArgumentNullException.ThrowIfNull(options);

        if (!string.IsNullOrWhiteSpace(telemetry.Endpoint))
        {
            return AlibabaCloudEndpointMode.ConfiguredEndpoint;
        }

        if (telemetry.UseSelfHostedDefaults)
        {
            return AlibabaCloudEndpointMode.SelfHostedDefaults;
        }

        return options.UseManagedOpenTelemetryIngestion
            ? AlibabaCloudEndpointMode.ManagedOpenTelemetryIngestion
            : AlibabaCloudEndpointMode.None;
    }

    private static void EnsureProviderIsSupported(string provider)
    {
        if (string.Equals(provider, "OpenTelemetry", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        throw new InvalidOperationException(
            $"Cephalon Alibaba Cloud observability integration only supports telemetry provider 'OpenTelemetry'. Configured provider '{provider}' is not supported.");
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

    private static OtlpExportProtocol ResolveExporterProtocol(string protocol, bool directManagedIngestion)
    {
        if (directManagedIngestion)
        {
            return ResolveManagedExporterProtocol(protocol);
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
                $"Telemetry protocol '{protocol}' is not supported by Cephalon Alibaba Cloud observability integration. Use 'otlp', 'otlp/grpc', or 'otlp/http'.")
        };
    }

    private static void ConfigureExporter(
        OtlpExporterOptions exporter,
        TelemetryExportOptions telemetry,
        AlibabaCloudTelemetryExportOptions options,
        OtlpExportProtocol exporterProtocol,
        AlibabaCloudEndpointMode endpointMode,
        TelemetrySignal signal)
    {
        ArgumentNullException.ThrowIfNull(exporter);
        ArgumentNullException.ThrowIfNull(telemetry);
        ArgumentNullException.ThrowIfNull(options);

        exporter.Protocol = exporterProtocol;

        if (endpointMode == AlibabaCloudEndpointMode.ManagedOpenTelemetryIngestion &&
            exporterProtocol == OtlpExportProtocol.Grpc)
        {
            exporter.Headers = BuildManagedGrpcHeaders(options.AuthenticationToken);
        }

        var endpoint = ResolveEndpoint(telemetry, options, exporterProtocol, endpointMode, signal);
        if (endpoint is null)
        {
            return;
        }

        exporter.Endpoint = endpointMode == AlibabaCloudEndpointMode.ManagedOpenTelemetryIngestion &&
            exporterProtocol == OtlpExportProtocol.HttpProtobuf
            ? endpoint
            : exporterProtocol == OtlpExportProtocol.HttpProtobuf
                ? BuildSignalEndpoint(endpoint, signal)
                : endpoint;
    }

    private static string BuildManagedGrpcHeaders(string? authenticationToken)
    {
        if (string.IsNullOrWhiteSpace(authenticationToken))
        {
            throw new InvalidOperationException(
                "Cephalon Alibaba Cloud managed OpenTelemetry ingestion requires Engine:Observability:Telemetry:AlibabaCloud:AuthenticationToken to be configured when OTLP/gRPC is used.");
        }

        return $"Authentication={authenticationToken.Trim()}";
    }

    private static Uri? ResolveEndpoint(
        TelemetryExportOptions telemetry,
        AlibabaCloudTelemetryExportOptions options,
        OtlpExportProtocol exporterProtocol,
        AlibabaCloudEndpointMode endpointMode,
        TelemetrySignal signal)
    {
        ArgumentNullException.ThrowIfNull(telemetry);
        ArgumentNullException.ThrowIfNull(options);

        return endpointMode switch
        {
            AlibabaCloudEndpointMode.ConfiguredEndpoint => ResolveAbsoluteEndpoint(
                telemetry.Endpoint,
                "Telemetry endpoint"),
            AlibabaCloudEndpointMode.SelfHostedDefaults => exporterProtocol switch
            {
                OtlpExportProtocol.Grpc => new Uri("http://localhost:4317", UriKind.Absolute),
                OtlpExportProtocol.HttpProtobuf => new Uri("http://localhost:4318", UriKind.Absolute),
                _ => throw new ArgumentOutOfRangeException(nameof(exporterProtocol))
            },
            AlibabaCloudEndpointMode.ManagedOpenTelemetryIngestion => ResolveManagedEndpoint(
                options,
                exporterProtocol,
                signal),
            _ => null
        };
    }

    private static Uri ResolveManagedEndpoint(
        AlibabaCloudTelemetryExportOptions options,
        OtlpExportProtocol exporterProtocol,
        TelemetrySignal signal)
    {
        ArgumentNullException.ThrowIfNull(options);

        return exporterProtocol switch
        {
            OtlpExportProtocol.Grpc => ResolveAbsoluteEndpoint(
                options.ManagedGrpcEndpoint,
                "Alibaba Cloud Managed Service for OpenTelemetry OTLP/gRPC endpoint"),
            OtlpExportProtocol.HttpProtobuf => signal switch
            {
                TelemetrySignal.Traces => ResolveAbsoluteEndpoint(
                    options.ManagedHttpTracesEndpoint,
                    "Alibaba Cloud Managed Service for OpenTelemetry OTLP/HTTP traces endpoint"),
                TelemetrySignal.Metrics => ResolveAbsoluteEndpoint(
                    options.ManagedHttpMetricsEndpoint,
                    "Alibaba Cloud Managed Service for OpenTelemetry OTLP/HTTP metrics endpoint"),
                TelemetrySignal.Logs => throw new InvalidOperationException(
                    "Cephalon Alibaba Cloud managed OpenTelemetry ingestion does not support direct logs over the shared OTLP contract. Keep logs on the shared collector path, SLS, or another runtime-specific route."),
                _ => throw new ArgumentOutOfRangeException(nameof(signal))
            },
            _ => throw new ArgumentOutOfRangeException(nameof(exporterProtocol))
        };
    }

    private static Uri ResolveAbsoluteEndpoint(string? value, string description)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Cephalon Alibaba Cloud observability integration requires {description.ToLowerInvariant()} to be configured.");
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
        ConfigureAlibabaCloudResource(resourceBuilder, platformAttributes);
        return resourceBuilder;
    }

    private static void ConfigureAlibabaCloudResource(
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
                    $"Alibaba Cloud hosted platform '{hostedPlatform}' is not supported. Use 'ecs', 'fc', 'functioncompute', or 'openshift'.");
            }

            attributes.Add(new KeyValuePair<string, object>("cloud.provider", "alibaba_cloud"));
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

    private enum AlibabaCloudEndpointMode
    {
        None,
        ConfiguredEndpoint,
        SelfHostedDefaults,
        ManagedOpenTelemetryIngestion
    }

    private enum TelemetrySignal
    {
        Logs,
        Metrics,
        Traces
    }
}
