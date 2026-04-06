using System.Text.Json;
using Cephalon.Abstractions.Data;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.KeyValueStore;

namespace Cephalon.Data.Nats.Services;

/// <summary>
/// NATS JetStream KV-backed inbox implementation that tracks processed messages as KV entries.
/// Idempotency is enforced via <c>CreateAsync</c> — if the key already exists,
/// <see cref="NatsKVCreateException" /> is caught and swallowed.
/// </summary>
internal sealed class NatsInbox : IInbox
{
    private readonly INatsConnection _nats;
    private readonly string _bucketName;

    /// <summary>
    /// Initializes a new instance of the <see cref="NatsInbox" /> class.
    /// </summary>
    /// <param name="nats">The NATS connection (connection is deferred to first use).</param>
    /// <param name="bucketName">The JetStream KV bucket name used to store inbox receipts.</param>
    public NatsInbox(INatsConnection nats, string bucketName)
    {
        ArgumentNullException.ThrowIfNull(nats);
        ArgumentException.ThrowIfNullOrWhiteSpace(bucketName);
        _nats = nats;
        _bucketName = bucketName;
    }

    /// <inheritdoc />
    public async ValueTask<bool> HasProcessedAsync(string messageId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        var js = new NatsJSContext(_nats);
        var kvCtx = new NatsKVContext(js);
        var kv = await kvCtx.CreateOrUpdateStoreAsync(new NatsKVConfig(_bucketName), cancellationToken).ConfigureAwait(false);

        var result = await kv.TryGetEntryAsync<byte[]>(messageId.Trim(), cancellationToken: cancellationToken).ConfigureAwait(false);
        return result.Success && result.Value.Error is null;
    }

    /// <inheritdoc />
    public async ValueTask MarkProcessedAsync(InboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var js = new NatsJSContext(_nats);
        var kvCtx = new NatsKVContext(js);
        var kv = await kvCtx.CreateOrUpdateStoreAsync(new NatsKVConfig(_bucketName), cancellationToken).ConfigureAwait(false);

        var bytes = JsonSerializer.SerializeToUtf8Bytes(message);

        try
        {
            await kv.CreateAsync(message.Id, bytes, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (NatsKVCreateException)
        {
            // Key already exists — idempotent, not an error.
        }
    }
}
