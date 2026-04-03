using Cephalon.Observability.Configuration;
using Cephalon.Observability.OpenShift.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;

namespace Cephalon.Observability.OpenShift.Hosting;

internal sealed class OpenShiftSummaryHostedService : IHostedService
{
    private static readonly Action<ILogger, string, string, string, string, string, string, Exception?> LogExportSummary =
        LoggerMessage.Define<string, string, string, string, string, string>(
            LogLevel.Information,
            new EventId(OpenShiftDiagnosticsConventions.ExportSummary.Id, OpenShiftDiagnosticsConventions.ExportSummary.Name),
            OpenShiftDiagnosticsConventions.ExportSummary.MessageTemplate);

    private readonly ILogger<OpenShiftSummaryHostedService> logger;
    private readonly TelemetryExportOptions telemetry;
    private readonly OpenShiftTelemetryExportOptions options;

    public OpenShiftSummaryHostedService(
        ILogger<OpenShiftSummaryHostedService> logger,
        TelemetryExportOptions telemetry,
        OpenShiftTelemetryExportOptions options)
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
                ResolveHostedPlatformDisplay(options.HostedPlatform),
                ResolveCollectorTarget(),
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

        return options.UseInClusterCollectorService ? "openshift-collector-service" : "not-configured";
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

        var serviceName = OpenShiftHostApplicationBuilderExtensions.ResolveCollectorServiceName(options);
        var collectorNamespace = OpenShiftHostApplicationBuilderExtensions.ResolveCollectorNamespace(options);
        var scheme = OpenShiftHostApplicationBuilderExtensions.ResolveCollectorScheme(options.CollectorScheme);
        var port = options.CollectorPort ?? (ResolveExporterProtocol() == OtlpExportProtocol.HttpProtobuf ? 4318 : 4317);

        return $"{scheme}://{serviceName}.{collectorNamespace}.svc.cluster.local:{port}";
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

    private static string ResolveHostedPlatformDisplay(string? hostedPlatform)
    {
        return string.IsNullOrWhiteSpace(hostedPlatform)
            ? "not-configured"
            : OpenShiftHostApplicationBuilderExtensions.NormalizeHostedPlatform(hostedPlatform) ?? hostedPlatform.Trim();
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

        return OpenShiftHostApplicationBuilderExtensions.ResolveCollectorScheme(options.CollectorScheme);
    }

    private OtlpExportProtocol ResolveExporterProtocol()
    {
        return OpenShiftHostApplicationBuilderExtensions.ResolveExporterProtocol(telemetry.Protocol);
    }
}
