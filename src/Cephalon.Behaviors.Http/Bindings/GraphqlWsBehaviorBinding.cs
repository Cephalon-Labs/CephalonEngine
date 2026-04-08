using System.Buffers;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cephalon.Abstractions.Behaviors;
using Cephalon.AspNetCore.Hosting;
using Cephalon.Behaviors.Http.Abstractions;
using Cephalon.Behaviors.Http.Hosting;
using Cephalon.Behaviors.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Cephalon.Behaviors.Http.Bindings;

/// <summary>
/// GraphQL over WebSocket transport binding (transport ID: <c>http.graphql-ws</c>).
/// Upgrades canonical routes such as <c>GET /graphql-ws/v1/cart/get</c> to a WebSocket connection.
/// The connection implements the
/// <c>graphql-transport-ws</c> sub-protocol:
/// <c>connection_init</c> → <c>connection_ack</c> → <c>subscribe</c> → <c>next</c> → <c>complete</c>.
/// </summary>
public sealed class GraphqlWsBehaviorBinding : IHttpBehaviorBinding
{
    private static readonly TimeSpan ConnectionInitTimeout = TimeSpan.FromSeconds(5);
    private readonly BehaviorApiSurfaceRouteResolver routeResolver;

    /// <summary>
    /// Initializes a new <see cref="GraphqlWsBehaviorBinding" />.
    /// </summary>
    /// <param name="configuration">
    /// Optional configuration used to resolve canonical behavior transport routes.
    /// When omitted, the binding falls back to the default <c>/graphql-ws/v1</c> route policy.
    /// </param>
    public GraphqlWsBehaviorBinding(IConfiguration? configuration = null)
    {
        routeResolver = new BehaviorApiSurfaceRouteResolver(configuration is null
            ? new ApiRoutesOptions()
            : ApiRoutesOptions.FromConfiguration(configuration));
    }

