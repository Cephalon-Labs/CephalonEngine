namespace Cephalon.Abstractions.Audit;

/// <summary>
/// Persists audit entries for the current runtime.
/// </summary>
public interface IAuditWriter
{
    /// <summary>
    /// Writes one audit entry.
    /// </summary>
    /// <param name="entry">The audit entry to write.</param>
    /// <param name="cancellationToken">The token that cancels the operation.</param>
    /// <returns>A task that completes when the audit entry has been written.</returns>
    ValueTask WriteAsync(AuditEntry entry, CancellationToken cancellationToken = default);
}
