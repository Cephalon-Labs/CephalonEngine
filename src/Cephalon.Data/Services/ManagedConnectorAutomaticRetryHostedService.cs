using Cephalon.Abstractions.Data;
using Cephalon.Data.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cephalon.Data.Services;

internal sealed class ManagedConnectorAutomaticRetryHostedService(
    IServiceScopeFactory scopeFactory,
    DataRuntimeOptions options,
    ILogger<ManagedConnectorAutomaticRetryHostedService> logger) : BackgroundService
{
    private static readonly Action<ILogger, int, Exception?> LogAutomaticRetryLoopStartedMessage =
        LoggerMessage.Define<int>(
            LogLevel.Information,
            new EventId(6240, nameof(LogAutomaticRetryLoopStarted)),
            "Managed-connector automatic retry loop started with polling interval {PollingIntervalSeconds}s.");
    private static readonly Action<ILogger, Exception?> LogAutomaticRetryLoopStoppedMessage =
        LoggerMessage.Define(
            LogLevel.Information,
            new EventId(6241, nameof(LogAutomaticRetryLoopStopped)),
            "Managed-connector automatic retry loop stopped.");
    private static readonly Action<ILogger, string, string, Exception?> LogAutomaticRetryAttemptSucceededMessage =
        LoggerMessage.Define<string, string>(
            LogLevel.Debug,
            new EventId(6242, nameof(LogAutomaticRetryAttemptSucceeded)),
            "Managed-connector automatic retry evaluated runtime '{ExecutionRuntimeId}' for operation '{OperationId}'.");
    private static readonly Action<ILogger, string, string, Exception?> LogAutomaticRetryAttemptFailedMessage =
        LoggerMessage.Define<string, string>(
            LogLevel.Warning,
            new EventId(6243, nameof(LogAutomaticRetryAttemptFailed)),
            "Managed-connector automatic retry failed while evaluating runtime '{ExecutionRuntimeId}' for operation '{OperationId}'.");

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!options.EnableManagedConnectorAutomaticRetryExecution)
        {
            return;
        }

        LogAutomaticRetryLoopStarted(logger, Math.Max(1, options.ManagedConnectorAutomaticRetryPollingIntervalSeconds));
        await base.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (!options.EnableManagedConnectorAutomaticRetryExecution)
        {
            return;
        }

        await base.StopAsync(cancellationToken).ConfigureAwait(false);
        LogAutomaticRetryLoopStopped(logger);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.EnableManagedConnectorAutomaticRetryExecution)
        {
            return;
        }

        await ExecuteEligibleRetriesAsync(stoppingToken).ConfigureAwait(false);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(1, options.ManagedConnectorAutomaticRetryPollingIntervalSeconds)));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                await ExecuteEligibleRetriesAsync(stoppingToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task ExecuteEligibleRetriesAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var runtimeCatalog = scope.ServiceProvider.GetRequiredService<ICdcCaptureExecutionRuntimeCatalog>();
        var commandExecutor = scope.ServiceProvider.GetRequiredService<ManagedConnectorCommandExecutor>();
        var eligibleRuntimes = runtimeCatalog.Runtimes
            .Where(static runtime => runtime.ManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecution.CanExecuteProviderOwnedControlPlaneApplyAndReconcileOnCurrentNode)
            .OrderBy(static runtime => runtime.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var runtime in eligibleRuntimes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var operationId = runtime.ManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecution.OperationId;
            if (string.IsNullOrWhiteSpace(operationId) ||
                string.Equals(
                    operationId,
                    CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionOperationIds.None,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                await commandExecutor.ExecuteAutomaticRetryAsync(runtime.Id, operationId, cancellationToken).ConfigureAwait(false);
                LogAutomaticRetryAttemptSucceeded(logger, runtime.Id, operationId);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                LogAutomaticRetryAttemptFailed(logger, runtime.Id, operationId, exception);
            }
        }
    }

    private static void LogAutomaticRetryLoopStarted(ILogger logger, int pollingIntervalSeconds)
    {
        LogAutomaticRetryLoopStartedMessage(logger, pollingIntervalSeconds, null);
    }

    private static void LogAutomaticRetryLoopStopped(ILogger logger)
    {
        LogAutomaticRetryLoopStoppedMessage(logger, null);
    }

    private static void LogAutomaticRetryAttemptSucceeded(ILogger logger, string executionRuntimeId, string operationId)
    {
        LogAutomaticRetryAttemptSucceededMessage(logger, executionRuntimeId, operationId, null);
    }

    private static void LogAutomaticRetryAttemptFailed(ILogger logger, string executionRuntimeId, string operationId, Exception exception)
    {
        LogAutomaticRetryAttemptFailedMessage(logger, executionRuntimeId, operationId, exception);
    }
}
