namespace Cephalon.Data.Qdrant.Configuration;

/// <summary>
/// Options controlling how <c>Cephalon.Data.Qdrant</c> connects to a Qdrant server and registers data services.
/// </summary>
public sealed class QdrantDataOptions
{
    /// <summary>The provider identifier used in capability and descriptor metadata.</summary>
    public const string ProviderId = "qdrant";

    /// <summary>The Qdrant server hostname. Defaults to <c>localhost</c>.</summary>
    public string Host { get; set; } = "localhost";

    /// <summary>The Qdrant gRPC port. Defaults to <c>6334</c>.</summary>
    public int Port { get; set; } = 6334;

    /// <summary>Optional Qdrant API key for authenticated clusters. Defaults to <see langword="null" />.</summary>
    public string? ApiKey { get; set; }

    /// <summary>Optional prefix applied to all managed collection names.</summary>
    public string CollectionPrefix { get; set; } = string.Empty;

    /// <summary>When <see langword="true" />, registers <see cref="Cephalon.Abstractions.Data.IOutbox" /> backed by a Qdrant vector collection.</summary>
    public bool RegisterOutbox { get; set; }

    /// <summary>When <see langword="true" />, registers <see cref="Cephalon.Abstractions.Data.IInbox" /> backed by a Qdrant vector collection.</summary>
    public bool RegisterInbox { get; set; }
}
