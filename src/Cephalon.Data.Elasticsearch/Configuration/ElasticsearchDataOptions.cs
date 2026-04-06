namespace Cephalon.Data.Elasticsearch.Configuration;

/// <summary>Configuration options for the Elasticsearch data provider (Engine:Data:Elasticsearch).</summary>
public sealed class ElasticsearchDataOptions
{
    /// <summary>Gets the canonical provider identifier emitted by the pack.</summary>
    public const string ProviderId = "elasticsearch";

    /// <summary>Gets or sets the Elasticsearch node URI. Defaults to localhost.</summary>
    public string Uri { get; set; } = "http://localhost:9200";

    /// <summary>Gets or sets an optional username for Basic authentication.</summary>
    public string? Username { get; set; }

    /// <summary>Gets or sets an optional password for Basic authentication.</summary>
    public string? Password { get; set; }

    /// <summary>Gets or sets an optional prefix applied to all Cephalon-managed index names (e.g. "app-").</summary>
    public string IndexPrefix { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the pack should register the Elasticsearch-backed outbox implementation.</summary>
    public bool RegisterOutbox { get; set; }

    /// <summary>Gets or sets a value indicating whether the pack should register the Elasticsearch-backed inbox implementation.</summary>
    public bool RegisterInbox { get; set; }
}
