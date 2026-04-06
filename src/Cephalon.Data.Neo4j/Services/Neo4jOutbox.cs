using System.Text.Json;
using Cephalon.Abstractions.Data;
using Cephalon.Data.Neo4j.Configuration;
using Neo4j.Driver;

namespace Cephalon.Data.Neo4j.Services;

/// <summary>
/// Neo4j-backed outbox that stages messages as graph nodes for durable delivery.
/// Idempotency is achieved via Cypher <c>MERGE</c> on <c>messageId</c> — repeated calls with the same id are no-ops.
/// </summary>
internal sealed class Neo4jOutbox : IOutbox
{
    private readonly IDriver _driver;
    private readonly Neo4jDataOptions _options;
    private volatile bool _constraintCreated;

    /// <summary>
    /// Initializes a new instance of the <see cref="Neo4jOutbox" /> class.
    /// </summary>
    /// <param name="driver">The Neo4j driver used to open sessions.</param>
    /// <param name="options">The Neo4j data options controlling label naming.</param>
    public Neo4jOutbox(IDriver driver, Neo4jDataOptions options)
    {
        ArgumentNullException.ThrowIfNull(driver);
        ArgumentNullException.ThrowIfNull(options);
        _driver = driver;
        _options = options;
    }

    /// <inheritdoc />
    public async ValueTask EnqueueAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        await EnsureConstraintAsync().ConfigureAwait(false);

        var label = $"{_options.LabelPrefix}OutboxMessage";
        var parameters = new
        {
            messageId = message.Id,
            channelId = message.ChannelId,
            messageType = message.MessageType,
            payload = message.Payload,
            contentType = message.ContentType ?? string.Empty,
            correlationId = message.CorrelationId ?? string.Empty,
            tenantId = message.TenantId ?? string.Empty,
            occurredAtUtc = message.OccurredAtUtc.UtcDateTime.ToString("O"),
            createdAtUtc = DateTime.UtcNow.ToString("O"),
            headersJson = JsonSerializer.Serialize(message.Headers),
            metadataJson = JsonSerializer.Serialize(message.Metadata)
        };

        await using var session = _driver.AsyncSession();
        await session.ExecuteWriteAsync(async tx =>
        {
            var result = await tx.RunAsync($@"
                MERGE (m:{label} {{messageId: $messageId}})
                ON CREATE SET
                    m.channelId = $channelId,
                    m.messageType = $messageType,
                    m.payload = $payload,
                    m.contentType = $contentType,
                    m.correlationId = $correlationId,
                    m.tenantId = $tenantId,
                    m.occurredAtUtc = $occurredAtUtc,
                    m.createdAtUtc = $createdAtUtc,
                    m.dispatchAttemptCount = 0,
                    m.headersJson = $headersJson,
                    m.metadataJson = $metadataJson
                RETURN m.messageId AS id",
                parameters).ConfigureAwait(false);
            _ = await result.ToListAsync().ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    private async Task EnsureConstraintAsync()
    {
        if (_constraintCreated) return;
        var label = $"{_options.LabelPrefix}OutboxMessage";
        await using var session = _driver.AsyncSession();
        await session.ExecuteWriteAsync(async tx =>
        {
            var result = await tx.RunAsync($@"
                CREATE CONSTRAINT {label.ToLowerInvariant()}_message_id IF NOT EXISTS
                FOR (m:{label}) REQUIRE m.messageId IS UNIQUE").ConfigureAwait(false);
            _ = await result.ToListAsync().ConfigureAwait(false);
        }).ConfigureAwait(false);
        _constraintCreated = true;
    }
}
