namespace Cephalon.Retrieval.Services;

/// <summary>
/// Describes one ranked document match returned by the managed query engine.
/// </summary>
/// <param name="CollectionId">The collection identifier that produced the match.</param>
/// <param name="DocumentId">The matched document identifier.</param>
/// <param name="Title">The matched document title.</param>
/// <param name="ContentSnippet">A short content snippet around the matched text.</param>
/// <param name="Score">The lexical relevance score assigned by the managed query engine.</param>
/// <param name="Uri">The optional source document URI.</param>
/// <param name="Tags">The normalized tags attached to the matched document.</param>
/// <param name="LastModifiedAtUtc">The UTC timestamp when the matched source document was last modified.</param>
/// <param name="Metadata">The operator-facing metadata attached to the matched document.</param>
public sealed record KnowledgeQueryMatch(
    string CollectionId,
    string DocumentId,
    string Title,
    string ContentSnippet,
    int Score,
    Uri? Uri,
    IReadOnlyList<string> Tags,
    DateTimeOffset? LastModifiedAtUtc,
    IReadOnlyDictionary<string, string> Metadata);
