namespace Cephalon.Diagnostics.Redaction;

/// <summary>
/// Filters values the engine is about to expose to a telemetry exporter, log sink, or any
/// other observer outside the trust boundary. Consumer apps and observability companion packs
/// register one or more <see cref="IRedactionFilter"/> implementations to redact secrets, PII,
/// authentication tokens, and other sensitive values before they leave the engine.
/// </summary>
/// <remarks>
/// <para>
/// The redaction surface is intentionally a contract type only at this maturity level
/// (taxonomy-only); the engine does not yet route emission through registered filters. When the
/// runtime wires this surface in, the contract guarantees are:
/// </para>
/// <list type="bullet">
///   <item>filters run synchronously at the engine boundary, before exporter dispatch</item>
///   <item>filters never throw — implementations that need to fail must return the original value
///         and log out-of-band; an exception inside a filter is treated as a bug</item>
///   <item>filters can short-circuit by returning a redacted constant (e.g. <c>"[REDACTED]"</c>)
///         or a structurally-equivalent placeholder; they cannot mutate the call site or change
///         the attribute name</item>
///   <item>filter ordering follows DI registration order; the first filter that returns a value
///         different from its input wins, and subsequent filters see the redacted value</item>
/// </list>
/// </remarks>
public interface IRedactionFilter
{
    /// <summary>
    /// Inspects the value about to be emitted and returns either the original value or a
    /// redacted replacement.
    /// </summary>
    /// <param name="context">
    /// Describes the call site (activity source, meter, attribute key, logger category) so the
    /// filter can scope its redaction decision to the relevant emission surfaces.
    /// </param>
    /// <param name="value">The value the engine is about to emit.</param>
    /// <returns>
    /// The original <paramref name="value"/> when no redaction applies, or a redacted
    /// replacement when the filter recognises the value as sensitive. Returning the original
    /// value is the no-op default path; filters should not throw or return <see langword="null"/>
    /// to signal "no opinion" — they return the input unchanged instead.
    /// </returns>
    object? Filter(RedactionContext context, object? value);
}
