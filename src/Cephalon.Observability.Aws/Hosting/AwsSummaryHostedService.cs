using Cephalon.Observability.Aws.Configuration;
using Cephalon.Observability.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cephalon.Observability.Aws.Hosting;

internal sealed class AwsSummaryHostedService : IHostedService
{
    private static readonly Action<ILogger, string, string, string, bool, string, Exception?> LogExportSummary =
        LoggerMessage.Define<string, string, string, bool, string>(
            LogLevel.Information,
            new EventId(AwsDiagnosticsConventions.ExportSummary.Id, AwsDiagnosticsConventions.ExportSummary.Name),
            AwsDiagnosticsConventions.ExportSummary.MessageTemplate);

    private readonly ILogger<AwsSummaryHostedService> logger;
    private readonly TelemetryExportOptions telemetry;
    private readonly AwsTelemetryExportOptions options;

    public AwsSummaryHostedService(
        ILogger<AwsSummaryHostedService> logger,
        TelemetryExportOptions telemetry,
        AwsTelemetryExportOptions options)
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
                ResolveTraceMode(),
                options.EnableAwsSdkInstrumentation,
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

        return telemetry.UseSelfHostedDefaults ? "self-hosted-defaults" : "not-configured";
    }

    private static string ResolveHostedPlatformDisplay(string? hostedPlatform)
    {
        return string.IsNullOrWhiteSpace(hostedPlatform)
            ? "not-configured"
            : AwsHostApplicationBuilderExtensions.NormalizeHostedPlatform(hostedPlatform) ?? hostedPlatform.Trim();
    }

    private string ResolveTraceMode()
    {
        return $"xray-trace-ids={options.UseXRayTraceIds}, xray-propagator={options.UseXRayPropagator}";
    }

    private string ResolveSignals()
    {
        return $"logs={telemetry.ExportLogs}, metrics={telemetry.ExportMetrics}, traces={telemetry.ExportTraces}";
    }
}
