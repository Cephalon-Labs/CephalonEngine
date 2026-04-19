namespace Cephalon.Abstractions.Execution;

/// <summary>
/// Describes one operator-facing durable-execution compensation action available for a workflow stream.
/// </summary>
public sealed class DurableExecutionCompensationAction
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DurableExecutionCompensationAction" /> class.
    /// </summary>
    /// <param name="id">The stable compensation-action identifier within the durable workflow.</param>
    /// <param name="displayName">The operator-facing compensation-action name.</param>
    /// <param name="description">A human-readable description of what the compensation action does.</param>
    /// <param name="triggerKind">
    /// The operator-facing trigger kind for the compensation action, such as <c>manual</c> or <c>on-failure</c>.
    /// </param>
    /// <param name="compensationBehaviorId">
    /// The stable behavior identifier to invoke when the compensation action maps to another Cephalon behavior.
    /// </param>
    /// <param name="metadata">Additional operator-facing metadata describing the compensation action.</param>
    public DurableExecutionCompensationAction(
        string id,
        string? displayName = null,
        string? description = null,
        string? triggerKind = null,
        string? compensationBehaviorId = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Compensation action id is required.", nameof(id));
        }

        Id = id.Trim();
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? Id : displayName.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        TriggerKind = string.IsNullOrWhiteSpace(triggerKind) ? "manual" : triggerKind.Trim();
        CompensationBehaviorId = string.IsNullOrWhiteSpace(compensationBehaviorId)
            ? null
            : compensationBehaviorId.Trim();
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the stable compensation-action identifier within the durable workflow.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the operator-facing compensation-action name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the human-readable compensation-action description when one was supplied.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the operator-facing trigger kind for the compensation action.
    /// </summary>
    public string TriggerKind { get; }

    /// <summary>
    /// Gets the stable behavior identifier to invoke when the compensation action maps to another Cephalon behavior.
    /// </summary>
    public string? CompensationBehaviorId { get; }

    /// <summary>
    /// Gets additional operator-facing metadata describing the compensation action.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }
}
