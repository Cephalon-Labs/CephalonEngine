using System.Reflection;
using Cephalon.Engine.Diagnostics;
using Cephalon.Observability.Configuration;
using Cephalon.Observability.Kubernetes.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Cephalon.Observability.Kubernetes.Hosting;

/// <summary>
/// Adds Kubernetes-hosted observability defaults and in-cluster collector wiring for Cephalon hosts.
/// </summary>
public static class KubernetesHostApplicationBuilderExtensions
{
    /// <summary>
    /// Adds Kubernetes-aware OpenTelemetry registration for the Cephalon engine diagnostics surface.
    /// </summary>
    /// <typeparam name="TBuilder">The host-application builder type to extend.</typeparam>
    /// <param name="builder">The target host-application builder.</param>
    /// <param name="configure">
    /// An optional callback that can extend or override the configuration-driven Kubernetes telemetry export options.
    /// </param>
    /// <returns>The same builder instance for fluent host composition.</returns>
    /// <remarks>
    /// <para>
    /// This package keeps Kubernetes-specific collector selection, cluster-local trust material, and resource defaults
    /// outside <c>Cephalon.Engine</c> and the baseline observability package. It still uses the shared
    /// <c>Engine:Observability:Telemetry</c> contract so hosts can keep one explicit telemetry surface.
    /// </para>
    /// <para>
    /// When <c>Endpoint</c> or <c>UseSelfHostedDefaults</c> is configured, the package keeps using the shared
    /// collector-oriented OTLP path and layers Kubernetes resource defaults on top. When those shared endpoint
    /// settings are absent and <c>UseInClusterCollectorService</c> is enabled, the package resolves an explicit
    /// in-cluster service endpoint so generic Kubernetes deployments can stay collector-first without forcing teams
    /// onto a vendor-specific companion package.
    /// </para>
    /// </remarks>
    public static TBuilder AddCephalonKubernetes<TBuilder>(
        this TBuilder builder,
        Action<KubernetesTelemetryExportOptions>? configure = null)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        var telemetry = ObservabilityOptions.FromConfiguration(builder.Configuration).Telemetry;
        var options = KubernetesTelemetryExportOptions.FromConfiguration(builder.Configuration);
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
        var resourceAttributes = ResolveResourceAttributes(options, builder.Environment.EnvironmentName);

        builder.Services.AddSingleton(telemetry);
        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton<IDiagnosticsConventionContributor, KubernetesDiagnosticsConventionContributor>();
        builder.Services.AddHostedService<KubernetesSummaryHostedService>();

