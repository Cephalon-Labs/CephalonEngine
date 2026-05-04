using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Cephalon.Abstractions.Modules;
using Cephalon.AspNetCore.Hosting;
using Cephalon.AspNetCore.JsonRpc.Hosting;
using Cephalon.AspNetCore.JsonRpc.Modules;
using Cephalon.Engine.Configuration;
using Cephalon.Tests.Support;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Hosting;

public sealed class JsonRpcErrorResponseHostingTests
{
    private const string EndpointPath = "/json-rpc/error-modes";

    [Fact]
    public async Task JsonRpcEndpoint_ReturnsParseError_WhenRequestBodyIsMalformedJson()
    {
        await using var app = await BuildHostAsync();
        var client = app.GetTestClient();

        using var request = new HttpRequestMessage(HttpMethod.Post, EndpointPath)
        {
            Content = new StringContent("{ this is not json", Encoding.UTF8, "application/json")
        };

        using var response = await client.SendAsync(request);
        var error = await ReadJsonRpcErrorAsync(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("2.0", error.JsonRpc);
        Assert.Equal(-32700, error.Code);
        Assert.Contains("Parse error", error.Message, StringComparison.Ordinal);
        Assert.Null(error.Id);
    }

    [Fact]
    public async Task JsonRpcEndpoint_ReturnsInvalidRequest_WhenJsonRpcVersionIsMissing()
    {
        await using var app = await BuildHostAsync();
        var client = app.GetTestClient();

        var rpcResponse = await client.PostAsJsonAsync(EndpointPath, new
        {
            method = "echo",
            @params = new Dictionary<string, string?> { ["text"] = "hi" },
            id = "req-1"
        });
        var error = await ReadJsonRpcErrorAsync(rpcResponse);

        Assert.Equal(HttpStatusCode.BadRequest, rpcResponse.StatusCode);
        Assert.Equal("2.0", error.JsonRpc);
        Assert.Equal(-32600, error.Code);
        Assert.Contains("Invalid Request", error.Message, StringComparison.Ordinal);
        Assert.Equal("req-1", error.Id);
    }

    [Fact]
    public async Task JsonRpcEndpoint_ReturnsMethodNotFound_WhenMethodIsUnknown()
    {
        await using var app = await BuildHostAsync();
        var client = app.GetTestClient();

        var rpcResponse = await client.PostAsJsonAsync(EndpointPath, new
        {
            jsonrpc = "2.0",
            method = "no.such.method",
            @params = new Dictionary<string, string?>(),
            id = "req-2"
        });
        var error = await ReadJsonRpcErrorAsync(rpcResponse);

        Assert.Equal(HttpStatusCode.OK, rpcResponse.StatusCode);
        Assert.Equal("2.0", error.JsonRpc);
        Assert.Equal(-32601, error.Code);
        Assert.Contains("no.such.method", error.Message, StringComparison.Ordinal);
        Assert.Equal("req-2", error.Id);
    }

    [Fact]
    public async Task JsonRpcEndpoint_ReturnsInvalidParams_WhenRequiredParameterIsMissing()
    {
        await using var app = await BuildHostAsync();
        var client = app.GetTestClient();

        var rpcResponse = await client.PostAsJsonAsync(EndpointPath, new
        {
            jsonrpc = "2.0",
            method = "echo",
            @params = new Dictionary<string, string?>(),
            id = "req-3"
        });
        var error = await ReadJsonRpcErrorAsync(rpcResponse);

        Assert.Equal(HttpStatusCode.OK, rpcResponse.StatusCode);
        Assert.Equal("2.0", error.JsonRpc);
        Assert.Equal(-32602, error.Code);
        Assert.Contains("text", error.Message, StringComparison.Ordinal);
        Assert.Equal("req-3", error.Id);
    }

    [Fact]
    public async Task JsonRpcEndpoint_ReturnsInternalError_WhenHandlerThrowsUnexpected()
    {
        await using var app = await BuildHostAsync();
        var client = app.GetTestClient();

        var rpcResponse = await client.PostAsJsonAsync(EndpointPath, new
        {
            jsonrpc = "2.0",
            method = "boom",
            @params = new Dictionary<string, string?>(),
            id = "req-4"
        });
        var error = await ReadJsonRpcErrorAsync(rpcResponse);

        Assert.Equal(HttpStatusCode.InternalServerError, rpcResponse.StatusCode);
        Assert.Equal("2.0", error.JsonRpc);
        Assert.Equal(-32603, error.Code);
        Assert.Contains("Internal error", error.Message, StringComparison.Ordinal);
        Assert.Equal("req-4", error.Id);
    }

    [Fact]
    public async Task JsonRpcEndpoint_ReturnsResult_WhenRequestIsWellFormed()
    {
        await using var app = await BuildHostAsync();
        var client = app.GetTestClient();

        var rpcResponse = await client.PostAsJsonAsync(EndpointPath, new
        {
            jsonrpc = "2.0",
            method = "echo",
            @params = new Dictionary<string, string?> { ["text"] = "ping" },
            id = "req-5"
        });

        Assert.Equal(HttpStatusCode.OK, rpcResponse.StatusCode);
        var payload = await rpcResponse.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        Assert.Equal("2.0", root.GetProperty("jsonRpc").GetString());
        Assert.Equal("req-5", root.GetProperty("id").GetString());
        Assert.False(root.TryGetProperty("error", out var errorElement) && errorElement.ValueKind != JsonValueKind.Null);
        Assert.Equal("ping", root.GetProperty("result").GetProperty("echoed").GetString());
    }

    private static async Task<WebApplication> BuildHostAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "JsonRpc";
        builder.AddJsonRpcTransport();
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new JsonRpcErrorModesTestModule());
        });

        var app = builder.Build();
        app.MapCephalon();
        await app.StartAsync();
        return app;
    }

    private static async Task<JsonRpcErrorEnvelope> ReadJsonRpcErrorAsync(HttpResponseMessage response)
    {
        var payload = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        var jsonRpc = root.GetProperty("jsonRpc").GetString() ?? string.Empty;
        var error = root.GetProperty("error");
        var code = error.GetProperty("code").GetInt32();
        var message = error.GetProperty("message").GetString() ?? string.Empty;
        string? id = null;
        if (root.TryGetProperty("id", out var idElement) && idElement.ValueKind == JsonValueKind.String)
        {
            id = idElement.GetString();
        }

        return new JsonRpcErrorEnvelope(jsonRpc, code, message, id);
    }

    private sealed record JsonRpcErrorEnvelope(string JsonRpc, int Code, string Message, string? Id);
}

