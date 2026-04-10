using System.Globalization;
using Cephalon.Abstractions.AppModel;
using Cephalon.Audit.EntityFramework.Configuration;
using Cephalon.Engine.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cephalon.Audit.EntityFramework.Services;

internal sealed class EntityFrameworkAuditHistoryRetentionHostedService<TDbContext>(
    IServiceScopeFactory scopeFactory,
    AppProfile appProfile,
    TimeProvider timeProvider,
    ILoggerFactory? loggerFactory = null) : IHostedService
    where TDbContext : DbContext, IEntityFrameworkAuditHistoryContext
{
    private readonly IServiceScopeFactory scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    private readonly AppProfile appProfile = appProfile ?? throw new ArgumentNullException(nameof(appProfile));
    private readonly TimeProvider timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    private readonly ILogger<EntityFrameworkAuditHistoryRetentionHostedService<TDbContext>> logger =
        loggerFactory?.CreateLogger<EntityFrameworkAuditHistoryRetentionHostedService<TDbContext>>()
        ?? NullLogger<EntityFrameworkAuditHistoryRetentionHostedService<TDbContext>>.Instance;
    private CancellationTokenSource? loopCancellation;
    private Task? loopTask;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var retention = ResolveRetentionSelection();
        if (retention is null)
        {
            return;
        }

        if (retention.ApplyOnStartup == true)
        {
            await RunRetentionPassAsync(retention, cancellationToken).ConfigureAwait(false);
        }

        if (retention.RunIntervalMinutes is not > 0)
        {
            return;
        }

        loopCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        loopTask = RunLoopAsync(retention, loopCancellation.Token);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (loopCancellation is null || loopTask is null)
        {
            return;
        }

        loopCancellation.Cancel();

        try
        {
            await loopTask.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            loopCancellation.Dispose();
            loopCancellation = null;
            loopTask = null;
        }
    }

    private async Task RunLoopAsync(
        AuditHistoryRetentionSelection retention,
        CancellationToken cancellationToken)
    {
        var interval = TimeSpan.FromMinutes(retention.RunIntervalMinutes!.Value);
        using var timer = new PeriodicTimer(interval, timeProvider);

        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                await RunRetentionPassAsync(retention, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    private async Task RunRetentionPassAsync(
        AuditHistoryRetentionSelection retention,
        CancellationToken cancellationToken)
    {
        var cutoffUtc = timeProvider.GetUtcNow().AddDays(-retention.MaxAgeDays!.Value);
        var deleteBatchSize = retention.DeleteBatchSize ?? AuditHistoryRetentionSettings.DefaultDeleteBatchSize;
        var totalDeleted = 0;

        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await using var scope = scopeFactory.CreateAsyncScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
                var staleEntries = await dbContext.AuditEntries
                    .Where(entry => entry.OccurredAtUtc < cutoffUtc)
                    .OrderBy(entry => entry.OccurredAtUtc)
                    .ThenBy(entry => entry.PersistedAtUtc)
                    .ThenBy(entry => entry.Id)
                    .Take(deleteBatchSize)
                    .ToArrayAsync(cancellationToken)
                    .ConfigureAwait(false);

                if (staleEntries.Length == 0)
                {
                    break;
                }

                dbContext.AuditEntries.RemoveRange(staleEntries);
                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                dbContext.ChangeTracker.Clear();
                totalDeleted += staleEntries.Length;

                if (staleEntries.Length < deleteBatchSize)
                {
                    break;
                }
            }

            if (totalDeleted > 0)
            {
                RetentionLoggerMessages.RetentionDeleted(
                    logger,
                    totalDeleted,
                    retention.MaxAgeDays.Value,
                    deleteBatchSize,
                    cutoffUtc.ToString("O", CultureInfo.InvariantCulture),
                    null);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            RetentionLoggerMessages.RetentionFailed(
                logger,
                retention.MaxAgeDays.Value,
                deleteBatchSize,
                cutoffUtc.ToString("O", CultureInfo.InvariantCulture),
                exception);
        }
    }

    private AuditHistoryRetentionSelection? ResolveRetentionSelection()
    {
        if (appProfile.Audit.Enabled == false ||
            appProfile.Audit.History.Enabled != true ||
            !EntityFrameworkAuditHistorySelection.MatchesProvider(appProfile.Audit.History.Provider))
        {
            return null;
        }

        return appProfile.Audit.History.Retention.Enabled == true
            ? appProfile.Audit.History.Retention
            : null;
    }
}

internal static class RetentionLoggerMessages
{
    private static readonly Action<ILogger, int, int, int, string, Exception?> RetentionDeletedMessage =
        LoggerMessage.Define<int, int, int, string>(
            LogLevel.Information,
            new EventId(4610, "AuditHistoryRetentionDeleted"),
            "Audit-history retention deleted {DeletedCount} rows older than {MaxAgeDays} days using batch size {DeleteBatchSize}. CutoffUtc {CutoffUtc}.");

    private static readonly Action<ILogger, int, int, string, Exception?> RetentionFailedMessage =
        LoggerMessage.Define<int, int, string>(
            LogLevel.Error,
            new EventId(4611, "AuditHistoryRetentionFailed"),
            "Audit-history retention failed for rows older than {MaxAgeDays} days using batch size {DeleteBatchSize}. CutoffUtc {CutoffUtc}.");

    public static void RetentionDeleted(
        ILogger logger,
        int deletedCount,
        int maxAgeDays,
        int deleteBatchSize,
        string cutoffUtc,
        Exception? exception)
    {
        RetentionDeletedMessage(logger, deletedCount, maxAgeDays, deleteBatchSize, cutoffUtc, exception);
    }

    public static void RetentionFailed(
        ILogger logger,
        int maxAgeDays,
        int deleteBatchSize,
        string cutoffUtc,
        Exception? exception)
    {
        RetentionFailedMessage(logger, maxAgeDays, deleteBatchSize, cutoffUtc, exception);
    }
}
