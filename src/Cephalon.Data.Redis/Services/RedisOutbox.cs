using System.Text.Json;
using Cephalon.Abstractions.Data;
using Cephalon.Data.Redis.Configuration;
using StackExchange.Redis;

namespace Cephalon.Data.Redis.Services;

/// <summary>
/// Redis-backed outbox implementation that stages messages for durable delivery using a Hash and Sorted Set.
/// </summary>
internal sealed class RedisOutbox : IOutbox
{
    private readonly IConnectionMultiplexer _multiplexer;
    private readonly RedisDataOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisOutbox" /> class.
    /// </summary>
    /// <param name="multiplexer">The Redis connection multiplexer.</param>
    /// <param name="options">The Redis data options controlling key prefix and connection settings.</param>
    public RedisOutbox(IConnectionMultiplexer multiplexer, RedisDataOptions options)
    {
        ArgumentNullException.ThrowIfNull(multiplexer);
        ArgumentNullException.ThrowIfNull(options);
        _multiplexer = multiplexer;
        _options = options;
    }

    /// <inheritdoc />
    public async ValueTask EnqueueAsync(
        OutboxMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var db = _multiplexer.GetDatabase();
        var hashKey = OutboxHashKey(message.Id);
        var pendingKey = PendingSetKey();
        var score = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var transaction = db.CreateTransaction();

        // Condition: only proceed if the hash does not already exist (idempotent staging).
        transaction.AddCondition(Condition.KeyNotExists(hashKey));

        var hashFields = new HashEntry[]
        {
            new("MessageId", message.Id),
            new("ChannelId", message.ChannelId),
            new("MessageType", message.MessageType),
            new("Payload", message.Payload),
            new("ContentType", message.ContentType ?? string.Empty),
            new("CorrelationId", message.CorrelationId ?? string.Empty),
            new("TenantId", message.TenantId ?? string.Empty),
            new("OccurredAtUtc", message.OccurredAtUtc.UtcDateTime.ToString("O")),
            new("CreatedAtUtc", DateTime.UtcNow.ToString("O")),
            new("DispatchedAtUtc", string.Empty),
            new("DispatchAttemptCount", 0),
            new("NextAttemptAtUtc", string.Empty),
            new("HeadersJson", JsonSerializer.Serialize(message.Headers)),
            new("MetadataJson", JsonSerializer.Serialize(message.Metadata))
        };

        _ = transaction.HashSetAsync(hashKey, hashFields);
        _ = transaction.SortedSetAddAsync(pendingKey, message.Id, score, SortedSetWhen.NotExists);

        // If the condition fails (key already exists), Execute returns false silently.
        await transaction.ExecuteAsync().ConfigureAwait(false);
    }

    private string OutboxHashKey(string messageId) =>
        $"{_options.KeyPrefix}outbox:msg:{messageId}";

    private string PendingSetKey() =>
        $"{_options.KeyPrefix}outbox:pending";
}
