namespace Cephalon.Diagnostics;

/// <summary>
/// Stable Cephalon-prefix attribute keys that complement OpenTelemetry semantic conventions.
/// Engine-emitted spans, metrics, and logs use these keys for engine concepts that have no
/// existing OpenTelemetry semantic-convention name (module identifiers, behavior identifiers,
/// cell identifiers, runtime catalog identifiers, etc.).
/// </summary>
/// <remarks>
/// <para>
/// Keys here are chosen to be stable across releases. Adding a new key is additive; renaming
/// or repurposing an existing key is a binary-stable break and follows the same review
/// discipline as a public API change.
/// </para>
/// <para>
/// Where an OpenTelemetry semantic convention already exists for a concept, the engine emits
/// the OpenTelemetry attribute name directly rather than re-declaring it under
/// <c>cephalon.*</c>. This class is intentionally a complement to OpenTelemetry semantic
/// conventions, not a replacement.
/// </para>
/// </remarks>
public static class CephalonDiagnosticsAttributeKeys
{
    /// <summary>
    /// Stable Cephalon-prefix attribute key carrying the engine module identifier under which
    /// the activity, metric, or log record was emitted.
    /// </summary>
    public const string ModuleId = "cephalon.module.id";

    /// <summary>
    /// Stable Cephalon-prefix attribute key carrying the engine behavior identifier that
    /// dispatched the request producing the activity, metric, or log record.
    /// </summary>
    public const string BehaviorId = "cephalon.behavior.id";

    /// <summary>
    /// Stable Cephalon-prefix attribute key carrying the engine cell identifier in cell-based
    /// architecture deployments.
    /// </summary>
    public const string CellId = "cephalon.cell.id";

    /// <summary>
    /// Stable Cephalon-prefix attribute key carrying the engine app blueprint identifier
    /// (modular-monolith, modular-vertical-slice, microservice, microservice-suite).
    /// </summary>
    public const string AppBlueprint = "cephalon.app.blueprint";

    /// <summary>
    /// Stable Cephalon-prefix attribute key carrying the multi-tenancy tenant identifier
    /// resolved for the current activity, metric, or log record.
    /// </summary>
    public const string TenantId = "cephalon.tenant.id";
}
