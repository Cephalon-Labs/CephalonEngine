namespace Cephalon.Abstractions.Transports;

/// <summary>
/// Describes one host-level REST endpoint override rule visible to the current runtime.
/// </summary>
public sealed class RestEndpointOverrideDescriptor
{
    /// <summary>
    /// Creates a REST endpoint override descriptor.
    /// </summary>
    /// <param name="id">The stable override identifier.</param>
    /// <param name="behaviorIds">The behavior identifiers targeted by the override rule.</param>
    /// <param name="sourceModuleIds">The source-module identifiers targeted by the override rule.</param>
    /// <param name="authoringStyles">The normalized shorthand authoring styles targeted by the override rule.</param>
    /// <param name="apiVersionMajor">The effective API major version applied when the rule matches.</param>
    public RestEndpointOverrideDescriptor(
        string id,
        IReadOnlyList<string>? behaviorIds = null,
        IReadOnlyList<string>? sourceModuleIds = null,
        IReadOnlyList<string>? authoringStyles = null,
        int? apiVersionMajor = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("A non-empty override id is required.", nameof(id));
        }

        if (apiVersionMajor is not > 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(apiVersionMajor),
                apiVersionMajor,
                "REST endpoint override rules require a positive API major version.");
        }

        Id = id.Trim();
        BehaviorIds = NormalizeList(behaviorIds);
        SourceModuleIds = NormalizeList(sourceModuleIds);
        AuthoringStyles = NormalizeList(authoringStyles);
        ApiVersionMajor = apiVersionMajor;
    }

    /// <summary>
    /// Gets the stable override identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the behavior identifiers targeted by this override rule.
    /// </summary>
    public IReadOnlyList<string> BehaviorIds { get; }

    /// <summary>
    /// Gets the source-module identifiers targeted by this override rule.
    /// </summary>
    public IReadOnlyList<string> SourceModuleIds { get; }

    /// <summary>
    /// Gets the normalized shorthand authoring styles targeted by this override rule.
    /// </summary>
    public IReadOnlyList<string> AuthoringStyles { get; }

    /// <summary>
    /// Gets the effective API major version applied when this override rule matches.
    /// </summary>
    public int? ApiVersionMajor { get; }

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
