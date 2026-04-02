using Cephalon.Observability.AlibabaCloud.Configuration;
using Cephalon.Observability.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;

namespace Cephalon.Observability.AlibabaCloud.Hosting;

internal sealed class AlibabaCloudSummaryHostedService : IHostedService
{
    private static readonly Action<ILogger, string, string, string, bool, string, Exception?> LogExportSummary =
        LoggerMessage.Define<string, string, string, bool, string>(
            LogLevel.Information,
            new EventId(AlibabaCloudDiagnosticsConventions.ExportSummary.Id, AlibabaCloudDiagnosticsConventions.ExportSummary.Name),
            AlibabaCloudDiagnosticsConventions.ExportSummary.MessageTemplate);

    private readonly ILogger<AlibabaCloudSummaryHostedService> logger;
    private readonly TelemetryExportOptions telemetry;
    private readonly AlibabaCloudTelemetryExportOptions options;

    public AlibabaCloudSummaryHostedService(
        ILogger<AlibabaCloudSummaryHostedService> logger,
        TelemetryExportOptions telemetry,
        AlibabaCloudTelemetryExportOptions options)
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
                ResolveAuthenticationMode(),
                ResolveUseManagedIngestion(),
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

        return options.UseManagedOpenTelemetryIngestion
            ? "alibaba-managed-ingestion"
            : "not-configured";
    }

    private string ResolveAuthenticationMode()
    {
        if (!ResolveUseManagedIngestion())
        {
            return "shared-collector-contract";
        }

        return ResolveManagedExporterProtocol() == OtlpExportProtocol.HttpProtobuf
            ? "endpoint-embedded-token"
            : "Authentication-header";
    }

    private bool ResolveUseManagedIngestion()
    {
        return options.UseManagedOpenTelemetryIngestion &&
            string.IsNullOrWhiteSpace(telemetry.Endpoint) &&
            !telemetry.UseSelfHostedDefaults;
    }

    private OtlpExportProtocol ResolveManagedExporterProtocol()
    {
        return AlibabaCloudHostApplicationBuilderExtensions.ResolveManagedExporterProtocol(telemetry.Protocol);
    }

    private static string ResolveHostedPlatformDisplay(string? hostedPlatform)
    {
        return string.IsNullOrWhiteSpace(hostedPlatform)
            ? "not-configured"
            : AlibabaCloudHostApplicationBuilderExtensions.NormalizeHostedPlatform(hostedPlatform) ?? hostedPlatform.Trim();
    }

    private string ResolveSignals()
    {
        if (ResolveUseManagedIngestion())
        {
            return $"logs=not-exported-by-managed-ingestion, metrics={telemetry.ExportMetrics}, traces={telemetry.ExportTraces}";
        }

        return $"logs={telemetry.ExportLogs}, metrics={telemetry.ExportMetrics}, traces={telemetry.ExportTraces}";
    }
}
