using System.Runtime.CompilerServices;
using System.Text.Json;
using Cephalon.Abstractions.EventSourcing;
using MongoDB.Driver;

namespace Cephalon.EventSourcing.MongoDB;

/// <summary>
/// MongoDB-backed implementation of <see cref="IEventStore" /> using a document collection for event streams.
/// </summary>
public sealed class MongoDbEventStore : IEventStore
{
    private readonly IMongoCollection<MongoDbEventEntry> _collection;
    private volatile bool _indexesCreated;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDbEventStore" /> class.
    /// </summary>
    /// <param name="collection">The MongoDB collection used to persist event entries.</param>
    public MongoDbEventStore(IMongoCollection<MongoDbEventEntry> collection)
    {
        ArgumentNullException.ThrowIfNull(collection);
        _collection = collection;
    }

    /// <inheritdoc />
    public async Task AppendAsync(
        string streamId,
        IReadOnlyCollection<IDomainEvent> events,
        long expectedVersion,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(streamId))
        {
            throw new ArgumentException("Stream id is required.", nameof(streamId));
        }

        ArgumentNullException.ThrowIfNull(events);

        if (events.Count == 0)
        {
            return;
        }

        await EnsureIndexesAsync(cancellationToken).ConfigureAwait(false);

        var normalizedStreamId = streamId.Trim();
        var actualVersion = await GetVersionAsync(normalizedStreamId, cancellationToken).ConfigureAwait(false);
        if (actualVersion != expectedVersion)
        {
            throw new EventStreamConcurrencyException(normalizedStreamId, expectedVersion, actualVersion);
        }

        var nextVersion = expectedVersion;
        var appendedAtUtc = DateTime.UtcNow;
        var entries = new List<MongoDbEventEntry>(events.Count);

        foreach (var evt in events)
        {
            ArgumentNullException.ThrowIfNull(evt);

            if (!string.Equals(evt.StreamId, normalizedStreamId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Domain event stream id '{evt.StreamId}' did not match append target '{normalizedStreamId}'.");
            }

            nextVersion++;
            if (evt.StreamVersion != nextVersion)
            {
                throw new InvalidOperationException(
                    $"Domain event '{evt.GetType().FullName}' declared stream version {evt.StreamVersion}, but the append expected version {nextVersion}.");
            }

            entries.Add(new MongoDbEventEntry
            {
                StreamId = normalizedStreamId,
                StreamVersion = evt.StreamVersion,
                EventType = evt.GetType().AssemblyQualifiedName
                    ?? throw new InvalidOperationException($"The event type '{evt.GetType().FullName}' must expose an assembly-qualified name."),
                Payload = JsonSerializer.Serialize(evt, evt.GetType()),
                OccurredAtUtc = evt.OccurredAtUtc,
                AppendedAtUtc = appendedAtUtc
            });
        }

        try
        {
            await _collection.InsertManyAsync(entries, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (MongoBulkWriteException ex) when (ex.WriteErrors.Any(error => error.Code == 11000))
        {
            // Duplicate key — optimistic concurrency conflict detected via unique index.
            throw new EventStreamConcurrencyException(
                normalizedStreamId,
                expectedVersion,
                await GetVersionAsync(normalizedStreamId, cancellationToken).ConfigureAwait(false));
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<IDomainEvent> ReadStreamAsync(
        string streamId,
        long fromVersion = 0,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(streamId))
        {
            throw new ArgumentException("Stream id is required.", nameof(streamId));
        }

        await EnsureIndexesAsync(cancellationToken).ConfigureAwait(false);

        var normalizedStreamId = streamId.Trim();
        var filter = Builders<MongoDbEventEntry>.Filter.And(
            Builders<MongoDbEventEntry>.Filter.Eq(entry => entry.StreamId, normalizedStreamId),
            Builders<MongoDbEventEntry>.Filter.Gte(entry => entry.StreamVersion, fromVersion));

        var sort = Builders<MongoDbEventEntry>.Sort.Ascending(entry => entry.StreamVersion);

        using var cursor = await _collection.FindAsync(
            filter,
            new FindOptions<MongoDbEventEntry> { Sort = sort },
            cancellationToken).ConfigureAwait(false);

        while (await cursor.MoveNextAsync(cancellationToken).ConfigureAwait(false))
        {
            foreach (var entry in cursor.Current)
            {
                var eventType = Type.GetType(entry.EventType, throwOnError: false);
                if (eventType is null)
                {
                    throw new InvalidOperationException(
                        $"The CLR type '{entry.EventType}' could not be resolved while reading stream '{normalizedStreamId}'.");
                }

                var evt = JsonSerializer.Deserialize(entry.Payload, eventType) as IDomainEvent;
                if (evt is null)
                {
                    throw new InvalidOperationException(
                        $"The payload for event type '{entry.EventType}' in stream '{normalizedStreamId}' could not be deserialized as an IDomainEvent.");
                }

                yield return evt;
            }
        }
    }

    /// <inheritdoc />
    public async Task<long> GetVersionAsync(
        string streamId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(streamId))
        {
            throw new ArgumentException("Stream id is required.", nameof(streamId));
        }

        await EnsureIndexesAsync(cancellationToken).ConfigureAwait(false);

        var normalizedStreamId = streamId.Trim();
        var filter = Builders<MongoDbEventEntry>.Filter.Eq(entry => entry.StreamId, normalizedStreamId);
        var sort = Builders<MongoDbEventEntry>.Sort.Descending(entry => entry.StreamVersion);

        var latestEntry = await _collection
            .Find(filter)
            .Sort(sort)
            .Limit(1)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return latestEntry is null ? -1 : latestEntry.StreamVersion;
    }

    private async Task EnsureIndexesAsync(CancellationToken cancellationToken)
    {
        if (_indexesCreated)
        {
            return;
        }

        await MongoDbEventSourcingConfiguration.EnsureIndexesAsync(_collection, cancellationToken).ConfigureAwait(false);
        _indexesCreated = true;
    }
}
