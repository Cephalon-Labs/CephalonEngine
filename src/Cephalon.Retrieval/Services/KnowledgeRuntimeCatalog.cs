using Cephalon.Abstractions.Retrieval;
using Cephalon.Retrieval.Configuration;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Cephalon.Retrieval.Services;

internal sealed class KnowledgeRuntimeCatalog(RetrievalOptions options) : IKnowledgeIndexCatalog
{
    private static readonly IReadOnlyDictionary<string, string> EmptyMetadata =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    private readonly Lock gate = new();
    private readonly Dictionary<string, KnowledgeIndexState> states = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IReadOnlyList<KnowledgeDocument>> indexedDocuments = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<KnowledgeIndexState> States
    {
        get
        {
            lock (gate)
            {
                return states.Values
                    .OrderBy(static state => state.CollectionId, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
        }
    }

    public KnowledgeIndexState? GetByCollectionId(string collectionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(collectionId);

        lock (gate)
        {
            return states.GetValueOrDefault(collectionId.Trim());
        }
    }

    public bool TryGet(string collectionId, out KnowledgeIndexState? state)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(collectionId);

        lock (gate)
        {
            return states.TryGetValue(collectionId.Trim(), out state);
        }
    }

    internal IReadOnlyList<KnowledgeDocument> GetIndexedDocuments(string collectionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(collectionId);

        lock (gate)
        {
            return indexedDocuments.TryGetValue(collectionId.Trim(), out var documents)
                ? documents
                : [];
        }
    }

    internal void RecordStarted(KnowledgeIndexingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        lock (gate)
        {
            var current = GetOrCreateStateUnsafe(request.CollectionId);
            states[request.CollectionId] = current with
            {
                LastRunId = request.RunId,
                LastOutcome = KnowledgeIndexingOutcomes.Started,
                LastObservedAtUtc = DateTimeOffset.UtcNow,
                FreshnessState = ResolveFreshnessState(KnowledgeIndexingOutcomes.Started, current.LastIndexedAtUtc),
                StartedCount = current.StartedCount + 1,
                LastActorId = request.ActorId,
                LastCorrelationId = request.CorrelationId,
                LastError = null,
                Metadata = CreateRequestMetadata(request)
            };
        }
    }

    internal KnowledgeIndexingResult RecordSucceeded(
        KnowledgeIndexingRequest request,
        IReadOnlyList<KnowledgeDocument> documents,
        int providerCount)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(documents);

        var observedAtUtc = DateTimeOffset.UtcNow;
        var sourceFreshnessUtc = documents
            .Select(static document => document.LastModifiedAtUtc)
            .Where(static value => value is not null)
            .DefaultIfEmpty()
            .Max();
        var metadata = CreateResultMetadata(request, providerCount, documents.Count);

        lock (gate)
        {
            indexedDocuments[request.CollectionId] = documents
                .OrderBy(static document => document.Id, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var current = GetOrCreateStateUnsafe(request.CollectionId);
            states[request.CollectionId] = current with
            {
                LastRunId = request.RunId,
                LastOutcome = KnowledgeIndexingOutcomes.Succeeded,
                LastObservedAtUtc = observedAtUtc,
                LastIndexedAtUtc = observedAtUtc,
                SourceFreshnessUtc = sourceFreshnessUtc,
                DocumentCount = documents.Count,
                FreshnessState = ResolveFreshnessState(KnowledgeIndexingOutcomes.Succeeded, observedAtUtc),
                SucceededCount = current.SucceededCount + 1,
                LastActorId = request.ActorId,
                LastCorrelationId = request.CorrelationId,
                LastError = null,
                Metadata = metadata
            };
        }

        return new KnowledgeIndexingResult(
            request.CollectionId,
            request.RunId,
            KnowledgeIndexingOutcomes.Succeeded,
            observedAtUtc,
            observedAtUtc,
            sourceFreshnessUtc,
            documents.Count,
            Error: null,
            metadata);
    }

    internal KnowledgeIndexingResult RecordFailed(
        KnowledgeIndexingRequest request,
        string error,
        int providerCount)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(error);

        var observedAtUtc = DateTimeOffset.UtcNow;
        var metadata = CreateResultMetadata(request, providerCount, documentCount: 0);

        lock (gate)
        {
            var current = GetOrCreateStateUnsafe(request.CollectionId);
            states[request.CollectionId] = current with
            {
                LastRunId = request.RunId,
                LastOutcome = KnowledgeIndexingOutcomes.Failed,
                LastObservedAtUtc = observedAtUtc,
                FreshnessState = KnowledgeIndexFreshnessStates.Failed,
                FailedCount = current.FailedCount + 1,
                LastActorId = request.ActorId,
                LastCorrelationId = request.CorrelationId,
                LastError = error.Trim(),
                Metadata = metadata
            };
        }

        return new KnowledgeIndexingResult(
            request.CollectionId,
            request.RunId,
            KnowledgeIndexingOutcomes.Failed,
            observedAtUtc,
            IndexedAtUtc: null,
            SourceFreshnessUtc: null,
            DocumentCount: 0,
            error.Trim(),
            metadata);
    }

