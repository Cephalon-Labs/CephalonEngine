namespace Cephalon.Abstractions.Audit;

/// <summary>
/// Streams persisted audit-history entries for export-oriented operator or application flows.
/// </summary>
public interface IAuditHistoryExporter
{
    /// <summary>
    /// Streams audit-history entries that match the supplied export request in stable export order.
    /// </summary>
    /// <param name="request">The export request to execute.</param>
    /// <param name="cancellationToken">The token that cancels the export stream.</param>
    /// <returns>The matching audit-history entries.</returns>
    IAsyncEnumerable<AuditHistoryEntry> ExportAsync(
        AuditHistoryExportRequest request,
        CancellationToken cancellationToken = default);
}
