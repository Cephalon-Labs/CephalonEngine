using Cephalon.Observability.Configuration;
using Cephalon.Observability.Gcp.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cephalon.Observability.Gcp.Hosting;

internal sealed class GcpSummaryHostedService : IHostedService
{
    private static readonly Action<ILogger, string, string, string, bool, string, Exception?> LogExportSummary =
        LoggerMessage.Define<string, string, string, bool, string>(
            LogLevel.Information,
            new EventId(GcpDiagnosticsConventions.ExportSummary.Id, GcpDiagnosticsConventions.ExportSummary.Name),
            GcpDiagnosticsConventions.ExportSummary.MessageTemplate);

    private readonly ILogger<GcpSummaryHostedService> logger;
    private readonly TelemetryExportOptions telemetry;
    private readonly GcpTelemetryExportOptions options;

    public GcpSummaryHostedService(
        ILogger<GcpSummaryHostedService> logger,
        TelemetryExportOptions telemetry,
        GcpTelemetryExportOptions options)
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
                ResolveCredentialMode(),
                options.UseGoogleManagedIngestion,
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

        return options.UseGoogleManagedIngestion
            ? "google-managed-ingestion"
            : "not-configured";
    }

    private string ResolveCredentialMode()
    {
        return options.UseApplicationDefaultCredentials
            ? "ApplicationDefaultCredentials"
            : "none";
    }

    private static string ResolveHostedPlatformDisplay(string? hostedPlatform)
    {
        return string.IsNullOrWhiteSpace(hostedPlatform)
            ? "not-configured"
            : GcpHostApplicationBuilderExtensions.NormalizeHostedPlatform(hostedPlatform) ?? hostedPlatform.Trim();
    }

    private string ResolveSignals()
    {
        if (string.IsNullOrWhiteSpace(telemetry.Endpoint) &&
            !telemetry.UseSelfHostedDefaults &&
            options.UseGoogleManagedIngestion)
        {
            return $"logs=platform-or-collector, metrics={telemetry.ExportMetrics}, traces={telemetry.ExportTraces}";
        }

        return $"logs={telemetry.ExportLogs}, metrics={telemetry.ExportMetrics}, traces={telemetry.ExportTraces}";
    }
}
