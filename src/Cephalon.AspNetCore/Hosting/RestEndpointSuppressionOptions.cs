using Cephalon.AspNetCore.Transports.Rest;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Cephalon.AspNetCore.Hosting;

/// <summary>
/// Describes one host-level suppression rule for descriptor-backed REST shorthand candidates.
/// </summary>
public sealed class RestEndpointSuppressionOptions
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
    /// Initializes a new instance of the <see cref="RestEndpointSuppressionOptions" /> class.
    /// </summary>
    /// <param name="id">The stable suppression identifier.</param>
    /// <param name="behaviorIds">The behavior identifiers targeted by the suppression rule.</param>
    /// <param name="sourceModuleIds">The source-module identifiers targeted by the suppression rule.</param>
    /// <param name="authoringStyles">
    /// The shorthand authoring styles targeted by the suppression rule. When omitted, the rule
    /// targets both `behavior-module-profile` and `behavior-module-generated`.
    /// </param>
    /// <param name="apiVersionMajors">
    /// The effective API major versions targeted by the suppression rule before any override
    /// actions are applied.
    /// </param>
    /// <param name="methods">
    /// The effective HTTP methods targeted by the suppression rule before any override actions are
    /// applied.
    /// </param>
    /// <param name="relativePatterns">
    /// The shorthand relative route patterns targeted by the suppression rule before any override
    /// actions are applied.
    /// </param>
    /// <param name="routeGroupPrefixes">
    /// The published route-group prefixes targeted by the suppression rule before any override
    /// actions are applied.
    /// </param>
    public RestEndpointSuppressionOptions(
        string id,
        IReadOnlyList<string>? behaviorIds = null,
        IReadOnlyList<string>? sourceModuleIds = null,
        IReadOnlyList<string>? authoringStyles = null,
        IReadOnlyList<int>? apiVersionMajors = null,
        IReadOnlyList<string>? methods = null,
        IReadOnlyList<string>? relativePatterns = null,
        IReadOnlyList<string>? routeGroupPrefixes = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        Id = id.Trim();
        BehaviorIds = NormalizeList(behaviorIds);
        SourceModuleIds = NormalizeList(sourceModuleIds);
        AuthoringStyles = NormalizeAuthoringStyles(authoringStyles);
        ApiVersionMajors = NormalizePositiveIntegers(
            apiVersionMajors,
            nameof(apiVersionMajors),
            "REST endpoint suppression API major versions must be positive integers.");
        Methods = NormalizeMethods(
            methods,
            nameof(methods),
            "REST endpoint suppression method");
        RelativePatterns = NormalizeRoutePatterns(
            relativePatterns,
            nameof(relativePatterns),
            "REST endpoint suppression relative pattern");
        RouteGroupPrefixes = NormalizeRoutePatterns(
            routeGroupPrefixes,
            nameof(routeGroupPrefixes),
            "REST endpoint suppression route-group prefix");

        if (BehaviorIds.Count == 0 && SourceModuleIds.Count == 0)
        {
            throw new ArgumentException(
                "REST endpoint suppression rules must target at least one behavior id or source module id.",
                nameof(behaviorIds));
        }
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

    /// <summary>
    /// Gets the effective API major versions targeted by this suppression rule before override actions are applied.
    /// </summary>
    public IReadOnlyList<int> ApiVersionMajors { get; }

    /// <summary>
    /// Gets the effective HTTP methods targeted by this suppression rule before override actions are applied.
    /// </summary>
    public IReadOnlyList<string> Methods { get; }

    /// <summary>
    /// Gets the shorthand relative route patterns targeted by this suppression rule before override actions are applied.
    /// </summary>
    public IReadOnlyList<string> RelativePatterns { get; }

    /// <summary>
    /// Gets the published route-group prefixes targeted by this suppression rule before override actions are applied.
    /// </summary>
    public IReadOnlyList<string> RouteGroupPrefixes { get; }

    /// <summary>
    /// Gets a value indicating whether any targeting values were explicitly supplied.
    /// </summary>
    public bool HasValues =>
        BehaviorIds.Count > 0 ||
        SourceModuleIds.Count > 0 ||
        ApiVersionMajors.Count > 0 ||
        Methods.Count > 0 ||
        RelativePatterns.Count > 0 ||
        RouteGroupPrefixes.Count > 0;

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
                $"REST endpoint suppression style '{unsupportedStyle}' is not supported. " +
                $"Supported shorthand authoring styles: {string.Join(", ", DefaultAuthoringStyles)}.",
                nameof(values));
        }

        return normalized;
    }

    private static int[] NormalizePositiveIntegers(
        IReadOnlyList<int>? values,
        string paramName,
        string errorMessage)
    {
        if (values is null)
        {
            return [];
        }

        var normalized = values
            .Distinct()
            .OrderBy(static value => value)
            .ToArray();
        if (normalized.Any(static value => value <= 0))
        {
            throw new ArgumentOutOfRangeException(paramName, errorMessage);
        }

        return normalized;
    }

    private static string[] NormalizeMethods(
        IReadOnlyList<string>? values,
        string paramName,
        string errorPrefix)
    {
        return values?
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(value => NormalizeMethod(value, paramName, errorPrefix))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }

    private static string NormalizeMethod(string value, string paramName, string errorPrefix)
    {
        return value.Trim().ToUpperInvariant() switch
        {
            "GET" => "GET",
            "POST" => "POST",
            "PUT" => "PUT",
            "PATCH" => "PATCH",
            "DELETE" => "DELETE",
            _ => throw new ArgumentException(
                $"{errorPrefix} '{value}' is not supported. Supported methods: GET, POST, PUT, PATCH, DELETE.",
                paramName)
        };
    }

    private static string[] NormalizeRoutePatterns(
        IReadOnlyList<string>? values,
        string paramName,
        string errorPrefix)
    {
        return values?
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(value => NormalizeRoutePattern(value, paramName, errorPrefix))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }

    private static string NormalizeRoutePattern(string value, string paramName, string errorPrefix)
    {
        var normalized = value.Trim();
        if (!normalized.StartsWith('/'))
        {
            throw new ArgumentException($"{errorPrefix}s must start with '/'.", paramName);
        }

        try
        {
            _ = RoutePatternFactory.Parse(normalized);
        }
        catch (Exception ex)
        {
            throw new ArgumentException(
                $"{errorPrefix} '{value}' is not a valid ASP.NET Core route pattern.",
                paramName,
                ex);
        }

        return normalized;
    }
}
