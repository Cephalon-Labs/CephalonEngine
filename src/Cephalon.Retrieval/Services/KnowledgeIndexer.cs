namespace Cephalon.Retrieval.Services;

internal sealed class KnowledgeIndexer(
    IKnowledgeCatalog catalog,
    IEnumerable<IKnowledgeDocumentProvider> providers,
    KnowledgeRuntimeCatalog runtimeCatalog) : IKnowledgeIndexer
{
    private readonly IKnowledgeDocumentProvider[] providers = providers.ToArray();

    public async ValueTask<KnowledgeIndexingResult> IndexAsync(
        KnowledgeIndexingRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        if (!catalog.TryGet(request.CollectionId, out var collection))
        {
            throw new InvalidOperationException(
                $"Knowledge collection '{request.CollectionId}' is not registered in the active retrieval runtime.");
        }

        var matchingProviders = providers
            .Where(provider => string.Equals(provider.CollectionId, request.CollectionId, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        runtimeCatalog.RecordStarted(request);

        if (matchingProviders.Length == 0)
        {
            return runtimeCatalog.RecordSkipped(
                request,
                $"No knowledge document provider is registered for collection '{request.CollectionId}'.",
                providerCount: 0);
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

            return runtimeCatalog.RecordSucceeded(request, documents, matchingProviders.Length);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            var result = runtimeCatalog.RecordFailed(request, exception.Message, matchingProviders.Length);
            throw new InvalidOperationException(
                $"Knowledge collection '{request.CollectionId}' could not be indexed. {exception.Message}",
                exception);
        }
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
