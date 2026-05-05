using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;
using Cephalon.Diagnostics;

namespace Cephalon.Retrieval.Services;

/// <summary>
/// Defines the stable activity source, meter, activity, counter, and tag names emitted by the
/// retrieval companion runtime. Names are sourced from <see cref="CephalonActivitySources.Retrieval"/>
/// and <see cref="CephalonMeters.Retrieval"/> so the retrieval pack and observability companion
/// packs share one canonical name set with the rest of the engine.
/// </summary>
public static class RetrievalDiagnostics
{
    /// <summary>
    /// Gets the stable activity-source name emitted by the retrieval runtime.
    /// </summary>
    public const string ActivitySourceName = CephalonActivitySources.Retrieval;

    /// <summary>
    /// Gets the stable meter name emitted by the retrieval runtime.
    /// </summary>
    public const string MeterName = CephalonMeters.Retrieval;

    /// <summary>
    /// Gets the stable activity name emitted around one managed knowledge-index run.
    /// </summary>
    public const string KnowledgeIndexActivityName = "retrieval.knowledge.index";

    /// <summary>
    /// Gets the stable activity name emitted around one managed knowledge-query.
    /// </summary>
    public const string KnowledgeQueryActivityName = "retrieval.knowledge.query";

    /// <summary>
    /// Gets the stable counter name for completed knowledge-index runs.
    /// </summary>
    public const string KnowledgeIndexCounterName = "cephalon.retrieval.index_runs";

    /// <summary>
    /// Gets the stable counter name for completed knowledge-queries.
    /// </summary>
    public const string KnowledgeQueryCounterName = "cephalon.retrieval.queries";

    /// <summary>
    /// Stable Cephalon-prefix tag carrying the indexer identifier responsible for the run.
    /// </summary>
    public const string IndexerIdTag = "cephalon.retrieval.indexer.id";

    /// <summary>
    /// Stable Cephalon-prefix tag carrying the query-engine identifier responsible for the query.
    /// </summary>
    public const string QueryEngineIdTag = "cephalon.retrieval.query_engine.id";

    /// <summary>
    /// Stable Cephalon-prefix tag carrying the knowledge-collection identifier emitted on the
    /// activity.
    /// </summary>
    public const string CollectionIdTag = "cephalon.retrieval.collection.id";

    /// <summary>
    /// Stable Cephalon-prefix tag carrying the knowledge-index run identifier emitted on the
    /// activity.
    /// </summary>
    public const string RunIdTag = "cephalon.retrieval.run.id";

    /// <summary>
    /// Stable Cephalon-prefix tag carrying the optional actor identifier emitted on the activity.
    /// </summary>
    public const string ActorIdTag = "cephalon.retrieval.actor.id";

    /// <summary>
    /// Stable Cephalon-prefix tag carrying the optional correlation identifier emitted on the
    /// activity.
    /// </summary>
    public const string CorrelationIdTag = "cephalon.retrieval.correlation.id";

    /// <summary>
    /// Stable Cephalon-prefix tag carrying the terminal indexing outcome emitted on the activity
    /// (started, succeeded, failed, or skipped).
    /// </summary>
    public const string IndexingOutcomeTag = "cephalon.retrieval.index.outcome";

    /// <summary>
    /// Stable Cephalon-prefix tag carrying the document count of the replacement index when one
    /// was published.
    /// </summary>
    public const string DocumentCountTag = "cephalon.retrieval.document.count";

    /// <summary>
    /// Stable Cephalon-prefix tag carrying the provider count consulted during the indexing run.
    /// </summary>
    public const string ProviderCountTag = "cephalon.retrieval.provider.count";

    /// <summary>
    /// Stable Cephalon-prefix tag carrying the requested-or-effective query length emitted on the
    /// query activity. Query text itself is not emitted because retrieval queries can carry user
    /// content that should never reach exporters in the clear.
    /// </summary>
    public const string QueryLengthTag = "cephalon.retrieval.query.length";

    /// <summary>
    /// Stable Cephalon-prefix tag carrying the resolved query result limit (after default and
    /// maximum-limit clamps) emitted on the query activity.
    /// </summary>
    public const string QueryLimitTag = "cephalon.retrieval.query.limit";

    /// <summary>
    /// Stable Cephalon-prefix tag carrying the count of matches returned by the query.
    /// </summary>
    public const string MatchCountTag = "cephalon.retrieval.match.count";

    /// <summary>
    /// Stable Cephalon-prefix tag carrying the terminal query outcome emitted on the activity
    /// (succeeded or failed). Unlike indexing, queries do not have a skipped or started state on
    /// the runtime path.
    /// </summary>
    public const string QueryOutcomeTag = "cephalon.retrieval.query.outcome";

    private static readonly string Version = typeof(RetrievalDiagnostics).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
        .InformationalVersion
        ?? typeof(RetrievalDiagnostics).Assembly.GetName().Version?.ToString()
        ?? "0.0.0";

    internal static readonly ActivitySource ActivitySource = new(ActivitySourceName, Version);
    internal static readonly Meter Meter = new(MeterName, Version);
    internal static readonly Counter<long> KnowledgeIndexCounter = Meter.CreateCounter<long>(
        KnowledgeIndexCounterName,
        unit: "runs",
        description: "Counts managed knowledge-index runs by terminal outcome.");
    internal static readonly Counter<long> KnowledgeQueryCounter = Meter.CreateCounter<long>(
        KnowledgeQueryCounterName,
        unit: "queries",
        description: "Counts managed knowledge-queries by terminal outcome.");
}
