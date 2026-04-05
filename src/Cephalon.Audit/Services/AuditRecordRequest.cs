using Cephalon.Abstractions.Audit;

namespace Cephalon.Audit.Services;

/// <summary>
/// Describes one audit request before Cephalon fills its default runtime context.
/// </summary>
public sealed class AuditRecordRequest
{
    /// <summary>
    /// Creates a new audit-record request.
    /// </summary>
    /// <param name="category">The logical audit category.</param>
    /// <param name="action">The logical action identifier associated with the audit event.</param>
    /// <param name="summary">The human-readable audit summary.</param>
    /// <param name="subjectType">The logical subject type associated with the entry.</param>
    /// <param name="subjectId">The stable subject identifier associated with the entry when one is known.</param>
    /// <param name="entryId">The stable audit-entry identifier when the caller wants to supply one explicitly.</param>
    /// <param name="occurredAtUtc">The occurrence time when the caller wants to supply it explicitly.</param>
    /// <param name="actor">The actor responsible for the audited operation when the caller wants to supply one explicitly.</param>
    /// <param name="outcome">The outcome recorded for the audited operation.</param>
    /// <param name="tenantId">The tenant identifier associated with the audited operation.</param>
    /// <param name="correlationId">The correlation identifier associated with the audited operation.</param>
    /// <param name="changes">Optional field-level changes captured for the operation.</param>
    /// <param name="tags">Optional descriptive tags associated with the entry.</param>
    /// <param name="metadata">Optional audit metadata.</param>
    public AuditRecordRequest(
        string category,
        string action,
        string summary,
        string subjectType,
        string? subjectId = null,
        string? entryId = null,
        DateTimeOffset? occurredAtUtc = null,
        AuditActor? actor = null,
        AuditOutcome outcome = AuditOutcome.Unknown,
        string? tenantId = null,
        string? correlationId = null,
        IReadOnlyList<AuditChange>? changes = null,
        IReadOnlyList<string>? tags = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            throw new ArgumentException("Audit category is required.", nameof(category));
        }

        if (string.IsNullOrWhiteSpace(action))
        {
            throw new ArgumentException("Audit action is required.", nameof(action));
        }

        if (string.IsNullOrWhiteSpace(summary))
        {
            throw new ArgumentException("Audit summary is required.", nameof(summary));
        }

        if (string.IsNullOrWhiteSpace(subjectType))
        {
            throw new ArgumentException("Audit subject type is required.", nameof(subjectType));
        }

        Category = category.Trim();
        Action = action.Trim();
        Summary = summary.Trim();
        SubjectType = subjectType.Trim();
        SubjectId = string.IsNullOrWhiteSpace(subjectId) ? null : subjectId.Trim();
        EntryId = string.IsNullOrWhiteSpace(entryId) ? null : entryId.Trim();
        OccurredAtUtc = occurredAtUtc;
        Actor = actor;
        Outcome = outcome;
        TenantId = string.IsNullOrWhiteSpace(tenantId) ? null : tenantId.Trim();
        CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? null : correlationId.Trim();
        Changes = changes ?? [];
        Tags = tags ?? [];
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

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
    /// Gets the caller-supplied audit-entry identifier when one is known.
    /// </summary>
    public string? EntryId { get; }

    /// <summary>
    /// Gets the caller-supplied occurrence time when one is known.
    /// </summary>
    public DateTimeOffset? OccurredAtUtc { get; }

    /// <summary>
    /// Gets the actor responsible for the audited operation when one was supplied explicitly.
    /// </summary>
    public AuditActor? Actor { get; }

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
}
