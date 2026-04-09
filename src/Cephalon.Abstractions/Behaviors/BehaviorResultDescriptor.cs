namespace Cephalon.Abstractions.Behaviors;

/// <summary>
/// Represents a transport-neutral behavior outcome descriptor that does not carry a payload value.
/// </summary>
public readonly record struct BehaviorResultDescriptor
{
    internal BehaviorResultDescriptor(
        BehaviorResultStatus status,
        string? message,
        string? code,
        BehaviorFault? fault)
    {
        Status = status;
        Message = string.IsNullOrWhiteSpace(message) ? null : message.Trim();
        Code = string.IsNullOrWhiteSpace(code) ? null : code.Trim();
        Fault = fault;
    }

    /// <summary>
    /// Gets the transport-neutral outcome status.
    /// </summary>
    public BehaviorResultStatus Status { get; }

    /// <summary>
    /// Gets the human-readable outcome message.
    /// </summary>
    public string? Message { get; }

    /// <summary>
    /// Gets the stable outcome code when one was supplied.
    /// </summary>
    public string? Code { get; }

    /// <summary>
    /// Gets the structured fault details when the outcome is not successful.
    /// </summary>
    public BehaviorFault? Fault { get; }
}
