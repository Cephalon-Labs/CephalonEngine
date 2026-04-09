namespace Cephalon.Abstractions.Behaviors;

/// <summary>
/// Describes a structured behavior outcome that can be projected into transport-specific responses.
/// </summary>
public interface IBehaviorResult
{
    /// <summary>
    /// Gets the transport-neutral outcome status.
    /// </summary>
    BehaviorResultStatus Status { get; }

    /// <summary>
    /// Gets the stable outcome code when one was supplied.
    /// </summary>
    string? Code { get; }

    /// <summary>
    /// Gets the human-readable outcome message.
    /// </summary>
    string Message { get; }

    /// <summary>
    /// Gets a value indicating whether the result represents a successful outcome.
    /// </summary>
    bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the result carries a payload value.
    /// </summary>
    bool HasValue { get; }

    /// <summary>
    /// Gets the boxed payload value when one was supplied.
    /// </summary>
    object? Value { get; }

    /// <summary>
    /// Gets the structured fault details when the outcome is not successful.
    /// </summary>
    BehaviorFault? Fault { get; }
}
