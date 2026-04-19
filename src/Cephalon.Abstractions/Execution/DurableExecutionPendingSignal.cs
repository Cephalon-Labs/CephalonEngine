namespace Cephalon.Abstractions.Execution;

/// <summary>
/// Describes one durable-execution signal that is currently pending for a workflow stream.
/// </summary>
public sealed class DurableExecutionPendingSignal
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DurableExecutionPendingSignal" /> class.
    /// </summary>
    /// <param name="id">The stable signal identifier within the durable workflow.</param>
    /// <param name="displayName">The operator-facing signal name.</param>
    /// <param name="description">A human-readable description of why the signal is awaited.</param>
    /// <param name="payloadType">The expected payload type name for the awaited signal when one is known.</param>
    /// <param name="metadata">Additional operator-facing metadata describing the signal.</param>
    public DurableExecutionPendingSignal(
        string id,
        string? displayName = null,
        string? description = null,
        string? payloadType = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Signal id is required.", nameof(id));
        }

        Id = id.Trim();
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? Id : displayName.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        PayloadType = string.IsNullOrWhiteSpace(payloadType) ? null : payloadType.Trim();
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the stable signal identifier within the durable workflow.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the operator-facing signal name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the human-readable signal description when one was supplied.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the expected payload type name when the awaited signal declares one.
    /// </summary>
    public string? PayloadType { get; }

    /// <summary>
    /// Gets additional operator-facing metadata describing the signal.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }
}
