namespace Cephalon.Behaviors.Patterns.Abstractions;

/// <summary>
/// Carries typed local output and event publications produced by one choreography-based saga step.
/// </summary>
/// <typeparam name="TOutput">The local output type returned by the choreography step.</typeparam>
public sealed class SagaChoreographyStepResult<TOutput> : ISagaChoreographyStepResult
{
    /// <summary>
    /// Initializes a new instance of <see cref="SagaChoreographyStepResult{TOutput}" />.
    /// </summary>
    /// <param name="output">The typed local output returned to the caller.</param>
    /// <param name="publications">The publications that continue or compensate the saga.</param>
    public SagaChoreographyStepResult(
        TOutput output,
        IReadOnlyList<SagaChoreographyPublication>? publications = null)
    {
        Output = output;
        Publications = publications is null
            ? []
            : publications
                .Where(static publication => publication is not null)
                .ToArray();
    }

    /// <summary>
    /// Gets the typed local output returned to the caller.
    /// </summary>
    public TOutput Output { get; }

    /// <summary>
    /// Gets the publications that continue or compensate the saga.
    /// </summary>
    public IReadOnlyList<SagaChoreographyPublication> Publications { get; }

    object? ISagaChoreographyStepResult.Output => Output;
}
