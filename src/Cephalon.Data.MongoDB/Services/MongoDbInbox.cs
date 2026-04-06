using Cephalon.Abstractions.Data;
using MongoDB.Driver;

namespace Cephalon.Data.MongoDB.Services;

/// <summary>
/// MongoDB-backed inbox implementation that tracks processed messages for idempotent handling.
/// </summary>
internal sealed class MongoDbInbox : IInbox
{
    private readonly IMongoCollection<MongoDbInboxEntry> _collection;
    private volatile bool _indexesCreated;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDbInbox" /> class.
    /// </summary>
    /// <param name="collection">The MongoDB collection used to persist processed message records.</param>
    public MongoDbInbox(IMongoCollection<MongoDbInboxEntry> collection)
    {
        ArgumentNullException.ThrowIfNull(collection);
        _collection = collection;
    }

    /// <inheritdoc />
    public async ValueTask<bool> HasProcessedAsync(
        string messageId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        await EnsureIndexesAsync(cancellationToken).ConfigureAwait(false);

        var filter = Builders<MongoDbInboxEntry>.Filter.Eq(entry => entry.MessageId, messageId.Trim());
        var count = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken).ConfigureAwait(false);
        return count > 0;
    }

    /// <inheritdoc />
    public async ValueTask MarkProcessedAsync(
        InboxMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        await EnsureIndexesAsync(cancellationToken).ConfigureAwait(false);

        var entry = new MongoDbInboxEntry
        {
            MessageId = message.Id,
            ChannelId = message.ChannelId,
            MessageType = message.MessageType,
            CorrelationId = message.CorrelationId,
            TenantId = message.TenantId,
            ReceivedAtUtc = message.ReceivedAtUtc.UtcDateTime,
            ProcessedAtUtc = DateTime.UtcNow
        };

        try
        {
            await _collection.InsertOneAsync(entry, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Code == 11000)
        {
            // Duplicate key — message already recorded as processed, ignore for idempotency.
        }
    }

    private async Task EnsureIndexesAsync(CancellationToken cancellationToken)
    {
        if (_indexesCreated)
        {
            return;
        }

        var uniqueIndex = new CreateIndexModel<MongoDbInboxEntry>(
            Builders<MongoDbInboxEntry>.IndexKeys.Ascending(entry => entry.MessageId),
            new CreateIndexOptions { Unique = true, Name = "ux_inbox_message_id" });

        await _collection.Indexes.CreateOneAsync(uniqueIndex, cancellationToken: cancellationToken).ConfigureAwait(false);
        _indexesCreated = true;
    }
}
