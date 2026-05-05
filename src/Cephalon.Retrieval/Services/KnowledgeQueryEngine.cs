using Cephalon.Retrieval.Configuration;
using Cephalon.Abstractions.Retrieval;
using Cephalon.Diagnostics.Redaction;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;

namespace Cephalon.Retrieval.Services;

internal sealed class KnowledgeQueryEngine(
    IKnowledgeCatalog catalog,
    KnowledgeRuntimeCatalog runtimeCatalog,
    RetrievalOptions options,
    RedactionPipeline? redactionPipeline = null) : IKnowledgeQueryEngine
{
    private static readonly char[] QuerySeparators =
    [
        ' ', '\t', '\r', '\n', '.', ',', ';', ':', '/', '\\', '-', '_', '(', ')', '[', ']', '{', '}', '"', '\''
    ];

    private const string QueryOutcomeSucceeded = "succeeded";
    private const string QueryOutcomeFailed = "failed";

    public ValueTask<KnowledgeQueryResult> QueryAsync(
        KnowledgeQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        using var queryActivity = RetrievalDiagnostics.ActivitySource.StartActivity(
            RetrievalDiagnostics.KnowledgeQueryActivityName,
            ActivityKind.Internal);
        SetTag(queryActivity, RetrievalDiagnostics.QueryEngineIdTag, InProcessRetrievalRuntimeIds.QueryEngineId);
        SetTag(queryActivity, RetrievalDiagnostics.CollectionIdTag, request.CollectionId);
        SetTag(queryActivity, RetrievalDiagnostics.QueryLengthTag, request.QueryText.Length);
        if (!string.IsNullOrEmpty(request.ActorId))
        {
            SetTag(queryActivity, RetrievalDiagnostics.ActorIdTag, request.ActorId);
        }
        if (!string.IsNullOrEmpty(request.CorrelationId))
        {
            SetTag(queryActivity, RetrievalDiagnostics.CorrelationIdTag, request.CorrelationId);
        }

        if (!catalog.TryGet(request.CollectionId, out _))
        {
            var missingError =
                $"Knowledge collection '{request.CollectionId}' is not registered in the active retrieval runtime.";
            CompleteQueryActivity(
                queryActivity,
                request.CollectionId,
                QueryOutcomeFailed,
                queryLimit: 0,
                matchCount: 0,
                error: missingError);
            throw new InvalidOperationException(missingError);
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

        CompleteQueryActivity(
            queryActivity,
            request.CollectionId,
            QueryOutcomeSucceeded,
            queryLimit: limit,
            matchCount: matches.Length);

        return ValueTask.FromResult(new KnowledgeQueryResult(
            request.CollectionId,
            request.QueryText,
            queriedAtUtc,
            matches,
            documents.Count,
            metadata));
    }

    /// <summary>
    /// Sets the terminal query-outcome tag, optional error status, and increments the
    /// knowledge-query counter for the given query activity. Tag values are routed through the
    /// redaction pipeline so consumer-registered redaction filters apply uniformly across the
    /// query span.
    /// </summary>
    private void CompleteQueryActivity(
        Activity? queryActivity,
        string collectionId,
        string outcome,
        int queryLimit,
        int matchCount,
        string? error = null)
    {
        SetTag(queryActivity, RetrievalDiagnostics.QueryOutcomeTag, outcome);
        SetTag(queryActivity, RetrievalDiagnostics.QueryLimitTag, queryLimit);
        SetTag(queryActivity, RetrievalDiagnostics.MatchCountTag, matchCount);

        if (string.Equals(outcome, QueryOutcomeFailed, StringComparison.OrdinalIgnoreCase))
        {
            queryActivity?.SetStatus(ActivityStatusCode.Error, error);
        }

        RetrievalDiagnostics.KnowledgeQueryCounter.Add(
            1,
            new TagList
            {
                { RetrievalDiagnostics.CollectionIdTag, collectionId },
                { RetrievalDiagnostics.QueryOutcomeTag, outcome }
            });
    }

    /// <summary>
    /// Sets a tag on <paramref name="activity"/> after routing the value through the consumer
    /// -registered <see cref="RedactionPipeline"/>. The pipeline is empty by default when no
    /// consumer registered any <see cref="IRedactionFilter"/>; in that case (and when DI did not
    /// supply a pipeline at all) this method short-circuits to passthrough so query emission
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
