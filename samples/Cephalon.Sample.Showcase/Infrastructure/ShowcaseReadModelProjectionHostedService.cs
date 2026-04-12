using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cephalon.Sample.Showcase.Infrastructure;

/// <summary>
/// Reconciles and continuously catches up the separate read-side database from the write-side store.
/// </summary>
internal sealed partial class ShowcaseReadModelProjectionHostedService(
    IServiceProvider serviceProvider,
    ILogger<ShowcaseReadModelProjectionHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    private const int BatchSize = 64;

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using (var startupScope = serviceProvider.CreateScope())
            {
                var sync = startupScope.ServiceProvider.GetRequiredService<ShowcaseReadModelSyncService>();
                if (sync.IsEnabled)
                {
                    await sync.RebuildAndAcknowledgePendingAsync(stoppingToken).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex)
        {
            LogStartupRebuildFailure(logger, ex);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(PollInterval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                using var scope = serviceProvider.CreateScope();
                var sync = scope.ServiceProvider.GetRequiredService<ShowcaseReadModelSyncService>();
                if (!sync.IsEnabled)
                {
                    continue;
                }

                while (!stoppingToken.IsCancellationRequested)
                {
                    var processed = await sync.ProcessPendingBatchAsync(BatchSize, stoppingToken).ConfigureAwait(false);
                    if (processed < BatchSize)
                    {
                        break;
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                LogBackgroundPassFailure(logger, ex);
            }
        }
    }

    [LoggerMessage(
        EventId = 1011,
        Level = LogLevel.Warning,
        Message = "Showcase read-model startup rebuild failed. The background sync loop will continue retrying queued projection jobs.")]
    private static partial void LogStartupRebuildFailure(
        ILogger logger,
        Exception exception);

    [LoggerMessage(
        EventId = 1012,
        Level = LogLevel.Warning,
        Message = "Showcase read-model background sync pass failed.")]
    private static partial void LogBackgroundPassFailure(
        ILogger logger,
        Exception exception);
}
