namespace Cephalon.Abstractions.Audit;

/// <summary>
/// Describes a host-agnostic audit-history query against the active audit-history reader.
/// </summary>
public sealed class AuditHistoryQuery
{
    /// <summary>
    /// Gets the default number of entries returned by a query when the caller does not supply one.
    /// </summary>
    public const int DefaultLimit = 50;

    /// <summary>
    /// Gets the maximum number of entries returned by one query.
    /// </summary>
    public const int MaxLimit = 200;

    /// <summary>
    /// Creates a new audit-history query.
    /// </summary>
    /// <param name="category">An optional logical audit category filter.</param>
    /// <param name="action">An optional logical action identifier filter.</param>
    /// <param name="subjectType">An optional logical subject-type filter.</param>
    /// <param name="subjectId">An optional stable subject identifier filter.</param>
    /// <param name="actorId">An optional stable actor identifier filter.</param>
    /// <param name="tenantId">An optional tenant identifier filter.</param>
    /// <param name="correlationId">An optional correlation identifier filter.</param>
    /// <param name="outcome">An optional audit-outcome filter.</param>
    /// <param name="occurredFromUtc">An optional inclusive lower occurrence bound.</param>
    /// <param name="occurredToUtc">An optional inclusive upper occurrence bound.</param>
    /// <param name="offset">The zero-based query offset.</param>
    /// <param name="limit">The requested page size, clamped to the supported range.</param>
    public AuditHistoryQuery(
        string? category = null,
        string? action = null,
        string? subjectType = null,
        string? subjectId = null,
        string? actorId = null,
        string? tenantId = null,
        string? correlationId = null,
        AuditOutcome? outcome = null,
        DateTimeOffset? occurredFromUtc = null,
        DateTimeOffset? occurredToUtc = null,
        int offset = 0,
        int limit = DefaultLimit)
    {
        Category = Normalize(category);
        Action = Normalize(action);
        SubjectType = Normalize(subjectType);
        SubjectId = Normalize(subjectId);
        ActorId = Normalize(actorId);
        TenantId = Normalize(tenantId);
        CorrelationId = Normalize(correlationId);
        Outcome = outcome;
        OccurredFromUtc = occurredFromUtc;
        OccurredToUtc = occurredToUtc;
        Offset = Math.Max(0, offset);
        Limit = Math.Clamp(limit <= 0 ? DefaultLimit : limit, 1, MaxLimit);
    }

    /// <summary>
    /// Gets the optional logical audit category filter.
    /// </summary>
    public string? Category { get; }

    /// <summary>
    /// Gets the optional logical action identifier filter.
    /// </summary>
    public string? Action { get; }

    /// <summary>
    /// Gets the optional logical subject-type filter.
    /// </summary>
    public string? SubjectType { get; }

    /// <summary>
    /// Gets the optional stable subject identifier filter.
    /// </summary>
    public string? SubjectId { get; }

    /// <summary>
    /// Gets the optional stable actor identifier filter.
    /// </summary>
    public string? ActorId { get; }

    /// <summary>
    /// Gets the optional tenant identifier filter.
    /// </summary>
    public string? TenantId { get; }

    /// <summary>
    /// Gets the optional correlation identifier filter.
    /// </summary>
    public string? CorrelationId { get; }

    /// <summary>
    /// Gets the optional audit-outcome filter.
    /// </summary>
    public AuditOutcome? Outcome { get; }

    /// <summary>
    /// Gets the optional inclusive lower occurrence bound.
    /// </summary>
    public DateTimeOffset? OccurredFromUtc { get; }

    /// <summary>
    /// Gets the optional inclusive upper occurrence bound.
    /// </summary>
    public DateTimeOffset? OccurredToUtc { get; }

    /// <summary>
    /// Gets the zero-based query offset.
    /// </summary>
    public int Offset { get; }

    /// <summary>
    /// Gets the requested page size after it has been normalized to the supported range.
    /// </summary>
    public int Limit { get; }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
