namespace Cephalon.Diagnostics.Redaction.Defaults;

/// <summary>
/// An <see cref="IRedactionFilter"/> that redacts values whose <see cref="RedactionContext.AttributeKey"/>
/// matches one of a fixed set of attribute keys. Useful for redacting well-known sensitive attributes
/// such as <c>http.request.header.authorization</c>, <c>http.request.header.cookie</c>, custom
/// <c>cephalon.tenant.secret</c>, or any other attribute the consumer app considers sensitive.
/// </summary>
/// <remarks>
/// <para>
/// Comparison is ordinal-case-insensitive so consumer apps can author attribute keys with
/// either casing without affecting the redaction decision. The filter never inspects the
/// value itself — it short-circuits purely on the attribute key.
/// </para>
/// <para>
/// The replacement value defaults to the literal <c>"[REDACTED]"</c> string. Consumer apps that
/// want a structurally-typed replacement (for example, a fixed-length zero-byte array for binary
/// attributes) can pass a different replacement to the constructor; the replacement is reused
/// verbatim every time the filter fires.
/// </para>
/// </remarks>
public sealed class KeyMatchRedactionFilter : IRedactionFilter
{
    /// <summary>
    /// The default replacement string emitted in place of values whose attribute key matches.
    /// </summary>
    public const string DefaultReplacement = "[REDACTED]";

    private readonly HashSet<string> bannedKeys;
    private readonly object? replacement;

    /// <summary>
    /// Creates a filter that redacts values whose <see cref="RedactionContext.AttributeKey"/>
    /// matches one of <paramref name="bannedAttributeKeys"/>.
    /// </summary>
    /// <param name="bannedAttributeKeys">The attribute keys whose values must be redacted.</param>
    /// <param name="replacement">
    /// Optional replacement value the filter emits in place of the original. When <see langword="null"/>
    /// the filter emits <see cref="DefaultReplacement"/> (the string <c>"[REDACTED]"</c>).
    /// </param>
    public KeyMatchRedactionFilter(IEnumerable<string> bannedAttributeKeys, object? replacement = null)
    {
        ArgumentNullException.ThrowIfNull(bannedAttributeKeys);

        bannedKeys = new HashSet<string>(bannedAttributeKeys, StringComparer.OrdinalIgnoreCase);
        this.replacement = replacement ?? DefaultReplacement;
    }

    /// <inheritdoc />
    public object? Filter(RedactionContext context, object? value)
    {
        return bannedKeys.Contains(context.AttributeKey) ? replacement : value;
    }
}