    /// <inheritdoc />
    public string TransportId => "http.graphql-ws";

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
            app.MapGet(route, async (HttpContext ctx) =>
            {
                // G-WS-01: must be a WebSocket upgrade request
                if (!ctx.WebSockets.IsWebSocketRequest)
                {
                    ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
                    return;
                }

                // G-GQL-WS-01: correct subprotocol name
                var logger = ctx.RequestServices.GetService<ILoggerFactory>()
                    ?.CreateLogger<GraphqlWsBehaviorBinding>();
                using var ws = await ctx.WebSockets.AcceptWebSocketAsync("graphql-transport-ws").ConfigureAwait(false);
                await HandleGraphqlWsAsync(ws, ctx, descriptor.Id, dispatcher, logger).ConfigureAwait(false);
            }).ExcludeFromDescription();
        }

        return Task.CompletedTask;
    }

    private static async Task HandleGraphqlWsAsync(
        WebSocket ws,
        HttpContext ctx,
        string behaviorId,
        BehaviorDispatcher dispatcher,
        ILogger? logger)
    {
        // G-GQL-WS-04: track active subscription ids
        var activeSubscriptions = new ConcurrentDictionary<string, CancellationTokenSource>(StringComparer.Ordinal);

        var buffer = ArrayPool<byte>.Shared.Rent(8192);
        try
        {
            // G-GQL-WS-03: connection_init must arrive within 5 s
            using var initTimeoutCts = new CancellationTokenSource(ConnectionInitTimeout);
            using var initLinked = CancellationTokenSource.CreateLinkedTokenSource(
                ctx.RequestAborted, initTimeoutCts.Token);

            bool connected = false;

            while (ws.State == WebSocketState.Open)
            {
                var readCt = connected ? ctx.RequestAborted : initLinked.Token;

                WebSocketReceiveResult result;
                var messageBuffer = new List<byte>();

                try
                {
                    do
                    {
                        result = await ws.ReceiveAsync(buffer, readCt).ConfigureAwait(false);
                        messageBuffer.AddRange(buffer[..result.Count]);
                    } while (!result.EndOfMessage);
                }
                catch (OperationCanceledException) when (initTimeoutCts.IsCancellationRequested && !connected)
                {
                    // G-GQL-WS-03: connection_init timeout → close code 4408
                    await ws.CloseAsync((WebSocketCloseStatus)4408, "Connection initialisation timeout",
                        CancellationToken.None).ConfigureAwait(false);
                    return;
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    if (logger is not null) GraphqlWsBehaviorBindingLogs.UnexpectedReceiveError(logger, ex, behaviorId);
                    ws.Abort();
                    return;
                }

                if (result.MessageType == WebSocketMessageType.Close ||
                    ws.State == WebSocketState.CloseReceived)
                {
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", CancellationToken.None)
                        .ConfigureAwait(false);
                    return;
                }

                JsonNode? message;
                try
                {
                    message = JsonNode.Parse(messageBuffer.ToArray());
                }
                catch (Exception ex)
                {
                    if (logger is not null) GraphqlWsBehaviorBindingLogs.MalformedJsonFrame(logger, ex, behaviorId);
                    continue;
                }

                var type = message?["type"]?.GetValue<string>();

                switch (type)
                {
                    case "connection_init":
                        connected = true;
                        await SendWsMessageAsync(ws, new { type = "connection_ack" }, ctx.RequestAborted)
                            .ConfigureAwait(false);
                        break;

                    case "subscribe":
                    {
                        var id = message?["id"]?.GetValue<string>() ?? "1";

                        // G-GQL-WS-04: duplicate subscription id → close code 4409
                        var newCts = new CancellationTokenSource();
                        if (!activeSubscriptions.TryAdd(id, newCts))
                        {
                            newCts.Dispose();
                            await ws.CloseAsync((WebSocketCloseStatus)4409,
                                $"Subscriber for '{id}' already exists",
                                CancellationToken.None).ConfigureAwait(false);
                            return;
                        }

                        var subCts = newCts;
                        using var subLinked = CancellationTokenSource.CreateLinkedTokenSource(
                            ctx.RequestAborted, subCts.Token);

                        var payload = message?["payload"];
                        var variablesRaw = payload?["variables"]?.ToJsonString();

                        // Use the variables object as the behavior input.
                        // This maps GraphQL variables to the behavior's typed input model.
                        object input = variablesRaw is not null
                            ? JsonSerializer.Deserialize<object>(variablesRaw)!
                            : JsonSerializer.Deserialize<object>("{}")!;

                        var context = DefaultBehaviorContext.From(ctx, behaviorId);

                        try
                        {
                            var dispatchResult = await dispatcher.DispatchAsync(
                                behaviorId, input, context, subLinked.Token).ConfigureAwait(false);

                            await SendWsMessageAsync(ws, new
                            {
                                type = "next",
                                id,
                                payload = new { data = dispatchResult }
                            }, ctx.RequestAborted).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            // subscription cancelled by client — no error event
                        }
                        catch (Exception ex)
                        {
                            // G-GQL-WS-05: error envelope
                            await SendWsMessageAsync(ws, new
                            {
                                type = "error",
                                id,
                                payload = new[] { new { message = ex.Message } }
                            }, ctx.RequestAborted).ConfigureAwait(false);
                        }
                        finally
                        {
                            activeSubscriptions.TryRemove(id, out var removed);
                            removed?.Dispose();
                        }

                        // send complete only if the subscription was not cancelled
                        if (!subLinked.Token.IsCancellationRequested)
                        {
                            await SendWsMessageAsync(ws, new { type = "complete", id }, ctx.RequestAborted)
                                .ConfigureAwait(false);
                        }

                        break;
                    }

                    case "complete":
                    {
                        // G-GQL-WS-02: client cancels a subscription
                        var id = message?["id"]?.GetValue<string>();
                        if (id is not null && activeSubscriptions.TryRemove(id, out var cts))
                        {
                            await cts.CancelAsync().ConfigureAwait(false);
                            cts.Dispose();
                        }

                        break;
                    }

                    case "connection_terminate":
                        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Terminated", CancellationToken.None)
                            .ConfigureAwait(false);
                        return;
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);

            // clean up any lingering subscriptions
            foreach (var cts in activeSubscriptions.Values)
            {
                cts.Dispose();
            }
        }
    }

    private static async Task SendWsMessageAsync(WebSocket ws, object payload, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(payload);
        var bytes = Encoding.UTF8.GetBytes(json);
        await ws.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, ct).ConfigureAwait(false);
    }
}

internal static partial class GraphqlWsBehaviorBindingLogs
{
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Unexpected error receiving GraphQL WebSocket message for behavior '{BehaviorId}', aborting connection.")]
    public static partial void UnexpectedReceiveError(ILogger logger, Exception exception, string behaviorId);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Skipping malformed JSON frame on GraphQL WebSocket for behavior '{BehaviorId}'.")]
    public static partial void MalformedJsonFrame(ILogger logger, Exception exception, string behaviorId);
}
