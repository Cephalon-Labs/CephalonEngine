namespace Cephalon.Data.OpenSearch.Configuration;

/// <summary>Configuration options for the OpenSearch data provider (Engine:Data:OpenSearch).</summary>
public sealed class OpenSearchDataOptions
{
    /// <summary>Gets the canonical provider identifier emitted by the pack.</summary>
    public const string ProviderId = "opensearch";

    /// <summary>Gets or sets the OpenSearch node URI. Defaults to localhost.</summary>
    public string Uri { get; set; } = "http://localhost:9200";

    /// <summary>Gets or sets an optional username for Basic authentication.</summary>
    public string? Username { get; set; }

    /// <summary>Gets or sets an optional password for Basic authentication.</summary>
    public string? Password { get; set; }

    /// <summary>Gets or sets an optional prefix applied to all Cephalon-managed index names (e.g. "app-").</summary>
    public string IndexPrefix { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the pack should register the OpenSearch-backed outbox implementation.</summary>
    public bool RegisterOutbox { get; set; }

    /// <summary>Gets or sets a value indicating whether the pack should register the OpenSearch-backed inbox implementation.</summary>
    public bool RegisterInbox { get; set; }
}
