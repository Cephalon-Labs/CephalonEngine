namespace Cephalon.Behaviors.Patterns.Abstractions;

/// <summary>
/// Carries the local output and event publications produced by one choreography-based saga step.
/// </summary>
public sealed class SagaChoreographyStepResult : ISagaChoreographyStepResult
{
    /// <summary>
    /// Initializes a new instance of <see cref="SagaChoreographyStepResult"/>.
    /// </summary>
    /// <param name="output">The optional local output returned to the caller.</param>
    /// <param name="publications">The publications that continue or compensate the saga.</param>
    public SagaChoreographyStepResult(
        object? output = null,
        IReadOnlyList<SagaChoreographyPublication>? publications = null)
    {
        Output = output;
        Publications = publications is null
            ? []
            : publications
                .Where(static publication => publication is not null)
                .ToArray();
    }

    /// <summary>Gets the optional local output returned to the caller.</summary>
    public object? Output { get; }

    /// <summary>Gets the publications that continue or compensate the saga.</summary>
    public IReadOnlyList<SagaChoreographyPublication> Publications { get; }
}
