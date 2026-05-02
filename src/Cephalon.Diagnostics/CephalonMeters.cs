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
}
