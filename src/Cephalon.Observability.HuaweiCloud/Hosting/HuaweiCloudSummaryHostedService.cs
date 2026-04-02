using Cephalon.Observability.Configuration;
using Cephalon.Observability.HuaweiCloud.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cephalon.Observability.HuaweiCloud.Hosting;

internal sealed class HuaweiCloudSummaryHostedService : IHostedService
{
    private static readonly Action<ILogger, string, string, string, bool, string, Exception?> LogExportSummary =
        LoggerMessage.Define<string, string, string, bool, string>(
            LogLevel.Information,
            new EventId(HuaweiCloudDiagnosticsConventions.ExportSummary.Id, HuaweiCloudDiagnosticsConventions.ExportSummary.Name),
            HuaweiCloudDiagnosticsConventions.ExportSummary.MessageTemplate);

    private readonly ILogger<HuaweiCloudSummaryHostedService> logger;
    private readonly TelemetryExportOptions telemetry;
    private readonly HuaweiCloudTelemetryExportOptions options;

    public HuaweiCloudSummaryHostedService(
        ILogger<HuaweiCloudSummaryHostedService> logger,
        TelemetryExportOptions telemetry,
        HuaweiCloudTelemetryExportOptions options)
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
                ResolveUseManagedTraceIngestion(),
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

        return options.UseApmManagedTraceIngestion
            ? "huawei-managed-trace-ingestion"
            : "not-configured";
    }

    private string ResolveAuthenticationMode()
    {
        return options.UseApmManagedTraceIngestion &&
            string.IsNullOrWhiteSpace(telemetry.Endpoint) &&
            !telemetry.UseSelfHostedDefaults
            ? "Authentication-header"
            : "shared-collector-contract";
    }

    private bool ResolveUseManagedTraceIngestion()
    {
        return options.UseApmManagedTraceIngestion &&
            string.IsNullOrWhiteSpace(telemetry.Endpoint) &&
            !telemetry.UseSelfHostedDefaults;
    }

    private static string ResolveHostedPlatformDisplay(string? hostedPlatform)
    {
        return string.IsNullOrWhiteSpace(hostedPlatform)
            ? "not-configured"
            : HuaweiCloudHostApplicationBuilderExtensions.NormalizeHostedPlatform(hostedPlatform) ?? hostedPlatform.Trim();
    }

    private string ResolveSignals()
    {
        if (ResolveUseManagedTraceIngestion())
        {
            return $"logs=not-exported-by-managed-apm, metrics=not-exported-by-managed-apm, traces={telemetry.ExportTraces}";
        }

        return $"logs={telemetry.ExportLogs}, metrics={telemetry.ExportMetrics}, traces={telemetry.ExportTraces}";
    }
}
