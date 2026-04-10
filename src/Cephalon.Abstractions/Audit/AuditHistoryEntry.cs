namespace Cephalon.Abstractions.Audit;

/// <summary>
/// Represents one audit entry returned from a durable or queryable audit-history store.
/// </summary>
public sealed class AuditHistoryEntry
{
    /// <summary>
    /// Creates a new audit-history entry.
    /// </summary>
    /// <param name="id">The stable audit-entry identifier.</param>
    /// <param name="category">The logical audit category such as <c>identity</c>, <c>tenant</c>, or <c>billing</c>.</param>
    /// <param name="action">The logical action identifier associated with the audit event.</param>
    /// <param name="summary">The human-readable audit summary.</param>
    /// <param name="subjectType">The logical subject type associated with the entry.</param>
    /// <param name="subjectId">The stable subject identifier associated with the entry when one is known.</param>
    /// <param name="occurredAtUtc">The time at which the audited operation occurred.</param>
    /// <param name="persistedAtUtc">The time at which the audit entry was durably persisted.</param>
    /// <param name="actor">The actor responsible for the audited operation.</param>
    /// <param name="outcome">The outcome recorded for the audited operation.</param>
    /// <param name="tenantId">The tenant identifier associated with the audited operation.</param>
    /// <param name="correlationId">The correlation identifier associated with the audited operation.</param>
    /// <param name="changes">Optional field-level changes captured for the operation.</param>
    /// <param name="tags">Optional descriptive tags associated with the entry.</param>
    /// <param name="metadata">Optional audit metadata.</param>
    public AuditHistoryEntry(
        string id,
        string category,
        string action,
        string summary,
        string subjectType,
        string? subjectId,
        DateTimeOffset occurredAtUtc,
        DateTimeOffset persistedAtUtc,
        AuditActor actor,
        AuditOutcome outcome = AuditOutcome.Unknown,
        string? tenantId = null,
        string? correlationId = null,
        IReadOnlyList<AuditChange>? changes = null,
        IReadOnlyList<string>? tags = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Audit history entry id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            throw new ArgumentException("Audit history entry category is required.", nameof(category));
        }

        if (string.IsNullOrWhiteSpace(action))
        {
            throw new ArgumentException("Audit history entry action is required.", nameof(action));
        }

        if (string.IsNullOrWhiteSpace(summary))
        {
            throw new ArgumentException("Audit history entry summary is required.", nameof(summary));
        }

        if (string.IsNullOrWhiteSpace(subjectType))
        {
            throw new ArgumentException("Audit history entry subject type is required.", nameof(subjectType));
        }

        if (occurredAtUtc == default)
        {
            throw new ArgumentException("Audit history occurrence time is required.", nameof(occurredAtUtc));
        }

        if (persistedAtUtc == default)
        {
            throw new ArgumentException("Audit history persistence time is required.", nameof(persistedAtUtc));
        }

        Id = id.Trim();
        Category = category.Trim();
        Action = action.Trim();
        Summary = summary.Trim();
        SubjectType = subjectType.Trim();
        SubjectId = Normalize(subjectId);
        OccurredAtUtc = occurredAtUtc;
        PersistedAtUtc = persistedAtUtc;
        Actor = actor ?? throw new ArgumentNullException(nameof(actor));
        Outcome = outcome;
        TenantId = Normalize(tenantId);
        CorrelationId = Normalize(correlationId);
        Changes = changes?
            .Where(static change => change is not null)
            .OrderBy(static change => change.FieldName, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
        Tags = tags?
            .Where(static tag => !string.IsNullOrWhiteSpace(tag))
            .Select(static tag => tag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static tag => tag, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the stable audit-entry identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the logical audit category.
    /// </summary>
    public string Category { get; }

    /// <summary>
    /// Gets the logical action identifier associated with the audit event.
    /// </summary>
    public string Action { get; }

    /// <summary>
    /// Gets the human-readable audit summary.
    /// </summary>
    public string Summary { get; }

    /// <summary>
    /// Gets the logical subject type associated with the entry.
    /// </summary>
    public string SubjectType { get; }

    /// <summary>
    /// Gets the stable subject identifier associated with the entry when one is known.
    /// </summary>
    public string? SubjectId { get; }

    /// <summary>
    /// Gets the time at which the audited operation occurred.
    /// </summary>
    public DateTimeOffset OccurredAtUtc { get; }

    /// <summary>
    /// Gets the time at which the audit entry was durably persisted.
    /// </summary>
    public DateTimeOffset PersistedAtUtc { get; }

    /// <summary>
    /// Gets the actor responsible for the audited operation.
    /// </summary>
    public AuditActor Actor { get; }

    /// <summary>
    /// Gets the outcome recorded for the audited operation.
    /// </summary>
    public AuditOutcome Outcome { get; }

    /// <summary>
    /// Gets the tenant identifier associated with the audited operation.
    /// </summary>
    public string? TenantId { get; }

    /// <summary>
    /// Gets the correlation identifier associated with the audited operation.
    /// </summary>
    public string? CorrelationId { get; }

    /// <summary>
    /// Gets the field-level changes captured for the operation.
    /// </summary>
    public IReadOnlyList<AuditChange> Changes { get; }

    /// <summary>
    /// Gets descriptive tags associated with the entry.
    /// </summary>
    public IReadOnlyList<string> Tags { get; }

    /// <summary>
    /// Gets audit metadata associated with the entry.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
