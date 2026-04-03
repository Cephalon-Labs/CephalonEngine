using System.Reflection;
using Cephalon.Engine.Diagnostics;
using Cephalon.Observability.Configuration;
using Cephalon.Observability.DigitalOcean.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Cephalon.Observability.DigitalOcean.Hosting;

/// <summary>
/// Adds DigitalOcean-hosted observability defaults and collector wiring for Cephalon hosts.
/// </summary>
public static class DigitalOceanHostApplicationBuilderExtensions
{
    /// <summary>
    /// Adds DigitalOcean-aware OpenTelemetry registration for the Cephalon engine diagnostics surface.
    /// </summary>
    /// <typeparam name="TBuilder">The host-application builder type to extend.</typeparam>
    /// <param name="builder">The target host-application builder.</param>
    /// <param name="configure">
    /// An optional callback that can extend or override the configuration-driven DigitalOcean telemetry export options.
    /// </param>
    /// <returns>The same builder instance for fluent host composition.</returns>
    /// <remarks>
    /// <para>
    /// This package keeps DigitalOcean-specific collector selection, best-effort Droplet metadata defaults,
    /// and hosted resource defaults outside <c>Cephalon.Engine</c> and the baseline observability package.
    /// It still uses the shared <c>Engine:Observability:Telemetry</c> contract so hosts can keep one explicit
    /// telemetry surface.
    /// </para>
    /// <para>
    /// When <c>Endpoint</c> or <c>UseSelfHostedDefaults</c> is configured, the package keeps using the shared
    /// collector-oriented OTLP path and layers DigitalOcean resource defaults on top. When those shared endpoint
    /// settings are absent and <c>UseInClusterCollectorService</c> is enabled, the package resolves an explicit
    /// in-cluster service endpoint for DOKS deployments so collector-first DigitalOcean setups stay explicit
    /// without over-claiming a managed DigitalOcean OTLP exporter path.
    /// </para>
    /// </remarks>
    public static TBuilder AddCephalonDigitalOcean<TBuilder>(
        this TBuilder builder,
        Action<DigitalOceanTelemetryExportOptions>? configure = null)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        var telemetry = ObservabilityOptions.FromConfiguration(builder.Configuration).Telemetry;
        var options = DigitalOceanTelemetryExportOptions.FromConfiguration(builder.Configuration);
        configure?.Invoke(options);

        var endpointMode = ResolveEndpointMode(telemetry, options);
        if (!ShouldRegister(telemetry, endpointMode))
        {
            return builder;
        }

        EnsureProviderIsSupported(telemetry.Provider);

        var exporterProtocol = ResolveExporterProtocol(telemetry.Protocol);
        var serviceName = ResolveServiceName(builder.Environment.ApplicationName);
        var serviceVersion = ResolveServiceVersion();
        var platformAttributes = ResolveHostedPlatformAttributes(options, builder.Environment.EnvironmentName);

        builder.Services.AddSingleton(telemetry);
        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton<IDiagnosticsConventionContributor, DigitalOceanDiagnosticsConventionContributor>();
        builder.Services.AddHostedService<DigitalOceanSummaryHostedService>();

        var openTelemetry = builder.Services
            .AddOpenTelemetry()
            .ConfigureResource(resource =>
            {
                resource.AddService(serviceName, serviceVersion: serviceVersion);
                ConfigureDigitalOceanResource(resource, options, platformAttributes);
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

        if (telemetry.ExportMetrics)
        {
            openTelemetry.WithMetrics(metrics =>
            {
                metrics.AddMeter(EngineDiagnostics.MeterName);
                metrics.AddOtlpExporter(exporter =>
                    ConfigureExporter(exporter, telemetry, options, exporterProtocol, endpointMode, TelemetrySignal.Metrics));
            });
        }

        if (telemetry.ExportLogs)
        {
            builder.Logging.AddOpenTelemetry(logging =>
            {
                logging.IncludeFormattedMessage = true;
                logging.IncludeScopes = true;
                logging.ParseStateValues = true;
                logging.SetResourceBuilder(BuildResourceBuilder(serviceName, serviceVersion, options, platformAttributes));
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
            "droplet" => "digitalocean_droplet",
            "digitalocean_droplet" => "digitalocean_droplet",
            "doks" => "digitalocean_kubernetes",
            "digitalocean_kubernetes" => "digitalocean_kubernetes",
            "kubernetes" => "digitalocean_kubernetes",
            "managed-kubernetes" => "digitalocean_kubernetes",
            "appplatform" => "digitalocean_app_platform",
            "app-platform" => "digitalocean_app_platform",
            "digitalocean_app_platform" => "digitalocean_app_platform",
            _ => null
        };
    }

    internal static bool HasAppPlatformBindings(DigitalOceanTelemetryExportOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return !string.IsNullOrWhiteSpace(ResolveAppId(options)) ||
            !string.IsNullOrWhiteSpace(ResolveAppUrl(options));
    }

    internal static OtlpExportProtocol ResolveExporterProtocol(string protocol)
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
                $"Telemetry protocol '{protocol}' is not supported by Cephalon DigitalOcean observability integration. Use 'otlp', 'otlp/grpc', or 'otlp/http'.")
        };
    }

