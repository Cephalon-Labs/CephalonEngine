using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Cephalon.Abstractions.EventSourcing;
using Cephalon.EventSourcing.Services;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace Cephalon.EventSourcing.Qdrant;

/// <summary>
/// Qdrant-backed implementation of <see cref="IEventStore" /> using vector-collection points
/// with 1-dimensional dummy vectors and payload-field storage.
/// Point IDs are derived via SHA-256 of <c>{streamId}:{streamVersion}</c> for uniqueness.
/// Optimistic concurrency is enforced at the application layer via a pre-append version check.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Known limitation</strong>: The version-read followed by upsert is not fully atomic.
/// Two concurrent writers that both observe the same <c>expectedVersion</c> will both proceed
/// to issue their upsert statements. The compound point-ID hash provides a structural safeguard,
/// but optimistic concurrency is primarily enforced at the application layer, consistent with
/// the ClickHouse and OpenSearch provider approaches.
/// </para>
/// </remarks>
public sealed class QdrantEventStore : IEventStore, IDisposable
{
    private readonly QdrantClient _client;
    private readonly string _collectionName;
    private readonly IEventTypeRegistry _eventTypes;
    private volatile bool _collectionEnsured;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    /// <summary>
    /// Initializes a new instance of the <see cref="QdrantEventStore" /> class.
    /// </summary>
    /// <param name="client">The Qdrant client used to manage collections and points.</param>
    /// <param name="collectionName">The Qdrant collection name used to persist event stream points.</param>
    public QdrantEventStore(QdrantClient client, string collectionName)
        : this(client, collectionName, EventTypeRegistry.Empty)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="QdrantEventStore" /> class.
    /// </summary>
    /// <param name="client">The Qdrant client used to manage collections and points.</param>
    /// <param name="collectionName">The Qdrant collection name used to persist event stream points.</param>
    /// <param name="eventTypes">The closed event-type registry used to serialize and rehydrate domain events.</param>
    public QdrantEventStore(
        QdrantClient client,
        string collectionName,
        IEventTypeRegistry eventTypes)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(collectionName);
        ArgumentNullException.ThrowIfNull(eventTypes);
        _client = client;
        _collectionName = collectionName;
        _eventTypes = eventTypes;
    }

    /// <inheritdoc />
    public async Task<long> GetVersionAsync(string streamId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(streamId))
        {
            throw new ArgumentException("Stream id is required.", nameof(streamId));
        }

        await EnsureCollectionAsync(cancellationToken).ConfigureAwait(false);
        return await GetVersionInternalAsync(streamId.Trim(), cancellationToken).ConfigureAwait(false);
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

        await EnsureCollectionAsync(cancellationToken).ConfigureAwait(false);

        var normalizedStreamId = streamId.Trim();
        var actualVersion = await GetVersionInternalAsync(normalizedStreamId, cancellationToken).ConfigureAwait(false);

        if (actualVersion != expectedVersion)
        {
            throw new EventStreamConcurrencyException(normalizedStreamId, expectedVersion, actualVersion);
        }

        var appendedAtUtc = DateTime.UtcNow;
        var nextVersion = expectedVersion;
        var points = new List<PointStruct>(events.Count);

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

            var eventType = _eventTypes.GetName(evt);
            var payload = _eventTypes.Serialize(evt);
            var pointId = DeriveGuid($"{normalizedStreamId}:{evt.StreamVersion}");

            var point = new PointStruct
            {
                Id = pointId,
                Vectors = new float[] { 0.0f }
            };

            point.Payload["stream_id"] = normalizedStreamId;
            point.Payload["stream_version"] = evt.StreamVersion;
            point.Payload["event_type"] = eventType;
            point.Payload["payload"] = payload;
            point.Payload["occurred_at_utc"] = evt.OccurredAtUtc.ToString("O");
            point.Payload["appended_at_utc"] = appendedAtUtc.ToString("O");

            points.Add(point);
        }

        await _client.UpsertAsync(_collectionName, points, cancellationToken: cancellationToken).ConfigureAwait(false);
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

        await EnsureCollectionAsync(cancellationToken).ConfigureAwait(false);

        var normalizedStreamId = streamId.Trim();

        var filter = new Filter();
        filter.Must.Add(new Condition
        {
            Field = new FieldCondition
            {
                Key = "stream_id",
                Match = new Match { Keyword = normalizedStreamId }
            }
        });
        filter.Must.Add(new Condition
        {
            Field = new FieldCondition
            {
                Key = "stream_version",
                Range = new global::Qdrant.Client.Grpc.Range { Gte = (double)fromVersion }
            }
        });

        var scrollResult = await _client.ScrollAsync(
            _collectionName,
            filter: filter,
            payloadSelector: true,
            limit: 10000,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        var sortedPoints = scrollResult.Result
            .OrderBy(p => p.Payload["stream_version"].IntegerValue)
            .ToList();

        foreach (var p in sortedPoints)
        {
            var eventTypeName = p.Payload["event_type"].StringValue;
            var payloadJson = p.Payload["payload"].StringValue;
            yield return _eventTypes.Deserialize(eventTypeName, payloadJson);
        }
    }

    private async Task<long> GetVersionInternalAsync(string normalizedStreamId, CancellationToken cancellationToken)
    {
        var filter = new Filter();
        filter.Must.Add(new Condition
        {
            Field = new FieldCondition
            {
                Key = "stream_id",
                Match = new Match { Keyword = normalizedStreamId }
            }
        });

        var scrollResult = await _client.ScrollAsync(
            _collectionName,
            filter: filter,
            payloadSelector: true,
            limit: 10000,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (scrollResult.Result.Count == 0)
        {
            return -1;
        }

        return scrollResult.Result.Max(p => p.Payload["stream_version"].IntegerValue);
    }

    private async Task EnsureCollectionAsync(CancellationToken cancellationToken)
    {
        if (_collectionEnsured)
        {
            return;
        }

        await _initLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_collectionEnsured)
            {
                return;
            }

            var exists = await _client.CollectionExistsAsync(_collectionName, cancellationToken).ConfigureAwait(false);
            if (!exists)
            {
                await _client.CreateCollectionAsync(
                    _collectionName,
                    new VectorParams { Size = 1, Distance = Distance.Cosine },
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                await _client.CreatePayloadIndexAsync(
                    _collectionName,
                    "stream_id",
                    PayloadSchemaType.Keyword,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                await _client.CreatePayloadIndexAsync(
                    _collectionName,
                    "stream_version",
                    PayloadSchemaType.Integer,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            _collectionEnsured = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _initLock.Dispose();
    }

    private static Guid DeriveGuid(string input)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return new Guid(hash[..16]);
    }
}
