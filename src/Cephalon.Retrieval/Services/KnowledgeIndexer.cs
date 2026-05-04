using Cephalon.Abstractions.Retrieval;
using Cephalon.Diagnostics.Redaction;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Cephalon.Retrieval.Services;

internal sealed class KnowledgeIndexer(
    IKnowledgeCatalog catalog,
    IEnumerable<IKnowledgeDocumentProvider> providers,
    KnowledgeRuntimeCatalog runtimeCatalog,
    RedactionPipeline? redactionPipeline = null) : IKnowledgeIndexer
{
    private readonly IKnowledgeDocumentProvider[] providers = providers.ToArray();

    public async ValueTask<KnowledgeIndexingResult> IndexAsync(
        KnowledgeIndexingRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        using var indexActivity = RetrievalDiagnostics.ActivitySource.StartActivity(
            RetrievalDiagnostics.KnowledgeIndexActivityName,
            ActivityKind.Internal);
        SetTag(indexActivity, RetrievalDiagnostics.IndexerIdTag, InProcessRetrievalRuntimeIds.IndexerId);
        SetTag(indexActivity, RetrievalDiagnostics.CollectionIdTag, request.CollectionId);
        SetTag(indexActivity, RetrievalDiagnostics.RunIdTag, request.RunId);
        if (!string.IsNullOrEmpty(request.ActorId))
        {
            SetTag(indexActivity, RetrievalDiagnostics.ActorIdTag, request.ActorId);
        }
        if (!string.IsNullOrEmpty(request.CorrelationId))
        {
            SetTag(indexActivity, RetrievalDiagnostics.CorrelationIdTag, request.CorrelationId);
        }

        if (!catalog.TryGet(request.CollectionId, out var collection))
        {
            var missingError =
                $"Knowledge collection '{request.CollectionId}' is not registered in the active retrieval runtime.";
            CompleteIndexActivity(
                indexActivity,
                request.CollectionId,
                KnowledgeIndexingOutcomes.Failed,
                documentCount: 0,
                providerCount: 0,
                error: missingError);
            throw new InvalidOperationException(missingError);
        }

        var matchingProviders = providers
            .Where(provider => string.Equals(provider.CollectionId, request.CollectionId, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        runtimeCatalog.RecordStarted(request);

        if (matchingProviders.Length == 0)
        {
            var skippedResult = runtimeCatalog.RecordSkipped(
                request,
                $"No knowledge document provider is registered for collection '{request.CollectionId}'.",
                providerCount: 0);
            CompleteIndexActivity(
                indexActivity,
                request.CollectionId,
                skippedResult.Outcome,
                documentCount: skippedResult.DocumentCount,
                providerCount: 0);
            return skippedResult;
        }

        try
        {
            var context = new KnowledgeDocumentProviderContext(
                collection,
                request.RunId,
                request.RequestedAtUtc,
                request.ActorId,
                request.CorrelationId,
                request.Metadata);

            var documents = new List<KnowledgeDocument>();
            foreach (var provider in matchingProviders)
            {
                var loaded = await provider.LoadDocumentsAsync(context, cancellationToken).ConfigureAwait(false);
                if (loaded is null)
                {
                    throw new InvalidOperationException(
                        $"Knowledge document provider for collection '{request.CollectionId}' returned a null document list.");
                }

                documents.AddRange(loaded);
            }

            ValidateDocuments(request.CollectionId, documents);

            var succeeded = runtimeCatalog.RecordSucceeded(request, documents, matchingProviders.Length);
            CompleteIndexActivity(
                indexActivity,
                request.CollectionId,
                succeeded.Outcome,
                documentCount: succeeded.DocumentCount,
                providerCount: matchingProviders.Length);
            return succeeded;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            var failed = runtimeCatalog.RecordFailed(request, exception.Message, matchingProviders.Length);
            CompleteIndexActivity(
                indexActivity,
                request.CollectionId,
                failed.Outcome,
                documentCount: failed.DocumentCount,
                providerCount: matchingProviders.Length,
                error: exception.Message);
            throw new InvalidOperationException(
                $"Knowledge collection '{request.CollectionId}' could not be indexed. {exception.Message}",
                exception);
        }
    }

    /// <summary>
    /// Sets the terminal indexing-outcome tag, optional error status, and increments the
    /// knowledge-index counter for the given indexing activity. Tag values are routed through the
    /// redaction pipeline so consumer-registered redaction filters apply uniformly across the
    /// indexing span.
    /// </summary>
    private void CompleteIndexActivity(
        Activity? indexActivity,
        string collectionId,
        string outcome,
        int documentCount,
        int providerCount,
        string? error = null)
    {
        SetTag(indexActivity, RetrievalDiagnostics.IndexingOutcomeTag, outcome);
        SetTag(indexActivity, RetrievalDiagnostics.DocumentCountTag, documentCount);
        SetTag(indexActivity, RetrievalDiagnostics.ProviderCountTag, providerCount);

        if (string.Equals(outcome, KnowledgeIndexingOutcomes.Failed, StringComparison.OrdinalIgnoreCase))
        {
            indexActivity?.SetStatus(ActivityStatusCode.Error, error);
        }

        RetrievalDiagnostics.KnowledgeIndexCounter.Add(
            1,
            new TagList
            {
                { RetrievalDiagnostics.CollectionIdTag, collectionId },
                { RetrievalDiagnostics.IndexingOutcomeTag, outcome }
            });
    }

    /// <summary>
    /// Sets a tag on <paramref name="activity"/> after routing the value through the consumer
    /// -registered <see cref="RedactionPipeline"/>. The pipeline is empty by default when no
    /// consumer registered any <see cref="IRedactionFilter"/>; in that case (and when DI did not
    /// supply a pipeline at all) this method short-circuits to passthrough so indexing emission
    /// stays cheap.
    /// </summary>
    private void SetTag(Activity? activity, string attributeKey, object? value)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag(attributeKey, Redact(activity, attributeKey, value));
    }

    private object? Redact(Activity? activity, string attributeKey, object? value)
    {
        if (redactionPipeline is null)
        {
            return value;
        }

        var context = new RedactionContext(
            ActivitySourceName: activity?.Source.Name,
            MeterName: null,
            AttributeKey: attributeKey,
            LoggerCategory: null);
        return redactionPipeline.Filter(context, value);
    }

    private static void ValidateDocuments(
        string collectionId,
        List<KnowledgeDocument> documents)
    {
        for (var index = 0; index < documents.Count; index++)
        {
            if (documents[index] is null)
            {
                throw new InvalidOperationException(
                    $"Knowledge document provider for collection '{collectionId}' returned a null document at index {index}.");
            }
        }

        var duplicate = documents
            .GroupBy(static document => document.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(static group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidOperationException(
                $"Knowledge collection '{collectionId}' produced duplicate document id '{duplicate.Key}'.");
        }
    }
}
