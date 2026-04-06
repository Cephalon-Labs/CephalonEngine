using System.Security.Cryptography;
using System.Text;
using Cephalon.Abstractions.Data;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace Cephalon.Data.Qdrant.Services;

/// <summary>
/// Qdrant-backed inbox implementation that tracks processed messages using vector-collection
/// points keyed by a UUID derived from the message id.
/// Idempotency is enforced by checking for an existing point before upserting.
/// </summary>
internal sealed class QdrantInbox : IInbox, IDisposable
{
    private readonly QdrantClient _client;
    private readonly string _collectionName;
    private volatile bool _collectionEnsured;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    /// <summary>
    /// Initializes a new instance of the <see cref="QdrantInbox" /> class.
    /// </summary>
    /// <param name="client">The Qdrant client used to manage collections and points.</param>
    /// <param name="collectionName">The Qdrant collection name used to store inbox receipts.</param>
    public QdrantInbox(QdrantClient client, string collectionName)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(collectionName);
        _client = client;
        _collectionName = collectionName;
    }

    /// <inheritdoc />
    public async ValueTask<bool> HasProcessedAsync(string messageId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        await EnsureCollectionAsync(cancellationToken).ConfigureAwait(false);

        var id = DeriveGuid(messageId.Trim());
        var result = await _client.RetrieveAsync(_collectionName, [id], cancellationToken: cancellationToken).ConfigureAwait(false);
        return result.Count > 0;
    }

    /// <inheritdoc />
    public async ValueTask MarkProcessedAsync(InboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        await EnsureCollectionAsync(cancellationToken).ConfigureAwait(false);

        var id = DeriveGuid(message.Id);

        var existing = await _client.RetrieveAsync(_collectionName, [id], cancellationToken: cancellationToken).ConfigureAwait(false);
        if (existing.Count > 0)
        {
            return;
        }

        var point = new PointStruct
        {
            Id = id,
            Vectors = new float[] { 0.0f }
        };

        point.Payload["message_id"] = message.Id;
        point.Payload["channel_id"] = message.ChannelId;
        point.Payload["message_type"] = message.MessageType;
        point.Payload["correlation_id"] = message.CorrelationId ?? string.Empty;
        point.Payload["tenant_id"] = message.TenantId ?? string.Empty;
        point.Payload["received_at_utc"] = message.ReceivedAtUtc.UtcDateTime.ToString("O");
        point.Payload["processed_at_utc"] = DateTime.UtcNow.ToString("O");

        await _client.UpsertAsync(_collectionName, [point], cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private async Task EnsureCollectionAsync(CancellationToken cancellationToken)
    {
        if (_collectionEnsured)
        {
            return;
        }

        await _initLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_collectionEnsured)
            {
                return;
            }

            var exists = await _client.CollectionExistsAsync(_collectionName, cancellationToken).ConfigureAwait(false);
            if (!exists)
            {
                await _client.CreateCollectionAsync(
                    _collectionName,
                    new VectorParams { Size = 1, Distance = Distance.Cosine },
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                await _client.CreatePayloadIndexAsync(
                    _collectionName,
                    "message_id",
                    PayloadSchemaType.Keyword,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            _collectionEnsured = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _initLock.Dispose();
    }

    private static Guid DeriveGuid(string input)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return new Guid(hash[..16]);
    }
}
