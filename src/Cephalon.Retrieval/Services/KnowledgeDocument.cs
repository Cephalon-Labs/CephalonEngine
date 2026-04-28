namespace Cephalon.Retrieval.Services;

/// <summary>
/// Describes one source document that can be indexed by the managed retrieval runtime.
/// </summary>
public sealed class KnowledgeDocument
{
    /// <summary>
    /// Creates a knowledge document for managed indexing.
    /// </summary>
    /// <param name="id">The stable document identifier within its collection.</param>
    /// <param name="title">The human-readable document title.</param>
    /// <param name="content">The searchable document content.</param>
    /// <param name="uri">An optional document URI for operator drill-down.</param>
    /// <param name="tags">Optional tags that classify the document.</param>
    /// <param name="lastModifiedAtUtc">The UTC timestamp when the source document was last modified.</param>
    /// <param name="metadata">Optional operator-facing metadata attached to the document.</param>
    public KnowledgeDocument(
        string id,
        string title,
        string content,
        Uri? uri = null,
        IReadOnlyList<string>? tags = null,
        DateTimeOffset? lastModifiedAtUtc = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Document id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Document title is required.", nameof(title));
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Document content is required.", nameof(content));
        }

        Id = id.Trim();
        Title = title.Trim();
        Content = content.Trim();
        Uri = uri;
        Tags = tags?
            .Where(static tag => !string.IsNullOrWhiteSpace(tag))
            .Select(static tag => tag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static tag => tag, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
        LastModifiedAtUtc = lastModifiedAtUtc;
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the stable document identifier within its collection.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the human-readable document title.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Gets the searchable document content.
    /// </summary>
    public string Content { get; }

    /// <summary>
    /// Gets an optional document URI for operator drill-down.
    /// </summary>
    public Uri? Uri { get; }

    /// <summary>
    /// Gets the normalized tags that classify the document.
    /// </summary>
    public IReadOnlyList<string> Tags { get; }

    /// <summary>
    /// Gets the UTC timestamp when the source document was last modified.
    /// </summary>
    public DateTimeOffset? LastModifiedAtUtc { get; }

    /// <summary>
    /// Gets optional operator-facing metadata attached to the document.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }
}
