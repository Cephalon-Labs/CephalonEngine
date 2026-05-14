using MongoDB.Driver;

namespace Cephalon.EventSourcing.MongoDB;

/// <summary>
/// Applies the Cephalon event-store schema to a MongoDB collection.
/// </summary>
public static class MongoDbEventSourcingConfiguration
{
    internal static string SnapshotCollectionName(string collectionName) =>
        string.Concat(collectionName, "_snapshots");

    /// <summary>
    /// Creates the compound unique index on <c>(StreamId, StreamVersion)</c> required by the MongoDB event-store provider.
    /// </summary>
    /// <param name="collection">The MongoDB collection to configure.</param>
    /// <param name="cancellationToken">The token that cancels the operation.</param>
    /// <returns>A task that completes when all indexes have been created.</returns>
    public static async Task EnsureIndexesAsync(
        IMongoCollection<MongoDbEventEntry> collection,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(collection);

        var uniqueStreamVersionIndex = new CreateIndexModel<MongoDbEventEntry>(
            Builders<MongoDbEventEntry>.IndexKeys
                .Ascending(entry => entry.StreamId)
                .Ascending(entry => entry.StreamVersion),
            new CreateIndexOptions { Unique = true, Name = "ux_event_stream_version" });

        var streamIdIndex = new CreateIndexModel<MongoDbEventEntry>(
            Builders<MongoDbEventEntry>.IndexKeys.Ascending(entry => entry.StreamId),
            new CreateIndexOptions { Name = "ix_event_stream_id" });

        var appendedAtIndex = new CreateIndexModel<MongoDbEventEntry>(
            Builders<MongoDbEventEntry>.IndexKeys.Ascending(entry => entry.AppendedAtUtc),
            new CreateIndexOptions { Name = "ix_event_appended_at" });

        await collection.Indexes.CreateManyAsync([uniqueStreamVersionIndex, streamIdIndex, appendedAtIndex], cancellationToken).ConfigureAwait(false);
    }

    internal static async Task EnsureSnapshotIndexesAsync(
        IMongoCollection<MongoDbEventSnapshotEntry> collection,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(collection);

        var uniqueStreamStateIndex = new CreateIndexModel<MongoDbEventSnapshotEntry>(
            Builders<MongoDbEventSnapshotEntry>.IndexKeys
                .Ascending(entry => entry.StreamId)
                .Ascending(entry => entry.StateType),
            new CreateIndexOptions { Unique = true, Name = "ux_event_snapshot_stream_state" });

        var savedAtIndex = new CreateIndexModel<MongoDbEventSnapshotEntry>(
            Builders<MongoDbEventSnapshotEntry>.IndexKeys.Ascending(entry => entry.SavedAtUtc),
            new CreateIndexOptions { Name = "ix_event_snapshot_saved_at" });

        await collection.Indexes.CreateManyAsync([uniqueStreamStateIndex, savedAtIndex], cancellationToken).ConfigureAwait(false);
    }
}
