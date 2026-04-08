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
    /// Gets or sets the root prefix used by the built-in GraphQL transport mapper.
    /// </summary>
    public string GraphQLPrefix { get; set; } = "/graphql";

    /// <summary>
    /// Gets or sets the canonical prefix used by the generic behavior JSON-RPC binding surface.
    /// </summary>
    public string JsonRpcPrefix { get; set; } = "/json-rpc";

    /// <summary>
    /// Gets or sets the root prefix used by the built-in gRPC transport mapper.
    /// </summary>
    public string GrpcPrefix { get; set; } = "/grpc";

    /// <summary>
    /// Gets or sets the canonical prefix used by the generic behavior WebSocket binding surface.
    /// </summary>
    public string WsPrefix { get; set; } = "/ws";

    /// <summary>
    /// Gets or sets the canonical prefix used by the generic behavior Server-Sent Events binding surface.
    /// </summary>
    public string SsePrefix { get; set; } = "/sse";

    /// <summary>
    /// Gets or sets the canonical prefix used by the generic behavior GraphQL-over-WebSocket binding surface.
    /// </summary>
    public string GraphQLWsPrefix { get; set; } = "/graphql-ws";

    /// <summary>
    /// Gets or sets the canonical prefix used by the generic behavior GraphQL-over-SSE binding surface.
    /// </summary>
    public string GraphQLSsePrefix { get; set; } = "/graphql-sse";

    /// <summary>
    /// Gets or sets the canonical prefix used by the generic behavior REST binding surface.
    /// </summary>
    /// <remarks>
    /// This compatibility alias mirrors <see cref="RestPrefix" /> unless a legacy behavior-specific prefix is configured.
    /// New configurations should use <c>ApiRoutes:Prefixes:Rest</c>.
    /// </remarks>
    public string BehaviorRestPrefix { get; set; } = "/api";

    /// <summary>
    /// Gets or sets the canonical prefix used by the generic behavior WebSocket binding surface.
    /// </summary>
    /// <remarks>
    /// This compatibility alias mirrors <see cref="WsPrefix" /> and remains for older consumers that referenced the previous property name.
    /// </remarks>
    public string WebSocketPrefix
    {
        get => WsPrefix;
        set => WsPrefix = value;
    }

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
        var restPrefix = section["Prefixes:Rest"] ?? section["RestPrefix"];
        var graphQlPrefix = section["Prefixes:GraphQL"] ?? section["GraphQLPrefix"] ?? section["GraphQlPrefix"];
        var jsonRpcPrefix = section["Prefixes:JsonRpc"] ?? section["JsonRpcPrefix"];
        var grpcPrefix = section["Prefixes:Grpc"] ?? section["GrpcPrefix"];
        var wsPrefix = section["Prefixes:Ws"] ?? section["WsPrefix"] ?? section["WebSocketPrefix"] ?? section["Prefixes:WebSocket"];
        var ssePrefix = section["Prefixes:Sse"] ?? section["SsePrefix"];
        var graphQlWsPrefix = section["Prefixes:GraphQLWs"] ?? section["GraphQLWsPrefix"] ?? section["GraphQlWsPrefix"];
        var graphQlSsePrefix = section["Prefixes:GraphQLSse"] ?? section["GraphQLSsePrefix"] ?? section["GraphQlSsePrefix"];
        var behaviorRestPrefix = section["BehaviorRestPrefix"] ?? section["Prefixes:BehaviorRest"];
        var defaultBehaviorDocumentName = section["DefaultBehaviorDocumentName"]?.Trim();
        var mapLegacyBehaviorRoutes = section["MapLegacyBehaviorRoutes"] ?? section["BehaviorLegacyAliases"];
        var normalizedRestPrefix = NormalizePrefix(restPrefix, "/api");
        var normalizedWsPrefix = NormalizePrefix(wsPrefix, "/ws");

        return new ApiRoutesOptions
        {
            RestPrefix = normalizedRestPrefix,
            GraphQLPrefix = NormalizePrefix(graphQlPrefix, "/graphql"),
            JsonRpcPrefix = NormalizePrefix(jsonRpcPrefix, "/json-rpc"),
            GrpcPrefix = NormalizePrefix(grpcPrefix, "/grpc"),
            WsPrefix = normalizedWsPrefix,
            SsePrefix = NormalizePrefix(ssePrefix, "/sse"),
            GraphQLWsPrefix = NormalizePrefix(graphQlWsPrefix, "/graphql-ws"),
            GraphQLSsePrefix = NormalizePrefix(graphQlSsePrefix, "/graphql-sse"),
            BehaviorRestPrefix = NormalizePrefix(behaviorRestPrefix, normalizedRestPrefix),
            DefaultBehaviorDocumentName = NormalizeDocumentName(defaultBehaviorDocumentName, configuration),
            MapLegacyBehaviorRoutes = GetBoolean(mapLegacyBehaviorRoutes, defaultValue: true),
            WebSocketPrefix = normalizedWsPrefix
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
