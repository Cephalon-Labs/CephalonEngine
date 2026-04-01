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
            new EventId(3000, "ManifestSummary"),
            "Runtime manifest {ManifestVersion} for engine {EngineVersion} is active on blueprint {BlueprintId}. Modules {ModuleCount}. Capabilities {CapabilityCount}.");
    private static readonly Action<ILogger, string, string, Exception?> LogDiagnosticsConventionMessage =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(3003, "DiagnosticsConvention"),
            "Diagnostics use meter {MeterName} and activity source {ActivitySourceName}.");
    private static readonly Action<ILogger, string, string, int, int, Exception?> LogModuleSummaryMessage =
        LoggerMessage.Define<string, string, int, int>(
            LogLevel.Information,
            new EventId(3001, "ModuleSummary"),
            "Module loaded '{ModuleId}' version {Version}. Dependencies {DependencyCount}. Tags {TagCount}.");
    private static readonly Action<ILogger, string, string, Exception?> LogCapabilitySummaryMessage =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(3002, "CapabilitySummary"),
            "Capability exposed '{CapabilityKey}' from module '{SourceModuleId}'.");
    private static readonly Action<ILogger, string, string, string, Exception?> LogOperationalHealthMessage =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Information,
            new EventId(3004, "OperationalHealth"),
            "Operational health reports liveness {LivenessState}, readiness {ReadinessState}, runtime status {RuntimeStatus}.");
    private static readonly Action<ILogger, string, string, string, bool, bool, bool, Exception?> LogTelemetryExportMessage =
        LoggerMessage.Define<string, string, string, bool, bool, bool>(
            LogLevel.Information,
            new EventId(3005, "TelemetryExport"),
            "Telemetry export guidance uses provider {Provider}, protocol {Protocol}, endpoint {Endpoint}, logs {ExportLogs}, metrics {ExportMetrics}, traces {ExportTraces}.");

    private readonly ILogger<ManifestSummaryHostedService> logger;
    private readonly IRuntime runtime;
    private readonly RuntimeHealthEvaluator health;
    private readonly ObservabilityOptions options;

    public ManifestSummaryHostedService(
        ILogger<ManifestSummaryHostedService> logger,
        IRuntime runtime,
        RuntimeHealthEvaluator health,
        ObservabilityOptions options)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        this.health = health ?? throw new ArgumentNullException(nameof(health));
        this.options = options ?? throw new ArgumentNullException(nameof(options));
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
                options.Telemetry.Endpoint ?? "not-configured",
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
}
