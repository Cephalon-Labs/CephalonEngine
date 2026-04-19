using Cephalon.Abstractions.Behaviors;

namespace Cephalon.Behaviors.Patterns.Abstractions;

/// <summary>
/// Provides a higher-level authoring helper for choreography-based saga steps that react to one event
/// and return staged publications plus optional local output through the shared choreography contracts.
/// </summary>
/// <typeparam name="TEvent">The input event type handled by the reactor.</typeparam>
public interface ISagaEventReactor<TEvent> : IAppBehavior<TEvent, SagaChoreographyStepResult>
{
    /// <summary>
    /// Reacts to one event and returns the choreography publications plus optional local output.
    /// </summary>
    /// <param name="input">The input event for this choreography step.</param>
    /// <param name="context">The ambient behavior context.</param>
    /// <param name="ct">A token that cancels the reaction.</param>
    /// <returns>The publications and optional local output produced by the choreography step.</returns>
    Task<SagaChoreographyStepResult> ReactAsync(
        TEvent input,
        IBehaviorContext context,
        CancellationToken ct = default);

    /// <inheritdoc />
    Task<SagaChoreographyStepResult> IAppBehavior<TEvent, SagaChoreographyStepResult>.HandleAsync(
        TEvent input,
        IBehaviorContext context,
        CancellationToken ct)
        => ReactAsync(input, context, ct);
}

/// <summary>
/// Provides a higher-level authoring helper for choreography-based saga steps that react to one event
/// and return a typed local output alongside staged publications.
/// </summary>
/// <typeparam name="TEvent">The input event type handled by the reactor.</typeparam>
/// <typeparam name="TOutput">The local output type returned by the reactor.</typeparam>
public interface ISagaEventReactor<TEvent, TOutput> : IAppBehavior<TEvent, SagaChoreographyStepResult<TOutput>>
{
    /// <summary>
    /// Reacts to one event and returns typed local output plus choreography publications.
    /// </summary>
    /// <param name="input">The input event for this choreography step.</param>
    /// <param name="context">The ambient behavior context.</param>
    /// <param name="ct">A token that cancels the reaction.</param>
    /// <returns>The typed local output and publications produced by the choreography step.</returns>
    Task<SagaChoreographyStepResult<TOutput>> ReactAsync(
        TEvent input,
        IBehaviorContext context,
        CancellationToken ct = default);

    /// <inheritdoc />
    Task<SagaChoreographyStepResult<TOutput>> IAppBehavior<TEvent, SagaChoreographyStepResult<TOutput>>.HandleAsync(
        TEvent input,
        IBehaviorContext context,
        CancellationToken ct)
        => ReactAsync(input, context, ct);
}
