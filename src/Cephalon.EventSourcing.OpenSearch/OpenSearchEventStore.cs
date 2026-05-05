using System.Runtime.CompilerServices;
using Cephalon.Abstractions.EventSourcing;
using Cephalon.EventSourcing.Services;
using OpenSearch.Client;
using OpenSearch.Net;

namespace Cephalon.EventSourcing.OpenSearch;

/// <summary>
/// OpenSearch-backed implementation of <see cref="IEventStore"/> using a search index for event streams.
/// </summary>
public sealed class OpenSearchEventStore : IEventStore
{
    private readonly OpenSearchClient _client;
    private readonly IEventTypeRegistry _eventTypes;
    private readonly string _indexName;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenSearchEventStore" /> class.
    /// </summary>
    /// <param name="client">The OpenSearch client used to index and read event documents.</param>
    /// <param name="indexName">The target OpenSearch index name for event stream documents.</param>
    public OpenSearchEventStore(OpenSearchClient client, string indexName)
        : this(client, EventTypeRegistry.Empty, indexName)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenSearchEventStore" /> class.
    /// </summary>
    /// <param name="client">The OpenSearch client used to index and read event documents.</param>
    /// <param name="eventTypes">The closed event-type registry used to serialize and rehydrate domain events.</param>
    /// <param name="indexName">The target OpenSearch index name for event stream documents.</param>
    public OpenSearchEventStore(
        OpenSearchClient client,
        IEventTypeRegistry eventTypes,
        string indexName)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(eventTypes);
        ArgumentException.ThrowIfNullOrWhiteSpace(indexName);
        _client = client;
        _eventTypes = eventTypes;
        _indexName = indexName;
    }

    /// <inheritdoc />
    public async Task AppendAsync(
        string streamId,
        IReadOnlyCollection<IDomainEvent> events,
        long expectedVersion,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(streamId)) throw new ArgumentException("Stream id is required.", nameof(streamId));
        ArgumentNullException.ThrowIfNull(events);
        if (events.Count == 0) return;

        var normalizedStreamId = streamId.Trim();
        var actualVersion = await GetVersionAsync(normalizedStreamId, cancellationToken).ConfigureAwait(false);
        if (actualVersion != expectedVersion)
            throw new EventStreamConcurrencyException(normalizedStreamId, expectedVersion, actualVersion);

        var nextVersion = expectedVersion;
        var appendedAtUtc = DateTime.UtcNow;

        foreach (var evt in events)
        {
            ArgumentNullException.ThrowIfNull(evt);
            if (!string.Equals(evt.StreamId, normalizedStreamId, StringComparison.Ordinal))
                throw new InvalidOperationException($"Domain event stream id '{evt.StreamId}' did not match append target '{normalizedStreamId}'.");

            nextVersion++;
            if (evt.StreamVersion != nextVersion)
                throw new InvalidOperationException($"Domain event '{evt.GetType().FullName}' declared stream version {evt.StreamVersion}, but the append expected version {nextVersion}.");

            var entry = new OpenSearchEventEntry
            {
                StreamId = normalizedStreamId,
                StreamVersion = evt.StreamVersion,
                EventType = _eventTypes.GetName(evt),
                Payload = _eventTypes.Serialize(evt),
                OccurredAtUtc = evt.OccurredAtUtc,
                AppendedAtUtc = appendedAtUtc
            };

            var docId = $"{normalizedStreamId}#{evt.StreamVersion}";
            var response = await _client.IndexAsync(entry, idx => idx
                    .Index(_indexName).Id(docId).OpType(OpType.Create),
                cancellationToken).ConfigureAwait(false);

            if (!response.IsValid)
            {
                if (response.ServerError?.Status == 409)
                    throw new EventStreamConcurrencyException(
                        normalizedStreamId, expectedVersion,
                        await GetVersionAsync(normalizedStreamId, cancellationToken).ConfigureAwait(false));
                throw new InvalidOperationException(
                    $"OpenSearch event store append failed for stream '{normalizedStreamId}' v{evt.StreamVersion}: {response.DebugInformation}");
            }
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<IDomainEvent> ReadStreamAsync(
        string streamId,
        long fromVersion = 0,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(streamId)) throw new ArgumentException("Stream id is required.", nameof(streamId));
        var normalizedStreamId = streamId.Trim();

        var response = await _client.SearchAsync<OpenSearchEventEntry>(s => s
            .Index(_indexName)
            .Size(10_000)
            .Query(q => q.Bool(b => b.Must(
                m => m.Term("stream_id", normalizedStreamId),
                m => m.Range(r => r.Field("stream_version").GreaterThanOrEquals(fromVersion)))))
            .Sort(ss => ss.Ascending("stream_version")),
            cancellationToken).ConfigureAwait(false);

        if (!response.IsValid)
            throw new InvalidOperationException($"OpenSearch event store read failed for stream '{normalizedStreamId}': {response.DebugInformation}");

        foreach (var hit in response.Hits)
        {
            if (hit.Source is null) continue;
            yield return _eventTypes.Deserialize(hit.Source.EventType, hit.Source.Payload);
        }
    }

    /// <inheritdoc />
    public async Task<long> GetVersionAsync(string streamId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(streamId)) throw new ArgumentException("Stream id is required.", nameof(streamId));
        var normalizedStreamId = streamId.Trim();

        var response = await _client.SearchAsync<OpenSearchEventEntry>(s => s
            .Index(_indexName)
            .Size(1)
            .Query(q => q.Term("stream_id", normalizedStreamId))
            .Sort(ss => ss.Descending("stream_version")),
            cancellationToken).ConfigureAwait(false);

        if (!response.IsValid)
            throw new InvalidOperationException($"OpenSearch event store version query failed for stream '{normalizedStreamId}': {response.DebugInformation}");

        return response.Hits.Count == 0 ? -1 : response.Hits.First().Source!.StreamVersion;
    }
}
