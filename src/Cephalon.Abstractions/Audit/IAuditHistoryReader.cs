namespace Cephalon.Abstractions.Audit;

/// <summary>
/// Reads persisted audit-history entries from the active runtime.
/// </summary>
public interface IAuditHistoryReader
{
    /// <summary>
    /// Resolves one audit-history entry by its stable identifier.
    /// </summary>
    /// <param name="auditEntryId">The stable audit-entry identifier to resolve.</param>
    /// <param name="cancellationToken">The token that cancels the operation.</param>
    /// <returns>The matching audit-history entry when one exists; otherwise <see langword="null" />.</returns>
    ValueTask<AuditHistoryEntry?> GetByIdAsync(
        string auditEntryId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries audit-history entries using the supplied host-agnostic filter set.
    /// </summary>
    /// <param name="query">The query to execute.</param>
    /// <param name="cancellationToken">The token that cancels the operation.</param>
    /// <returns>The resulting page of audit-history entries.</returns>
    ValueTask<AuditHistoryQueryResult> QueryAsync(
        AuditHistoryQuery query,
        CancellationToken cancellationToken = default);
}
