namespace Cephalon.Abstractions.Behaviors;

/// <summary>
/// Provides ambient context to a behavior during its execution.
/// The context exposes reply semantics, metadata, and cancellation.
/// </summary>
/// <remarks>
/// <para>
/// When the behavior topology uses the <c>direct</c> pattern, <see cref="ReplyAsync" />
/// is not supported and will throw <see cref="NotSupportedException" />.
/// Use the behavior's return value to communicate results in the direct pattern.
/// </para>
/// </remarks>
public interface IBehaviorContext
{
    /// <summary>
    /// Gets the stable identifier of the behavior being executed.
    /// </summary>
    string BehaviorId { get; }

    /// <summary>
    /// Gets the correlation identifier for the current execution, or <see langword="null"/> if not provided.
    /// </summary>
    string? CorrelationId { get; }

    /// <summary>
    /// Gets ambient metadata associated with the current execution (e.g. correlation id, tenant id).
    /// </summary>
    IReadOnlyDictionary<string, string> Metadata { get; }

    /// <summary>
    /// Gets the event store for the current behavior context, or <see langword="null"/>
    /// if event sourcing is not configured for this behavior.
    /// </summary>
    Cephalon.Abstractions.EventSourcing.IEventStore? EventStore { get; }

    /// <summary>
    /// Sends a reply message back to the caller through the active transport.
    /// </summary>
    /// <param name="reply">The reply object to send.</param>
    /// <param name="cancellationToken">A token that cancels the reply.</param>
    /// <returns>A task that completes when the reply has been dispatched.</returns>
    /// <exception cref="NotSupportedException">
    /// Thrown when the active behavior topology uses the <c>direct</c> pattern,
    /// which does not support fire-and-forget reply semantics.
    /// </exception>
    Task ReplyAsync(object reply, CancellationToken cancellationToken = default);
}
