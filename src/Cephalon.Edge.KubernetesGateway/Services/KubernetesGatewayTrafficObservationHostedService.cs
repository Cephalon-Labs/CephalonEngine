using Cephalon.Abstractions.Technologies;
using Cephalon.Edge.KubernetesGateway.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cephalon.Edge.KubernetesGateway.Services;

internal sealed class KubernetesGatewayTrafficObservationHostedService(
    ICellTrafficAutomationRuntimeCatalog catalog,
    ICellTrafficAutomationMaterializationReportSink reportSink,
    KubernetesGatewayTrafficAutomationMaterializer materializer,
    TimeProvider timeProvider,
    ILogger<KubernetesGatewayTrafficObservationHostedService> logger) : BackgroundService
{
    private static readonly Action<ILogger, string, int, Exception?> LogObservationLoopStartedMessage =
        LoggerMessage.Define<string, int>(
            LogLevel.Information,
            new EventId(21400, nameof(LogObservationLoopStarted)),
            "Kubernetes Gateway control-plane reconciliation loop started in mode '{Mode}' with polling interval {PollingIntervalSeconds}s.");
    private static readonly Action<ILogger, Exception?> LogObservationLoopStoppedMessage =
        LoggerMessage.Define(
            LogLevel.Information,
            new EventId(21401, nameof(LogObservationLoopStopped)),
            "Kubernetes Gateway control-plane reconciliation loop stopped.");
    private static readonly Action<ILogger, string, string?, Exception?> LogObservationFailedMessage =
        LoggerMessage.Define<string, string?>(
            LogLevel.Warning,
            new EventId(21402, nameof(LogObservationFailed)),
            "Kubernetes Gateway control-plane reconciliation failed for automation '{AutomationId}' on provider '{ProviderId}'.");
    private static readonly Action<ILogger, string, Exception?> LogCleanupSweepFailedMessage =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(21403, nameof(LogCleanupSweepFailed)),
            "Kubernetes Gateway cleanup sweep failed: {Error}");

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!materializer.SupportsLiveReconciliation)
        {
            return;
        }

        LogObservationLoopStarted(
            logger,
            materializer.UsesApplyAndReconcile
                ? KubernetesGatewayTrafficObservationModes.ApplyAndReconcile
                : KubernetesGatewayTrafficObservationModes.ObserveOnly,
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
