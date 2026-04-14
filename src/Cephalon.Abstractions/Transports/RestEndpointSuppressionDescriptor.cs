namespace Cephalon.Abstractions.Transports;

/// <summary>
/// Describes one host-level REST endpoint suppression rule visible to the current runtime.
/// </summary>
public sealed class RestEndpointSuppressionDescriptor
{
    /// <summary>
    /// Creates a REST endpoint suppression descriptor.
    /// </summary>
    /// <param name="id">The stable suppression identifier.</param>
    /// <param name="behaviorIds">The behavior identifiers targeted by the suppression rule.</param>
    /// <param name="sourceModuleIds">The source-module identifiers targeted by the suppression rule.</param>
    /// <param name="authoringStyles">The normalized shorthand authoring styles targeted by the suppression rule.</param>
    public RestEndpointSuppressionDescriptor(
        string id,
        IReadOnlyList<string>? behaviorIds = null,
        IReadOnlyList<string>? sourceModuleIds = null,
        IReadOnlyList<string>? authoringStyles = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("A non-empty suppression id is required.", nameof(id));
        }

        Id = id.Trim();
        BehaviorIds = NormalizeList(behaviorIds);
        SourceModuleIds = NormalizeList(sourceModuleIds);
        AuthoringStyles = NormalizeList(authoringStyles);
    }

    /// <summary>
    /// Gets the stable suppression identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the behavior identifiers targeted by this suppression rule.
    /// </summary>
    public IReadOnlyList<string> BehaviorIds { get; }

    /// <summary>
    /// Gets the source-module identifiers targeted by this suppression rule.
    /// </summary>
    public IReadOnlyList<string> SourceModuleIds { get; }

    /// <summary>
    /// Gets the normalized shorthand authoring styles targeted by this suppression rule.
    /// </summary>
    public IReadOnlyList<string> AuthoringStyles { get; }

    private static string[] NormalizeList(IReadOnlyList<string>? values)
    {
        return values?
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }
}
