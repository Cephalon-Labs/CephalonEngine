namespace Cephalon.Data.Neo4j.Configuration;

/// <summary>
/// Options controlling how <c>Cephalon.Data.Neo4j</c> connects to Neo4j and registers data services.
/// </summary>
public sealed class Neo4jDataOptions
{
    /// <summary>The configuration section path used by default for Neo4j data settings.</summary>
    public const string SectionPath = "Engine:Data:Neo4j";

    /// <summary>The provider identifier used in capability and descriptor metadata.</summary>
    public const string ProviderId = "neo4j";

    /// <summary>The default Neo4j Bolt URI used when neither URI setting is supplied.</summary>
    public const string DefaultUri = "bolt://localhost:7687";

    /// <summary>The root <c>Uris</c> entry name to resolve for Neo4j.</summary>
    /// <remarks>Use either <see cref="UriName" /> or <see cref="Uri" />.</remarks>
    public string? UriName { get; set; }

    /// <summary>The inline Neo4j Bolt URI (e.g. <c>bolt://localhost:7687</c>).</summary>
    /// <remarks>Use either <see cref="Uri" /> or <see cref="UriName" />.</remarks>
    public string? Uri { get; set; }

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
