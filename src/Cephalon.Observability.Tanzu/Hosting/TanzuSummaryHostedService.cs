using Cephalon.Observability.Configuration;
using Cephalon.Observability.Tanzu.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;

namespace Cephalon.Observability.Tanzu.Hosting;

internal sealed class TanzuSummaryHostedService : IHostedService
{
    private static readonly Action<ILogger, string, string, string, string, string, string, Exception?> LogExportSummary =
        LoggerMessage.Define<string, string, string, string, string, string>(
            LogLevel.Information,
            new EventId(TanzuDiagnosticsConventions.ExportSummary.Id, TanzuDiagnosticsConventions.ExportSummary.Name),
            TanzuDiagnosticsConventions.ExportSummary.MessageTemplate);

    private readonly ILogger<TanzuSummaryHostedService> logger;
    private readonly TelemetryExportOptions telemetry;
    private readonly TanzuTelemetryExportOptions options;

    public TanzuSummaryHostedService(
        ILogger<TanzuSummaryHostedService> logger,
        TelemetryExportOptions telemetry,
        TanzuTelemetryExportOptions options)
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
                ResolveHandoffMode(),
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

        return options.UseInClusterProxyService ? "tanzu-proxy-service" : "not-configured";
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

        if (!options.UseInClusterProxyService)
        {
            return "not-configured";
        }

        var endpoint = TanzuHostApplicationBuilderExtensions.BuildInClusterProxyEndpoint(
            options,
            ResolveExporterProtocol());

        return ResolveExporterProtocol() == OtlpExportProtocol.HttpProtobuf
            ? TanzuHostApplicationBuilderExtensions.BuildSignalEndpoint(endpoint, TelemetrySignal.Traces).ToString()
            : endpoint.ToString();
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

    private string ResolveHandoffMode()
    {
        var headersMode = string.IsNullOrWhiteSpace(options.Headers) ? "none" : "configured";

        if (options.UseInClusterProxyService &&
            string.IsNullOrWhiteSpace(telemetry.Endpoint) &&
            !telemetry.UseSelfHostedDefaults)
        {
            return $"wavefront-proxy-traces, headers={headersMode}";
        }

        return $"shared-otlp, headers={headersMode}";
    }

    private static string ResolveHostedPlatformDisplay(string? hostedPlatform)
    {
        return string.IsNullOrWhiteSpace(hostedPlatform)
            ? "not-configured"
            : TanzuHostApplicationBuilderExtensions.NormalizeHostedPlatform(hostedPlatform) ?? hostedPlatform.Trim();
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

        return TanzuHostApplicationBuilderExtensions.ResolveProxyScheme(options.ProxyScheme);
    }

    private OtlpExportProtocol ResolveExporterProtocol()
    {
        return TanzuHostApplicationBuilderExtensions.ResolveExporterProtocol(telemetry.Protocol);
    }
}
