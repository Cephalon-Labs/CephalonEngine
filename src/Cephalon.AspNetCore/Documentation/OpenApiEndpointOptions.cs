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
    /// <summary>
    /// Gets the root configuration section used for OpenAPI endpoint routing.
    /// </summary>
    public const string SectionName = "OpenApi";

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

        return new OpenApiEndpointOptions
        {
            RoutePattern = NormalizeRoutePattern(routePattern),
            ScalarRoutePrefix = NormalizeRoutePrefix(scalarRoutePrefix)
        };
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
