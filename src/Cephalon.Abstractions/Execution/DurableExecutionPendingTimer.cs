namespace Cephalon.Abstractions.Execution;

/// <summary>
/// Describes one durable-execution timer that is currently pending for a workflow stream.
/// </summary>
public sealed class DurableExecutionPendingTimer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DurableExecutionPendingTimer" /> class.
    /// </summary>
    /// <param name="id">The stable timer identifier within the durable workflow.</param>
    /// <param name="dueAtUtc">The UTC timestamp when the timer is next due.</param>
    /// <param name="displayName">The operator-facing timer name.</param>
    /// <param name="description">A human-readable description of why the timer is pending.</param>
    /// <param name="metadata">Additional operator-facing metadata describing the timer.</param>
    public DurableExecutionPendingTimer(
        string id,
        DateTimeOffset dueAtUtc,
        string? displayName = null,
        string? description = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Timer id is required.", nameof(id));
        }

        Id = id.Trim();
        DueAtUtc = dueAtUtc;
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? Id : displayName.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the stable timer identifier within the durable workflow.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the UTC timestamp when the timer is next due.
    /// </summary>
    public DateTimeOffset DueAtUtc { get; }

    /// <summary>
    /// Gets the operator-facing timer name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the human-readable timer description when one was supplied.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets additional operator-facing metadata describing the timer.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }
}
