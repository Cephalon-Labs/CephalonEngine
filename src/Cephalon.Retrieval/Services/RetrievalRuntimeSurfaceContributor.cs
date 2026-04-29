using Cephalon.Abstractions.Retrieval;
using Cephalon.Abstractions.Technologies;
using Cephalon.Retrieval.Configuration;
using System.Globalization;

namespace Cephalon.Retrieval.Services;

internal sealed class RetrievalRuntimeSurfaceContributor(
    IKnowledgeCatalog catalog,
    RetrievalOptions options,
    IEnumerable<IKnowledgeDocumentProvider> providers,
    IKnowledgeIndexCatalog indexCatalog) : ITechnologyRuntimeContributor
{
    private readonly IKnowledgeDocumentProvider[] providers = providers.ToArray();

    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        var configuredBackgroundCollectionIds = BackgroundReindexingOptions.ResolveConfiguredCollectionIds(options);
        var configuredBackgroundCollectionSet = configuredBackgroundCollectionIds.Length == 0
            ? null
            : new HashSet<string>(configuredBackgroundCollectionIds, StringComparer.OrdinalIgnoreCase);
        var providerIndex = providers
            .GroupBy(static provider => provider.CollectionId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => group.Count(),
                StringComparer.OrdinalIgnoreCase);

        return new TechnologyRuntimeSurface(
            technologyId: "knowledge-retrieval",
            surfaceId: "knowledge-collections",
            displayName: "Knowledge Collections",
            description: "Registered knowledge collections, managed indexing readiness, query execution posture, and freshness state available to the active retrieval runtime.",
            entries: catalog.Collections
                .Select(collection => CreateEntry(
                    collection,
                    providerIndex,
                    configuredBackgroundCollectionIds,
                    configuredBackgroundCollectionSet))
                .ToArray());
    }

    private TechnologyRuntimeEntry CreateEntry(
        KnowledgeCollectionDescriptor collection,
        IReadOnlyDictionary<string, int> providerIndex,
        string[] configuredBackgroundCollectionIds,
        IReadOnlySet<string>? configuredBackgroundCollectionSet)
    {
        var providerCount = providerIndex.GetValueOrDefault(collection.Id);
        var backgroundReindexingScheduled = IsBackgroundReindexingScheduled(collection.Id, configuredBackgroundCollectionSet);
        indexCatalog.TryGet(collection.Id, out var state);
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["tags"] = string.Join(",", collection.Tags),
            ["indexingEnabled"] = options.EnableIngestion.ToString().ToLowerInvariant(),
            ["queryingEnabled"] = options.EnableQuerying.ToString().ToLowerInvariant(),
            ["backgroundReindexingEnabled"] = (options.EnableIngestion && options.EnableBackgroundReindexing).ToString().ToLowerInvariant(),
            ["backgroundReindexingScheduled"] = backgroundReindexingScheduled.ToString().ToLowerInvariant(),
            ["backgroundReindexingOwnership"] = ResolveBackgroundReindexingOwnership(backgroundReindexingScheduled),
            ["backgroundReindexingCollectionScope"] = BackgroundReindexingOptions.ResolveCollectionScope(configuredBackgroundCollectionIds),
            ["backgroundReindexingConfiguredCollectionCount"] = configuredBackgroundCollectionIds.Length.ToString(CultureInfo.InvariantCulture),
            ["backgroundReindexingRunOnStartup"] = options.RunBackgroundReindexOnStartup.ToString().ToLowerInvariant(),
            ["backgroundReindexingInitialDelaySeconds"] = Math.Max(0, options.BackgroundReindexInitialDelaySeconds).ToString(CultureInfo.InvariantCulture),
            ["backgroundReindexingIntervalSeconds"] = Math.Max(0, options.BackgroundReindexIntervalSeconds).ToString(CultureInfo.InvariantCulture),
            ["providerConfigured"] = (providerCount > 0).ToString().ToLowerInvariant(),
            ["providerCount"] = providerCount.ToString(CultureInfo.InvariantCulture),
            ["indexingOwnership"] = ResolveIndexingOwnership(providerCount),
            ["queryOwnership"] = ResolveQueryOwnership(providerCount, state),
            ["runtimeState"] = ResolveRuntimeState(state),
            ["freshnessState"] = ResolveFreshnessState(state),
            ["documentCount"] = (state?.DocumentCount ?? 0).ToString(CultureInfo.InvariantCulture),
            ["queryCount"] = (state?.QueryCount ?? 0).ToString(CultureInfo.InvariantCulture)
        };

        if (state is not null)
        {
            metadata["lastOutcome"] = state.LastOutcome ?? "unknown";
            metadata["lastRunId"] = state.LastRunId ?? string.Empty;
            metadata["totalIndexRuns"] = state.TotalIndexRuns.ToString(CultureInfo.InvariantCulture);
            metadata["startedCount"] = state.StartedCount.ToString(CultureInfo.InvariantCulture);
            metadata["succeededCount"] = state.SucceededCount.ToString(CultureInfo.InvariantCulture);
            metadata["failedCount"] = state.FailedCount.ToString(CultureInfo.InvariantCulture);
            metadata["skippedCount"] = state.SkippedCount.ToString(CultureInfo.InvariantCulture);
            metadata["lastIndexedAtUtc"] = state.LastIndexedAtUtc?.ToString("O") ?? string.Empty;
            metadata["lastObservedAtUtc"] = state.LastObservedAtUtc?.ToString("O") ?? string.Empty;
            metadata["sourceFreshnessUtc"] = state.SourceFreshnessUtc?.ToString("O") ?? string.Empty;
            metadata["lastQueryMatchedCount"] = state.LastQueryMatchedCount.ToString(CultureInfo.InvariantCulture);
            metadata["lastQueryLength"] = state.LastQueryLength.ToString(CultureInfo.InvariantCulture);
            metadata["lastQueriedAtUtc"] = state.LastQueriedAtUtc?.ToString("O") ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(state.LastQueryFingerprint))
            {
                metadata["lastQueryFingerprint"] = state.LastQueryFingerprint;
            }

            if (!string.IsNullOrWhiteSpace(state.LastActorId))
            {
                metadata["lastActorId"] = state.LastActorId;
            }

            if (!string.IsNullOrWhiteSpace(state.LastCorrelationId))
            {
                metadata["lastCorrelationId"] = state.LastCorrelationId;
            }

            if (!string.IsNullOrWhiteSpace(state.LastError))
            {
                metadata["lastError"] = state.LastError;
            }

            if (state.Metadata.Count > 0)
            {
                metadata["reportedMetadataKeys"] = string.Join(
                    ",",
                    state.Metadata.Keys.OrderBy(static key => key, StringComparer.OrdinalIgnoreCase));

                foreach (var pair in state.Metadata.OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrWhiteSpace(pair.Key))
                    {
                        metadata[$"reported.{pair.Key.Trim()}"] = pair.Value;
                    }
                }
            }
        }

        return new TechnologyRuntimeEntry(
            id: collection.Id,
            displayName: collection.DisplayName,
            description: collection.Description,
            metadata: metadata);
    }

    private bool IsBackgroundReindexingScheduled(
        string collectionId,
        IReadOnlySet<string>? configuredBackgroundCollectionSet)
    {
        if (!options.EnableIngestion || !options.EnableBackgroundReindexing)
        {
            return false;
        }

        return configuredBackgroundCollectionSet is null || configuredBackgroundCollectionSet.Contains(collectionId);
    }

    private string ResolveBackgroundReindexingOwnership(bool backgroundReindexingScheduled)
    {
        if (!options.EnableIngestion || !options.EnableBackgroundReindexing)
        {
            return "not-configured";
        }

        return backgroundReindexingScheduled ? "cephalon-managed" : "not-selected";
    }

    private string ResolveIndexingOwnership(int providerCount)
    {
        if (!options.EnableIngestion)
        {
            return "not-configured";
        }

        return providerCount > 0 ? "cephalon-managed" : "awaiting-provider";
    }

    private string ResolveQueryOwnership(
        int providerCount,
        KnowledgeIndexState? state)
    {
        if (!options.EnableQuerying)
        {
            return "not-configured";
        }

        if (state?.SucceededCount > 0)
        {
            return "cephalon-managed";
        }

        return providerCount > 0 ? "awaiting-index" : "awaiting-provider";
    }

    private static string ResolveRuntimeState(KnowledgeIndexState? state)
    {
        return state?.LastOutcome switch
        {
            KnowledgeIndexingOutcomes.Succeeded => "indexed",
            KnowledgeIndexingOutcomes.Failed => "failed",
            KnowledgeIndexingOutcomes.Skipped => "skipped",
            KnowledgeIndexingOutcomes.Started => "indexing",
            _ => "not-indexed"
        };
    }

    private string ResolveFreshnessState(KnowledgeIndexState? state)
    {
        if (state is null)
        {
            return KnowledgeIndexFreshnessStates.NotIndexed;
        }

        return state.LastOutcome switch
        {
            KnowledgeIndexingOutcomes.Failed => KnowledgeIndexFreshnessStates.Failed,
            KnowledgeIndexingOutcomes.Skipped => KnowledgeIndexFreshnessStates.Skipped,
            KnowledgeIndexingOutcomes.Succeeded when state.LastIndexedAtUtc is { } indexedAtUtc =>
                DateTimeOffset.UtcNow - indexedAtUtc > ResolveFreshnessWindow()
                    ? KnowledgeIndexFreshnessStates.Stale
                    : KnowledgeIndexFreshnessStates.Fresh,
            _ => state.FreshnessState
        };
    }

    private TimeSpan ResolveFreshnessWindow()
    {
        return options.FreshnessStaleAfterSeconds <= 0
            ? TimeSpan.MaxValue
            : TimeSpan.FromSeconds(options.FreshnessStaleAfterSeconds);
    }
}
