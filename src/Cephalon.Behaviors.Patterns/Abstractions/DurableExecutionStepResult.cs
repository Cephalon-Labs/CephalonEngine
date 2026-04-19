using Cephalon.Abstractions.EventSourcing;
using Cephalon.Abstractions.Execution;

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
    /// <param name="pendingTimers">The durable timers that should remain pending after the step succeeds.</param>
    /// <param name="pendingSignals">The durable signals that should remain pending after the step succeeds.</param>
    public DurableExecutionStepResult(
        TOutput? output = default,
        IReadOnlyList<IDomainEvent>? events = null,
        bool isCompleted = false,
        IReadOnlyList<DurableExecutionPendingTimer>? pendingTimers = null,
        IReadOnlyList<DurableExecutionPendingSignal>? pendingSignals = null)
    {
        Output = output;
        Events = events is null
            ? []
            : events.ToArray();
        IsCompleted = isCompleted;
        PendingTimers = NormalizePendingTimers(pendingTimers);
        PendingSignals = NormalizePendingSignals(pendingSignals);
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

    /// <summary>
    /// Gets the durable timers that should remain pending after the step succeeds.
    /// </summary>
    public IReadOnlyList<DurableExecutionPendingTimer> PendingTimers { get; }

    /// <summary>
    /// Gets the durable signals that should remain pending after the step succeeds.
    /// </summary>
    public IReadOnlyList<DurableExecutionPendingSignal> PendingSignals { get; }

    private static DurableExecutionPendingTimer[] NormalizePendingTimers(
        IReadOnlyList<DurableExecutionPendingTimer>? pendingTimers)
    {
        if (pendingTimers is null || pendingTimers.Count == 0)
        {
            return [];
        }

        if (pendingTimers.Any(static timer => timer is null))
        {
            throw new ArgumentException("Pending timers cannot contain null entries.", nameof(pendingTimers));
        }

        var duplicateTimerId = pendingTimers
            .GroupBy(static timer => timer.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(static group => group.Count() > 1)?
            .Key;
        if (!string.IsNullOrWhiteSpace(duplicateTimerId))
        {
            throw new ArgumentException(
                $"Pending timer id '{duplicateTimerId}' was declared more than once.",
                nameof(pendingTimers));
        }

        return pendingTimers
            .OrderBy(static timer => timer.DueAtUtc)
            .ThenBy(static timer => timer.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static DurableExecutionPendingSignal[] NormalizePendingSignals(
        IReadOnlyList<DurableExecutionPendingSignal>? pendingSignals)
    {
        if (pendingSignals is null || pendingSignals.Count == 0)
        {
            return [];
        }

        if (pendingSignals.Any(static signal => signal is null))
        {
            throw new ArgumentException("Pending signals cannot contain null entries.", nameof(pendingSignals));
        }

        var duplicateSignalId = pendingSignals
            .GroupBy(static signal => signal.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(static group => group.Count() > 1)?
            .Key;
        if (!string.IsNullOrWhiteSpace(duplicateSignalId))
        {
            throw new ArgumentException(
                $"Pending signal id '{duplicateSignalId}' was declared more than once.",
                nameof(pendingSignals));
        }

        return pendingSignals
            .OrderBy(static signal => signal.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