    internal static string ResolveCollectorScheme(string? scheme)
    {
        if (string.IsNullOrWhiteSpace(scheme))
        {
            return "http";
        }

        return scheme.Trim().ToLowerInvariant() switch
        {
            "http" => "http",
            "https" => "https",
            _ => throw new InvalidOperationException(
                $"Cephalon DigitalOcean observability integration only supports collector scheme 'http' or 'https'. Configured value '{scheme}' is not supported.")
        };
    }

    internal static string ResolveCollectorServiceName(DigitalOceanTelemetryExportOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return string.IsNullOrWhiteSpace(options.CollectorServiceName)
            ? throw new InvalidOperationException(
                "Cephalon DigitalOcean observability integration requires Engine:Observability:Telemetry:DigitalOcean:CollectorServiceName to be configured when UseInClusterCollectorService is enabled.")
            : options.CollectorServiceName.Trim();
    }

    internal static string? ResolveWorkloadNamespace(DigitalOceanTelemetryExportOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return Normalize(options.Namespace) ?? Normalize(Environment.GetEnvironmentVariable("POD_NAMESPACE"));
    }

    internal static string ResolveCollectorNamespace(DigitalOceanTelemetryExportOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return Normalize(options.CollectorNamespace)
            ?? ResolveWorkloadNamespace(options)
            ?? throw new InvalidOperationException(
                "Cephalon DigitalOcean observability integration requires Engine:Observability:Telemetry:DigitalOcean:CollectorNamespace, Engine:Observability:Telemetry:DigitalOcean:Namespace, or POD_NAMESPACE to be available when UseInClusterCollectorService is enabled.");
    }

    internal static string? ResolveAppId(DigitalOceanTelemetryExportOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return Normalize(options.AppId) ??
            Normalize(Environment.GetEnvironmentVariable("APP_ID")) ??
            Normalize(Environment.GetEnvironmentVariable("DIGITALOCEAN_APP_ID"));
    }

    internal static string? ResolveAppUrl(DigitalOceanTelemetryExportOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return Normalize(options.AppUrl) ??
            Normalize(Environment.GetEnvironmentVariable("APP_URL")) ??
            Normalize(Environment.GetEnvironmentVariable("DIGITALOCEAN_APP_URL"));
    }

    private static bool ShouldRegister(TelemetryExportOptions telemetry, DigitalOceanEndpointMode endpointMode)
    {
        ArgumentNullException.ThrowIfNull(telemetry);

        return endpointMode != DigitalOceanEndpointMode.None &&
            (telemetry.ExportLogs || telemetry.ExportMetrics || telemetry.ExportTraces);
    }

    private static DigitalOceanEndpointMode ResolveEndpointMode(
        TelemetryExportOptions telemetry,
        DigitalOceanTelemetryExportOptions options)
    {
        ArgumentNullException.ThrowIfNull(telemetry);
        ArgumentNullException.ThrowIfNull(options);

        if (!string.IsNullOrWhiteSpace(telemetry.Endpoint))
        {
            return DigitalOceanEndpointMode.ConfiguredEndpoint;
        }

        if (telemetry.UseSelfHostedDefaults)
        {
            return DigitalOceanEndpointMode.SelfHostedDefaults;
        }

        return options.UseInClusterCollectorService
            ? DigitalOceanEndpointMode.InClusterCollectorService
            : DigitalOceanEndpointMode.None;
    }

