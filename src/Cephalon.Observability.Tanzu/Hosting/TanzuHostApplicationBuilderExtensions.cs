using System.Reflection;
using Cephalon.Abstractions.Technologies;
using Cephalon.Engine.Diagnostics;
using Cephalon.Observability.Configuration;
using Cephalon.Observability.Tanzu.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Cephalon.Observability.Tanzu.Hosting;

/// <summary>
/// Adds VMware Tanzu-hosted observability defaults and proxy handoff wiring for Cephalon hosts.
/// </summary>
public static class TanzuHostApplicationBuilderExtensions
{
    /// <summary>
    /// Adds Tanzu-aware OpenTelemetry registration for the Cephalon engine diagnostics surface.
    /// </summary>
    /// <typeparam name="TBuilder">The host-application builder type to extend.</typeparam>
    /// <param name="builder">The target host-application builder.</param>
    /// <param name="configure">
    /// An optional callback that can extend or override the configuration-driven Tanzu telemetry export options.
    /// </param>
    /// <returns>The same builder instance for fluent host composition.</returns>
    /// <remarks>
    /// <para>
    /// This package keeps Tanzu-specific proxy inputs and hosted resource defaults outside
    /// <c>Cephalon.Engine</c> and the baseline observability package. It still uses the shared
    /// <c>Engine:Observability:Telemetry</c> contract so hosts can keep one explicit telemetry surface.
    /// </para>
    /// <para>
    /// When <c>Endpoint</c> or <c>UseSelfHostedDefaults</c> is configured, the package keeps using the shared
    /// collector-oriented OTLP path and layers Tanzu resource defaults on top. When those shared endpoint
    /// settings are absent and <c>UseInClusterProxyService</c> is enabled, the package enables an explicit
    /// trace-focused Tanzu proxy handoff path instead of pretending the current Tanzu documentation describes
    /// one generic vendor-direct OTLP backend for every signal.
    /// </para>
    /// </remarks>
    public static TBuilder AddCephalonTanzu<TBuilder>(
        this TBuilder builder,
        Action<TanzuTelemetryExportOptions>? configure = null)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        var telemetry = ObservabilityOptions.FromConfiguration(builder.Configuration).Telemetry;
        var options = TanzuTelemetryExportOptions.FromConfiguration(builder.Configuration);
        configure?.Invoke(options);

        var endpointMode = ResolveEndpointMode(telemetry, options);
        if (endpointMode == TanzuEndpointMode.None ||
            !(telemetry.ExportLogs || telemetry.ExportMetrics || telemetry.ExportTraces))
        {
            return builder;
        }

        EnsureProviderIsSupported(telemetry.Provider);
        ValidateSignalSelection(telemetry, endpointMode);

        var exporterProtocol = ResolveExporterProtocol(telemetry.Protocol);
        var serviceName = ResolveServiceName(builder.Environment.ApplicationName);
        var serviceVersion = ResolveServiceVersion();
        var platformAttributes = ResolveHostedPlatformAttributes(options, builder.Environment.EnvironmentName);