internal sealed class JsonRpcErrorModesTestModule : ModuleBase, IJsonRpcModule
{
    private const string ProtocolVersion = "2.0";

    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "json-rpc-error-modes",
        displayName: "JSON-RPC Error Modes",
        description: "Test module that exercises the canonical JSON-RPC 2.0 error envelope shapes.",
        tags: ["test-only"],
        version: "1.0.0",
        metadata: new Dictionary<string, string>
        {
            ["layer"] = "transport-test",
            ["surface"] = "json-rpc"
        });

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public void MapJsonRpcEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/error-modes");
        group.MapPost("/", HandleAsync);
    }

    private static async Task HandleAsync(HttpContext context)
    {
        // Buffer the body so we can attempt to parse it twice (once for parse-error detection,
        // once for the canonical request shape) without exhausting the request stream.
        string body;
        using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: false))
        {
            body = await reader.ReadToEndAsync(context.RequestAborted);
        }

        JsonElement root;
        try
        {
            using var probe = JsonDocument.Parse(body);
            root = probe.RootElement.Clone();
        }
        catch (JsonException)
        {
            await WriteErrorAsync(
                context,
                statusCode: StatusCodes.Status400BadRequest,
                code: -32700,
                message: "Parse error",
                id: null);
            return;
        }

        if (root.ValueKind != JsonValueKind.Object)
        {
            await WriteErrorAsync(
                context,
                statusCode: StatusCodes.Status400BadRequest,
                code: -32600,
                message: "Invalid Request",
                id: TryReadId(root));
            return;
        }

        var requestId = TryReadId(root);

        if (!root.TryGetProperty("jsonrpc", out var versionElement)
            || versionElement.ValueKind != JsonValueKind.String
            || !string.Equals(versionElement.GetString(), ProtocolVersion, StringComparison.Ordinal))
        {
            await WriteErrorAsync(
                context,
                statusCode: StatusCodes.Status400BadRequest,
                code: -32600,
                message: "Invalid Request: jsonrpc must be \"2.0\".",
                id: requestId);
            return;
        }

        if (!root.TryGetProperty("method", out var methodElement)
            || methodElement.ValueKind != JsonValueKind.String)
        {
            await WriteErrorAsync(
                context,
                statusCode: StatusCodes.Status400BadRequest,
                code: -32600,
                message: "Invalid Request: method is required.",
                id: requestId);
            return;
        }

        var method = methodElement.GetString() ?? string.Empty;
        var hasParams = root.TryGetProperty("params", out var paramsElement)
            && paramsElement.ValueKind == JsonValueKind.Object;

        try
        {
            switch (method)
            {
                case "echo":
                    if (!hasParams
                        || !paramsElement.TryGetProperty("text", out var textElement)
                        || textElement.ValueKind != JsonValueKind.String)
                    {
                        await WriteErrorAsync(
                            context,
                            statusCode: StatusCodes.Status200OK,
                            code: -32602,
                            message: "Invalid params: \"text\" is required.",
                            id: requestId);
                        return;
                    }

                    await WriteResultAsync(
                        context,
                        statusCode: StatusCodes.Status200OK,
                        result: new { echoed = textElement.GetString() },
                        id: requestId);
                    return;

                case "boom":
                    throw new InvalidOperationException("Simulated handler failure for internal-error coverage.");

                default:
                    await WriteErrorAsync(
                        context,
                        statusCode: StatusCodes.Status200OK,
                        code: -32601,
                        message: $"Method '{method}' was not found.",
                        id: requestId);
                    return;
            }
        }
        catch (Exception)
        {
            await WriteErrorAsync(
                context,
                statusCode: StatusCodes.Status500InternalServerError,
                code: -32603,
                message: "Internal error: handler threw an unexpected exception.",
                id: requestId);
        }
    }

    private static string? TryReadId(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (!root.TryGetProperty("id", out var idElement))
        {
            return null;
        }

        return idElement.ValueKind == JsonValueKind.String ? idElement.GetString() : null;
    }

    private static Task WriteErrorAsync(HttpContext context, int statusCode, int code, string message, string? id)
    {
        context.Response.StatusCode = statusCode;
        var envelope = new
        {
            jsonRpc = ProtocolVersion,
            result = (object?)null,
            error = new { code, message },
            id
        };
        return context.Response.WriteAsJsonAsync(envelope, cancellationToken: context.RequestAborted);
    }

    private static Task WriteResultAsync(HttpContext context, int statusCode, object result, string? id)
    {
        context.Response.StatusCode = statusCode;
        var envelope = new
        {
            jsonRpc = ProtocolVersion,
            result,
            error = (object?)null,
            id
        };
        return context.Response.WriteAsJsonAsync(envelope, cancellationToken: context.RequestAborted);
    }
}