        var openTelemetry = builder.Services
            .AddOpenTelemetry()
            .ConfigureResource(resource =>
            {
                resource.AddService(serviceName, serviceVersion: serviceVersion);
                ConfigureKubernetesResource(resource, resourceAttributes);
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
                logging.SetResourceBuilder(BuildResourceBuilder(serviceName, serviceVersion, resourceAttributes));
                logging.AddOtlpExporter(exporter =>
                    ConfigureExporter(exporter, telemetry, options, exporterProtocol, endpointMode, TelemetrySignal.Logs));
            });
        }

        return builder;
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
                $"Telemetry protocol '{protocol}' is not supported by Cephalon Kubernetes observability integration. Use 'otlp', 'otlp/grpc', or 'otlp/http'.")
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
                $"Cephalon Kubernetes observability integration only supports collector scheme 'http' or 'https'. Configured value '{scheme}' is not supported.")
        };
    }

    internal static string ResolveCollectorServiceName(KubernetesTelemetryExportOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return string.IsNullOrWhiteSpace(options.CollectorServiceName)
            ? throw new InvalidOperationException(
                "Cephalon Kubernetes observability integration requires Engine:Observability:Telemetry:Kubernetes:CollectorServiceName to be configured when UseInClusterCollectorService is enabled.")
            : options.CollectorServiceName.Trim();
    }

    internal static string? ResolveWorkloadNamespace(KubernetesTelemetryExportOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return Normalize(options.Namespace) ?? Normalize(Environment.GetEnvironmentVariable("POD_NAMESPACE"));
    }

    internal static string ResolveCollectorNamespace(KubernetesTelemetryExportOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return Normalize(options.CollectorNamespace)
            ?? ResolveWorkloadNamespace(options)
            ?? throw new InvalidOperationException(
                "Cephalon Kubernetes observability integration requires Engine:Observability:Telemetry:Kubernetes:CollectorNamespace, Engine:Observability:Telemetry:Kubernetes:Namespace, or POD_NAMESPACE to be available when UseInClusterCollectorService is enabled.");
    }

    internal static string ResolveServiceDnsSuffix(string? serviceDnsSuffix)
    {
        var normalized = Normalize(serviceDnsSuffix);
        return string.IsNullOrWhiteSpace(normalized)
            ? "svc.cluster.local"
            : normalized.Trim('.').Replace('/', '.');
    }

    internal static string? ResolvePodName(KubernetesTelemetryExportOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return Normalize(options.PodName)
            ?? Normalize(Environment.GetEnvironmentVariable("POD_NAME"))
            ?? Normalize(Environment.GetEnvironmentVariable("HOSTNAME"));
    }

    internal static string? ResolvePodUid(KubernetesTelemetryExportOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return Normalize(options.PodUid) ?? Normalize(Environment.GetEnvironmentVariable("POD_UID"));
    }

    internal static string? ResolveNodeName(KubernetesTelemetryExportOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return Normalize(options.NodeName) ?? Normalize(Environment.GetEnvironmentVariable("NODE_NAME"));
    }

    internal static string? ResolveContainerName(KubernetesTelemetryExportOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return Normalize(options.ContainerName) ?? Normalize(Environment.GetEnvironmentVariable("CONTAINER_NAME"));
    }

    private static bool ShouldRegister(TelemetryExportOptions telemetry, KubernetesEndpointMode endpointMode)
    {
        ArgumentNullException.ThrowIfNull(telemetry);

        return endpointMode != KubernetesEndpointMode.None &&
            (telemetry.ExportLogs || telemetry.ExportMetrics || telemetry.ExportTraces);
    }

    private static KubernetesEndpointMode ResolveEndpointMode(
        TelemetryExportOptions telemetry,
        KubernetesTelemetryExportOptions options)
    {
        ArgumentNullException.ThrowIfNull(telemetry);
        ArgumentNullException.ThrowIfNull(options);

        if (!string.IsNullOrWhiteSpace(telemetry.Endpoint))
        {
            return KubernetesEndpointMode.ConfiguredEndpoint;
        }

        if (telemetry.UseSelfHostedDefaults)
        {
            return KubernetesEndpointMode.SelfHostedDefaults;
        }

        return options.UseInClusterCollectorService
            ? KubernetesEndpointMode.InClusterCollectorService
            : KubernetesEndpointMode.None;
    }

    private static void EnsureProviderIsSupported(string provider)
    {
        if (string.Equals(provider, "OpenTelemetry", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        throw new InvalidOperationException(
            $"Cephalon Kubernetes observability integration only supports telemetry provider 'OpenTelemetry'. Configured provider '{provider}' is not supported.");
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
        KubernetesTelemetryExportOptions options,
        OtlpExportProtocol exporterProtocol,
        KubernetesEndpointMode endpointMode,
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
        KubernetesTelemetryExportOptions options,
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
                "Cephalon Kubernetes observability integration currently only supports TrustedCaCertificatePath for HTTPS OTLP/HTTP traces and metrics. OTLP/gRPC custom CA handling should stay explicit in the collector or host trust store.");
        }

        if (signal == TelemetrySignal.Logs)
        {
            throw new InvalidOperationException(
                "Cephalon Kubernetes observability integration cannot apply TrustedCaCertificatePath to OTLP/HTTP logs because the current OpenTelemetry logging exporter does not support custom HttpClientFactory wiring. Disable log export for that path or rely on system trust.");
        }

        var trustedCaCertificatePath = options.TrustedCaCertificatePath.Trim();
        exporter.HttpClientFactory = () => KubernetesTrustedCaHttpClientHandler.CreateHttpClient(trustedCaCertificatePath);
    }

    private static Uri? ResolveEndpoint(
        TelemetryExportOptions telemetry,
        KubernetesTelemetryExportOptions options,
        OtlpExportProtocol exporterProtocol,
        KubernetesEndpointMode endpointMode)
    {
        ArgumentNullException.ThrowIfNull(telemetry);
        ArgumentNullException.ThrowIfNull(options);

        return endpointMode switch
        {
            KubernetesEndpointMode.ConfiguredEndpoint => ResolveAbsoluteEndpoint(
                telemetry.Endpoint,
                "Telemetry endpoint"),
            KubernetesEndpointMode.SelfHostedDefaults => exporterProtocol switch
            {
                OtlpExportProtocol.Grpc => new Uri("http://localhost:4317", UriKind.Absolute),
                OtlpExportProtocol.HttpProtobuf => new Uri("http://localhost:4318", UriKind.Absolute),
                _ => throw new ArgumentOutOfRangeException(nameof(exporterProtocol))
            },
            KubernetesEndpointMode.InClusterCollectorService => BuildInClusterCollectorEndpoint(options, exporterProtocol),
            _ => null
        };
    }

    private static Uri BuildInClusterCollectorEndpoint(
        KubernetesTelemetryExportOptions options,
        OtlpExportProtocol exporterProtocol)
    {
        ArgumentNullException.ThrowIfNull(options);

        var serviceName = ResolveCollectorServiceName(options);
        var collectorNamespace = ResolveCollectorNamespace(options);
        var scheme = ResolveCollectorScheme(options.CollectorScheme);
        var port = options.CollectorPort ?? (exporterProtocol == OtlpExportProtocol.HttpProtobuf ? 4318 : 4317);
        var suffix = ResolveServiceDnsSuffix(options.ServiceDnsSuffix);
        var endpoint = $"{scheme}://{serviceName}.{collectorNamespace}.{suffix}:{port}";

        return new Uri(endpoint, UriKind.Absolute);
    }

    private static Uri ResolveAbsoluteEndpoint(string? value, string description)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Cephalon Kubernetes observability integration requires {description.ToLowerInvariant()} to be configured.");
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
        ConfigureKubernetesResource(resourceBuilder, resourceAttributes);
        return resourceBuilder;
    }

    private static void ConfigureKubernetesResource(
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

    private static List<KeyValuePair<string, object>> ResolveResourceAttributes(
        KubernetesTelemetryExportOptions options,
        string? environmentName)
    {
        ArgumentNullException.ThrowIfNull(options);

        var attributes = new List<KeyValuePair<string, object>>();

        var clusterName = Normalize(options.ClusterName);
        if (!string.IsNullOrWhiteSpace(clusterName))
        {
            attributes.Add(new KeyValuePair<string, object>("k8s.cluster.name", clusterName));
        }

        var namespaceName = ResolveWorkloadNamespace(options);
        if (!string.IsNullOrWhiteSpace(namespaceName))
        {
            attributes.Add(new KeyValuePair<string, object>("k8s.namespace.name", namespaceName));
            attributes.Add(new KeyValuePair<string, object>("service.namespace", namespaceName));
        }

        var podName = ResolvePodName(options);
        if (!string.IsNullOrWhiteSpace(podName))
        {
            attributes.Add(new KeyValuePair<string, object>("k8s.pod.name", podName));
        }

        var podUid = ResolvePodUid(options);
        if (!string.IsNullOrWhiteSpace(podUid))
        {
            attributes.Add(new KeyValuePair<string, object>("k8s.pod.uid", podUid));
        }

        var nodeName = ResolveNodeName(options);
        if (!string.IsNullOrWhiteSpace(nodeName))
        {
            attributes.Add(new KeyValuePair<string, object>("k8s.node.name", nodeName));
        }

        var containerName = ResolveContainerName(options);
        if (!string.IsNullOrWhiteSpace(containerName))
        {
            attributes.Add(new KeyValuePair<string, object>("k8s.container.name", containerName));
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

internal enum KubernetesEndpointMode
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