        builder.Services.AddSingleton(telemetry);
        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton<IDiagnosticsConventionContributor, TanzuDiagnosticsConventionContributor>();
        builder.Services.AddHostedService<TanzuSummaryHostedService>();
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, TanzuTelemetryRuntimeContributor>());

        var openTelemetry = builder.Services
            .AddOpenTelemetry()
            .ConfigureResource(resource =>
            {
                resource.AddService(serviceName, serviceVersion: serviceVersion);
                ConfigureTanzuResource(resource, platformAttributes);
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
            "tkg" => "vmware_tanzu_kubernetes_grid",
            "tkgm" => "vmware_tanzu_kubernetes_grid",
            "tanzu-kubernetes-grid" => "vmware_tanzu_kubernetes_grid",
            "tanzukubernetesgrid" => "vmware_tanzu_kubernetes_grid",
            "tkgi" => "vmware_tanzu_kubernetes_grid_integrated",
            "tanzu-kubernetes-grid-integrated" => "vmware_tanzu_kubernetes_grid_integrated",
            "tanzu-kubernetes-grid-integrated-edition" => "vmware_tanzu_kubernetes_grid_integrated",
            "tanzukubernetesgridintegratededition" => "vmware_tanzu_kubernetes_grid_integrated",
            "tap" => "vmware_tanzu_application_platform",
            "tanzu-application-platform" => "vmware_tanzu_application_platform",
            "tanzuapplicationplatform" => "vmware_tanzu_application_platform",
            _ => null
        };
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
                $"Telemetry protocol '{protocol}' is not supported by Cephalon Tanzu observability integration. Use 'otlp', 'otlp/grpc', or 'otlp/http'.")
        };
    }

    internal static string ResolveProxyScheme(string? scheme)
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
                $"Cephalon Tanzu observability integration only supports proxy scheme 'http' or 'https'. Configured value '{scheme}' is not supported.")
        };
    }

    internal static string ResolveProxyServiceName(TanzuTelemetryExportOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return string.IsNullOrWhiteSpace(options.ProxyServiceName)
            ? throw new InvalidOperationException(
                "Cephalon Tanzu observability integration requires Engine:Observability:Telemetry:Tanzu:ProxyServiceName to be configured when UseInClusterProxyService is enabled.")
            : options.ProxyServiceName.Trim();
    }

    internal static string? ResolveWorkloadNamespace(TanzuTelemetryExportOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return Normalize(options.Namespace) ?? Normalize(Environment.GetEnvironmentVariable("POD_NAMESPACE"));
    }

    internal static string ResolveProxyNamespace(TanzuTelemetryExportOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return Normalize(options.ProxyNamespace)
            ?? ResolveWorkloadNamespace(options)
            ?? throw new InvalidOperationException(
                "Cephalon Tanzu observability integration requires Engine:Observability:Telemetry:Tanzu:ProxyNamespace, Engine:Observability:Telemetry:Tanzu:Namespace, or POD_NAMESPACE to be available when UseInClusterProxyService is enabled.");
    }

    internal static int ResolveProxyPort(TanzuTelemetryExportOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return options.ProxyPort is > 0
            ? options.ProxyPort.Value
            : throw new InvalidOperationException(
                "Cephalon Tanzu observability integration requires Engine:Observability:Telemetry:Tanzu:ProxyPort to be configured when UseInClusterProxyService is enabled so the proxy handoff path stays explicit.");
    }

    internal static Uri BuildInClusterProxyEndpoint(
        TanzuTelemetryExportOptions options,
        OtlpExportProtocol exporterProtocol)
    {
        ArgumentNullException.ThrowIfNull(options);

        var serviceName = ResolveProxyServiceName(options);
        var proxyNamespace = ResolveProxyNamespace(options);
        var scheme = ResolveProxyScheme(options.ProxyScheme);
        var port = ResolveProxyPort(options);
        var path = NormalizePath(options.ProxyPath);

        var builder = new UriBuilder(scheme, $"{serviceName}.{proxyNamespace}.svc.cluster.local", port);
        if (!string.IsNullOrWhiteSpace(path))
        {
            builder.Path = path;
        }

        return builder.Uri;
    }

    internal static Uri BuildSignalEndpoint(Uri endpoint, TelemetrySignal signal)
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

    private static bool ShouldExportMetrics(TelemetryExportOptions telemetry, TanzuEndpointMode endpointMode)
    {
        return telemetry.ExportMetrics && endpointMode != TanzuEndpointMode.InClusterProxyService;
    }

    private static bool ShouldExportLogs(TelemetryExportOptions telemetry, TanzuEndpointMode endpointMode)
    {
        return telemetry.ExportLogs && endpointMode != TanzuEndpointMode.InClusterProxyService;
    }

    private static TanzuEndpointMode ResolveEndpointMode(
        TelemetryExportOptions telemetry,
        TanzuTelemetryExportOptions options)
    {
        ArgumentNullException.ThrowIfNull(telemetry);
        ArgumentNullException.ThrowIfNull(options);

        if (!string.IsNullOrWhiteSpace(telemetry.Endpoint))
        {
            return TanzuEndpointMode.ConfiguredEndpoint;
        }

        if (telemetry.UseSelfHostedDefaults)
        {
            return TanzuEndpointMode.SelfHostedDefaults;
        }

        return options.UseInClusterProxyService
            ? TanzuEndpointMode.InClusterProxyService
            : TanzuEndpointMode.None;
    }

    private static void EnsureProviderIsSupported(string provider)
    {
        if (string.Equals(provider, "OpenTelemetry", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        throw new InvalidOperationException(
            $"Cephalon Tanzu observability integration only supports telemetry provider 'OpenTelemetry'. Configured provider '{provider}' is not supported.");
    }

    private static void ValidateSignalSelection(
        TelemetryExportOptions telemetry,
        TanzuEndpointMode endpointMode)
    {
        ArgumentNullException.ThrowIfNull(telemetry);

        if (endpointMode != TanzuEndpointMode.InClusterProxyService)
        {
            return;
        }

        if (!telemetry.ExportTraces)
        {
            throw new InvalidOperationException(
                "Cephalon Tanzu observability integration requires ExportTraces=true when UseInClusterProxyService is enabled because the documented Tanzu proxy handoff path is trace-oriented.");
        }

        if (telemetry.ExportLogs || telemetry.ExportMetrics)
        {
            throw new InvalidOperationException(
                "Cephalon Tanzu observability integration only supports traces when UseInClusterProxyService is enabled. Keep logs and metrics on a shared OTLP endpoint or the explicit self-hosted defaults instead of treating the Tanzu proxy handoff path as a generic managed backend.");
        }
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
        TanzuTelemetryExportOptions options,
        OtlpExportProtocol exporterProtocol,
        TanzuEndpointMode endpointMode,
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
        TanzuTelemetryExportOptions options,
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
                "Cephalon Tanzu observability integration currently only supports TrustedCaCertificatePath for HTTPS OTLP/HTTP traces and metrics. OTLP/gRPC custom CA handling should stay explicit in the proxy, collector, or host trust store.");
        }

        if (signal == TelemetrySignal.Logs)
        {
            throw new InvalidOperationException(
                "Cephalon Tanzu observability integration cannot apply TrustedCaCertificatePath to OTLP/HTTP logs because the current OpenTelemetry logging exporter does not support custom HttpClientFactory wiring. Disable log export for that path or rely on system trust.");
        }

        var trustedCaCertificatePath = options.TrustedCaCertificatePath.Trim();
        exporter.HttpClientFactory = () => TanzuTrustedCaHttpClientHandler.CreateHttpClient(trustedCaCertificatePath);
    }

    private static Uri? ResolveEndpoint(
        TelemetryExportOptions telemetry,
        TanzuTelemetryExportOptions options,
        OtlpExportProtocol exporterProtocol,
        TanzuEndpointMode endpointMode)
    {
        ArgumentNullException.ThrowIfNull(telemetry);
        ArgumentNullException.ThrowIfNull(options);

        return endpointMode switch
        {
            TanzuEndpointMode.ConfiguredEndpoint => ResolveAbsoluteEndpoint(
                telemetry.Endpoint,
                "Telemetry endpoint"),
            TanzuEndpointMode.SelfHostedDefaults => exporterProtocol switch
            {
                OtlpExportProtocol.Grpc => new Uri("http://localhost:4317", UriKind.Absolute),
                OtlpExportProtocol.HttpProtobuf => new Uri("http://localhost:4318", UriKind.Absolute),
                _ => throw new ArgumentOutOfRangeException(nameof(exporterProtocol))
            },
            TanzuEndpointMode.InClusterProxyService => BuildInClusterProxyEndpoint(options, exporterProtocol),
            _ => null
        };
    }

    private static Uri ResolveAbsoluteEndpoint(string? value, string description)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Cephalon Tanzu observability integration requires {description.ToLowerInvariant()} to be configured.");
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
        ConfigureTanzuResource(resourceBuilder, platformAttributes);
        return resourceBuilder;
    }

    private static void ConfigureTanzuResource(
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
        TanzuTelemetryExportOptions options,
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
                $"VMware Tanzu hosted platform '{options.HostedPlatform}' is not supported. Use 'tkg', 'tkgi', or 'tap'.");
        }

        if (!string.IsNullOrWhiteSpace(normalizedHostedPlatform))
        {
            attributes.Add(new KeyValuePair<string, object>("cloud.provider", "vmware"));
            attributes.Add(new KeyValuePair<string, object>("cloud.platform", normalizedHostedPlatform));
        }

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

        if (!string.IsNullOrWhiteSpace(environmentName))
        {
            attributes.Add(new KeyValuePair<string, object>(
                "deployment.environment.name",
                environmentName.Trim()));
        }

        return attributes;
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? NormalizePath(string? value)
    {
        var normalized = Normalize(value);
        if (normalized is null)
        {
            return null;
        }

        return normalized.Length > 0 && normalized[0] == '/'
            ? normalized
            : "/" + normalized;
    }
}

internal enum TanzuEndpointMode
{
    None,
    ConfiguredEndpoint,
    SelfHostedDefaults,
    InClusterProxyService
}

internal enum TelemetrySignal
{
    Logs,
    Metrics,
    Traces
}
