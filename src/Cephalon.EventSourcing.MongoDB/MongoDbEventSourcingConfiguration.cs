using MongoDB.Driver;

namespace Cephalon.EventSourcing.MongoDB;

/// <summary>
/// Applies the Cephalon event-store schema to a MongoDB collection.
/// </summary>
public static class MongoDbEventSourcingConfiguration
{
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
}
