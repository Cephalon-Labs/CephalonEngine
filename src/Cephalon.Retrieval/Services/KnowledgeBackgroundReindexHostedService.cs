using Cephalon.Abstractions.Retrieval;
using Cephalon.Retrieval.Configuration;
using Microsoft.Extensions.Hosting;
using System.Globalization;

namespace Cephalon.Retrieval.Services;

internal sealed class KnowledgeBackgroundReindexHostedService(
    RetrievalOptions options,
    IKnowledgeCatalog catalog,
    IKnowledgeIndexer indexer) : BackgroundService
{
    private const string ActorId = "cephalon-retrieval-background-scheduler";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var initialDelay = ResolveInitialDelay();
        if (initialDelay > TimeSpan.Zero)
        {
            await Task.Delay(initialDelay, stoppingToken).ConfigureAwait(false);
        }

        if (options.RunBackgroundReindexOnStartup)
        {
            await RunScheduledReindexAsync(stoppingToken).ConfigureAwait(false);
        }

        var interval = ResolveInterval();
        if (interval is null)
        {
            return;
        }

        using var timer = new PeriodicTimer(interval.Value);
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            await RunScheduledReindexAsync(stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task RunScheduledReindexAsync(CancellationToken cancellationToken)
    {
        var collections = ResolveScheduledCollectionIds();
        if (collections.Length == 0)
        {
            return;
        }

        var iterationId = CreateIterationId();
        foreach (var collectionId in collections)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var runId = $"retrieval-background-reindex-{collectionId}-{iterationId}";
            var request = new KnowledgeIndexingRequest(
                collectionId,
                runId,
                actorId: ActorId,
                correlationId: $"retrieval-background-reindex-{iterationId}",
                metadata: CreateSchedulerMetadata(iterationId));

            try
            {
                await indexer.IndexAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (InvalidOperationException) when (!cancellationToken.IsCancellationRequested)
            {
                // The indexer already records failed/skipped posture. Continue so one collection cannot starve the rest.
            }
        }
    }

    private string[] ResolveScheduledCollectionIds()
    {
        var configured = BackgroundReindexingOptions.ResolveConfiguredCollectionIds(options);

        var collections = catalog.Collections
            .Select(static collection => collection.Id)
            .OrderBy(static id => id, StringComparer.OrdinalIgnoreCase);

        if (configured.Length == 0)
        {
            return collections.ToArray();
        }

        var configuredSet = new HashSet<string>(configured, StringComparer.OrdinalIgnoreCase);
        return collections
            .Where(configuredSet.Contains)
            .ToArray();
    }

    private Dictionary<string, string> CreateSchedulerMetadata(string iterationId)
    {
        var configured = BackgroundReindexingOptions.ResolveConfiguredCollectionIds(options);
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["trigger"] = "retrieval-background-scheduler",
            ["scheduler"] = "cephalon-retrieval-background-reindex",
            ["schedulerIterationId"] = iterationId,
            ["collectionScope"] = BackgroundReindexingOptions.ResolveCollectionScope(configured),
            ["runOnStartup"] = options.RunBackgroundReindexOnStartup.ToString().ToLowerInvariant(),
            ["initialDelaySeconds"] = Math.Max(0, options.BackgroundReindexInitialDelaySeconds).ToString(CultureInfo.InvariantCulture),
            ["intervalSeconds"] = Math.Max(0, options.BackgroundReindexIntervalSeconds).ToString(CultureInfo.InvariantCulture)
        };
    }

    private TimeSpan ResolveInitialDelay()
    {
        return options.BackgroundReindexInitialDelaySeconds <= 0
            ? TimeSpan.Zero
            : TimeSpan.FromSeconds(options.BackgroundReindexInitialDelaySeconds);
    }

    private TimeSpan? ResolveInterval()
    {
        return options.BackgroundReindexIntervalSeconds <= 0
            ? null
            : TimeSpan.FromSeconds(options.BackgroundReindexIntervalSeconds);
    }

    private static string CreateIterationId()
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}");
    }
}
