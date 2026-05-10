namespace Cephalon.Abstractions.Data;

/// <summary>
/// Records and reads event-dispatch remediation command results for operator audit and command idempotency.
/// </summary>
public interface IEventDispatchRemediationCommandJournal : IEventDispatchRemediationRuntimeCatalog
{
    /// <summary>
    /// Gets the storage and audit posture exposed by this command journal.
    /// </summary>
    EventDispatchRemediationCommandJournalDescriptor Descriptor { get; }

    /// <summary>
    /// Records one event-dispatch remediation command result.
    /// </summary>
    /// <param name="result">The command result to record.</param>
    /// <param name="cancellationToken">A token that observes cancellation requests.</param>
    /// <returns>A task that completes when the command result has been recorded.</returns>
    ValueTask RecordAsync(
        EventDispatchRemediationResult result,
        CancellationToken cancellationToken = default);
}
