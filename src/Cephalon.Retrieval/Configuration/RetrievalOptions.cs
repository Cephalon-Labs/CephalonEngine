using Cephalon.Retrieval.Services;

namespace Cephalon.Retrieval.Configuration;

/// <summary>
/// Configures the built-in retrieval runtime pack.
/// </summary>
/// <remarks>
/// These options seed the host-owned part of the retrieval runtime. Installed modules can still
/// contribute additional knowledge collections through <see cref="Services.IKnowledgeCollectionContributor" />.
/// </remarks>
public sealed class RetrievalOptions
{
    /// <summary>
    /// Creates retrieval options with the default host-owned features enabled.
    /// </summary>
    public RetrievalOptions()
    {
    }

    /// <summary>
    /// Gets the host-defined knowledge collections that should be available to the retrieval runtime.
    /// </summary>
    public IList<KnowledgeCollectionDescriptor> Collections { get; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether ingestion features are enabled.
    /// </summary>
    public bool EnableIngestion { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether query features are enabled.
    /// </summary>
    public bool EnableQuerying { get; set; } = true;
}
