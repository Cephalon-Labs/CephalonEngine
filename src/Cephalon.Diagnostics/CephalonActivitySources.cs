using System.Diagnostics;

namespace Cephalon.Diagnostics;

/// <summary>
/// Well-known <see cref="ActivitySource"/> names emitted by the Cephalon engine and its
/// shipped host adapters. Consumers should not create activities under these names directly;
/// the names are exposed so observability companion packs and downstream tooling can subscribe
/// to engine-emitted spans through stable identifiers.
/// </summary>
/// <remarks>
/// <para>
/// Activity attribute names emitted under these sources follow OpenTelemetry semantic
/// conventions where conventions exist (HTTP, DB, messaging, RPC, runtime). Cephalon-specific
/// attributes use the <c>cephalon.*</c> prefix declared in
/// <see cref="CephalonDiagnosticsAttributeKeys"/>.
/// </para>
/// </remarks>
public static class CephalonActivitySources
{
    /// <summary>
    /// Stable name of the engine-level activity source, used by composition, runtime, and
    /// behavior-dispatch spans emitted from <c>Cephalon.Engine</c>.
    /// </summary>
    public const string Engine = "Cephalon.Engine";

    /// <summary>
    /// Stable name of the ASP.NET Core host adapter activity source, used by
    /// <c>Cephalon.AspNetCore</c> mapping and request-pipeline spans.
    /// </summary>
    public const string AspNetCore = "Cephalon.AspNetCore";

    /// <summary>
    /// Stable name of the worker host adapter activity source, used by <c>Cephalon.Worker</c>
    /// hosted-service and lifecycle spans.
    /// </summary>
    public const string Worker = "Cephalon.Worker";

    /// <summary>
    /// Stable name of the eventing companion-pack activity source, used by
    /// <c>Cephalon.Eventing</c> publication, subscription, and dispatch spans.
    /// </summary>
    public const string Eventing = "Cephalon.Eventing";

    /// <summary>
    /// Stable name of the multi-tenancy governance companion-pack activity source, used by
    /// <c>Cephalon.MultiTenancy.Governance</c> membership, invitation, delivery dispatch,
    /// delivery-status reconciliation, tenant-administration, and domain-ownership spans.
    /// </summary>
    public const string MultiTenancyGovernance = "Cephalon.MultiTenancy.Governance";

    /// <summary>
    /// Stable name of the agentics companion-pack activity source, used by
    /// <c>Cephalon.Agentics</c> tool-execution, dispatcher, run-state, retry, idempotency,
    /// approval, and terminal-failure spans.
    /// </summary>
    public const string Agentics = "Cephalon.Agentics";

    /// <summary>
    /// Stable name of the retrieval companion-pack activity source, used by
    /// <c>Cephalon.Retrieval</c> indexing, query, freshness, and reindex spans.
    /// </summary>
    public const string Retrieval = "Cephalon.Retrieval";
}