    private static void EnsureProviderIsSupported(string provider)
    {
        if (string.Equals(provider, "OpenTelemetry", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        throw new InvalidOperationException(
            $"Cephalon DigitalOcean observability integration only supports telemetry provider 'OpenTelemetry'. Configured provider '{provider}' is not supported.");
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

    private static void ConfigureExporter(
        OtlpExporterOptions exporter,
        TelemetryExportOptions telemetry,
        DigitalOceanTelemetryExportOptions options,
        OtlpExportProtocol exporterProtocol,
        DigitalOceanEndpointMode endpointMode,
        TelemetrySignal signal)
    {
        ArgumentNullException.ThrowIfNull(exporter);
        ArgumentNullException.ThrowIfNull(telemetry);
        ArgumentNullException.ThrowIfNull(options);

        exporter.Protocol = exporterProtocol;

        if (!string.IsNullOrWhiteSpace(options.Headers))
        {
            exporter.Headers = options.Headers.Trim();
        }

        var endpoint = ResolveEndpoint(telemetry, options, exporterProtocol, endpointMode);
        if (endpoint is null)
        {
            return;
        }

        ConfigureTrustedCa(exporter, options, exporterProtocol, signal, endpoint);

        exporter.Endpoint = exporterProtocol == OtlpExportProtocol.HttpProtobuf
            ? BuildSignalEndpoint(endpoint, signal)
            : endpoint;
    }

    private static void ConfigureTrustedCa(
        OtlpExporterOptions exporter,
        DigitalOceanTelemetryExportOptions options,
        OtlpExportProtocol exporterProtocol,
        TelemetrySignal signal,
        Uri endpoint)
    {
        ArgumentNullException.ThrowIfNull(exporter);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(endpoint);

        if (string.IsNullOrWhiteSpace(options.TrustedCaCertificatePath) ||
            !string.Equals(endpoint.Scheme, "https", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (exporterProtocol != OtlpExportProtocol.HttpProtobuf)
        {
            throw new InvalidOperationException(
                "Cephalon DigitalOcean observability integration currently only supports TrustedCaCertificatePath for HTTPS OTLP/HTTP traces and metrics. OTLP/gRPC custom CA handling should stay explicit in the collector or host trust store.");
        }

        if (signal == TelemetrySignal.Logs)
        {
            throw new InvalidOperationException(
                "Cephalon DigitalOcean observability integration cannot apply TrustedCaCertificatePath to OTLP/HTTP logs because the current OpenTelemetry logging exporter does not support custom HttpClientFactory wiring. Disable log export for that path or rely on system trust.");
        }

        var trustedCaCertificatePath = options.TrustedCaCertificatePath.Trim();
        exporter.HttpClientFactory = () => DigitalOceanTrustedCaHttpClientHandler.CreateHttpClient(trustedCaCertificatePath);
    }

    private static Uri? ResolveEndpoint(
        TelemetryExportOptions telemetry,
        DigitalOceanTelemetryExportOptions options,
        OtlpExportProtocol exporterProtocol,
        DigitalOceanEndpointMode endpointMode)
    {
        ArgumentNullException.ThrowIfNull(telemetry);
        ArgumentNullException.ThrowIfNull(options);

        return endpointMode switch
        {
            DigitalOceanEndpointMode.ConfiguredEndpoint => ResolveAbsoluteEndpoint(
                telemetry.Endpoint,
                "Telemetry endpoint"),
            DigitalOceanEndpointMode.SelfHostedDefaults => exporterProtocol switch
            {
                OtlpExportProtocol.Grpc => new Uri("http://localhost:4317", UriKind.Absolute),
                OtlpExportProtocol.HttpProtobuf => new Uri("http://localhost:4318", UriKind.Absolute),
                _ => throw new ArgumentOutOfRangeException(nameof(exporterProtocol))
            },
            DigitalOceanEndpointMode.InClusterCollectorService => BuildInClusterCollectorEndpoint(options, exporterProtocol),
            _ => null
        };
    }

    private static Uri BuildInClusterCollectorEndpoint(
        DigitalOceanTelemetryExportOptions options,
        OtlpExportProtocol exporterProtocol)
    {
        ArgumentNullException.ThrowIfNull(options);

        var normalizedPlatform = string.IsNullOrWhiteSpace(options.HostedPlatform)
            ? null
            : NormalizeHostedPlatform(options.HostedPlatform);
        if (!string.IsNullOrWhiteSpace(options.HostedPlatform) &&
            !string.Equals(normalizedPlatform, "digitalocean_kubernetes", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Cephalon DigitalOcean observability integration only supports UseInClusterCollectorService for DOKS-hosted deployments. Set HostedPlatform to 'doks' or remove the in-cluster collector setting.");
        }

        var serviceName = ResolveCollectorServiceName(options);
        var collectorNamespace = ResolveCollectorNamespace(options);
        var scheme = ResolveCollectorScheme(options.CollectorScheme);
        var port = options.CollectorPort ?? (exporterProtocol == OtlpExportProtocol.HttpProtobuf ? 4318 : 4317);
        var endpoint = $"{scheme}://{serviceName}.{collectorNamespace}.svc.cluster.local:{port}";

        return new Uri(endpoint, UriKind.Absolute);
    }

    private static Uri ResolveAbsoluteEndpoint(string? value, string description)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Cephalon DigitalOcean observability integration requires {description.ToLowerInvariant()} to be configured.");
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
        DigitalOceanTelemetryExportOptions options,
        IReadOnlyList<KeyValuePair<string, object>> platformAttributes)
    {
        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(serviceName, serviceVersion: serviceVersion);
        ConfigureDigitalOceanResource(resourceBuilder, options, platformAttributes);
        return resourceBuilder;
    }

    private static void ConfigureDigitalOceanResource(
        ResourceBuilder resourceBuilder,
        DigitalOceanTelemetryExportOptions options,
        IReadOnlyList<KeyValuePair<string, object>> platformAttributes)
    {
        ArgumentNullException.ThrowIfNull(resourceBuilder);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(platformAttributes);

        if (platformAttributes.Count > 0)
        {
            resourceBuilder.AddAttributes(platformAttributes);
        }

        var normalizedHostedPlatform = string.IsNullOrWhiteSpace(options.HostedPlatform)
            ? null
            : NormalizeHostedPlatform(options.HostedPlatform);
        if (string.Equals(normalizedHostedPlatform, "digitalocean_droplet", StringComparison.Ordinal) &&
            options.UseDropletMetadataDefaults)
        {
            resourceBuilder.AddDetector(new DigitalOceanDropletResourceDetector(options));
        }
    }

    private static List<KeyValuePair<string, object>> ResolveHostedPlatformAttributes(
        DigitalOceanTelemetryExportOptions options,
        string? environmentName)
    {
        ArgumentNullException.ThrowIfNull(options);

        var attributes = new List<KeyValuePair<string, object>>();
        var normalizedHostedPlatform = string.IsNullOrWhiteSpace(options.HostedPlatform)
            ? null
            : NormalizeHostedPlatform(options.HostedPlatform);
        if (!string.IsNullOrWhiteSpace(options.HostedPlatform) && normalizedHostedPlatform is null)
        {
            throw new InvalidOperationException(
                $"DigitalOcean hosted platform '{options.HostedPlatform}' is not supported. Use 'droplet', 'doks', or 'app-platform'.");
        }

        if (!string.IsNullOrWhiteSpace(normalizedHostedPlatform))
        {
            attributes.Add(new KeyValuePair<string, object>("cloud.provider", "digitalocean"));
            attributes.Add(new KeyValuePair<string, object>("cloud.platform", normalizedHostedPlatform));
        }

        var region = Normalize(options.Region);
        if (!string.IsNullOrWhiteSpace(region))
        {
            attributes.Add(new KeyValuePair<string, object>("cloud.region", region));
        }

        switch (normalizedHostedPlatform)
        {
            case "digitalocean_droplet":
            {
                var dropletId = Normalize(options.DropletId);
                if (!string.IsNullOrWhiteSpace(dropletId))
                {
                    attributes.Add(new KeyValuePair<string, object>("host.id", dropletId));
                }

                break;
            }
            case "digitalocean_kubernetes":
            {
                var clusterName = Normalize(options.ClusterName);
                var workloadNamespace = ResolveWorkloadNamespace(options);
                var podName = Normalize(Environment.GetEnvironmentVariable("HOSTNAME"));

                if (!string.IsNullOrWhiteSpace(clusterName))
                {
                    attributes.Add(new KeyValuePair<string, object>("k8s.cluster.name", clusterName));
                }

                if (!string.IsNullOrWhiteSpace(workloadNamespace))
                {
                    attributes.Add(new KeyValuePair<string, object>("k8s.namespace.name", workloadNamespace));
                    attributes.Add(new KeyValuePair<string, object>("service.namespace", workloadNamespace));
                }

                if (!string.IsNullOrWhiteSpace(podName))
                {
                    attributes.Add(new KeyValuePair<string, object>("k8s.pod.name", podName));
                }

                break;
            }
            case "digitalocean_app_platform":
            {
                var appId = ResolveAppId(options);
                var appUrl = ResolveAppUrl(options);

                if (!string.IsNullOrWhiteSpace(appId))
                {
                    attributes.Add(new KeyValuePair<string, object>("digitalocean.app.id", appId));
                }

                if (!string.IsNullOrWhiteSpace(appUrl))
                {
                    attributes.Add(new KeyValuePair<string, object>("digitalocean.app.url", appUrl));
                }

                break;
            }
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

        if (endpoint.AbsolutePath.EndsWith(signalPath, StringComparison.OrdinalIgnoreCase))
        {
            return endpoint;
        }

        var builder = new UriBuilder(endpoint);
        var basePath = builder.Path.TrimEnd('/');
        builder.Path = string.IsNullOrEmpty(basePath) ? signalPath : $"{basePath}{signalPath}";
        return builder.Uri;
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

internal enum DigitalOceanEndpointMode
{
    None,
    ConfiguredEndpoint,
    SelfHostedDefaults,
    InClusterCollectorService
}

internal enum TelemetrySignal
{
    Logs,
    Metrics,
    Traces
}
