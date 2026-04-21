using Cephalon.Abstractions.Technologies;
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
    private static readonly Action<ILogger, int, Exception?> LogObservationLoopStartedMessage =
        LoggerMessage.Define<int>(
            LogLevel.Information,
            new EventId(21400, nameof(LogObservationLoopStarted)),
            "Kubernetes Gateway live observation loop started with polling interval {PollingIntervalSeconds}s.");
    private static readonly Action<ILogger, Exception?> LogObservationLoopStoppedMessage =
        LoggerMessage.Define(
            LogLevel.Information,
            new EventId(21401, nameof(LogObservationLoopStopped)),
            "Kubernetes Gateway live observation loop stopped.");
    private static readonly Action<ILogger, string, string?, Exception?> LogObservationFailedMessage =
        LoggerMessage.Define<string, string?>(
            LogLevel.Warning,
            new EventId(21402, nameof(LogObservationFailed)),
            "Kubernetes Gateway live observation failed for automation '{AutomationId}' on provider '{ProviderId}'.");

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!materializer.SupportsLiveObservation)
        {
            return;
        }

        LogObservationLoopStarted(logger, (int)materializer.ObservationPollingInterval.TotalSeconds);
        await base.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (!materializer.SupportsLiveObservation)
        {
            return;
        }

        await base.StopAsync(cancellationToken).ConfigureAwait(false);
        LogObservationLoopStopped(logger);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!materializer.SupportsLiveObservation)
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

        foreach (var automation in automations)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var result = await materializer.ObserveAsync(automation, cancellationToken).ConfigureAwait(false);
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

    private static void LogObservationLoopStarted(ILogger logger, int pollingIntervalSeconds) =>
        LogObservationLoopStartedMessage(logger, pollingIntervalSeconds, null);

    private static void LogObservationLoopStopped(ILogger logger) =>
        LogObservationLoopStoppedMessage(logger, null);

    private static void LogObservationFailed(
        ILogger logger,
        string automationId,
        string? providerId,
        Exception exception) =>
        LogObservationFailedMessage(logger, automationId, providerId, exception);
}