    internal KnowledgeIndexingResult RecordSkipped(
        KnowledgeIndexingRequest request,
        string reason,
        int providerCount)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        var observedAtUtc = DateTimeOffset.UtcNow;
        var metadata = CreateResultMetadata(request, providerCount, documentCount: 0);

        lock (gate)
        {
            var current = GetOrCreateStateUnsafe(request.CollectionId);
            states[request.CollectionId] = current with
            {
                LastRunId = request.RunId,
                LastOutcome = KnowledgeIndexingOutcomes.Skipped,
                LastObservedAtUtc = observedAtUtc,
                FreshnessState = KnowledgeIndexFreshnessStates.Skipped,
                SkippedCount = current.SkippedCount + 1,
                LastActorId = request.ActorId,
                LastCorrelationId = request.CorrelationId,
                LastError = reason.Trim(),
                Metadata = metadata
            };
        }

        return new KnowledgeIndexingResult(
            request.CollectionId,
            request.RunId,
            KnowledgeIndexingOutcomes.Skipped,
            observedAtUtc,
            IndexedAtUtc: null,
            SourceFreshnessUtc: null,
            DocumentCount: 0,
            reason.Trim(),
            metadata);
    }

    internal void RecordQuery(
        KnowledgeQueryRequest request,
        int matchedCount,
        DateTimeOffset queriedAtUtc,
        IReadOnlyDictionary<string, string> metadata)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(metadata);

        lock (gate)
        {
            var current = GetOrCreateStateUnsafe(request.CollectionId);
            states[request.CollectionId] = current with
            {
                QueryCount = current.QueryCount + 1,
                LastQueriedAtUtc = queriedAtUtc,
                LastQueryFingerprint = CreateQueryFingerprint(request.QueryText),
                LastQueryLength = request.QueryText.Length,
                LastQueryMatchedCount = matchedCount,
                LastActorId = request.ActorId,
                LastCorrelationId = request.CorrelationId,
                Metadata = metadata
            };
        }
    }

    internal string ResolveFreshnessState(KnowledgeIndexState? state)
    {
        if (state is null)
        {
            return KnowledgeIndexFreshnessStates.NotIndexed;
        }

        return ResolveFreshnessState(state.LastOutcome, state.LastIndexedAtUtc);
    }

    private KnowledgeIndexState GetOrCreateStateUnsafe(string collectionId)
    {
        return states.TryGetValue(collectionId, out var current)
            ? current
            : new KnowledgeIndexState(
                CollectionId: collectionId,
                LastRunId: null,
                LastOutcome: null,
                LastObservedAtUtc: null,
                LastIndexedAtUtc: null,
                SourceFreshnessUtc: null,
                DocumentCount: 0,
                FreshnessState: KnowledgeIndexFreshnessStates.NotIndexed,
                StartedCount: 0,
                SucceededCount: 0,
                FailedCount: 0,
                SkippedCount: 0,
                QueryCount: 0,
                LastQueriedAtUtc: null,
                LastQueryFingerprint: null,
                LastQueryLength: 0,
                LastQueryMatchedCount: 0,
                LastActorId: null,
                LastCorrelationId: null,
                LastError: null,
                Metadata: EmptyMetadata);
    }

    private string ResolveFreshnessState(string? outcome, DateTimeOffset? lastIndexedAtUtc)
    {
        return outcome switch
        {
            KnowledgeIndexingOutcomes.Failed => KnowledgeIndexFreshnessStates.Failed,
            KnowledgeIndexingOutcomes.Skipped => KnowledgeIndexFreshnessStates.Skipped,
            KnowledgeIndexingOutcomes.Succeeded when lastIndexedAtUtc is { } indexedAtUtc =>
                DateTimeOffset.UtcNow - indexedAtUtc > ResolveFreshnessWindow()
                    ? KnowledgeIndexFreshnessStates.Stale
                    : KnowledgeIndexFreshnessStates.Fresh,
            KnowledgeIndexingOutcomes.Started when lastIndexedAtUtc is not null => KnowledgeIndexFreshnessStates.Fresh,
            _ => KnowledgeIndexFreshnessStates.NotIndexed
        };
    }

    private TimeSpan ResolveFreshnessWindow()
    {
        return options.FreshnessStaleAfterSeconds <= 0
            ? TimeSpan.MaxValue
            : TimeSpan.FromSeconds(options.FreshnessStaleAfterSeconds);
    }

    private static IReadOnlyDictionary<string, string> CreateRequestMetadata(KnowledgeIndexingRequest request)
    {
        return request.Metadata.Count == 0
            ? EmptyMetadata
            : new Dictionary<string, string>(request.Metadata, StringComparer.OrdinalIgnoreCase);
    }

    private static Dictionary<string, string> CreateResultMetadata(
        KnowledgeIndexingRequest request,
        int providerCount,
        int documentCount)
    {
        var metadata = new Dictionary<string, string>(request.Metadata, StringComparer.OrdinalIgnoreCase)
        {
            ["providerCount"] = providerCount.ToString(CultureInfo.InvariantCulture),
            ["documentCount"] = documentCount.ToString(CultureInfo.InvariantCulture)
        };

        return metadata;
    }

    private static string CreateQueryFingerprint(string queryText)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(queryText.Trim()));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
