using System.Collections.Concurrent;
using Cephalon.Behaviors.Patterns.Abstractions;

namespace Cephalon.Behaviors.Patterns.Publishers;

/// <summary>
/// Stores choreography publications in memory for local development, tests, and diagnostics.
/// </summary>
public sealed class InMemorySagaChoreographyPublisher : ISagaChoreographyPublisher
{
    private readonly ConcurrentQueue<SagaChoreographyPublication> _publishedPublications = new();

    /// <summary>Gets the publications that have been accepted by this publisher.</summary>
    public IReadOnlyList<SagaChoreographyPublication> PublishedPublications => _publishedPublications.ToArray();

    /// <inheritdoc />
    public ValueTask PublishAsync(SagaChoreographyPublication publication, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(publication);

        cancellationToken.ThrowIfCancellationRequested();
        _publishedPublications.Enqueue(publication);
        return ValueTask.CompletedTask;
    }
}
