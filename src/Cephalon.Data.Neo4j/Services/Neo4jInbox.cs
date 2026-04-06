using Cephalon.Abstractions.Data;
using Cephalon.Data.Neo4j.Configuration;
using Neo4j.Driver;

namespace Cephalon.Data.Neo4j.Services;

/// <summary>
/// Neo4j-backed inbox that tracks processed messages as graph nodes.
/// Idempotency is achieved via Cypher <c>MERGE</c> on <c>messageId</c>.
/// </summary>
internal sealed class Neo4jInbox : IInbox
{
    private readonly IDriver _driver;
    private readonly Neo4jDataOptions _options;
    private volatile bool _constraintCreated;

    /// <summary>
    /// Initializes a new instance of the <see cref="Neo4jInbox" /> class.
    /// </summary>
    /// <param name="driver">The Neo4j driver used to open sessions.</param>
    /// <param name="options">The Neo4j data options controlling label naming.</param>
    public Neo4jInbox(IDriver driver, Neo4jDataOptions options)
    {
        ArgumentNullException.ThrowIfNull(driver);
        ArgumentNullException.ThrowIfNull(options);
        _driver = driver;
        _options = options;
    }

    /// <inheritdoc />
    public async ValueTask<bool> HasProcessedAsync(string messageId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        await EnsureConstraintAsync().ConfigureAwait(false);

        var label = $"{_options.LabelPrefix}InboxReceipt";
        var trimmedId = messageId.Trim();
        await using var session = _driver.AsyncSession();
        return await session.ExecuteReadAsync(async tx =>
        {
            var result = await tx.RunAsync(
                $"MATCH (r:{label} {{messageId: $messageId}}) RETURN count(r) AS cnt",
                new { messageId = trimmedId }).ConfigureAwait(false);
            var record = await result.SingleAsync().ConfigureAwait(false);
            return record["cnt"].As<long>() > 0;
        }).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask MarkProcessedAsync(InboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        await EnsureConstraintAsync().ConfigureAwait(false);

        var label = $"{_options.LabelPrefix}InboxReceipt";
        var parameters = new
        {
            messageId = message.Id,
            channelId = message.ChannelId,
            messageType = message.MessageType,
            correlationId = message.CorrelationId ?? string.Empty,
            tenantId = message.TenantId ?? string.Empty,
            receivedAtUtc = message.ReceivedAtUtc.UtcDateTime.ToString("O"),
            processedAtUtc = DateTime.UtcNow.ToString("O")
        };

        await using var session = _driver.AsyncSession();
        await session.ExecuteWriteAsync(async tx =>
        {
            var result = await tx.RunAsync($@"
                MERGE (r:{label} {{messageId: $messageId}})
                ON CREATE SET
                    r.channelId = $channelId,
                    r.messageType = $messageType,
                    r.correlationId = $correlationId,
                    r.tenantId = $tenantId,
                    r.receivedAtUtc = $receivedAtUtc,
                    r.processedAtUtc = $processedAtUtc
                RETURN r.messageId AS id",
                parameters).ConfigureAwait(false);
            _ = await result.ToListAsync().ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    private async Task EnsureConstraintAsync()
    {
        if (_constraintCreated) return;
        var label = $"{_options.LabelPrefix}InboxReceipt";
        await using var session = _driver.AsyncSession();
        await session.ExecuteWriteAsync(async tx =>
        {
            var result = await tx.RunAsync($@"
                CREATE CONSTRAINT {label.ToLowerInvariant()}_message_id IF NOT EXISTS
                FOR (r:{label}) REQUIRE r.messageId IS UNIQUE").ConfigureAwait(false);
            _ = await result.ToListAsync().ConfigureAwait(false);
        }).ConfigureAwait(false);
        _constraintCreated = true;
    }
}
