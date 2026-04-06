using System.Runtime.CompilerServices;
using System.Text.Json;
using Cephalon.Abstractions.EventSourcing;
using StackExchange.Redis;

namespace Cephalon.EventSourcing.Redis;

/// <summary>
/// Redis Streams-backed implementation of <see cref="IEventStore" /> that appends domain events as stream entries.
/// </summary>
/// <remarks>
/// <para>
/// Each logical event stream is stored as a Redis Stream at key <c>{keyPrefix}stream:{streamId}</c>.
/// Every entry in the stream carries the fields <c>StreamVersion</c>, <c>EventType</c>, <c>Payload</c>,
/// <c>OccurredAtUtc</c>, and <c>AppendedAtUtc</c>.
/// </para>
/// <para>
/// <strong>Concurrency semantics:</strong> this provider performs an optimistic check — it reads the current
/// stream version before appending and throws <see cref="EventStreamConcurrencyException" /> if the version
/// does not match. There is no atomic test-and-set; a narrow concurrent race between the version read and the
/// subsequent <c>XADD</c> calls is possible. This is a known limitation of this slice. For workloads requiring
/// stronger guarantees, consider pairing with a Lua script or Redis transactions (MULTI/EXEC) in a future slice.
/// </para>
/// </remarks>
public sealed class RedisEventStore : IEventStore
{
    private readonly IConnectionMultiplexer _multiplexer;
    private readonly string _keyPrefix;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisEventStore" /> class.
    /// </summary>
    /// <param name="multiplexer">The Redis connection multiplexer.</param>
    /// <param name="keyPrefix">The key prefix applied to all stream keys (e.g. <c>"cephalon:"</c>).</param>
    public RedisEventStore(IConnectionMultiplexer multiplexer, string keyPrefix = "cephalon:")
    {
        ArgumentNullException.ThrowIfNull(multiplexer);
        ArgumentException.ThrowIfNullOrWhiteSpace(keyPrefix);
        _multiplexer = multiplexer;
        _keyPrefix = keyPrefix;
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

        var normalizedStreamId = streamId.Trim();
        var actualVersion = await GetVersionAsync(normalizedStreamId, cancellationToken).ConfigureAwait(false);
        if (actualVersion != expectedVersion)
        {
            throw new EventStreamConcurrencyException(normalizedStreamId, expectedVersion, actualVersion);
        }

        var streamKey = RedisEventSourcingConfiguration.StreamKey(_keyPrefix, normalizedStreamId);
        var db = _multiplexer.GetDatabase();
        var nextVersion = expectedVersion;
        var appendedAtUtc = DateTime.UtcNow.ToString("O");

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

            var eventType = evt.GetType().AssemblyQualifiedName
                ?? throw new InvalidOperationException(
                    $"The event type '{evt.GetType().FullName}' must expose an assembly-qualified name.");

            var payload = JsonSerializer.Serialize(evt, evt.GetType());

            var fields = new NameValueEntry[]
            {
                new("StreamVersion", nextVersion),
                new("EventType", eventType),
                new("Payload", payload),
                new("OccurredAtUtc", evt.OccurredAtUtc.ToString("O")),
                new("AppendedAtUtc", appendedAtUtc)
            };

            await db.StreamAddAsync(streamKey, fields).ConfigureAwait(false);
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

        var normalizedStreamId = streamId.Trim();
        var streamKey = RedisEventSourcingConfiguration.StreamKey(_keyPrefix, normalizedStreamId);
        var db = _multiplexer.GetDatabase();

        var entries = await db.StreamRangeAsync(streamKey, "-", "+").ConfigureAwait(false);

        foreach (var entry in entries)
        {
            var streamVersion = ParseLong(entry, "StreamVersion");
            if (streamVersion < fromVersion)
            {
                continue;
            }

            var eventTypeStr = GetField(entry, "EventType");
            var payload = GetField(entry, "Payload");

            var eventType = Type.GetType(eventTypeStr, throwOnError: false);
            if (eventType is null)
            {
                throw new InvalidOperationException(
                    $"The CLR type '{eventTypeStr}' could not be resolved while reading stream '{normalizedStreamId}'.");
            }

            var evt = JsonSerializer.Deserialize(payload, eventType) as IDomainEvent;
            if (evt is null)
            {
                throw new InvalidOperationException(
                    $"The payload for event type '{eventTypeStr}' in stream '{normalizedStreamId}' could not be deserialized as an IDomainEvent.");
            }

            yield return evt;
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

        var normalizedStreamId = streamId.Trim();
        var streamKey = RedisEventSourcingConfiguration.StreamKey(_keyPrefix, normalizedStreamId);
        var db = _multiplexer.GetDatabase();

        // Read the last entry in descending order to get the current StreamVersion.
        var entries = await db.StreamRangeAsync(streamKey, "-", "+", count: 1, messageOrder: Order.Descending)
            .ConfigureAwait(false);

        if (entries.Length == 0)
        {
            return -1;
        }

        return ParseLong(entries[0], "StreamVersion");
    }

    private static string GetField(StreamEntry entry, string fieldName)
    {
        foreach (var nameValue in entry.Values)
        {
            if (nameValue.Name == fieldName)
            {
                return nameValue.Value.ToString();
            }
        }

        throw new InvalidOperationException(
            $"Redis Stream entry '{entry.Id}' is missing the required field '{fieldName}'.");
    }

    private static long ParseLong(StreamEntry entry, string fieldName)
    {
        var value = GetField(entry, fieldName);
        if (!long.TryParse(value, out var result))
        {
            throw new InvalidOperationException(
                $"Redis Stream entry '{entry.Id}' field '{fieldName}' value '{value}' could not be parsed as a long integer.");
        }

        return result;
    }
}
