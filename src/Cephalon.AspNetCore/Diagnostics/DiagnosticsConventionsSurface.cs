namespace Cephalon.AspNetCore.Diagnostics;

/// <summary>
/// Describes the canonical OpenTelemetry name set the Cephalon engine and its host adapters
/// emit telemetry under, projected so operators and AI tooling can introspect what the engine
/// emits without reading source.
/// </summary>
/// <param name="ActivitySources">
/// The stable <see cref="System.Diagnostics.ActivitySource"/> names emitted by the engine and
/// its shipped host adapters. Names come from the <c>Cephalon.Diagnostics</c> package's
/// <c>CephalonActivitySources</c> static class.
/// </param>
/// <param name="Meters">
/// The stable <see cref="System.Diagnostics.Metrics.Meter"/> names. Names come from the
/// <c>Cephalon.Diagnostics</c> package's <c>CephalonMeters</c> static class. These typically
/// match the activity-source names because the engine emits both kinds of instruments under
/// the same logical namespace.
/// </param>
/// <param name="CephalonAttributeKeys">
/// The <c>cephalon.*</c> attribute keys that complement OpenTelemetry semantic conventions.
/// Names come from the <c>Cephalon.Diagnostics</c> package's
/// <c>CephalonDiagnosticsAttributeKeys</c> static class. Engine concepts that have no
/// OpenTelemetry semantic-convention equivalent live here; concepts that have a semconv
/// equivalent are emitted under the OpenTelemetry attribute name directly and are not
/// re-declared in this surface.
/// </param>
public sealed record DiagnosticsConventionsSurface(
    IReadOnlyList<string> ActivitySources,
    IReadOnlyList<string> Meters,
    IReadOnlyList<string> CephalonAttributeKeys);
