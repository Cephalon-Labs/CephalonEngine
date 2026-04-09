namespace Cephalon.Abstractions.Behaviors;

/// <summary>Represents a structured fault from a behavior execution.</summary>
public sealed class BehaviorFault
{
    /// <summary>Initializes a new instance of <see cref="BehaviorFault"/>.</summary>
    public BehaviorFault() { }

    /// <summary>Gets or sets the fault code.</summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>Gets or sets the fault message.</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>Gets or sets the fault severity.</summary>
    public BehaviorFaultSeverity Severity { get; init; } = BehaviorFaultSeverity.Error;

    /// <summary>Gets or sets additional fault details.</summary>
    public string? Details { get; init; }

    /// <summary>Gets or sets nested faults.</summary>
    public IReadOnlyList<BehaviorFault> InnerFaults { get; init; } = [];
}
