using System.Diagnostics.Metrics;

namespace Cephalon.Diagnostics;

/// <summary>
/// Well-known <see cref="Meter"/> names emitted by the Cephalon engine and its shipped host
/// adapters. Consumers should not create meters under these names directly; the names are
/// exposed so observability companion packs and downstream tooling can subscribe to
/// engine-emitted instruments through stable identifiers.
/// </summary>
/// <remarks>
/// Metric instrument names emitted under these meters follow OpenTelemetry semantic
/// conventions where conventions exist (HTTP server, runtime, messaging). Cephalon-specific
/// instruments use the <c>cephalon.*</c> prefix declared in
/// <see cref="CephalonDiagnosticsAttributeKeys"/>.
/// </remarks>
public static class CephalonMeters
{
    /// <summary>
    /// Stable name of the engine-level meter, used by composition, runtime, and
    /// behavior-dispatch metrics emitted from <c>Cephalon.Engine</c>.
    /// </summary>
    public const string Engine = "Cephalon.Engine";

    /// <summary>
    /// Stable name of the ASP.NET Core host adapter meter, used by <c>Cephalon.AspNetCore</c>
    /// request-pipeline metrics.
    /// </summary>
    public const string AspNetCore = "Cephalon.AspNetCore";

    /// <summary>
    /// Stable name of the worker host adapter meter, used by <c>Cephalon.Worker</c>
    /// hosted-service and lifecycle metrics.
    /// </summary>
    public const string Worker = "Cephalon.Worker";

    /// <summary>
    /// Stable name of the eventing companion-pack meter, used by <c>Cephalon.Eventing</c>
    /// publication, subscription, and dispatch metrics.
    /// </summary>
    public const string Eventing = "Cephalon.Eventing";

    /// <summary>
    /// Stable name of the multi-tenancy governance companion-pack meter, used by
    /// <c>Cephalon.MultiTenancy.Governance</c> membership, invitation, delivery dispatch,
    /// delivery-status, tenant-administration, and domain-ownership metrics.
    /// </summary>
    public const string MultiTenancyGovernance = "Cephalon.MultiTenancy.Governance";

    /// <summary>
    /// Stable name of the agentics companion-pack meter, used by <c>Cephalon.Agentics</c>
    /// tool-execution, dispatcher, run-state, retry, idempotency, approval, and
    /// terminal-failure metrics.
    /// </summary>
    public const string Agentics = "Cephalon.Agentics";

    /// <summary>
    /// Stable name of the retrieval companion-pack meter, used by <c>Cephalon.Retrieval</c>
    /// indexing, query, freshness, and reindex metrics.
    /// </summary>
    public const string Retrieval = "Cephalon.Retrieval";
}
