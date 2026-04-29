using Cephalon.MultiTenancy.Governance.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cephalon.MultiTenancy.Governance.Services;

internal sealed class TenantInvitationDeliveryRetryHostedService(
    MultiTenancyGovernanceOptions options,
    ITenantInvitationDeliveryRetryRunner retryRunner,
    TenantInvitationDeliveryRetryRuntimeReporter runtimeReporter,
    TimeProvider timeProvider,
    ILogger<TenantInvitationDeliveryRetryHostedService> logger) : BackgroundService
{
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!TenantInvitationDeliveryRetryConfiguration.IsBackgroundSchedulingEnabled(options))
        {
            return;
        }

        MultiTenancyGovernanceLoggerMessages.TenantInvitationDeliveryRetryBackgroundSchedulingStarted(
            logger,
            TenantInvitationDeliveryRetryConfiguration.ResolveBackgroundSchedulingIntervalSeconds(options),
            TenantInvitationDeliveryRetryQueueStores.ResolveMaxItems(options),
            null);
        await base.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (!TenantInvitationDeliveryRetryConfiguration.IsBackgroundSchedulingEnabled(options))
        {
            return;
        }

        await base.StopAsync(cancellationToken).ConfigureAwait(false);
        MultiTenancyGovernanceLoggerMessages.TenantInvitationDeliveryRetryBackgroundSchedulingStopped(logger, null);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!TenantInvitationDeliveryRetryConfiguration.IsBackgroundSchedulingEnabled(options))
        {
            return;
        }

        if (options.InvitationDeliveryRetryBackgroundRunOnStartup)
        {
            await RetryOnceAsync(stoppingToken).ConfigureAwait(false);
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(
            TenantInvitationDeliveryRetryConfiguration.ResolveBackgroundSchedulingIntervalSeconds(options)));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                await RetryOnceAsync(stoppingToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task RetryOnceAsync(CancellationToken cancellationToken)
    {
        var startedAtUtc = timeProvider.GetUtcNow();
        runtimeReporter.MarkStarted(startedAtUtc);

        try
        {
            var result = await retryRunner.RetryPendingAsync(
                new TenantInvitationDeliveryRetryRequest(
                    atUtc: startedAtUtc,
                    maxItems: TenantInvitationDeliveryRetryQueueStores.ResolveMaxItems(options),
                    dueOnly: true,
                    source: string.IsNullOrWhiteSpace(options.InvitationDeliveryRetryBackgroundSource)
                        ? "background-invitation-delivery-retry"
                        : options.InvitationDeliveryRetryBackgroundSource,
                    metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        [TenantInvitationDeliveryMetadataKeys.DeliveryRetryBackgroundScheduling] = "true",
                        [TenantInvitationDeliveryMetadataKeys.DeliveryRetryBackgroundOwnership] = "cephalon-managed",
                        [TenantInvitationDeliveryMetadataKeys.DeliveryRetryQueueOwnership] = "cephalon-managed"
                    }),
                cancellationToken).ConfigureAwait(false);

            runtimeReporter.MarkCompleted(result, timeProvider.GetUtcNow());
            MultiTenancyGovernanceLoggerMessages.TenantInvitationDeliveryRetryBackgroundSchedulingCompleted(
                logger,
                result.Outcome,
                result.AttemptedCount,
                result.DispatchedCount,
                result.FailedCount,
                result.ExhaustedCount,
                result.TerminalCount,
                null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            runtimeReporter.MarkFailed(exception, timeProvider.GetUtcNow());
            MultiTenancyGovernanceLoggerMessages.TenantInvitationDeliveryRetryBackgroundSchedulingFailed(logger, exception.Message, exception);
        }
    }
}
