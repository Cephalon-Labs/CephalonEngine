namespace Cephalon.Data.Nats.Configuration;

/// <summary>
/// Options controlling how <c>Cephalon.Data.Nats</c> connects to a NATS server and registers data services.
/// </summary>
public sealed class NatsDataOptions
{
    /// <summary>The configuration section path used by default for NATS data settings.</summary>
    public const string SectionPath = "Engine:Data:Nats";

    /// <summary>The provider identifier used in capability and descriptor metadata.</summary>
    public const string ProviderId = "nats";

    /// <summary>The default NATS server URI used when neither URI setting is supplied.</summary>
    public const string DefaultUri = "nats://localhost:4222";

    /// <summary>The root <c>Uris</c> entry name to resolve for NATS.</summary>
    /// <remarks>Use either <see cref="UriName" /> or <see cref="Uri" />.</remarks>
    public string? UriName { get; set; }

    /// <summary>The inline NATS server URI. Defaults to <c>nats://localhost:4222</c> when left unset.</summary>
    /// <remarks>Use either <see cref="Uri" /> or <see cref="UriName" />.</remarks>
    public string? Uri { get; set; }

    /// <summary>Prefix applied to all managed JetStream KV bucket names. Defaults to <c>cephalon</c>.</summary>
    public string BucketPrefix { get; set; } = "cephalon";

    /// <summary>When <see langword="true" />, registers <see cref="Cephalon.Abstractions.Data.IOutbox" /> backed by a NATS JetStream KV bucket.</summary>
    public bool RegisterOutbox { get; set; }

    /// <summary>When <see langword="true" />, registers <see cref="Cephalon.Abstractions.Data.IInbox" /> backed by a NATS JetStream KV bucket.</summary>
    public bool RegisterInbox { get; set; }
}
