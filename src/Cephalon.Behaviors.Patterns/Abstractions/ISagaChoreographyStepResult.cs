namespace Cephalon.Behaviors.Patterns.Abstractions;

/// <summary>
/// Describes the local output and staged publications produced by one choreography-based saga step.
/// </summary>
public interface ISagaChoreographyStepResult
{
    /// <summary>
    /// Gets the optional local output returned to the caller.
    /// </summary>
    object? Output { get; }

    /// <summary>
    /// Gets the publications that continue or compensate the saga.
    /// </summary>
    IReadOnlyList<SagaChoreographyPublication> Publications { get; }
}
