namespace Cephalon.Diagnostics.Redaction;

/// <summary>
/// Describes the call site that is producing a value the engine is about to expose to a
/// telemetry exporter, log sink, or any other observer outside the trust boundary.
/// </summary>
/// <param name="ActivitySourceName">
/// The canonical activity source name (per <see cref="CephalonActivitySources"/>) under which
/// the value is about to be emitted, or <see langword="null"/> when the call site is not span-shaped
/// (e.g. a log record emitted directly through <c>ILogger</c> rather than as a span attribute).
/// </param>
/// <param name="MeterName">
/// The canonical meter name (per <see cref="CephalonMeters"/>) under which the value is about
/// to be emitted, or <see langword="null"/> when the call site is not metric-shaped.
/// </param>
/// <param name="AttributeKey">
/// The attribute key (e.g. one of the keys declared in
/// <see cref="CephalonDiagnosticsAttributeKeys"/>, or an OpenTelemetry semantic-convention key
/// like <c>http.url</c>) under which the value is about to be emitted. Filters use the
/// attribute key to decide whether the value is candidate for redaction.
/// </param>
/// <param name="LoggerCategory">
/// The <c>ILogger</c> category that is producing the value, or <see langword="null"/> when the
/// emission is not log-shaped.
/// </param>
public readonly record struct RedactionContext(
    string? ActivitySourceName,
    string? MeterName,
    string AttributeKey,
    string? LoggerCategory);
