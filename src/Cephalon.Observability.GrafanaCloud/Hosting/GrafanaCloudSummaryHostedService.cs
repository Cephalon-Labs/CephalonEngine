using Cephalon.Observability.Configuration;
using Cephalon.Observability.GrafanaCloud.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cephalon.Observability.GrafanaCloud.Hosting;

internal sealed class GrafanaCloudSummaryHostedService : IHostedService
{
    private static readonly Action<ILogger, string, string, string, string, string, Exception?> LogExportSummary =
        LoggerMessage.Define<string, string, string, string, string>(
            LogLevel.Information,
            new EventId(GrafanaCloudDiagnosticsConventions.ExportSummary.Id, GrafanaCloudDiagnosticsConventions.ExportSummary.Name),
            GrafanaCloudDiagnosticsConventions.ExportSummary.MessageTemplate);

    private readonly ILogger<GrafanaCloudSummaryHostedService> logger;
    private readonly TelemetryExportOptions telemetry;
    private readonly GrafanaCloudTelemetryExportOptions options;
    private readonly IHostEnvironment environment;

    public GrafanaCloudSummaryHostedService(
        ILogger<GrafanaCloudSummaryHostedService> logger,
        TelemetryExportOptions telemetry,
        GrafanaCloudTelemetryExportOptions options,
        IHostEnvironment environment)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        this.environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            LogExportSummary(
                logger,
                ResolveEndpointMode(),
                ResolveEndpointTarget(),
                ResolveAuthenticationMode(),
                ResolveResourceContext(),
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

        return GrafanaCloudHostApplicationBuilderExtensions.HasDirectEndpointConfiguration(options)
            ? "grafana-cloud-direct"
            : "not-configured";
    }

    private string ResolveEndpointTarget()
    {
        if (!string.IsNullOrWhiteSpace(telemetry.Endpoint))
        {
            return telemetry.Endpoint;
        }

        if (telemetry.UseSelfHostedDefaults)
        {
            return GrafanaCloudHostApplicationBuilderExtensions.ResolveSelfHostedCollectorEndpoint(telemetry.Protocol) ?? "not-configured";
        }

        return string.IsNullOrWhiteSpace(options.Endpoint)
            ? "not-configured"
            : options.Endpoint;
    }

    private string ResolveAuthenticationMode()
    {
        if (!GrafanaCloudHostApplicationBuilderExtensions.HasDirectEndpointConfiguration(options) ||
            !string.IsNullOrWhiteSpace(telemetry.Endpoint) ||
            telemetry.UseSelfHostedDefaults)
        {
            return "shared-collector-contract";
        }

        if (!string.IsNullOrWhiteSpace(options.Headers))
        {
            return "explicit-headers";
        }

        return !string.IsNullOrWhiteSpace(options.InstanceId) && !string.IsNullOrWhiteSpace(options.AccessPolicyToken)
            ? "access-policy-basic"
            : "not-configured";
    }

    private string ResolveResourceContext()
    {
        var descriptors = new List<string>();

        if (!string.IsNullOrWhiteSpace(options.ServiceNamespace))
        {
            descriptors.Add($"service.namespace={options.ServiceNamespace}");
        }

        if (!string.IsNullOrWhiteSpace(environment.EnvironmentName))
        {
            descriptors.Add($"deployment.environment.name={environment.EnvironmentName.Trim()}");
        }

        return descriptors.Count == 0 ? "service-only" : string.Join(", ", descriptors);
    }

    private string ResolveSignals()
    {
        return $"logs={telemetry.ExportLogs}, metrics={telemetry.ExportMetrics}, traces={telemetry.ExportTraces}";
    }
}
