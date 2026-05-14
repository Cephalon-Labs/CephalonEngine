using System.Text.Json;
using Cephalon.Abstractions.EventSourcing;
using StackExchange.Redis;

namespace Cephalon.EventSourcing.Redis.Services;

internal sealed class RedisSnapshotStore(
    IConnectionMultiplexer multiplexer,
    string keyPrefix) : ISnapshotStore
{
    private const string SaveSnapshotScript = """
        local current = redis.call('HGET', KEYS[1], 'StreamVersion')
        local requested = tonumber(ARGV[1])

        if current ~= false and tonumber(current) > requested then
            return current
        end

        redis.call('HSET', KEYS[1],
            'StreamId', ARGV[2],
            'StateType', ARGV[3],
            'StreamVersion', ARGV[1],
            'Payload', ARGV[4],
            'SavedAtUtc', ARGV[5])

        return ''
        """;

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
        var stateType = CreateStateTypeKey<TState>();
        var snapshotKey = RedisEventSourcingConfiguration.SnapshotKey(keyPrefix, normalizedStreamId, stateType);
        var payload = JsonSerializer.Serialize(state);
        var db = multiplexer.GetDatabase();
        var staleVersion = await db
            .ScriptEvaluateAsync(
                SaveSnapshotScript,
                [snapshotKey],
                [
                    version,
                    normalizedStreamId,
                    stateType,
                    payload,
                    DateTime.UtcNow.ToString("O")
                ])
            .ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        var existingVersion = staleVersion.ToString();
        if (!string.IsNullOrEmpty(existingVersion))
        {
            throw new InvalidOperationException(
                $"Snapshot for stream '{normalizedStreamId}' already represents version {existingVersion}, so version {version} cannot replace it.");
        }
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
        var stateType = CreateStateTypeKey<TState>();
        var snapshotKey = RedisEventSourcingConfiguration.SnapshotKey(keyPrefix, normalizedStreamId, stateType);
        var db = multiplexer.GetDatabase();
        RedisValue[] fields = ["Payload", "StreamVersion"];
        var values = await db.HashGetAsync(snapshotKey, fields).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        var payload = values[0];
        var versionValue = values[1];
        if (payload.IsNullOrEmpty || versionValue.IsNullOrEmpty)
        {
            return (default, -1);
        }

        if (!long.TryParse(versionValue.ToString(), out var version))
        {
            throw new InvalidOperationException(
                $"Redis snapshot '{snapshotKey}' field 'StreamVersion' value '{versionValue}' could not be parsed as a long integer.");
        }

        var state = JsonSerializer.Deserialize<TState>(payload.ToString());
        return (state, version);
    }

    private static string CreateStateTypeKey<TState>()
    {
        var type = typeof(TState);
        var assemblyName = type.Assembly.GetName().Name;
        var typeName = type.FullName ?? type.Name;

        return string.IsNullOrWhiteSpace(assemblyName)
            ? typeName
            : string.Concat(assemblyName, ":", typeName);
    }
}
