namespace Cephalon.Data.OpenSearch.Configuration;

/// <summary>Configuration options for the OpenSearch data provider (Engine:Data:OpenSearch).</summary>
public sealed class OpenSearchDataOptions
{
    /// <summary>Gets the configuration section path used by default for OpenSearch data settings.</summary>
    public const string SectionPath = "Engine:Data:OpenSearch";

    /// <summary>Gets the canonical provider identifier emitted by the pack.</summary>
    public const string ProviderId = "opensearch";

    /// <summary>Gets the default OpenSearch node URI used when neither URI setting is supplied.</summary>
    public const string DefaultUri = "http://localhost:9200";

    /// <summary>Gets or sets the root <c>Uris</c> entry name to resolve for OpenSearch.</summary>
    /// <remarks>Use either <see cref="UriName" /> or <see cref="Uri" />.</remarks>
    public string? UriName { get; set; }

    /// <summary>Gets or sets the inline OpenSearch node URI.</summary>
    /// <remarks>Use either <see cref="Uri" /> or <see cref="UriName" />.</remarks>
    public string? Uri { get; set; }

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
