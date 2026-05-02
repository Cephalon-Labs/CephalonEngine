using System.Text.RegularExpressions;

namespace Cephalon.Diagnostics.Redaction.Defaults;

/// <summary>
/// An <see cref="IRedactionFilter"/> that scans the string representation of a value for a
/// regex match (e.g. an authorization-header pattern, a credit-card-number pattern, an email
/// pattern) and replaces matched substrings with a redacted placeholder. Useful for catching
/// sensitive values that may end up inside larger strings such as HTTP request URLs, log
/// messages, or exception text.
/// </summary>
/// <remarks>
/// <para>
/// Non-string values are returned unchanged; this filter only inspects values that are
/// actually <see cref="string"/>s. Consumer apps that need to redact inside non-string types
/// (e.g. <see cref="System.Uri"/>, custom complex types) should author a dedicated filter
/// rather than rely on string regex.
/// </para>
/// <para>
/// The supplied <see cref="Regex"/> is reused per-call without recompilation, so callers should
/// either pass a <see cref="RegexOptions.Compiled"/> regex or one cached at startup. Filter
/// methods run on the engine boundary hot path and must stay allocation-light; the regex
/// engine itself manages backtracking budgets.
/// </para>
/// </remarks>
public sealed class RegexRedactionFilter : IRedactionFilter
{
    /// <summary>
    /// The default replacement string emitted in place of regex-matched substrings.
    /// </summary>
    public const string DefaultReplacement = "[REDACTED]";

    private readonly Regex pattern;
    private readonly string replacement;

    /// <summary>
    /// Creates a filter that replaces every regex-matched substring with
    /// <paramref name="replacement"/> (or <see cref="DefaultReplacement"/> when omitted).
    /// </summary>
    /// <param name="pattern">The regex pattern matching sensitive substrings.</param>
    /// <param name="replacement">
    /// Optional replacement string emitted in place of each match. When <see langword="null"/>
    /// the filter emits <see cref="DefaultReplacement"/> (the string <c>"[REDACTED]"</c>).
    /// </param>
    public RegexRedactionFilter(Regex pattern, string? replacement = null)
    {
        ArgumentNullException.ThrowIfNull(pattern);

        this.pattern = pattern;
        this.replacement = replacement ?? DefaultReplacement;
    }

    /// <inheritdoc />
    public object? Filter(RedactionContext context, object? value)
    {
        if (value is not string text || text.Length == 0)
        {
            return value;
        }

        return pattern.IsMatch(text) ? pattern.Replace(text, replacement) : text;
    }
}
