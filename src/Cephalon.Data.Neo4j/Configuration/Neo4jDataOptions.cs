namespace Cephalon.Data.Neo4j.Configuration;

/// <summary>
/// Options controlling how <c>Cephalon.Data.Neo4j</c> connects to Neo4j and registers data services.
/// </summary>
public sealed class Neo4jDataOptions
{
    /// <summary>The provider identifier used in capability and descriptor metadata.</summary>
    public const string ProviderId = "neo4j";

    /// <summary>The Neo4j Bolt URI (e.g. <c>bolt://localhost:7687</c>).</summary>
    public string Uri { get; set; } = "bolt://localhost:7687";

    /// <summary>Neo4j username.</summary>
    public string Username { get; set; } = "neo4j";

    /// <summary>Neo4j password.</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>Optional label prefix applied to all managed node labels (e.g. <c>"cephalon_"</c>).</summary>
    public string LabelPrefix { get; set; } = "Cephalon";

    /// <summary>When <see langword="true" />, registers <see cref="Cephalon.Abstractions.Data.IOutbox" /> backed by Neo4j <c>:OutboxMessage</c> nodes.</summary>
    public bool RegisterOutbox { get; set; }

    /// <summary>When <see langword="true" />, registers <see cref="Cephalon.Abstractions.Data.IInbox" /> backed by Neo4j <c>:InboxReceipt</c> nodes.</summary>
    public bool RegisterInbox { get; set; }
}
