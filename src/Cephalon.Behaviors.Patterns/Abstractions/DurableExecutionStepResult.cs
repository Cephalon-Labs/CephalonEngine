using Cephalon.Abstractions.EventSourcing;

namespace Cephalon.Behaviors.Patterns.Abstractions;

/// <summary>
/// Describes the next durable-execution outcome after one workflow step runs.
/// </summary>
/// <typeparam name="TOutput">The local output returned to the caller when one is available.</typeparam>
public sealed class DurableExecutionStepResult<TOutput>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DurableExecutionStepResult{TOutput}" /> class.
    /// </summary>
    /// <param name="output">The local output to return to the caller.</param>
    /// <param name="events">The ordered domain events to append durably after a successful step.</param>
    /// <param name="isCompleted">Marks the workflow as complete after this step.</param>
    public DurableExecutionStepResult(
        TOutput? output = default,
        IReadOnlyList<IDomainEvent>? events = null,
        bool isCompleted = false)
    {
        Output = output;
        Events = events is null
            ? []
            : events.ToArray();
        IsCompleted = isCompleted;
    }

    /// <summary>
    /// Gets the local output returned to the caller when one is available.
    /// </summary>
    public TOutput? Output { get; }

    /// <summary>
    /// Gets the ordered domain events to append durably after a successful step.
    /// </summary>
    public IReadOnlyList<IDomainEvent> Events { get; }

    /// <summary>
    /// Gets a value indicating whether the workflow should be considered complete after this step.
    /// </summary>
    public bool IsCompleted { get; }
}
