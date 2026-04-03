using Cephalon.Observability.Configuration;
using Cephalon.Observability.Kubernetes.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;

namespace Cephalon.Observability.Kubernetes.Hosting;

internal sealed class KubernetesSummaryHostedService : IHostedService
{
    private static readonly Action<ILogger, string, string, string, string, string, string, Exception?> LogExportSummary =
        LoggerMessage.Define<string, string, string, string, string, string>(
            LogLevel.Information,
            new EventId(KubernetesDiagnosticsConventions.ExportSummary.Id, KubernetesDiagnosticsConventions.ExportSummary.Name),
            KubernetesDiagnosticsConventions.ExportSummary.MessageTemplate);

    private readonly ILogger<KubernetesSummaryHostedService> logger;
    private readonly TelemetryExportOptions telemetry;
    private readonly KubernetesTelemetryExportOptions options;

    public KubernetesSummaryHostedService(
        ILogger<KubernetesSummaryHostedService> logger,
        TelemetryExportOptions telemetry,
        KubernetesTelemetryExportOptions options)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
        this.options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            LogExportSummary(
                logger,
                ResolveEndpointMode(),
                ResolveCollectorTarget(),
                ResolveResourceContext(),
                ResolveTrustMode(),
                ResolveHeadersMode(),
                ResolveSignals(),
                null);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private string ResolveEndpointMode()
    {
        if (!string.IsNullOrWhiteSpace(telemetry.Endpoint))
        {
            return "configured-endpoint";
        }

        if (telemetry.UseSelfHostedDefaults)
        {
            return "self-hosted-defaults";
        }

        return options.UseInClusterCollectorService ? "kubernetes-collector-service" : "not-configured";
    }

    private string ResolveCollectorTarget()
    {
        if (!string.IsNullOrWhiteSpace(telemetry.Endpoint))
        {
            return "shared-collector-contract";
        }

        if (telemetry.UseSelfHostedDefaults)
        {
            return ResolveExporterProtocol() == OtlpExportProtocol.HttpProtobuf
                ? "localhost:4318"
                : "localhost:4317";
        }

        if (!options.UseInClusterCollectorService)
        {
            return "not-configured";
        }

        var serviceName = KubernetesHostApplicationBuilderExtensions.ResolveCollectorServiceName(options);
        var collectorNamespace = KubernetesHostApplicationBuilderExtensions.ResolveCollectorNamespace(options);
        var scheme = KubernetesHostApplicationBuilderExtensions.ResolveCollectorScheme(options.CollectorScheme);
        var port = options.CollectorPort ?? (ResolveExporterProtocol() == OtlpExportProtocol.HttpProtobuf ? 4318 : 4317);
        var suffix = KubernetesHostApplicationBuilderExtensions.ResolveServiceDnsSuffix(options.ServiceDnsSuffix);

        return $"{scheme}://{serviceName}.{collectorNamespace}.{suffix}:{port}";
    }

    private string ResolveResourceContext()
    {
        var descriptors = new List<string>();

        if (!string.IsNullOrWhiteSpace(options.ClusterName))
        {
            descriptors.Add($"cluster={options.ClusterName}");
        }

        var namespaceName = KubernetesHostApplicationBuilderExtensions.ResolveWorkloadNamespace(options);
        if (!string.IsNullOrWhiteSpace(namespaceName))
        {
            descriptors.Add($"namespace={namespaceName}");
        }

        var podName = KubernetesHostApplicationBuilderExtensions.ResolvePodName(options);
        if (!string.IsNullOrWhiteSpace(podName))
        {
            descriptors.Add($"pod={podName}");
        }

        var nodeName = KubernetesHostApplicationBuilderExtensions.ResolveNodeName(options);
        if (!string.IsNullOrWhiteSpace(nodeName))
        {
            descriptors.Add($"node={nodeName}");
        }

        var containerName = KubernetesHostApplicationBuilderExtensions.ResolveContainerName(options);
        if (!string.IsNullOrWhiteSpace(containerName))
        {
            descriptors.Add($"container={containerName}");
        }

        return descriptors.Count == 0 ? "none" : string.Join(", ", descriptors);
    }

    private string ResolveTrustMode()
    {
        if (string.IsNullOrWhiteSpace(options.TrustedCaCertificatePath))
        {
            return ResolveCollectorSchemeForSummary() == "https" ? "system-trust" : "not-applicable-over-http";
        }

        return ResolveCollectorSchemeForSummary() == "https"
            ? "custom-ca-bundle"
            : "custom-ca-bundle-configured-over-http";
    }

    private string ResolveHeadersMode()
    {
        return string.IsNullOrWhiteSpace(options.Headers) ? "none" : "configured";
    }

    private string ResolveSignals()
    {
        return $"logs={telemetry.ExportLogs}, metrics={telemetry.ExportMetrics}, traces={telemetry.ExportTraces}";
    }

    private string ResolveCollectorSchemeForSummary()
    {
        if (!string.IsNullOrWhiteSpace(telemetry.Endpoint) &&
            Uri.TryCreate(telemetry.Endpoint, UriKind.Absolute, out var endpoint))
        {
            return endpoint.Scheme;
        }

        if (telemetry.UseSelfHostedDefaults)
        {
            return "http";
        }

        return KubernetesHostApplicationBuilderExtensions.ResolveCollectorScheme(options.CollectorScheme);
    }

    private OtlpExportProtocol ResolveExporterProtocol()
    {
        return KubernetesHostApplicationBuilderExtensions.ResolveExporterProtocol(telemetry.Protocol);
    }
}
