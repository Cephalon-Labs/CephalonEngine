using Cephalon.AspNetCore.Documentation;
using Microsoft.Extensions.Configuration;

namespace Cephalon.AspNetCore.Hosting;

/// <summary>
/// Configures host-level HTTP route prefixes for Cephalon ASP.NET Core transports.
/// </summary>
/// <remarks>
/// These settings describe the public HTTP surface of the ASP.NET Core adapter. They intentionally stay out of the engine core.
/// </remarks>
public sealed class ApiRoutesOptions
{
    /// <summary>
    /// Gets the configuration section used for API route settings.
    /// </summary>
    public const string SectionName = "ApiRoutes";

    /// <summary>
    /// Gets or sets the root prefix used by the built-in REST transport mapper.
    /// </summary>
    public string RestPrefix { get; set; } = "/api";

    /// <summary>
    /// Gets or sets the canonical prefix used by the generic behavior REST binding surface.
    /// </summary>
    public string BehaviorRestPrefix { get; set; } = "/api/behaviors";

    /// <summary>
    /// Gets or sets the canonical prefix used by the generic behavior JSON-RPC binding surface.
    /// </summary>
    public string JsonRpcPrefix { get; set; } = "/rpc";

    /// <summary>
    /// Gets or sets the canonical prefix used by the generic behavior Server-Sent Events binding surface.
    /// </summary>
    public string SsePrefix { get; set; } = "/events";

    /// <summary>
    /// Gets or sets the canonical prefix used by the generic behavior WebSocket binding surface.
    /// </summary>
    public string WebSocketPrefix { get; set; } = "/ws";

    /// <summary>
    /// Gets or sets the default document/version segment projected into generic behavior transport routes.
    /// </summary>
    public string DefaultBehaviorDocumentName { get; set; } = OpenApiDocumentNames.DefaultDocumentName;

    /// <summary>
    /// Gets or sets a value indicating whether legacy <c>/behaviors/{id}/...</c> aliases remain active.
    /// </summary>
    public bool MapLegacyBehaviorRoutes { get; set; } = true;

    /// <summary>
    /// Binds and normalizes API route settings from configuration.
    /// </summary>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="sectionPath">The configuration section path to bind.</param>
    /// <returns>The normalized route settings.</returns>
    public static ApiRoutesOptions FromConfiguration(
        IConfiguration configuration,
        string sectionPath = SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration.GetSection(sectionPath);
        var restPrefix = section["RestPrefix"] ?? section["Prefixes:Rest"];
        var behaviorRestPrefix = section["BehaviorRestPrefix"] ?? section["Prefixes:BehaviorRest"];
        var jsonRpcPrefix = section["JsonRpcPrefix"] ?? section["Prefixes:JsonRpc"];
        var ssePrefix = section["SsePrefix"] ?? section["Prefixes:Sse"];
        var webSocketPrefix = section["WebSocketPrefix"] ?? section["Prefixes:WebSocket"];
        var defaultBehaviorDocumentName = section["DefaultBehaviorDocumentName"]?.Trim();
        var mapLegacyBehaviorRoutes = section["MapLegacyBehaviorRoutes"] ?? section["BehaviorLegacyAliases"];

        return new ApiRoutesOptions
        {
            RestPrefix = NormalizePrefix(restPrefix, "/api"),
            BehaviorRestPrefix = NormalizePrefix(behaviorRestPrefix, "/api/behaviors"),
            JsonRpcPrefix = NormalizePrefix(jsonRpcPrefix, "/rpc"),
            SsePrefix = NormalizePrefix(ssePrefix, "/events"),
            WebSocketPrefix = NormalizePrefix(webSocketPrefix, "/ws"),
            DefaultBehaviorDocumentName = NormalizeDocumentName(defaultBehaviorDocumentName, configuration),
            MapLegacyBehaviorRoutes = GetBoolean(mapLegacyBehaviorRoutes, defaultValue: true)
        };
    }

    private static bool GetBoolean(string? value, bool defaultValue) =>
        bool.TryParse(value, out var parsed) ? parsed : defaultValue;

    private static string NormalizeDocumentName(string? value, IConfiguration configuration)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value.Trim();
        }

        return OpenApiDocumentNames.ResolveDefault(configuration);
    }

    private static string NormalizePrefix(string? value, string defaultValue)
    {
        var normalized = string.IsNullOrWhiteSpace(value)
            ? defaultValue
            : value.Trim();

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
                "ApiRoutes route prefixes must resolve to non-root paths such as '/api'.");
        }

        return normalized;
    }
}
