using System.Text.Json;
using Cephalon.Abstractions.EventSourcing;
using MongoDB.Driver;

namespace Cephalon.EventSourcing.MongoDB.Services;

internal sealed class MongoDbSnapshotStore(IMongoCollection<MongoDbEventSnapshotEntry> collection) : ISnapshotStore
{
    private volatile bool _indexesCreated;

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

        await EnsureIndexesAsync(cancellationToken).ConfigureAwait(false);

        var normalizedStreamId = streamId.Trim();
        var stateType = CreateStateTypeKey<TState>();
        var filter = Builders<MongoDbEventSnapshotEntry>.Filter.And(
            Builders<MongoDbEventSnapshotEntry>.Filter.Eq(entry => entry.StreamId, normalizedStreamId),
            Builders<MongoDbEventSnapshotEntry>.Filter.Eq(entry => entry.StateType, stateType),
            Builders<MongoDbEventSnapshotEntry>.Filter.Lte(entry => entry.StreamVersion, version));
        var update = Builders<MongoDbEventSnapshotEntry>.Update
            .SetOnInsert(entry => entry.StreamId, normalizedStreamId)
            .SetOnInsert(entry => entry.StateType, stateType)
            .Set(entry => entry.StreamVersion, version)
            .Set(entry => entry.Payload, JsonSerializer.Serialize(state))
            .Set(entry => entry.SavedAtUtc, DateTime.UtcNow);

        try
        {
            await collection
                .UpdateOneAsync(
                    filter,
                    update,
                    new UpdateOptions { IsUpsert = true },
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Code == 11000)
        {
            throw await CreateOlderSnapshotExceptionAsync(
                    normalizedStreamId,
                    stateType,
                    version,
                    ex,
                    cancellationToken)
                .ConfigureAwait(false);
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

        await EnsureIndexesAsync(cancellationToken).ConfigureAwait(false);

        var normalizedStreamId = streamId.Trim();
        var stateType = CreateStateTypeKey<TState>();
        var filter = Builders<MongoDbEventSnapshotEntry>.Filter.And(
            Builders<MongoDbEventSnapshotEntry>.Filter.Eq(entry => entry.StreamId, normalizedStreamId),
            Builders<MongoDbEventSnapshotEntry>.Filter.Eq(entry => entry.StateType, stateType));

        var snapshot = await collection
            .Find(filter)
            .SortByDescending(static entry => entry.StreamVersion)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (snapshot is null)
        {
            return (default, -1);
        }

        var state = JsonSerializer.Deserialize<TState>(snapshot.Payload);
        return (state, snapshot.StreamVersion);
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

    private async Task<InvalidOperationException> CreateOlderSnapshotExceptionAsync(
        string streamId,
        string stateType,
        long requestedVersion,
        Exception innerException,
        CancellationToken cancellationToken)
    {
        var currentVersion = await GetCurrentVersionAsync(streamId, stateType, cancellationToken).ConfigureAwait(false);
        return new InvalidOperationException(
            $"Snapshot for stream '{streamId}' already represents version {currentVersion}, so version {requestedVersion} cannot replace it.",
            innerException);
    }

    private async Task<long> GetCurrentVersionAsync(
        string streamId,
        string stateType,
        CancellationToken cancellationToken)
    {
        var filter = Builders<MongoDbEventSnapshotEntry>.Filter.And(
            Builders<MongoDbEventSnapshotEntry>.Filter.Eq(entry => entry.StreamId, streamId),
            Builders<MongoDbEventSnapshotEntry>.Filter.Eq(entry => entry.StateType, stateType));

        var snapshot = await collection
            .Find(filter)
            .SortByDescending(static entry => entry.StreamVersion)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return snapshot?.StreamVersion ?? -1;
    }

    private async Task EnsureIndexesAsync(CancellationToken cancellationToken)
    {
        if (_indexesCreated)
        {
            return;
        }

        await MongoDbEventSourcingConfiguration.EnsureSnapshotIndexesAsync(collection, cancellationToken).ConfigureAwait(false);
        _indexesCreated = true;
    }
}
