using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cephalon.Sample.Showcase.Infrastructure;

/// <summary>
/// Coordinates durable read-model synchronization by staging projection jobs in the write store
/// and opportunistically flushing them to the read store inside the current host instance.
/// </summary>
internal sealed partial class ShowcaseReadModelSyncService(
    ShowcaseWriteDbContext? writeDb,
    ShowcaseReadDbContext? readDb,
    ShowcaseReadModelProjector projector,
    ILogger<ShowcaseReadModelSyncService> logger)
{
    private const int MaxStoredErrorLength = 3500;
    private readonly List<ShowcaseReadProjectionJobEntity> stagedJobs = [];
    private readonly HashSet<string> stagedJobKeys = new(StringComparer.OrdinalIgnoreCase);

    public bool IsEnabled => writeDb is not null && readDb is not null;

    public void EnqueueProducts(IEnumerable<string> productIds)
    {
        Enqueue(ShowcaseProjectionScope.Products, productIds);
    }

    public void EnqueueInventory(IEnumerable<string> productIds)
    {
        Enqueue(ShowcaseProjectionScope.Inventory, productIds);
    }

    public void EnqueueOrders(IEnumerable<string> orderIds)
    {
        Enqueue(ShowcaseProjectionScope.Orders, orderIds);
    }

    public void EnqueueShipments(IEnumerable<string> shipmentIds)
    {
        Enqueue(ShowcaseProjectionScope.Shipments, shipmentIds);
    }

    public void EnqueueAll()
    {
        Enqueue(ShowcaseProjectionScope.All, ["*"]);
    }

    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        if (!IsEnabled || stagedJobs.Count == 0)
        {
            return;
        }

        await ProcessJobsAsync(stagedJobs, "request-flush", cancellationToken).ConfigureAwait(false);
        try
        {
            await writeDb!.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            LogCompletionPersistenceFailure(logger, ex);
        }

        stagedJobs.Clear();
        stagedJobKeys.Clear();
    }

    public async Task<int> ProcessPendingBatchAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        if (!IsEnabled || batchSize <= 0)
        {
            return 0;
        }

        var now = DateTime.UtcNow;
        var pendingJobs = await writeDb!.ReadProjectionJobs
            .Where(job => job.CompletedAtUtc == null && job.AvailableAtUtc <= now)
            .OrderBy(job => job.CreatedAtUtc)
            .ThenBy(job => job.Id)
            .Take(batchSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (pendingJobs.Count == 0)
        {
            return 0;
        }

        await ProcessJobsAsync(pendingJobs, "background-worker", cancellationToken).ConfigureAwait(false);
        await writeDb.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return pendingJobs.Count(job => job.CompletedAtUtc is not null);
    }

    public async Task RebuildAndAcknowledgePendingAsync(CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            return;
        }

        await projector.ProjectAllAsync(cancellationToken).ConfigureAwait(false);

        var pendingJobs = await writeDb!.ReadProjectionJobs
            .Where(job => job.CompletedAtUtc == null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (pendingJobs.Count == 0)
        {
            return;
        }

        var completedAtUtc = DateTime.UtcNow;
        foreach (var job in pendingJobs)
        {
            MarkCompleted(job, completedAtUtc);
        }

        await writeDb.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private void Enqueue(
        ShowcaseProjectionScope scope,
        IEnumerable<string> entityKeys)
    {
        ArgumentNullException.ThrowIfNull(entityKeys);

        if (!IsEnabled || scope == ShowcaseProjectionScope.None)
        {
            return;
        }

        var normalizedKeys = scope == ShowcaseProjectionScope.All
            ? ["*"]
            : NormalizeKeys(entityKeys);

        if (normalizedKeys.Count == 0)
        {
            return;
        }

        var createdAtUtc = DateTime.UtcNow;
        foreach (var entityKey in normalizedKeys)
        {
            var stagingKey = $"{ToStorageScope(scope)}::{entityKey}";
            if (!stagedJobKeys.Add(stagingKey))
            {
                continue;
            }

            var job = new ShowcaseReadProjectionJobEntity
            {
                Scope = ToStorageScope(scope),
                EntityKey = entityKey,
                CreatedAtUtc = createdAtUtc,
                AvailableAtUtc = createdAtUtc
            };

            stagedJobs.Add(job);
            writeDb!.ReadProjectionJobs.Add(job);
        }
    }

    private async Task ProcessJobsAsync(
        IReadOnlyCollection<ShowcaseReadProjectionJobEntity> jobs,
        string executionMode,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(jobs);

        foreach (var group in jobs.GroupBy(job => ParseScope(job.Scope)))
        {
            var scope = group.Key;
            var groupedJobs = group.ToArray();

            try
            {
                if (scope == ShowcaseProjectionScope.None)
                {
                    throw new InvalidOperationException(
                        $"Unsupported showcase read-projection scope '{groupedJobs[0].Scope}'.");
                }

                await ApplyScopeAsync(scope, groupedJobs, cancellationToken).ConfigureAwait(false);

                var completedAtUtc = DateTime.UtcNow;
                foreach (var job in groupedJobs)
                {
                    MarkCompleted(job, completedAtUtc);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                LogScopeRetryScheduled(logger, scope, executionMode, groupedJobs.Length, ex);

                var attemptedAtUtc = DateTime.UtcNow;
                foreach (var job in groupedJobs)
                {
                    MarkFailed(job, attemptedAtUtc, ex);
                }
            }
        }
    }

    private Task ApplyScopeAsync(
        ShowcaseProjectionScope scope,
        IReadOnlyCollection<ShowcaseReadProjectionJobEntity> jobs,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(jobs);

        return scope switch
        {
            ShowcaseProjectionScope.Products => projector.ProjectProductsAsync(GetDistinctKeys(jobs), cancellationToken),
            ShowcaseProjectionScope.Inventory => projector.ProjectInventoryAsync(GetDistinctKeys(jobs), cancellationToken),
            ShowcaseProjectionScope.Orders => projector.ProjectOrdersAsync(GetDistinctKeys(jobs), cancellationToken),
            ShowcaseProjectionScope.Shipments => projector.ProjectShipmentsAsync(GetDistinctKeys(jobs), cancellationToken),
            ShowcaseProjectionScope.All => projector.ProjectAllAsync(cancellationToken),
            _ => throw new InvalidOperationException($"Unsupported showcase read-projection scope '{scope}'.")
        };
    }

    private static List<string> GetDistinctKeys(IReadOnlyCollection<ShowcaseReadProjectionJobEntity> jobs)
    {
        ArgumentNullException.ThrowIfNull(jobs);

        return jobs
            .Select(job => job.EntityKey)
            .Where(static key => !string.IsNullOrWhiteSpace(key) && !string.Equals(key, "*", StringComparison.Ordinal))
            .Select(static key => key.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<string> NormalizeKeys(IEnumerable<string> keys)
    {
        ArgumentNullException.ThrowIfNull(keys);

        return keys
            .Where(static key => !string.IsNullOrWhiteSpace(key))
            .Select(static key => key.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static ShowcaseProjectionScope ParseScope(string? scope)
    {
        if (string.IsNullOrWhiteSpace(scope))
        {
            return ShowcaseProjectionScope.None;
        }

        return scope.Trim().ToLowerInvariant() switch
        {
            "products" => ShowcaseProjectionScope.Products,
            "inventory" => ShowcaseProjectionScope.Inventory,
            "orders" => ShowcaseProjectionScope.Orders,
            "shipments" => ShowcaseProjectionScope.Shipments,
            "all" => ShowcaseProjectionScope.All,
            _ => ShowcaseProjectionScope.None
        };
    }

    private static string ToStorageScope(ShowcaseProjectionScope scope)
    {
        return scope switch
        {
            ShowcaseProjectionScope.Products => "products",
            ShowcaseProjectionScope.Inventory => "inventory",
            ShowcaseProjectionScope.Orders => "orders",
            ShowcaseProjectionScope.Shipments => "shipments",
            ShowcaseProjectionScope.All => "all",
            _ => throw new InvalidOperationException($"Unsupported showcase read-projection scope '{scope}'.")
        };
    }

    private static void MarkCompleted(
        ShowcaseReadProjectionJobEntity job,
        DateTime completedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(job);

        job.AttemptCount++;
        job.LastAttemptAtUtc = completedAtUtc;
        job.AvailableAtUtc = completedAtUtc;
        job.CompletedAtUtc = completedAtUtc;
        job.LastError = null;
    }

    private static void MarkFailed(
        ShowcaseReadProjectionJobEntity job,
        DateTime attemptedAtUtc,
        Exception exception)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(exception);

        job.AttemptCount++;
        job.LastAttemptAtUtc = attemptedAtUtc;
        job.AvailableAtUtc = attemptedAtUtc.Add(ComputeRetryDelay(job.AttemptCount));
        job.LastError = TrimError(exception);
    }

    private static TimeSpan ComputeRetryDelay(int attemptCount)
    {
        var seconds = Math.Min(60, Math.Max(5, attemptCount * attemptCount * 2));
        return TimeSpan.FromSeconds(seconds);
    }

    private static string TrimError(Exception exception)
    {
        var text = exception.ToString();
        return text.Length <= MaxStoredErrorLength
            ? text
            : text[..MaxStoredErrorLength];
    }

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Warning,
        Message = "Showcase read-model sync could not persist job completion metadata after the write-side request completed. Pending jobs will be retried by the background worker.")]
    private static partial void LogCompletionPersistenceFailure(
        ILogger logger,
        Exception exception);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Warning,
        Message = "Showcase read-model sync failed while processing scope {Scope} in {ExecutionMode}. {JobCount} job(s) will be retried.")]
    private static partial void LogScopeRetryScheduled(
        ILogger logger,
        ShowcaseProjectionScope scope,
        string executionMode,
        int jobCount,
        Exception exception);
}
