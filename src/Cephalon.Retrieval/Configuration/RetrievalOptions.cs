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

    /// <summary>
    /// Gets or sets the default maximum number of matches returned when a query request does not choose one explicitly.
    /// </summary>
    public int DefaultQueryLimit { get; set; } = 10;

    /// <summary>
    /// Gets or sets the upper bound applied to query result limits.
    /// </summary>
    public int MaximumQueryLimit { get; set; } = 25;

    /// <summary>
    /// Gets or sets the number of seconds after which the latest successful index is considered stale for operator reporting.
    /// </summary>
    public int FreshnessStaleAfterSeconds { get; set; } = 3600;

    /// <summary>
    /// Gets or sets a value indicating whether Cephalon should run the opt-in background reindex scheduler.
    /// </summary>
    public bool EnableBackgroundReindexing { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the scheduler should run once when the host starts.
    /// </summary>
    public bool RunBackgroundReindexOnStartup { get; set; } = true;

    /// <summary>
    /// Gets or sets the startup delay, in seconds, before the first background reindex run.
    /// </summary>
    public int BackgroundReindexInitialDelaySeconds { get; set; } = 5;

    /// <summary>
    /// Gets or sets the interval, in seconds, between background reindex runs. Values less than one disable repeated runs after the optional startup run.
    /// </summary>
    public int BackgroundReindexIntervalSeconds { get; set; } = 300;

    /// <summary>
    /// Gets the optional collection ids included in background reindexing. When empty, every registered collection is included.
    /// </summary>
    public IList<string> BackgroundReindexCollectionIds { get; } = [];
}
