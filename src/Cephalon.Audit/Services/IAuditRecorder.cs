using Cephalon.Abstractions.Audit;

namespace Cephalon.Audit.Services;

/// <summary>
/// Records audit entries through the active Cephalon audit pipeline.
/// </summary>
public interface IAuditRecorder
{
    /// <summary>
    /// Records one audit entry and returns the normalized entry that was written.
    /// </summary>
    /// <param name="request">The audit request to record.</param>
    /// <param name="cancellationToken">The token that cancels the operation.</param>
    /// <returns>The normalized audit entry that was written.</returns>
    ValueTask<AuditEntry> RecordAsync(
        AuditRecordRequest request,
        CancellationToken cancellationToken = default);
}
