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
    /// Initializes a new <see cref="ApiRoutesOptions" /> with the canonical Cephalon route-prefix defaults.
    /// </summary>
    public ApiRoutesOptions()
    {
    }

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
    /// Gets or sets the default document/version segment projected into generic behavior transport routes.
    /// </summary>
    public string DefaultBehaviorDocumentName { get; set; } = OpenApiDocumentNames.DefaultDocumentName;

    /// <summary>
    /// Gets or sets a value indicating whether behavior-aware REST endpoints should emit the Cephalon result envelope.
    /// </summary>
    public bool UseResultModelEnvelope { get; set; }

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
        var restPrefix = section["Prefixes:Rest"];
        var graphQlPrefix = section["Prefixes:GraphQL"];
        var jsonRpcPrefix = section["Prefixes:JsonRpc"];
        var grpcPrefix = section["Prefixes:Grpc"];
        var wsPrefix = section["Prefixes:Ws"];
        var ssePrefix = section["Prefixes:Sse"];
        var graphQlWsPrefix = section["Prefixes:GraphQLWs"];
        var graphQlSsePrefix = section["Prefixes:GraphQLSse"];
        var defaultBehaviorDocumentName = section["DefaultBehaviorDocumentName"]?.Trim();
        var normalizedRestPrefix = NormalizePrefix(restPrefix, "/api", allowRoot: true);
        var normalizedWsPrefix = NormalizePrefix(wsPrefix, "/ws");
        var useResultModelEnvelope = bool.TryParse(section["ResultEnvelope:Enabled"], out var envelopeEnabled) && envelopeEnabled;

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
            DefaultBehaviorDocumentName = NormalizeDocumentName(defaultBehaviorDocumentName, configuration),
            UseResultModelEnvelope = useResultModelEnvelope
        };
    }

    private static string NormalizeDocumentName(string? value, IConfiguration configuration)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value.Trim();
        }

        return OpenApiDocumentNames.ResolveBehaviorRouteDefault(configuration);
    }

    private static string NormalizePrefix(string? value, string defaultValue, bool allowRoot = false)
    {
        if (value is null)
        {
            return defaultValue;
        }

        var normalized = value.Trim();

        if (normalized.Length == 0 || normalized == "/")
        {
            if (allowRoot)
            {
                return string.Empty;
            }

            throw new InvalidOperationException(
                "ApiRoutes route prefixes must resolve to non-root paths such as '/graphql'.");
        }

        if (!normalized.StartsWith('/'))
        {
            normalized = $"/{normalized}";
        }

        normalized = normalized.Length > 1
            ? normalized.TrimEnd('/')
            : normalized;

        if (normalized == "/")
        {
            return allowRoot
                ? string.Empty
                : throw new InvalidOperationException(
                    "ApiRoutes route prefixes must resolve to non-root paths such as '/graphql'.");
        }

        return normalized;
    }
}
