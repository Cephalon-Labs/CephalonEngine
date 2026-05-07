using System.Reflection;
using Cephalon.Abstractions.Technologies;
using Cephalon.Engine.Diagnostics;
using Cephalon.Observability.Configuration;
using Cephalon.Observability.OracleCloud.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Cephalon.Observability.OracleCloud.Hosting;

/// <summary>
/// Adds Oracle Cloud-hosted observability defaults and optional Oracle Cloud APM managed ingestion for Cephalon hosts.
/// </summary>
public static class OracleCloudHostApplicationBuilderExtensions
{
    /// <summary>
    /// Adds Oracle Cloud-aware OpenTelemetry registration for the Cephalon engine diagnostics surface.
    /// </summary>
    /// <typeparam name="TBuilder">The host-application builder type to extend.</typeparam>
    /// <param name="builder">The target host-application builder.</param>
    /// <param name="configure">
    /// An optional callback that can extend or override the configuration-driven Oracle Cloud telemetry export options.
    /// </param>
    /// <returns>The same builder instance for fluent host composition.</returns>
    /// <remarks>
    /// <para>
    /// This package keeps Oracle Cloud-specific resource defaults and managed APM ingestion concerns
    /// outside <c>Cephalon.Engine</c> and the baseline observability package. It still uses the shared
    /// <c>Engine:Observability:Telemetry</c> contract so hosts can keep one explicit telemetry surface.
    /// </para>
    /// <para>
    /// When <c>Endpoint</c> or <c>UseSelfHostedDefaults</c> is configured, the package keeps using the shared
    /// collector-oriented OTLP path and layers Oracle Cloud resource defaults on top. When those shared
    /// endpoint settings are absent and <c>UseManagedOpenTelemetryIngestion</c> is enabled, the package targets
    /// Oracle Cloud APM OTLP/HTTP ingestion for traces and metrics only. Logs stay on the shared collector path,
    /// Oracle Log Analytics, or another runtime-specific route instead of being redirected implicitly.
    /// </para>
    /// </remarks>
    public static TBuilder AddCephalonOracleCloud<TBuilder>(
        this TBuilder builder,
        Action<OracleCloudTelemetryExportOptions>? configure = null)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        var telemetry = ObservabilityOptions.FromConfiguration(builder.Configuration).Telemetry;
        var options = OracleCloudTelemetryExportOptions.FromConfiguration(builder.Configuration);
        configure?.Invoke(options);

        var endpointMode = ResolveEndpointMode(telemetry, options);
        if (!ShouldRegister(telemetry, endpointMode))
        {
            return builder;
        }

        EnsureProviderIsSupported(telemetry.Provider);

        var directManagedIngestion = endpointMode == OracleCloudEndpointMode.ManagedOpenTelemetryIngestion;
        var exporterProtocol = ResolveExporterProtocol(telemetry.Protocol, directManagedIngestion);
        var serviceName = ResolveServiceName(builder.Environment.ApplicationName);
        var serviceVersion = ResolveServiceVersion();
        var resourceAttributes = ResolveHostedPlatformAttributes(options.HostedPlatform, options.Region, builder.Environment.EnvironmentName);

