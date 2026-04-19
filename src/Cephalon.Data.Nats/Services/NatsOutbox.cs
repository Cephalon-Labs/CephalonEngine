using System.Text.Json;
using Cephalon.Abstractions.Data;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.KeyValueStore;

namespace Cephalon.Data.Nats.Services;

/// <summary>
/// NATS JetStream KV-backed outbox implementation that stages messages as KV entries.
/// Idempotency is enforced via <c>CreateAsync</c> — if the key already exists,
/// <see cref="NatsKVCreateException" /> is caught and swallowed.
/// </summary>
internal sealed class NatsOutbox : IOutbox
{
    private readonly INatsConnection _nats;
    private readonly string _bucketName;

    /// <summary>
    /// Initializes a new instance of the <see cref="NatsOutbox" /> class.
    /// </summary>
    /// <param name="nats">The NATS connection (connection is deferred to first use).</param>
    /// <param name="bucketName">The JetStream KV bucket name used to store outbox messages.</param>
    public NatsOutbox(INatsConnection nats, string bucketName)
    {
        ArgumentNullException.ThrowIfNull(nats);
        ArgumentException.ThrowIfNullOrWhiteSpace(bucketName);
        _nats = nats;
        _bucketName = bucketName;
    }

    /// <inheritdoc />
    public string OutboxId => "nats-outbox";

    /// <inheritdoc />
    public async ValueTask EnqueueAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var js = new NatsJSContext(_nats);
        var kvCtx = new NatsKVContext(js);
        var kv = await kvCtx.CreateOrUpdateStoreAsync(new NatsKVConfig(_bucketName), cancellationToken).ConfigureAwait(false);

        var bytes = JsonSerializer.SerializeToUtf8Bytes(NatsOutboxRecord.Create(message));

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
