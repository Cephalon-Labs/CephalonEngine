using Cephalon.Observability.Configuration;
using Cephalon.Observability.OracleCloud.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cephalon.Observability.OracleCloud.Hosting;

internal sealed class OracleCloudSummaryHostedService : IHostedService
{
    private static readonly Action<ILogger, string, string, string, bool, string, Exception?> LogExportSummary =
        LoggerMessage.Define<string, string, string, bool, string>(
            LogLevel.Information,
            new EventId(OracleCloudDiagnosticsConventions.ExportSummary.Id, OracleCloudDiagnosticsConventions.ExportSummary.Name),
            OracleCloudDiagnosticsConventions.ExportSummary.MessageTemplate);

    private readonly ILogger<OracleCloudSummaryHostedService> logger;
    private readonly TelemetryExportOptions telemetry;
    private readonly OracleCloudTelemetryExportOptions options;

    public OracleCloudSummaryHostedService(
        ILogger<OracleCloudSummaryHostedService> logger,
        TelemetryExportOptions telemetry,
        OracleCloudTelemetryExportOptions options)
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
            ? "oracle-apm-managed-ingestion"
            : "not-configured";
    }

    private string ResolveAuthenticationMode()
    {
        if (!ResolveUseManagedIngestion())
        {
            return "shared-collector-contract";
        }

        var modes = new List<string>();

        if (telemetry.ExportTraces)
        {
            modes.Add(options.UsePublicTraceDataKey ? "traces=public-data-key" : "traces=private-data-key");
        }

        if (telemetry.ExportMetrics)
        {
            modes.Add("metrics=private-data-key");
        }

        return modes.Count == 0 ? "not-configured" : string.Join(", ", modes);
    }

    private bool ResolveUseManagedIngestion()
    {
        return options.UseManagedOpenTelemetryIngestion &&
            string.IsNullOrWhiteSpace(telemetry.Endpoint) &&
            !telemetry.UseSelfHostedDefaults;
    }

    private static string ResolveHostedPlatformDisplay(string? hostedPlatform)
    {
        return string.IsNullOrWhiteSpace(hostedPlatform)
            ? "not-configured"
            : OracleCloudHostApplicationBuilderExtensions.NormalizeHostedPlatform(hostedPlatform) ?? hostedPlatform.Trim();
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
