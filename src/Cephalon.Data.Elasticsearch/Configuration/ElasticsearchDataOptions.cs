namespace Cephalon.Data.Elasticsearch.Configuration;

/// <summary>Configuration options for the Elasticsearch data provider (Engine:Data:Elasticsearch).</summary>
public sealed class ElasticsearchDataOptions
{
    /// <summary>Gets the configuration section path used by default for Elasticsearch data settings.</summary>
    public const string SectionPath = "Engine:Data:Elasticsearch";

    /// <summary>Gets the canonical provider identifier emitted by the pack.</summary>
    public const string ProviderId = "elasticsearch";

    /// <summary>Gets the default Elasticsearch node URI used when neither URI setting is supplied.</summary>
    public const string DefaultUri = "http://localhost:9200";

    /// <summary>Gets or sets the root <c>Uris</c> entry name to resolve for Elasticsearch.</summary>
    /// <remarks>Use either <see cref="UriName" /> or <see cref="Uri" />.</remarks>
    public string? UriName { get; set; }

    /// <summary>Gets or sets the inline Elasticsearch node URI.</summary>
    /// <remarks>Use either <see cref="Uri" /> or <see cref="UriName" />.</remarks>
    public string? Uri { get; set; }

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
