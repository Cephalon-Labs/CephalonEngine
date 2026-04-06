namespace Cephalon.Data.Nats.Configuration;

/// <summary>
/// Options controlling how <c>Cephalon.Data.Nats</c> connects to a NATS server and registers data services.
/// </summary>
public sealed class NatsDataOptions
{
    /// <summary>The provider identifier used in capability and descriptor metadata.</summary>
    public const string ProviderId = "nats";

    /// <summary>The NATS server URL. Defaults to <c>nats://localhost:4222</c>.</summary>
    public string Url { get; set; } = "nats://localhost:4222";

    /// <summary>Prefix applied to all managed JetStream KV bucket names. Defaults to <c>cephalon</c>.</summary>
    public string BucketPrefix { get; set; } = "cephalon";

    /// <summary>When <see langword="true" />, registers <see cref="Cephalon.Abstractions.Data.IOutbox" /> backed by a NATS JetStream KV bucket.</summary>
    public bool RegisterOutbox { get; set; }

    /// <summary>When <see langword="true" />, registers <see cref="Cephalon.Abstractions.Data.IInbox" /> backed by a NATS JetStream KV bucket.</summary>
    public bool RegisterInbox { get; set; }
}
