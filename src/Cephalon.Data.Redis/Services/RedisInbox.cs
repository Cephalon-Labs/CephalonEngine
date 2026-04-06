using Cephalon.Abstractions.Data;
using Cephalon.Data.Redis.Configuration;
using StackExchange.Redis;

namespace Cephalon.Data.Redis.Services;

/// <summary>
/// Redis-backed inbox implementation that tracks processed messages for idempotent handling using a Redis Set.
/// </summary>
internal sealed class RedisInbox : IInbox
{
    private readonly IConnectionMultiplexer _multiplexer;
    private readonly RedisDataOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisInbox" /> class.
    /// </summary>
    /// <param name="multiplexer">The Redis connection multiplexer.</param>
    /// <param name="options">The Redis data options controlling key prefix and connection settings.</param>
    public RedisInbox(IConnectionMultiplexer multiplexer, RedisDataOptions options)
    {
        ArgumentNullException.ThrowIfNull(multiplexer);
        ArgumentNullException.ThrowIfNull(options);
        _multiplexer = multiplexer;
        _options = options;
    }

    /// <inheritdoc />
    public async ValueTask<bool> HasProcessedAsync(
        string messageId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        var db = _multiplexer.GetDatabase();
        return await db.SetContainsAsync(InboxReceiptsKey(), messageId.Trim()).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask MarkProcessedAsync(
        InboxMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var db = _multiplexer.GetDatabase();
        // SADD is naturally idempotent — adding an existing member is a no-op.
        await db.SetAddAsync(InboxReceiptsKey(), message.Id).ConfigureAwait(false);
    }

    private string InboxReceiptsKey() =>
        $"{_options.KeyPrefix}inbox:receipts";
}
