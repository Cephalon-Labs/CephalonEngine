using Microsoft.Extensions.Configuration;

namespace Cephalon.AspNetCore.Documentation;

/// <summary>
/// Configures the host-level OpenAPI JSON and Scalar UI endpoints exposed by Cephalon ASP.NET Core hosts.
/// </summary>
/// <remarks>
/// These options stay in the ASP.NET Core adapter because they describe HTTP route layout for generated
/// documentation assets rather than engine-core behavior.
/// </remarks>
public sealed class OpenApiEndpointOptions
{
    private static readonly int[] DefaultBehaviorRestDocumentedStatusCodes =
    [
        200,
        201,
        202,
        204,
        400,
        401,
        403,
        404,
        409,
        500
    ];

    /// <summary>
    /// Gets the root configuration section used for OpenAPI endpoint routing.
    /// </summary>
    public const string SectionName = "OpenApi";

    /// <summary>
    /// Initializes a new <see cref="OpenApiEndpointOptions" /> with the canonical Cephalon OpenAPI and Scalar routes.
    /// </summary>
    public OpenApiEndpointOptions()
    {
    }

    /// <summary>
    /// Gets or sets the route pattern used by <c>MapOpenApi(...)</c>.
    /// </summary>
    /// <remarks>
    /// The pattern must include the <c>{documentName}</c> placeholder so versioned and named documents remain addressable.
    /// </remarks>
    public string RoutePattern { get; set; } = "/openapi/{documentName}.json";

    /// <summary>
    /// Gets or sets the route prefix used by the Scalar UI.
    /// </summary>
    /// <remarks>
    /// The value may be supplied with or without a leading slash. Cephalon normalizes it to a rooted path such as <c>/scalar</c>.
    /// </remarks>
    public string ScalarRoutePrefix { get; set; } = "/scalar";

    /// <summary>
    /// Gets or sets the HTTP status codes that Cephalon's behavior-owned REST helpers publish in OpenAPI documents by default.
    /// </summary>
    /// <remarks>
    /// This list controls documentation metadata only. It does not change the runtime HTTP status codes emitted by ASP.NET Core.
    /// </remarks>
    public IReadOnlyList<int> BehaviorRestDocumentedStatusCodes { get; set; } = DefaultBehaviorRestDocumentedStatusCodes;

    /// <summary>
    /// Binds and normalizes OpenAPI endpoint options from configuration.
    /// </summary>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="sectionPath">The configuration section path to bind.</param>
    /// <returns>The normalized OpenAPI endpoint options.</returns>
    public static OpenApiEndpointOptions FromConfiguration(
        IConfiguration configuration,
        string sectionPath = SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration.GetSection(sectionPath);
        var routePattern = section["RoutePattern"];
        var scalarRoutePrefix = section["Scalar:RoutePrefix"] ?? section["ScalarRoutePrefix"];
        var documentedStatusCodes = NormalizeStatusCodes(
            ReadStatusCodes(section.GetSection("BehaviorRest:DocumentedStatusCodes")) ?? DefaultBehaviorRestDocumentedStatusCodes);

        return new OpenApiEndpointOptions
        {
            RoutePattern = NormalizeRoutePattern(routePattern),
            ScalarRoutePrefix = NormalizeRoutePrefix(scalarRoutePrefix),
            BehaviorRestDocumentedStatusCodes = documentedStatusCodes
        };
    }

    private static int[] NormalizeStatusCodes(IEnumerable<int> statusCodes)
    {
        ArgumentNullException.ThrowIfNull(statusCodes);

        var normalized = statusCodes
            .Distinct()
            .ToArray();

        foreach (var statusCode in normalized)
        {
            if (statusCode is < 100 or > 599)
            {
                throw new InvalidOperationException(
                    "OpenApi:BehaviorRest:DocumentedStatusCodes entries must be valid HTTP status codes between 100 and 599.");
            }
        }

        return normalized;
    }

    private static int[]? ReadStatusCodes(IConfigurationSection section)
    {
        var configuredValues = section.GetChildren()
            .Select(static child => child.Value)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value =>
            {
                if (!int.TryParse(value, out var statusCode))
                {
                    throw new InvalidOperationException(
                        "OpenApi:BehaviorRest:DocumentedStatusCodes entries must be valid integer HTTP status codes.");
                }

                return statusCode;
            })
            .ToArray();

        return configuredValues.Length == 0
            ? null
            : configuredValues;
    }

    private static string NormalizeRoutePattern(string? routePattern)
    {
        var normalized = string.IsNullOrWhiteSpace(routePattern)
            ? "/openapi/{documentName}.json"
            : routePattern.Trim();

        if (!normalized.StartsWith('/'))
        {
            normalized = $"/{normalized}";
        }

        if (!normalized.Contains("{documentName}", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "OpenApi:RoutePattern must include the '{documentName}' placeholder so Cephalon can expose multiple documents safely.");
        }

        return normalized;
    }

    private static string NormalizeRoutePrefix(string? routePrefix)
    {
        var normalized = string.IsNullOrWhiteSpace(routePrefix)
            ? "/scalar"
            : routePrefix.Trim();

        if (!normalized.StartsWith('/'))
        {
            normalized = $"/{normalized}";
        }

        normalized = normalized.Length > 1
            ? normalized.TrimEnd('/')
            : normalized;

        if (normalized == "/")
        {
            throw new InvalidOperationException(
                "OpenApi:Scalar:RoutePrefix must resolve to a non-root path such as '/scalar'.");
        }

        return normalized;
    }
}