        builder.Services.AddSingleton(telemetry);
        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton<IDiagnosticsConventionContributor, OracleCloudDiagnosticsConventionContributor>();
        builder.Services.AddHostedService<OracleCloudSummaryHostedService>();
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, OracleCloudTelemetryRuntimeContributor>());

        var openTelemetry = builder.Services
            .AddOpenTelemetry()
            .ConfigureResource(resource =>
            {
                resource.AddService(serviceName, serviceVersion: serviceVersion);
                ConfigureOracleCloudResource(resource, resourceAttributes);
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
                logging.SetResourceBuilder(BuildResourceBuilder(serviceName, serviceVersion, resourceAttributes));
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
            "compute" => "oracle_cloud_compute",
            "instance" => "oracle_cloud_compute",
            "vm" => "oracle_cloud_compute",
            "oracle_cloud_compute" => "oracle_cloud_compute",
            "oke" => "oracle_cloud_oke",
            "oracle_cloud_oke" => "oracle_cloud_oke",
            "functions" => "oracle_cloud_functions",
            "function" => "oracle_cloud_functions",
            "fn" => "oracle_cloud_functions",
            "oraclefunctions" => "oracle_cloud_functions",
            "oracle-functions" => "oracle_cloud_functions",
            "oracle_cloud_functions" => "oracle_cloud_functions",
            _ => null
        };
    }

    internal static OtlpExportProtocol ResolveManagedExporterProtocol(string protocol)
    {
        if (string.IsNullOrWhiteSpace(protocol))
        {
            throw new InvalidOperationException(
                "Cephalon Oracle Cloud managed OpenTelemetry ingestion requires telemetry protocol 'otlp/http'.");
        }

        return protocol.Trim().ToLowerInvariant() switch
        {
            "http" => OtlpExportProtocol.HttpProtobuf,
            "otlp/http" => OtlpExportProtocol.HttpProtobuf,
            "otlp-http" => OtlpExportProtocol.HttpProtobuf,
            "http/protobuf" => OtlpExportProtocol.HttpProtobuf,
            "httpprotobuf" => OtlpExportProtocol.HttpProtobuf,
            _ => throw new InvalidOperationException(
                $"Cephalon Oracle Cloud managed OpenTelemetry ingestion requires telemetry protocol 'otlp/http'. Configured protocol '{protocol}' is not supported.")
        };
    }

    private static bool ShouldRegister(TelemetryExportOptions telemetry, OracleCloudEndpointMode endpointMode)
    {
        ArgumentNullException.ThrowIfNull(telemetry);

        return endpointMode switch
        {
            OracleCloudEndpointMode.None => false,
            OracleCloudEndpointMode.ManagedOpenTelemetryIngestion => telemetry.ExportMetrics || telemetry.ExportTraces,
            _ => telemetry.ExportLogs || telemetry.ExportMetrics || telemetry.ExportTraces
        };
    }

    private static bool ShouldExportLogs(TelemetryExportOptions telemetry, OracleCloudEndpointMode endpointMode)
    {
        ArgumentNullException.ThrowIfNull(telemetry);

        return telemetry.ExportLogs && endpointMode != OracleCloudEndpointMode.ManagedOpenTelemetryIngestion;
    }

    private static bool ShouldExportMetrics(TelemetryExportOptions telemetry, OracleCloudEndpointMode endpointMode)
    {
        ArgumentNullException.ThrowIfNull(telemetry);

        return telemetry.ExportMetrics;
    }

    private static OracleCloudEndpointMode ResolveEndpointMode(
        TelemetryExportOptions telemetry,
        OracleCloudTelemetryExportOptions options)
    {
        ArgumentNullException.ThrowIfNull(telemetry);
        ArgumentNullException.ThrowIfNull(options);

        if (!string.IsNullOrWhiteSpace(telemetry.Endpoint))
        {
            return OracleCloudEndpointMode.ConfiguredEndpoint;
        }

        if (telemetry.UseSelfHostedDefaults)
        {
            return OracleCloudEndpointMode.SelfHostedDefaults;
        }

        return options.UseManagedOpenTelemetryIngestion
            ? OracleCloudEndpointMode.ManagedOpenTelemetryIngestion
            : OracleCloudEndpointMode.None;
    }

    private static void EnsureProviderIsSupported(string provider)
    {
        if (string.Equals(provider, "OpenTelemetry", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        throw new InvalidOperationException(
            $"Cephalon Oracle Cloud observability integration only supports telemetry provider 'OpenTelemetry'. Configured provider '{provider}' is not supported.");
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
                $"Telemetry protocol '{protocol}' is not supported by Cephalon Oracle Cloud observability integration. Use 'otlp', 'otlp/grpc', or 'otlp/http'.")
        };
    }

    private static void ConfigureExporter(
        OtlpExporterOptions exporter,
        TelemetryExportOptions telemetry,
        OracleCloudTelemetryExportOptions options,
        OtlpExportProtocol exporterProtocol,
        OracleCloudEndpointMode endpointMode,
        TelemetrySignal signal)
    {
        ArgumentNullException.ThrowIfNull(exporter);
        ArgumentNullException.ThrowIfNull(telemetry);
        ArgumentNullException.ThrowIfNull(options);

        exporter.Protocol = exporterProtocol;

        if (endpointMode == OracleCloudEndpointMode.ManagedOpenTelemetryIngestion)
        {
            exporter.Headers = BuildManagedHeaders(options, signal);
        }

        var endpoint = ResolveEndpoint(telemetry, options, exporterProtocol, endpointMode, signal);
        if (endpoint is null)
        {
            return;
        }

        exporter.Endpoint = endpointMode == OracleCloudEndpointMode.ManagedOpenTelemetryIngestion
            ? endpoint
            : exporterProtocol == OtlpExportProtocol.HttpProtobuf
                ? BuildSignalEndpoint(endpoint, signal)
                : endpoint;
    }

    private static string BuildManagedHeaders(OracleCloudTelemetryExportOptions options, TelemetrySignal signal)
    {
        ArgumentNullException.ThrowIfNull(options);

        var dataKey = signal switch
        {
            TelemetrySignal.Traces => RequireDataKey(
                options.TraceDataKey,
                "Engine:Observability:Telemetry:OracleCloud:TraceDataKey"),
            TelemetrySignal.Metrics => RequireDataKey(
                options.MetricsDataKey,
                "Engine:Observability:Telemetry:OracleCloud:MetricsDataKey"),
            TelemetrySignal.Logs => throw new InvalidOperationException(
                "Cephalon Oracle Cloud managed OpenTelemetry ingestion does not support direct logs over the shared OTLP contract. Keep logs on the shared collector path, Oracle Log Analytics, or another runtime-specific route."),
            _ => throw new ArgumentOutOfRangeException(nameof(signal))
        };

        return $"Authorization=dataKey {dataKey}";
    }

    private static string RequireDataKey(string? value, string configurationKey)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Cephalon Oracle Cloud managed OpenTelemetry ingestion requires {configurationKey} to be configured.");
        }

        return value.Trim();
    }

    private static Uri? ResolveEndpoint(
        TelemetryExportOptions telemetry,
        OracleCloudTelemetryExportOptions options,
        OtlpExportProtocol exporterProtocol,
        OracleCloudEndpointMode endpointMode,
        TelemetrySignal signal)
    {
        ArgumentNullException.ThrowIfNull(telemetry);
        ArgumentNullException.ThrowIfNull(options);

        return endpointMode switch
        {
            OracleCloudEndpointMode.ConfiguredEndpoint => ResolveAbsoluteEndpoint(
                telemetry.Endpoint,
                "Telemetry endpoint"),
            OracleCloudEndpointMode.SelfHostedDefaults => exporterProtocol switch
            {
                OtlpExportProtocol.Grpc => new Uri("http://localhost:4317", UriKind.Absolute),
                OtlpExportProtocol.HttpProtobuf => new Uri("http://localhost:4318", UriKind.Absolute),
                _ => throw new ArgumentOutOfRangeException(nameof(exporterProtocol))
            },
            OracleCloudEndpointMode.ManagedOpenTelemetryIngestion => ResolveManagedEndpoint(options, signal),
            _ => null
        };
    }

    private static Uri ResolveManagedEndpoint(
        OracleCloudTelemetryExportOptions options,
        TelemetrySignal signal)
    {
        ArgumentNullException.ThrowIfNull(options);

        var dataUploadEndpoint = ResolveAbsoluteEndpoint(
            options.DataUploadEndpoint,
            "Oracle Cloud APM data upload endpoint");

        return signal switch
        {
            TelemetrySignal.Traces => BuildManagedSignalEndpoint(
                dataUploadEndpoint,
                options.UsePublicTraceDataKey
                    ? "/20200101/opentelemetry/public/v1/traces"
                    : "/20200101/opentelemetry/private/v1/traces"),
            TelemetrySignal.Metrics => BuildManagedSignalEndpoint(
                dataUploadEndpoint,
                "/20200101/opentelemetry/v1/metrics"),
            TelemetrySignal.Logs => throw new InvalidOperationException(
                "Cephalon Oracle Cloud managed OpenTelemetry ingestion does not support direct logs over the shared OTLP contract. Keep logs on the shared collector path, Oracle Log Analytics, or another runtime-specific route."),
            _ => throw new ArgumentOutOfRangeException(nameof(signal))
        };
    }

    private static Uri BuildManagedSignalEndpoint(Uri dataUploadEndpoint, string signalPath)
    {
        var builder = new UriBuilder(dataUploadEndpoint);
        var normalizedBasePath = TrimKnownManagedSuffix(builder.Path.TrimEnd('/'));
        builder.Path = string.IsNullOrEmpty(normalizedBasePath)
            ? signalPath
            : $"{normalizedBasePath}{signalPath}";
        return builder.Uri;
    }

    private static string TrimKnownManagedSuffix(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        var suffixes = new[]
        {
            "/20200101/opentelemetry/public/v1/traces",
            "/20200101/opentelemetry/private/v1/traces",
            "/20200101/opentelemetry/v1/metrics",
            "/20200101/opentelemetry"
        };

        foreach (var suffix in suffixes)
        {
            if (path.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                return path[..^suffix.Length];
            }
        }

        return path;
    }

    private static Uri ResolveAbsoluteEndpoint(string? value, string description)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Cephalon Oracle Cloud observability integration requires {description.ToLowerInvariant()} to be configured.");
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
        IReadOnlyList<KeyValuePair<string, object>> resourceAttributes)
    {
        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(serviceName, serviceVersion: serviceVersion);
        ConfigureOracleCloudResource(resourceBuilder, resourceAttributes);
        return resourceBuilder;
    }

    private static void ConfigureOracleCloudResource(
        ResourceBuilder resourceBuilder,
        IReadOnlyList<KeyValuePair<string, object>> resourceAttributes)
    {
        ArgumentNullException.ThrowIfNull(resourceBuilder);
        ArgumentNullException.ThrowIfNull(resourceAttributes);

        if (resourceAttributes.Count > 0)
        {
            resourceBuilder.AddAttributes(resourceAttributes);
        }
    }

    private static List<KeyValuePair<string, object>> ResolveHostedPlatformAttributes(
        string? hostedPlatform,
        string? configuredRegion,
        string? environmentName)
    {
        var attributes = new List<KeyValuePair<string, object>>();
        var providerAdded = false;

        if (!string.IsNullOrWhiteSpace(hostedPlatform))
        {
            var normalizedPlatform = NormalizeHostedPlatform(hostedPlatform);
            if (normalizedPlatform is null)
            {
                throw new InvalidOperationException(
                    $"Oracle Cloud hosted platform '{hostedPlatform}' is not supported. Use 'compute', 'oke', or 'functions'.");
            }

            attributes.Add(new KeyValuePair<string, object>("cloud.provider", "oracle_cloud"));
            attributes.Add(new KeyValuePair<string, object>("cloud.platform", normalizedPlatform));
            providerAdded = true;
        }

        if (!string.IsNullOrWhiteSpace(configuredRegion))
        {
            if (!providerAdded)
            {
                attributes.Add(new KeyValuePair<string, object>("cloud.provider", "oracle_cloud"));
            }

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

    private enum OracleCloudEndpointMode
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
