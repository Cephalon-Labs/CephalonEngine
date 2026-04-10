namespace Cephalon.Abstractions.Audit;

/// <summary>
/// Represents one page of audit-history results returned by an <see cref="IAuditHistoryReader" />.
/// </summary>
public sealed class AuditHistoryQueryResult
{
    /// <summary>
    /// Creates a new audit-history query result.
    /// </summary>
    /// <param name="entries">The returned audit-history entries.</param>
    /// <param name="offset">The zero-based query offset that produced this page.</param>
    /// <param name="limit">The normalized page size used for the query.</param>
    /// <param name="totalCount">The total number of matching entries before paging was applied.</param>
    public AuditHistoryQueryResult(
        IReadOnlyList<AuditHistoryEntry>? entries,
        int offset,
        int limit,
        int totalCount)
    {
        Entries = entries?
            .Where(static entry => entry is not null)
            .ToArray() ?? [];
        Offset = Math.Max(0, offset);
        Limit = Math.Max(1, limit);
        TotalCount = Math.Max(0, totalCount);
    }

    /// <summary>
    /// Gets the returned audit-history entries.
    /// </summary>
    public IReadOnlyList<AuditHistoryEntry> Entries { get; }

    /// <summary>
    /// Gets the zero-based query offset that produced this page.
    /// </summary>
    public int Offset { get; }

    /// <summary>
    /// Gets the normalized page size used for the query.
    /// </summary>
    public int Limit { get; }

    /// <summary>
    /// Gets the total number of matching entries before paging was applied.
    /// </summary>
    public int TotalCount { get; }

    /// <summary>
    /// Gets a value indicating whether more entries remain beyond this page.
    /// </summary>
    public bool HasMore => Offset + Entries.Count < TotalCount;
}
