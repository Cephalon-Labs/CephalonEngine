using System.Text.Json;
using Cephalon.Abstractions.EventSourcing;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.KeyValueStore;

namespace Cephalon.EventSourcing.Nats.Services;

internal sealed class NatsSnapshotStore(
    INatsConnection nats,
    string bucketName) : ISnapshotStore
{
    private const int MaxSaveAttempts = 5;

    public async Task SaveSnapshotAsync<TState>(
        string streamId,
        long version,
        TState state,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(streamId))
        {
            throw new ArgumentException("Stream id is required.", nameof(streamId));
        }

        if (version < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version), version, "Snapshot version cannot be negative.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var normalizedStreamId = streamId.Trim();
        var stateType = NatsEventSourcingConfiguration.CreateStateTypeKey<TState>();
        var snapshotKey = NatsEventSourcingConfiguration.SnapshotKey(normalizedStreamId, stateType);
        var entry = new NatsSnapshotEntry
        {
            StreamId = normalizedStreamId,
            StateType = stateType,
            StreamVersion = version,
            Payload = JsonSerializer.Serialize(state),
            SavedAtUtc = DateTime.UtcNow
        };
        var payload = JsonSerializer.SerializeToUtf8Bytes(entry);
        var kv = await GetOrCreateBucketAsync(cancellationToken).ConfigureAwait(false);

        for (var attempt = 0; attempt < MaxSaveAttempts; attempt++)
        {
            var current = await kv.TryGetEntryAsync<byte[]>(
                    snapshotKey,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (!current.Success || current.Value.Value is null)
            {
                try
                {
                    await kv.CreateAsync(snapshotKey, payload, cancellationToken: cancellationToken).ConfigureAwait(false);
                    return;
                }
                catch (NatsKVCreateException) when (attempt + 1 < MaxSaveAttempts)
                {
                    continue;
                }
            }
            else
            {
                var currentEntry = DeserializeEntry(current.Value.Value, snapshotKey);
                if (currentEntry.StreamVersion > version)
                {
                    ThrowStaleSnapshot(normalizedStreamId, currentEntry.StreamVersion, version);
                }

                try
                {
                    await kv.UpdateAsync(
                            snapshotKey,
                            payload,
                            current.Value.Revision,
                            cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                    return;
                }
                catch (NatsKVException) when (attempt + 1 < MaxSaveAttempts)
                {
                    continue;
                }
            }
        }

        throw new InvalidOperationException(
            $"Snapshot for stream '{normalizedStreamId}' could not be saved after {MaxSaveAttempts} NATS KV revision attempts.");
    }

    public async Task<(TState? State, long Version)> LoadSnapshotAsync<TState>(
        string streamId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(streamId))
        {
            throw new ArgumentException("Stream id is required.", nameof(streamId));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var normalizedStreamId = streamId.Trim();
        var stateType = NatsEventSourcingConfiguration.CreateStateTypeKey<TState>();
        var snapshotKey = NatsEventSourcingConfiguration.SnapshotKey(normalizedStreamId, stateType);
        var kv = await GetOrCreateBucketAsync(cancellationToken).ConfigureAwait(false);
        var current = await kv.TryGetEntryAsync<byte[]>(
                snapshotKey,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        if (!current.Success || current.Value.Value is null)
        {
            return (default, -1);
        }

        var entry = DeserializeEntry(current.Value.Value, snapshotKey);
        var state = JsonSerializer.Deserialize<TState>(entry.Payload);
        return (state, entry.StreamVersion);
    }

    private async Task<INatsKVStore> GetOrCreateBucketAsync(CancellationToken cancellationToken)
    {
        var js = new NatsJSContext(nats);
        var kvCtx = new NatsKVContext(js);
        return await kvCtx.CreateOrUpdateStoreAsync(new NatsKVConfig(bucketName), cancellationToken).ConfigureAwait(false);
    }

    private static NatsSnapshotEntry DeserializeEntry(byte[] payload, string snapshotKey)
    {
        return JsonSerializer.Deserialize<NatsSnapshotEntry>(payload) ??
            throw new InvalidOperationException($"NATS snapshot '{snapshotKey}' could not be deserialized.");
    }

    private static void ThrowStaleSnapshot(string streamId, long existingVersion, long requestedVersion)
    {
        throw new InvalidOperationException(
            $"Snapshot for stream '{streamId}' already represents version {existingVersion}, so version {requestedVersion} cannot replace it.");
    }

    private sealed class NatsSnapshotEntry
    {
        public string StreamId { get; init; } = string.Empty;

        public string StateType { get; init; } = string.Empty;

        public long StreamVersion { get; init; }

        public string Payload { get; init; } = string.Empty;

        public DateTime SavedAtUtc { get; init; }
    }
}
