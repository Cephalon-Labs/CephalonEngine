using Cephalon.Observability.Configuration;
using Cephalon.Observability.NewRelic.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cephalon.Observability.NewRelic.Hosting;

internal sealed class NewRelicSummaryHostedService : IHostedService
{
    private static readonly Action<ILogger, string, string, string, string, string, string, Exception?> LogExportSummary =
        LoggerMessage.Define<string, string, string, string, string, string>(
            LogLevel.Information,
            new EventId(NewRelicDiagnosticsConventions.ExportSummary.Id, NewRelicDiagnosticsConventions.ExportSummary.Name),
            NewRelicDiagnosticsConventions.ExportSummary.MessageTemplate);

    private readonly ILogger<NewRelicSummaryHostedService> logger;
    private readonly TelemetryExportOptions telemetry;
    private readonly NewRelicTelemetryExportOptions options;
    private readonly IHostEnvironment environment;

    public NewRelicSummaryHostedService(
        ILogger<NewRelicSummaryHostedService> logger,
        TelemetryExportOptions telemetry,
        NewRelicTelemetryExportOptions options,
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
                ResolveRegionSelection(),
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

        return NewRelicHostApplicationBuilderExtensions.HasDirectEndpointConfiguration(options)
            ? "newrelic-direct"
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
            return NewRelicHostApplicationBuilderExtensions.ResolveSelfHostedCollectorEndpoint(telemetry.Protocol) ?? "not-configured";
        }

        if (!NewRelicHostApplicationBuilderExtensions.HasDirectEndpointConfiguration(options))
        {
            return "not-configured";
        }

        return !string.IsNullOrWhiteSpace(options.Endpoint)
            ? options.Endpoint
            : NewRelicHostApplicationBuilderExtensions.ResolveRegionalEndpoint(options.Region).ToString();
    }

    private string ResolveAuthenticationMode()
    {
        if (!NewRelicHostApplicationBuilderExtensions.HasDirectEndpointConfiguration(options) ||
            !string.IsNullOrWhiteSpace(telemetry.Endpoint) ||
            telemetry.UseSelfHostedDefaults)
        {
            return "shared-collector-contract";
        }

        if (!string.IsNullOrWhiteSpace(options.Headers))
        {
            return "explicit-headers";
        }

        return !string.IsNullOrWhiteSpace(options.LicenseKey)
            ? "api-key"
            : "not-configured";
    }

    private string ResolveRegionSelection()
    {
        if (!NewRelicHostApplicationBuilderExtensions.HasDirectEndpointConfiguration(options) ||
            !string.IsNullOrWhiteSpace(telemetry.Endpoint) ||
            telemetry.UseSelfHostedDefaults)
        {
            return "shared-collector-contract";
        }

        return !string.IsNullOrWhiteSpace(options.Endpoint)
            ? "custom-endpoint"
            : NewRelicHostApplicationBuilderExtensions.NormalizeRegion(options.Region);
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
