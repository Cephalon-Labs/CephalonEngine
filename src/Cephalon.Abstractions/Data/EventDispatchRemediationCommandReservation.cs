namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes the result of reserving one event-dispatch remediation command identifier before dispatch-store mutation.
/// </summary>
public sealed class EventDispatchRemediationCommandReservation
{
    /// <summary>
    /// Creates a new command-reservation result.
    /// </summary>
    /// <param name="commandId">The stable remediation command identifier that was reserved or detected as a duplicate.</param>
    /// <param name="reserved">A value indicating whether the journal reserved the command identifier for the caller.</param>
    /// <param name="existingCommand">The existing command state when the identifier was already reserved or recorded.</param>
    /// <param name="metadata">Operator-facing metadata that describes the reservation policy and state.</param>
    public EventDispatchRemediationCommandReservation(
        string commandId,
        bool reserved,
        EventDispatchRemediationRuntimeState? existingCommand,
        IReadOnlyDictionary<string, string> metadata)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandId);
        ArgumentNullException.ThrowIfNull(metadata);

        CommandId = commandId.Trim();
        Reserved = reserved;
        ExistingCommand = existingCommand;
        Metadata = metadata;
    }

    /// <summary>
    /// Gets the stable remediation command identifier that was reserved or detected as a duplicate.
    /// </summary>
    public string CommandId { get; }

    /// <summary>
    /// Gets a value indicating whether the journal reserved the command identifier for the caller.
    /// </summary>
    public bool Reserved { get; }

    /// <summary>
    /// Gets the existing command state when the identifier was already reserved or recorded.
    /// </summary>
    public EventDispatchRemediationRuntimeState? ExistingCommand { get; }

    /// <summary>
    /// Gets operator-facing metadata that describes the reservation policy and state.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }
}
