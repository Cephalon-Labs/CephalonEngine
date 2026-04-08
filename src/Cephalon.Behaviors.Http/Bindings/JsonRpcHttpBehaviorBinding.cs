using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Cephalon.Abstractions.Behaviors;
using Cephalon.AspNetCore.Hosting;
using Cephalon.Behaviors.Http.Abstractions;
using Cephalon.Behaviors.Http.Hosting;
using Cephalon.Behaviors.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Cephalon.Behaviors.Http.Bindings;

// ─── source-generated serialiser context (zero-reflection) ───────────────────

/// <summary>Represents a JSON-RPC 2.0 request envelope.</summary>
public sealed class JsonRpcRequest
{
    /// <summary>Gets or sets the JSON-RPC version string (must be "2.0").</summary>
    [JsonPropertyName("jsonrpc")]
    public string? Jsonrpc { get; set; }

    /// <summary>Gets or sets the method name.</summary>
    [JsonPropertyName("method")]
    public string? Method { get; set; }

    /// <summary>Gets or sets the optional parameters.</summary>
    [JsonPropertyName("params")]
    public JsonElement? Params { get; set; }

    /// <summary>Gets or sets the request identifier.</summary>
    [JsonPropertyName("id")]
    public JsonElement? Id { get; set; }
}

/// <summary>Represents a successful JSON-RPC 2.0 response envelope.</summary>
public sealed class JsonRpcSuccessResponse
{
    /// <summary>Gets or sets the JSON-RPC version (always "2.0").</summary>
    [JsonPropertyName("jsonrpc")]
    public string Jsonrpc { get; set; } = "2.0";

    /// <summary>Gets or sets the result payload.</summary>
    [JsonPropertyName("result")]
    public object? Result { get; set; }

    /// <summary>Gets or sets the echoed request identifier.</summary>
    [JsonPropertyName("id")]
    public JsonElement? Id { get; set; }
}

/// <summary>Represents an error JSON-RPC 2.0 response envelope.</summary>
public sealed class JsonRpcErrorResponse
{
    /// <summary>Gets or sets the JSON-RPC version (always "2.0").</summary>
    [JsonPropertyName("jsonrpc")]
    public string Jsonrpc { get; set; } = "2.0";

    /// <summary>Gets or sets the error object.</summary>
    [JsonPropertyName("error")]
    public JsonRpcError Error { get; set; } = new();

    /// <summary>Gets or sets the echoed request identifier (null for parse errors).</summary>
    [JsonPropertyName("id")]
    public JsonElement? Id { get; set; }
}

/// <summary>Represents the error object inside a JSON-RPC 2.0 error response.</summary>
public sealed class JsonRpcError
{
    /// <summary>Gets or sets the numeric error code.</summary>
    [JsonPropertyName("code")]
    public int Code { get; set; }

    /// <summary>Gets or sets the short error message.</summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>Gets or sets optional additional error data.</summary>
    [JsonPropertyName("data")]
    public string? Data { get; set; }
}

[JsonSerializable(typeof(JsonRpcRequest))]
[JsonSerializable(typeof(JsonRpcSuccessResponse))]
[JsonSerializable(typeof(JsonRpcErrorResponse))]
[JsonSerializable(typeof(object))]
internal sealed partial class JsonRpcSerializerContext : JsonSerializerContext { }

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// JSON-RPC 2.0 HTTP transport binding (transport ID: <c>http.jsonrpc</c>).
/// Accepts canonical routes such as <c>POST /json-rpc/v1/cart/get</c>, while optionally keeping the
/// legacy <c>/behaviors/{id}/jsonrpc</c> alias enabled for compatibility, and returns a JSON-RPC
/// 2.0 response or error object.
/// Per the JSON-RPC 2.0 specification the HTTP status is always <c>200 OK</c>.
/// </summary>
/// <remarks>
/// Canonical routes are derived from the shared <see cref="BehaviorApiSurfaceDescriptor" /> plus
/// the configured JSON-RPC prefix (canonically <c>ApiRoutes:Prefixes:JsonRpc</c>) and the
/// resolved default behavior document name.
/// </remarks>
public sealed class JsonRpcHttpBehaviorBinding : IHttpBehaviorBinding
{
    private readonly BehaviorApiSurfaceRouteResolver routeResolver;

