using Cephalon.Observability.Configuration;
using Cephalon.Observability.DigitalOcean.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;

namespace Cephalon.Observability.DigitalOcean.Hosting;

internal sealed class DigitalOceanSummaryHostedService : IHostedService
{
    private static readonly Action<ILogger, string, string, string, string, string, string, Exception?> LogExportSummary =
        LoggerMessage.Define<string, string, string, string, string, string>(
            LogLevel.Information,
            new EventId(DigitalOceanDiagnosticsConventions.ExportSummary.Id, DigitalOceanDiagnosticsConventions.ExportSummary.Name),
            DigitalOceanDiagnosticsConventions.ExportSummary.MessageTemplate);

    private readonly ILogger<DigitalOceanSummaryHostedService> logger;
    private readonly TelemetryExportOptions telemetry;
    private readonly DigitalOceanTelemetryExportOptions options;

    public DigitalOceanSummaryHostedService(
        ILogger<DigitalOceanSummaryHostedService> logger,
        TelemetryExportOptions telemetry,
        DigitalOceanTelemetryExportOptions options)
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
                ResolveContextMode(),
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

        return options.UseInClusterCollectorService ? "doks-collector-service" : "not-configured";
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

        var serviceName = DigitalOceanHostApplicationBuilderExtensions.ResolveCollectorServiceName(options);
        var collectorNamespace = DigitalOceanHostApplicationBuilderExtensions.ResolveCollectorNamespace(options);
        var scheme = DigitalOceanHostApplicationBuilderExtensions.ResolveCollectorScheme(options.CollectorScheme);
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

    private string ResolveContextMode()
    {
        var descriptors = new List<string>();
        var normalizedHostedPlatform = string.IsNullOrWhiteSpace(options.HostedPlatform)
            ? null
            : DigitalOceanHostApplicationBuilderExtensions.NormalizeHostedPlatform(options.HostedPlatform);

        if (string.Equals(normalizedHostedPlatform, "digitalocean_droplet", StringComparison.Ordinal))
        {
            descriptors.Add($"metadata={(options.UseDropletMetadataDefaults ? "enabled" : "disabled")}");
        }

        if (string.Equals(normalizedHostedPlatform, "digitalocean_app_platform", StringComparison.Ordinal))
        {
            descriptors.Add(
                $"app-bindings={(DigitalOceanHostApplicationBuilderExtensions.HasAppPlatformBindings(options) ? "configured" : "not-configured")}");
        }

        descriptors.Add($"headers={(string.IsNullOrWhiteSpace(options.Headers) ? "none" : "configured")}");
        return string.Join(", ", descriptors);
    }

    private static string ResolveHostedPlatformDisplay(string? hostedPlatform)
    {
        return string.IsNullOrWhiteSpace(hostedPlatform)
            ? "not-configured"
            : DigitalOceanHostApplicationBuilderExtensions.NormalizeHostedPlatform(hostedPlatform) ?? hostedPlatform.Trim();
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

        return DigitalOceanHostApplicationBuilderExtensions.ResolveCollectorScheme(options.CollectorScheme);
    }

    private OtlpExportProtocol ResolveExporterProtocol()
    {
        return DigitalOceanHostApplicationBuilderExtensions.ResolveExporterProtocol(telemetry.Protocol);
    }
}
