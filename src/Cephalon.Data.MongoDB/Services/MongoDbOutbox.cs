using System.Text.Json;
using Cephalon.Abstractions.Data;
using MongoDB.Driver;

namespace Cephalon.Data.MongoDB.Services;

/// <summary>
/// MongoDB-backed outbox implementation that stages messages for durable delivery.
/// </summary>
internal sealed class MongoDbOutbox : IOutbox
{
    private readonly IMongoCollection<MongoDbOutboxEntry> _collection;
    private volatile bool _indexesCreated;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDbOutbox" /> class.
    /// </summary>
    /// <param name="collection">The MongoDB collection used to persist outbox messages.</param>
    public MongoDbOutbox(IMongoCollection<MongoDbOutboxEntry> collection)
    {
        ArgumentNullException.ThrowIfNull(collection);
        _collection = collection;
    }

    /// <inheritdoc />
    public async ValueTask EnqueueAsync(
        OutboxMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        await EnsureIndexesAsync(cancellationToken).ConfigureAwait(false);

        var entry = new MongoDbOutboxEntry
        {
            MessageId = message.Id,
            ChannelId = message.ChannelId,
            MessageType = message.MessageType,
            Payload = message.Payload,
            ContentType = message.ContentType,
            CorrelationId = message.CorrelationId,
            TenantId = message.TenantId,
            OccurredAtUtc = message.OccurredAtUtc.UtcDateTime,
            CreatedAtUtc = DateTime.UtcNow,
            DispatchAttemptCount = 0,
            HeadersJson = JsonSerializer.Serialize(message.Headers),
            MetadataJson = JsonSerializer.Serialize(message.Metadata)
        };

        try
        {
            await _collection.InsertOneAsync(entry, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Code == 11000)
        {
            // Duplicate key — message already staged, ignore for idempotency.
        }
    }

    private async Task EnsureIndexesAsync(CancellationToken cancellationToken)
    {
        if (_indexesCreated)
        {
            return;
        }

        var uniqueIndex = new CreateIndexModel<MongoDbOutboxEntry>(
            Builders<MongoDbOutboxEntry>.IndexKeys.Ascending(entry => entry.MessageId),
            new CreateIndexOptions { Unique = true, Name = "ux_outbox_message_id" });

        var dispatchedIndex = new CreateIndexModel<MongoDbOutboxEntry>(
            Builders<MongoDbOutboxEntry>.IndexKeys.Ascending(entry => entry.DispatchedAtUtc),
            new CreateIndexOptions { Name = "ix_outbox_dispatched_at" });

        var eligibilityIndex = new CreateIndexModel<MongoDbOutboxEntry>(
            Builders<MongoDbOutboxEntry>.IndexKeys
                .Ascending(entry => entry.DispatchedAtUtc)
                .Ascending(entry => entry.NextAttemptAtUtc)
                .Ascending(entry => entry.CreatedAtUtc),
            new CreateIndexOptions { Name = "ix_outbox_dispatch_eligibility" });

        await _collection.Indexes.CreateManyAsync([uniqueIndex, dispatchedIndex, eligibilityIndex], cancellationToken).ConfigureAwait(false);
        _indexesCreated = true;
    }
}
