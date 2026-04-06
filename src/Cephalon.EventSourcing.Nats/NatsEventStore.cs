using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Cephalon.Abstractions.EventSourcing;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.KeyValueStore;

namespace Cephalon.EventSourcing.Nats;

/// <summary>
/// NATS JetStream KV-backed implementation of <see cref="IEventStore" />.
/// Each event is stored as a KV entry with key format <c>{streamId}/{streamVersion:D20}</c>.
/// Zero-padding ensures correct lexicographic ordering of version keys.
/// Optimistic concurrency is enforced via a pre-append version check and <c>CreateAsync</c>
/// which throws <see cref="NatsKVCreateException" /> if the key already exists.
/// </summary>
public sealed class NatsEventStore : IEventStore
{
    private readonly INatsConnection _nats;
    private readonly string _bucketName;

    /// <summary>
    /// Initializes a new instance of the <see cref="NatsEventStore" /> class.
    /// </summary>
    /// <param name="nats">The NATS connection (connection is deferred to first use).</param>
    /// <param name="bucketName">The JetStream KV bucket name used to persist event stream entries.</param>
    public NatsEventStore(INatsConnection nats, string bucketName)
    {
        ArgumentNullException.ThrowIfNull(nats);
        ArgumentException.ThrowIfNullOrWhiteSpace(bucketName);
        _nats = nats;
        _bucketName = bucketName;
    }

    /// <inheritdoc />
    public async Task<long> GetVersionAsync(string streamId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(streamId))
        {
            throw new ArgumentException("Stream id is required.", nameof(streamId));
        }

        var kv = await GetOrCreateBucketAsync(cancellationToken).ConfigureAwait(false);
        return await GetVersionInternalAsync(kv, streamId.Trim(), cancellationToken).ConfigureAwait(false);
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

        var kv = await GetOrCreateBucketAsync(cancellationToken).ConfigureAwait(false);
        var normalizedStreamId = streamId.Trim();

        var actualVersion = await GetVersionInternalAsync(kv, normalizedStreamId, cancellationToken).ConfigureAwait(false);
        if (actualVersion != expectedVersion)
        {
            throw new EventStreamConcurrencyException(normalizedStreamId, expectedVersion, actualVersion);
        }

        var nextVersion = expectedVersion;
        var appendedAtUtc = DateTime.UtcNow;

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
                ?? throw new InvalidOperationException($"The event type '{evt.GetType().FullName}' must expose an assembly-qualified name.");

            var entry = new NatsEventEntry
            {
                StreamId = normalizedStreamId,
                StreamVersion = evt.StreamVersion,
                EventType = eventType,
                Payload = JsonSerializer.Serialize(evt, evt.GetType()),
                OccurredAtUtc = evt.OccurredAtUtc,
                AppendedAtUtc = appendedAtUtc
            };

            var bytes = JsonSerializer.SerializeToUtf8Bytes(entry);
            var key = FormatKey(normalizedStreamId, evt.StreamVersion);

            try
            {
                await kv.CreateAsync(key, bytes, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (NatsKVCreateException)
            {
                // Key already exists — concurrent writer raced ahead, treat as concurrency conflict.
                var rereadVersion = await GetVersionInternalAsync(kv, normalizedStreamId, cancellationToken).ConfigureAwait(false);
                throw new EventStreamConcurrencyException(normalizedStreamId, expectedVersion, rereadVersion);
            }
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

        var kv = await GetOrCreateBucketAsync(cancellationToken).ConfigureAwait(false);
        var normalizedStreamId = streamId.Trim();
        var prefix = $"{normalizedStreamId}/";

        var matchingKeys = new List<string>();
        await foreach (var key in kv.GetKeysAsync(cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            if (key.StartsWith(prefix, StringComparison.Ordinal))
            {
                var versionStr = key[prefix.Length..];
                if (long.TryParse(versionStr, out var version) && version >= fromVersion)
                {
                    matchingKeys.Add(key);
                }
            }
        }

        // Keys are zero-padded so lexicographic order == numeric order.
        matchingKeys.Sort(StringComparer.Ordinal);

        foreach (var key in matchingKeys)
        {
            var entryResult = await kv.TryGetEntryAsync<byte[]>(key, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (!entryResult.Success || entryResult.Value.Value is null)
            {
                continue;
            }

            var natsEntry = JsonSerializer.Deserialize<NatsEventEntry>(entryResult.Value.Value);
            if (natsEntry is null)
            {
                continue;
            }

            var eventType = Type.GetType(natsEntry.EventType, throwOnError: false);
            if (eventType is null)
            {
                throw new InvalidOperationException(
                    $"The CLR type '{natsEntry.EventType}' could not be resolved while reading stream '{normalizedStreamId}'.");
            }

            var evt = JsonSerializer.Deserialize(natsEntry.Payload, eventType) as IDomainEvent;
            if (evt is null)
            {
                throw new InvalidOperationException(
                    $"The payload for event type '{natsEntry.EventType}' in stream '{normalizedStreamId}' could not be deserialized as an IDomainEvent.");
            }

            yield return evt;
        }
    }

    private async Task<INatsKVStore> GetOrCreateBucketAsync(CancellationToken cancellationToken)
    {
        var js = new NatsJSContext(_nats);
        var kvCtx = new NatsKVContext(js);
        return await kvCtx.CreateOrUpdateStoreAsync(new NatsKVConfig(_bucketName), cancellationToken).ConfigureAwait(false);
    }

    private static async Task<long> GetVersionInternalAsync(INatsKVStore kv, string normalizedStreamId, CancellationToken cancellationToken)
    {
        var prefix = $"{normalizedStreamId}/";
        long maxVersion = -1;

        await foreach (var key in kv.GetKeysAsync(cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            if (key.StartsWith(prefix, StringComparison.Ordinal))
            {
                var versionStr = key[prefix.Length..];
                if (long.TryParse(versionStr, out var version) && version > maxVersion)
                {
                    maxVersion = version;
                }
            }
        }

        return maxVersion;
    }

    private static string FormatKey(string streamId, long version)
    {
        return $"{streamId}/{version:D20}";
    }
}
