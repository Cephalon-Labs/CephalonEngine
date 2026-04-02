using Cephalon.Observability.AzureMonitor.Configuration;
using Cephalon.Observability.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cephalon.Observability.AzureMonitor.Hosting;

internal sealed class AzureMonitorSummaryHostedService : IHostedService
{
    private static readonly Action<ILogger, string, string, bool, bool, bool, Exception?> LogExportSummary =
        LoggerMessage.Define<string, string, bool, bool, bool>(
            LogLevel.Information,
            new EventId(AzureMonitorDiagnosticsConventions.ExportSummary.Id, AzureMonitorDiagnosticsConventions.ExportSummary.Name),
            AzureMonitorDiagnosticsConventions.ExportSummary.MessageTemplate);

    private readonly ILogger<AzureMonitorSummaryHostedService> logger;
    private readonly TelemetryExportOptions telemetry;
    private readonly AzureMonitorExportOptions options;

    public AzureMonitorSummaryHostedService(
        ILogger<AzureMonitorSummaryHostedService> logger,
        TelemetryExportOptions telemetry,
        AzureMonitorExportOptions options)
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
                options.UseDefaultAzureCredential ? "DefaultAzureCredential" : "ConnectionString",
                ResolveHostedPlatformDisplay(options.HostedPlatform),
                telemetry.ExportLogs,
                telemetry.ExportMetrics,
                telemetry.ExportTraces,
                null);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private static string ResolveHostedPlatformDisplay(string? hostedPlatform)
    {
        return string.IsNullOrWhiteSpace(hostedPlatform)
            ? "not-configured"
            : AzureMonitorHostApplicationBuilderExtensions.NormalizeHostedPlatform(hostedPlatform) ?? hostedPlatform.Trim();
    }
}
