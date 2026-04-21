using Cephalon.Abstractions.Technologies;
using Cephalon.Edge.Traefik.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cephalon.Edge.Traefik.Services;

internal sealed class TraefikTrafficObservationHostedService(
    ICellTrafficAutomationRuntimeCatalog catalog,
    ICellTrafficAutomationMaterializationReportSink reportSink,
    TraefikTrafficAutomationMaterializer materializer,
    TimeProvider timeProvider,
    ILogger<TraefikTrafficObservationHostedService> logger) : BackgroundService
{
    private static readonly Action<ILogger, string, int, Exception?> LogObservationLoopStartedMessage =
        LoggerMessage.Define<string, int>(
            LogLevel.Information,
            new EventId(21500, nameof(LogObservationLoopStarted)),
            "Traefik control-plane observation loop started in mode '{Mode}' with polling interval {PollingIntervalSeconds}s.");
    private static readonly Action<ILogger, Exception?> LogObservationLoopStoppedMessage =
        LoggerMessage.Define(
            LogLevel.Information,
            new EventId(21501, nameof(LogObservationLoopStopped)),
            "Traefik control-plane observation loop stopped.");
    private static readonly Action<ILogger, string, string?, Exception?> LogObservationFailedMessage =
        LoggerMessage.Define<string, string?>(
            LogLevel.Warning,
            new EventId(21502, nameof(LogObservationFailed)),
            "Traefik control-plane observation failed for automation '{AutomationId}' on provider '{ProviderId}'.");
    private static readonly Action<ILogger, string, Exception?> LogCleanupSweepFailedMessage =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(21503, nameof(LogCleanupSweepFailed)),
            "Traefik cleanup sweep failed: {Error}");

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!materializer.SupportsLiveReconciliation)
        {
            return;
        }

        LogObservationLoopStarted(
            logger,
            materializer.UsesApplyAndReconcile
                ? TraefikTrafficObservationModes.ApplyAndReconcile
                : TraefikTrafficObservationModes.ObserveOnly,
            (int)materializer.ObservationPollingInterval.TotalSeconds);
        await base.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (!materializer.SupportsLiveReconciliation)
        {
            return;
        }

        await base.StopAsync(cancellationToken).ConfigureAwait(false);
        LogObservationLoopStopped(logger);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!materializer.SupportsLiveReconciliation)
        {
            return;
        }

        using var timer = new PeriodicTimer(materializer.ObservationPollingInterval, timeProvider);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                await ObserveAvailableAsync(stoppingToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task ObserveAvailableAsync(CancellationToken cancellationToken)
    {
        var automations = catalog.Automations
            .Where(automation => string.Equals(
                automation.ProviderMaterializerId,
                materializer.MaterializerId,
                StringComparison.OrdinalIgnoreCase))
            .OrderBy(static automation => automation.SourceCellId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static automation => automation.TargetCellId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static automation => automation.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (materializer.SupportsCleanupSweep)
        {
            var cleanupResult = await materializer.SweepCleanupAsync(automations, cancellationToken).ConfigureAwait(false);
            if (string.Equals(cleanupResult.State, "failed", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(cleanupResult.Error))
            {
                LogCleanupSweepFailed(logger, cleanupResult.Error!);
            }
        }

        foreach (var automation in automations)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var result = await materializer.RefreshAsync(automation, cancellationToken).ConfigureAwait(false);
                await reportSink.ReportProviderAsync(
                    automation.Id,
                    materializer.MaterializerId,
                    result,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                LogObservationFailed(logger, automation.Id, automation.ProviderId, exception);
                await reportSink.ReportProviderAsync(
                    automation.Id,
                    materializer.MaterializerId,
                    new CellTrafficAutomationProviderMaterializationResult(
                        CellTrafficAutomationProviderMaterializationStates.Failed,
                        timeProvider.GetUtcNow(),
                        exception.Message),
                    cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static void LogObservationLoopStarted(ILogger logger, string mode, int pollingIntervalSeconds) =>
        LogObservationLoopStartedMessage(logger, mode, pollingIntervalSeconds, null);

    private static void LogObservationLoopStopped(ILogger logger) =>
        LogObservationLoopStoppedMessage(logger, null);

    private static void LogObservationFailed(
        ILogger logger,
        string automationId,
        string? providerId,
        Exception exception) =>
        LogObservationFailedMessage(logger, automationId, providerId, exception);

    private static void LogCleanupSweepFailed(ILogger logger, string error) =>
        LogCleanupSweepFailedMessage(logger, error, null);
}
