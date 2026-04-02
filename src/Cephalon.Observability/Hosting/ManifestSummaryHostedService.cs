using Cephalon.Engine.Diagnostics;
using Cephalon.Engine.Runtime;
using Cephalon.Observability.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cephalon.Observability.Hosting;

internal sealed class ManifestSummaryHostedService : IHostedService
{
    private static readonly Action<ILogger, string, string, string, int, int, Exception?> LogManifestSummaryMessage =
        LoggerMessage.Define<string, string, string, int, int>(
            LogLevel.Information,
            new EventId(ObservabilityDiagnosticsConventions.ManifestSummary.Id, ObservabilityDiagnosticsConventions.ManifestSummary.Name),
            ObservabilityDiagnosticsConventions.ManifestSummary.MessageTemplate);
    private static readonly Action<ILogger, string, string, Exception?> LogDiagnosticsConventionMessage =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(ObservabilityDiagnosticsConventions.DiagnosticsConvention.Id, ObservabilityDiagnosticsConventions.DiagnosticsConvention.Name),
            ObservabilityDiagnosticsConventions.DiagnosticsConvention.MessageTemplate);
    private static readonly Action<ILogger, string, string, int, int, Exception?> LogModuleSummaryMessage =
        LoggerMessage.Define<string, string, int, int>(
            LogLevel.Information,
            new EventId(ObservabilityDiagnosticsConventions.ModuleSummary.Id, ObservabilityDiagnosticsConventions.ModuleSummary.Name),
            ObservabilityDiagnosticsConventions.ModuleSummary.MessageTemplate);
    private static readonly Action<ILogger, string, string, Exception?> LogCapabilitySummaryMessage =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(ObservabilityDiagnosticsConventions.CapabilitySummary.Id, ObservabilityDiagnosticsConventions.CapabilitySummary.Name),
            ObservabilityDiagnosticsConventions.CapabilitySummary.MessageTemplate);
    private static readonly Action<ILogger, string, string, string, Exception?> LogOperationalHealthMessage =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Information,
            new EventId(ObservabilityDiagnosticsConventions.OperationalHealth.Id, ObservabilityDiagnosticsConventions.OperationalHealth.Name),
            ObservabilityDiagnosticsConventions.OperationalHealth.MessageTemplate);
    private static readonly Action<ILogger, string, string, string, bool, bool, bool, Exception?> LogTelemetryExportMessage =
        LoggerMessage.Define<string, string, string, bool, bool, bool>(
            LogLevel.Information,
            new EventId(ObservabilityDiagnosticsConventions.TelemetryExport.Id, ObservabilityDiagnosticsConventions.TelemetryExport.Name),
            ObservabilityDiagnosticsConventions.TelemetryExport.MessageTemplate);
    private static readonly Action<ILogger, string, string, string, int, Exception?> LogDiagnosticsCatalogEntryMessage =
        LoggerMessage.Define<string, string, string, int>(
            LogLevel.Information,
            new EventId(ObservabilityDiagnosticsConventions.DiagnosticsCatalogEntry.Id, ObservabilityDiagnosticsConventions.DiagnosticsCatalogEntry.Name),
            ObservabilityDiagnosticsConventions.DiagnosticsCatalogEntry.MessageTemplate);

    private readonly ILogger<ManifestSummaryHostedService> logger;
    private readonly IRuntime runtime;
    private readonly RuntimeHealthEvaluator health;
    private readonly ObservabilityOptions options;
    private readonly IRuntimeDiagnosticsCatalog diagnosticsCatalog;

    public ManifestSummaryHostedService(
        ILogger<ManifestSummaryHostedService> logger,
        IRuntime runtime,
        RuntimeHealthEvaluator health,
        ObservabilityOptions options,
        IRuntimeDiagnosticsCatalog diagnosticsCatalog)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        this.health = health ?? throw new ArgumentNullException(nameof(health));
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        this.diagnosticsCatalog = diagnosticsCatalog ?? throw new ArgumentNullException(nameof(diagnosticsCatalog));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (options.LogManifestSummary && logger.IsEnabled(LogLevel.Information))
        {
            LogManifestSummaryMessage(
                logger,
                runtime.Manifest.ManifestVersion,
                runtime.Manifest.EngineVersion,
                runtime.Manifest.AppProfile.BlueprintId,
                runtime.Manifest.Modules.Count,
                runtime.Manifest.Capabilities.Count,
                null);

            LogDiagnosticsConventionMessage(
                logger,
                EngineDiagnostics.MeterName,
                EngineDiagnostics.ActivitySourceName,
                null);

            foreach (var convention in diagnosticsCatalog.Conventions)
            {
                var eventIdRange = convention.MinimumEventId.HasValue && convention.MaximumEventId.HasValue
                    ? $"{convention.MinimumEventId.Value}-{convention.MaximumEventId.Value}"
                    : "n/a";
                LogDiagnosticsCatalogEntryMessage(
                    logger,
                    convention.Source,
                    convention.LoggerCategoryPrefix,
                    eventIdRange,
                    convention.Events.Count,
                    null);
            }

            var liveness = health.EvaluateLiveness();
            var readiness = health.EvaluateReadiness();
            LogOperationalHealthMessage(
                logger,
                liveness.State.ToString(),
                readiness.State.ToString(),
                runtime.Status.ToString(),
                null);

            LogTelemetryExportMessage(
                logger,
                options.Telemetry.Provider,
                options.Telemetry.Protocol,
                ResolveTelemetryEndpointDisplay(options.Telemetry),
                options.Telemetry.ExportLogs,
                options.Telemetry.ExportMetrics,
                options.Telemetry.ExportTraces,
                null);
        }

        if (options.LogModuleSummary && logger.IsEnabled(LogLevel.Information))
        {
            foreach (var module in runtime.Manifest.Modules)
            {
                LogModuleSummaryMessage(
                    logger,
                    module.Id,
                    module.Version,
                    module.DependsOn.Count,
                    module.Tags.Count,
                    null);
            }
        }

        if (options.LogCapabilitySummary && logger.IsEnabled(LogLevel.Information))
        {
            foreach (var capability in runtime.Manifest.Capabilities)
            {
                LogCapabilitySummaryMessage(logger, capability.Key, capability.SourceModuleId, null);
            }
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private static string ResolveTelemetryEndpointDisplay(TelemetryExportOptions telemetry)
    {
        ArgumentNullException.ThrowIfNull(telemetry);

        if (!string.IsNullOrWhiteSpace(telemetry.Endpoint))
        {
            return telemetry.Endpoint;
        }

        if (!telemetry.UseSelfHostedDefaults)
        {
            return "not-configured";
        }

        return ResolveSelfHostedCollectorEndpoint(telemetry.Protocol) is { } endpoint
            ? $"{endpoint} (self-hosted default)"
            : $"not-configured (unsupported self-hosted protocol '{telemetry.Protocol}')";
    }

    private static string? ResolveSelfHostedCollectorEndpoint(string? protocol)
    {
        var normalizedProtocol = string.IsNullOrWhiteSpace(protocol)
            ? "otlp"
            : protocol.Trim().ToLowerInvariant();

        return normalizedProtocol switch
        {
            "otlp" => "http://localhost:4317",
            "grpc" => "http://localhost:4317",
            "otlp/grpc" => "http://localhost:4317",
            "http" => "http://localhost:4318",
            "otlp/http" => "http://localhost:4318",
            "otlp-http" => "http://localhost:4318",
            "http/protobuf" => "http://localhost:4318",
            "httpprotobuf" => "http://localhost:4318",
            _ => null
        };
    }
}
