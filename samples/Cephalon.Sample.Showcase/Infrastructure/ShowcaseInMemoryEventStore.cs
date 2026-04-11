using Cephalon.Abstractions.EventSourcing;

namespace Cephalon.Sample.Showcase.Infrastructure;

/// <summary>
/// Keeps a sample-only in-memory event stream store so the showcase cart behaviors can execute
/// through the HTTP behavior dispatcher without requiring external infrastructure.
/// </summary>
internal sealed class ShowcaseInMemoryEventStore : IEventStore
{
    private readonly Lock syncRoot = new();
    private readonly Dictionary<string, List<IDomainEvent>> streams = new(StringComparer.Ordinal);

    public IReadOnlyList<ShowcaseEventStreamSnapshot> GetSnapshots()
    {
        lock (syncRoot)
        {
            return streams
                .OrderBy(static pair => pair.Key, StringComparer.Ordinal)
                .Select(static pair => new ShowcaseEventStreamSnapshot(pair.Key, [.. pair.Value]))
                .ToArray();
        }
    }

    public void Reset()
    {
        lock (syncRoot)
        {
            streams.Clear();
        }
    }

    /// <inheritdoc />
    public Task AppendAsync(
        string streamId,
        IReadOnlyCollection<IDomainEvent> events,
        long expectedVersion,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId);
        ArgumentNullException.ThrowIfNull(events);
        cancellationToken.ThrowIfCancellationRequested();

        lock (syncRoot)
        {
            var stream = GetOrCreateStream(streamId);
            var actualVersion = stream.Count == 0 ? -1 : stream[^1].StreamVersion;
            if (actualVersion != expectedVersion)
            {
                throw new EventStreamConcurrencyException(streamId, expectedVersion, actualVersion);
            }

            foreach (var @event in events)
            {
                stream.Add(@event);
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<long> GetVersionAsync(
        string streamId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId);
        cancellationToken.ThrowIfCancellationRequested();

        lock (syncRoot)
        {
            return Task.FromResult(
                streams.TryGetValue(streamId, out var stream) && stream.Count > 0
                    ? stream[^1].StreamVersion
                    : -1L);
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<IDomainEvent> ReadStreamAsync(
        string streamId,
        long fromVersion = 0,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId);

        List<IDomainEvent> snapshot;
        lock (syncRoot)
        {
            snapshot = streams.TryGetValue(streamId, out var stream)
                ? [..stream.Where(static @event => @event.StreamVersion >= 0)]
                : [];
        }

        foreach (var @event in snapshot.Where(@event => @event.StreamVersion >= fromVersion))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return @event;
            await Task.CompletedTask;
        }
    }

    private List<IDomainEvent> GetOrCreateStream(string streamId)
    {
        if (!streams.TryGetValue(streamId, out var stream))
        {
            stream = [];
            streams[streamId] = stream;
        }

        return stream;
    }
}

internal sealed record ShowcaseEventStreamSnapshot(
    string StreamId,
    IReadOnlyList<IDomainEvent> Events)
{
    public int EventCount => Events.Count;

    public long Version => Events.Count == 0 ? -1 : Events[^1].StreamVersion;

    public string? LastEventType => Events.Count == 0 ? null : Events[^1].GetType().Name;

    public DateTimeOffset? LastOccurredAtUtc => Events.Count == 0
        ? null
        : new DateTimeOffset(DateTime.SpecifyKind(Events[^1].OccurredAtUtc, DateTimeKind.Utc));
}