    /// <summary>
    /// Initializes a new <see cref="JsonRpcHttpBehaviorBinding" />.
    /// </summary>
    /// <param name="configuration">
    /// Optional configuration used to resolve canonical behavior transport routes.
    /// When omitted, the binding falls back to the default <c>/json-rpc/v1</c> route policy.
    /// </param>
    public JsonRpcHttpBehaviorBinding(IConfiguration? configuration = null)
    {
        routeResolver = new BehaviorApiSurfaceRouteResolver(configuration is null
            ? new ApiRoutesOptions()
            : ApiRoutesOptions.FromConfiguration(configuration));
    }

    /// <inheritdoc />
    public string TransportId => "http.jsonrpc";

    /// <inheritdoc />
    public Task MapAsync(
        WebApplication app,
        BehaviorTopologyDescriptor descriptor,
        BehaviorDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(dispatcher);

        foreach (var route in routeResolver.ResolveRoutes(TransportId, descriptor))
        {
            app.MapPost(route, async (HttpContext ctx) =>
            {
                // G-RPC-02: Malformed JSON → error -32700
                JsonNode? requestNode;
                try
                {
                    using var doc = await JsonDocument.ParseAsync(ctx.Request.Body, cancellationToken: ctx.RequestAborted)
                        .ConfigureAwait(false);
                    requestNode = JsonNode.Parse(doc.RootElement.GetRawText());
                }
                catch (JsonException ex)
                {
                    return BuildErrorResult(null, -32700, "Parse error", ex.Message);
                }

                var id = requestNode?["id"];

                // G-RPC-01: Validate jsonrpc == "2.0"
                var jsonrpcVersion = requestNode?["jsonrpc"]?.GetValue<string>();
                if (!string.Equals(jsonrpcVersion, "2.0", StringComparison.Ordinal))
                {
                    return BuildErrorResult(id, -32600, "Invalid Request", "jsonrpc must be \"2.0\"");
                }

                var method = requestNode?["method"]?.GetValue<string>();
                var paramsNode = requestNode?["params"];

                if (method is null)
                {
                    return BuildErrorResult(id, -32600, "Invalid Request", "Missing 'method' field");
                }

                // G-RPC-03: Accept canonical "handle" plus common aliases "invoke" and "execute".
                if (!string.Equals(method, "handle", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(method, "invoke", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(method, "execute", StringComparison.OrdinalIgnoreCase))
                {
                    return BuildErrorResult(id, -32601, "Method not found", $"Unknown method '{method}'");
                }

                object input = paramsNode is not null
                    ? JsonSerializer.Deserialize<object>(paramsNode.ToJsonString())!
                    : JsonSerializer.Deserialize<object>("{}")!;

                try
                {
                    var context = DefaultBehaviorContext.From(ctx, descriptor.Id);
                    var result = await dispatcher.DispatchAsync(descriptor.Id, input, context, ctx.RequestAborted)
                        .ConfigureAwait(false);

                    // G-RPC-04 / G-RPC-06: Success — always 200 OK, application/json
                    return BuildSuccessResult(id, result);
                }
                catch (BehaviorNotFoundException ex)
                {
                    return BuildErrorResult(id, -32601, "Method not found", ex.Message);
                }
                catch (BehaviorSecurityException ex)
                {
                    // G-RPC-07
                    return BuildErrorResult(id, -32003, "Security violation", ex.Message);
                }
                catch (Exception ex)
                {
                    return BuildErrorResult(id, -32603, "Internal error", ex.Message);
                }
            });
        }

        return Task.CompletedTask;
    }

    // G-RPC-05/06: always 200 OK, Content-Type: application/json
    private static IResult BuildSuccessResult(JsonNode? id, object? result)
    {
        var response = new JsonRpcSuccessResponse
        {
            Result = result,
            Id = id is not null ? JsonDocument.Parse(id.ToJsonString()).RootElement : (JsonElement?)null
        };
        // Use default serializer (not source-gen context) because the Result property
        // contains domain types that are not registered in JsonRpcSerializerContext.
        var json = JsonSerializer.Serialize(response);
        return Results.Content(json, "application/json", statusCode: 200);
    }

    private static IResult BuildErrorResult(JsonNode? id, int code, string message, string? data = null)
    {
        var response = new JsonRpcErrorResponse
        {
            Error = new JsonRpcError { Code = code, Message = message, Data = data },
            Id = id is not null ? JsonDocument.Parse(id.ToJsonString()).RootElement : (JsonElement?)null
        };
        var json = JsonSerializer.Serialize(response, JsonRpcSerializerContext.Default.JsonRpcErrorResponse);
        // G-RPC-06: HTTP status always 200
        return Results.Content(json, "application/json", statusCode: 200);
    }
}
