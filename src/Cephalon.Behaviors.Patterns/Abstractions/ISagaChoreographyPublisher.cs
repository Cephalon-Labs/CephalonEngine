namespace Cephalon.Behaviors.Patterns.Abstractions;

/// <summary>
/// Publishes choreography publications produced by a saga choreography behavior step.
/// </summary>
public interface ISagaChoreographyPublisher
{
    /// <summary>
    /// Publishes one choreography publication through the active choreography handoff path.
    /// </summary>
    /// <param name="publication">The publication to stage.</param>
    /// <param name="cancellationToken">A token that cancels the publication.</param>
    /// <returns>A task that completes when the publication has been accepted.</returns>
    ValueTask PublishAsync(SagaChoreographyPublication publication, CancellationToken cancellationToken = default);
}
