using Cephalon.AspNetCore.Transports.Rest;

namespace Cephalon.AspNetCore.Hosting;

/// <summary>
/// Describes one host-level override rule for descriptor-backed REST shorthand candidates.
/// </summary>
public sealed class RestEndpointOverrideOptions
{
    private static readonly string[] DefaultAuthoringStyles =
    [
        RestEndpointRuntimeMetadata.BehaviorModuleGeneratedAuthoringStyle,
        RestEndpointRuntimeMetadata.BehaviorModuleProfileAuthoringStyle
    ];

    private static readonly HashSet<string> SupportedAuthoringStyles = new(
        DefaultAuthoringStyles,
        StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="RestEndpointOverrideOptions" /> class.
    /// </summary>
    /// <param name="id">The stable override identifier.</param>
    /// <param name="behaviorIds">The behavior identifiers targeted by the override rule.</param>
    /// <param name="sourceModuleIds">The source-module identifiers targeted by the override rule.</param>
    /// <param name="authoringStyles">
    /// The shorthand authoring styles targeted by the override rule. When omitted, the rule
    /// targets both <c>behavior-module-profile</c> and <c>behavior-module-generated</c>.
    /// </param>
    /// <param name="apiVersionMajor">
    /// The effective API major version applied when the rule matches a shorthand candidate.
    /// </param>
    /// <param name="method">
    /// The effective HTTP method applied when the rule matches a shorthand candidate.
    /// </param>
    public RestEndpointOverrideOptions(
        string id,
        IReadOnlyList<string>? behaviorIds = null,
        IReadOnlyList<string>? sourceModuleIds = null,
        IReadOnlyList<string>? authoringStyles = null,
        int? apiVersionMajor = null,
        string? method = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        Id = id.Trim();
        BehaviorIds = NormalizeList(behaviorIds);
        SourceModuleIds = NormalizeList(sourceModuleIds);
        AuthoringStyles = NormalizeAuthoringStyles(authoringStyles);
        Method = NormalizeMethod(method);

        if (BehaviorIds.Count == 0 && SourceModuleIds.Count == 0)
        {
            throw new ArgumentException(
                "REST endpoint override rules must target at least one behavior id or source module id.",
                nameof(behaviorIds));
        }

        if (apiVersionMajor.HasValue && apiVersionMajor <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(apiVersionMajor),
                apiVersionMajor,
                "REST endpoint override rules must define a positive API major version.");
        }

        ApiVersionMajor = apiVersionMajor;

        if (!ApiVersionMajor.HasValue && Method is null)
        {
            throw new ArgumentException(
                "REST endpoint override rules must define at least one override action such as ApiVersionMajor or Method.",
                nameof(apiVersionMajor));
        }
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

    /// <summary>
    /// Gets the effective HTTP method applied when this override rule matches.
    /// </summary>
    public string? Method { get; }

    /// <summary>
    /// Gets a value indicating whether any targeting values or override actions were explicitly supplied.
    /// </summary>
    public bool HasValues =>
        BehaviorIds.Count > 0 ||
        SourceModuleIds.Count > 0 ||
        ApiVersionMajor.HasValue ||
        Method is not null;

    private static string[] NormalizeList(IReadOnlyList<string>? values)
    {
        return values?
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }

    private static string[] NormalizeAuthoringStyles(IReadOnlyList<string>? values)
    {
        var normalized = NormalizeList(values);
        if (normalized.Length == 0)
        {
            return DefaultAuthoringStyles;
        }

        var unsupportedStyle = normalized.FirstOrDefault(style => !SupportedAuthoringStyles.Contains(style));
        if (unsupportedStyle is not null)
        {
            throw new ArgumentException(
                $"REST endpoint override style '{unsupportedStyle}' is not supported. " +
                $"Supported shorthand authoring styles: {string.Join(", ", DefaultAuthoringStyles)}.",
                nameof(values));
        }

        return normalized;
    }

    private static string? NormalizeMethod(string? method)
    {
        if (string.IsNullOrWhiteSpace(method))
        {
            return null;
        }

        return method.Trim().ToUpperInvariant() switch
        {
            "GET" => "GET",
            "POST" => "POST",
            "PUT" => "PUT",
            "PATCH" => "PATCH",
            "DELETE" => "DELETE",
            _ => throw new ArgumentException(
                $"REST endpoint override method '{method}' is not supported. Supported methods: GET, POST, PUT, PATCH, DELETE.",
                nameof(method))
        };
    }
}
