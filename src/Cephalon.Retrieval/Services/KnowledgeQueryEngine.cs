using Cephalon.Retrieval.Configuration;
using Cephalon.Abstractions.Retrieval;
using System.Globalization;

namespace Cephalon.Retrieval.Services;

internal sealed class KnowledgeQueryEngine(
    IKnowledgeCatalog catalog,
    KnowledgeRuntimeCatalog runtimeCatalog,
    RetrievalOptions options) : IKnowledgeQueryEngine
{
    private static readonly char[] QuerySeparators =
    [
        ' ', '\t', '\r', '\n', '.', ',', ';', ':', '/', '\\', '-', '_', '(', ')', '[', ']', '{', '}', '"', '\''
    ];

    public ValueTask<KnowledgeQueryResult> QueryAsync(
        KnowledgeQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        if (!catalog.TryGet(request.CollectionId, out _))
        {
            throw new InvalidOperationException(
                $"Knowledge collection '{request.CollectionId}' is not registered in the active retrieval runtime.");
        }

        var queriedAtUtc = DateTimeOffset.UtcNow;
        var documents = runtimeCatalog.GetIndexedDocuments(request.CollectionId);
        var limit = ResolveLimit(request.MaxResults);
        var terms = Tokenize(request.QueryText);
        var matches = documents
            .Select(document => CreateMatch(request.CollectionId, document, request.QueryText, terms))
            .Where(static match => match is not null)
            .Select(static match => match!)
            .OrderByDescending(static match => match.Score)
            .ThenBy(static match => match.Title, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .ToArray();

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["queryLimit"] = limit.ToString(CultureInfo.InvariantCulture),
            ["totalIndexedDocuments"] = documents.Count.ToString(CultureInfo.InvariantCulture)
        };
        foreach (var pair in request.Metadata)
        {
            if (!string.IsNullOrWhiteSpace(pair.Key))
            {
                metadata[pair.Key.Trim()] = pair.Value;
            }
        }

        runtimeCatalog.RecordQuery(request, matches.Length, queriedAtUtc, metadata);

        return ValueTask.FromResult(new KnowledgeQueryResult(
            request.CollectionId,
            request.QueryText,
            queriedAtUtc,
            matches,
            documents.Count,
            metadata));
    }

    private int ResolveLimit(int? requested)
    {
        var defaultLimit = options.DefaultQueryLimit > 0
            ? options.DefaultQueryLimit
            : 10;
        var maximum = options.MaximumQueryLimit > 0
            ? options.MaximumQueryLimit
            : 25;
        return Math.Min(requested ?? defaultLimit, maximum);
    }

    private static KnowledgeQueryMatch? CreateMatch(
        string collectionId,
        KnowledgeDocument document,
        string queryText,
        string[] terms)
    {
        var score = Score(document, queryText, terms);
        if (score <= 0)
        {
            return null;
        }

        return new KnowledgeQueryMatch(
            collectionId,
            document.Id,
            document.Title,
            CreateSnippet(document.Content, terms),
            score,
            document.Uri,
            document.Tags,
            document.LastModifiedAtUtc,
            document.Metadata);
    }

    private static int Score(
        KnowledgeDocument document,
        string queryText,
        string[] terms)
    {
        var score = 0;
        if (document.Title.Contains(queryText, StringComparison.OrdinalIgnoreCase))
        {
            score += 8;
        }

        if (document.Content.Contains(queryText, StringComparison.OrdinalIgnoreCase))
        {
            score += 5;
        }

        foreach (var term in terms)
        {
            if (document.Title.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                score += 3;
            }

            if (document.Tags.Contains(term, StringComparer.OrdinalIgnoreCase))
            {
                score += 2;
            }

            if (document.Content.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                score += 1;
            }

            if (document.Metadata.Values.Any(value => value.Contains(term, StringComparison.OrdinalIgnoreCase)))
            {
                score += 1;
            }
        }

        return score;
    }

    private static string[] Tokenize(string queryText)
    {
        return queryText
            .Split(QuerySeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(static term => term.Length > 1)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string CreateSnippet(
        string content,
        string[] terms)
    {
        var firstIndex = terms
            .Select(term => content.IndexOf(term, StringComparison.OrdinalIgnoreCase))
            .Where(static index => index >= 0)
            .DefaultIfEmpty(0)
            .Min();
        var start = Math.Max(0, firstIndex - 40);
        var length = Math.Min(160, content.Length - start);
        var snippet = content.Substring(start, length).Trim();
        if (start > 0)
        {
            snippet = $"...{snippet}";
        }

        if (start + length < content.Length)
        {
            snippet = $"{snippet}...";
        }

        return snippet;
    }
}
