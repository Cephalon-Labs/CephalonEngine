using System.Buffers;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
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
/// Bidirectional WebSocket transport binding (transport ID: <c>http.ws</c>).
/// Upgrades canonical routes such as <c>GET /ws/v1/cart/get</c> to a full-duplex WebSocket
/// connection.
/// Each received JSON text frame is dispatched to the behavior and the result
/// is sent back as a JSON text frame. The connection is closed gracefully on
/// client close or cancellation.
/// </summary>
/// <remarks>
/// Canonical routes are derived from the shared <see cref="BehaviorApiSurfaceDescriptor" /> plus
/// the configured WebSocket prefix (canonically <c>ApiRoutes:Prefixes:Ws</c>) and the resolved
/// default behavior document name. <see cref="ApiRoutesOptions.WsPrefix" /> controls the
/// configurable root prefix.
/// </remarks>
public sealed class WebSocketBehaviorBinding : IHttpBehaviorBinding
{
    private readonly BehaviorApiSurfaceRouteResolver routeResolver;

    /// <summary>
    /// Initializes a new <see cref="WebSocketBehaviorBinding" />.
    /// </summary>
    /// <param name="configuration">
    /// Optional configuration used to resolve canonical behavior transport routes.
    /// When omitted, the binding falls back to the default <c>/ws/v1</c> route policy.
    /// </param>
    public WebSocketBehaviorBinding(IConfiguration? configuration = null)
    {
        routeResolver = new BehaviorApiSurfaceRouteResolver(configuration is null
            ? new ApiRoutesOptions()
            : ApiRoutesOptions.FromConfiguration(configuration));
    }

    /// <inheritdoc />
    public string TransportId => "http.ws";

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

                var logger = ctx.RequestServices.GetService<ILoggerFactory>()
                    ?.CreateLogger<WebSocketBehaviorBinding>();
                using var ws = await ctx.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);
                await HandleWebSocketAsync(ws, ctx, descriptor.Id, dispatcher, logger).ConfigureAwait(false);
            }).ExcludeFromDescription();
        }

        return Task.CompletedTask;
    }

    private static async Task HandleWebSocketAsync(
        WebSocket ws,
        HttpContext ctx,
        string behaviorId,
        BehaviorDispatcher dispatcher,
        ILogger? logger)
    {
        // G-WS-05: rent from ArrayPool, release in finally
        var buffer = ArrayPool<byte>.Shared.Rent(4096);
        try
        {
            while (ws.State == WebSocketState.Open && !ctx.RequestAborted.IsCancellationRequested)
            {
                WebSocketReceiveResult result;
                var messageBuffer = new List<byte>();

                try
                {
                    do
                    {
                        result = await ws.ReceiveAsync(buffer, ctx.RequestAborted).ConfigureAwait(false);
                        messageBuffer.AddRange(buffer[..result.Count]);
                    } while (!result.EndOfMessage);
                }
                catch (OperationCanceledException)
                {
                    // G-WS-04: let OperationCanceledException propagate out of the loop
                    break;
                }
                catch (Exception ex)
                {
                    // G-WS-03: abort on unexpected receive error
                    if (logger is not null) WebSocketBehaviorBindingLogs.UnexpectedReceiveError(logger, ex, behaviorId);
                    ws.Abort();
                    return;
                }

                // G-WS-02: handle CloseReceived state
                if (result.MessageType == WebSocketMessageType.Close ||
                    ws.State == WebSocketState.CloseReceived)
                {
                    await ws.CloseAsync(
                        result.CloseStatus ?? WebSocketCloseStatus.NormalClosure,
                        result.CloseStatusDescription ?? "Closed",
                        CancellationToken.None).ConfigureAwait(false);
                    return;
                }

                if (result.MessageType != WebSocketMessageType.Text) continue;

                object? input;
                try
                {
                    input = JsonSerializer.Deserialize<object>(messageBuffer.ToArray());
                }
                catch (Exception ex)
                {
                    if (logger is not null) WebSocketBehaviorBindingLogs.MalformedJsonFrame(logger, ex, behaviorId);
                    var errBytes = Encoding.UTF8.GetBytes(
                        JsonSerializer.Serialize(new { error = "Invalid JSON frame" }));
                    await ws.SendAsync(errBytes, WebSocketMessageType.Text, endOfMessage: true, ctx.RequestAborted)
                        .ConfigureAwait(false);
                    continue;
                }

                if (input is null) continue;

                try
                {
                    var context = DefaultBehaviorContext.From(ctx, behaviorId);
                    var dispatchResult = await dispatcher.DispatchAsync(behaviorId, input, context, ctx.RequestAborted)
                        .ConfigureAwait(false);

                    var responseJson = JsonSerializer.Serialize(dispatchResult);
                    var responseBytes = Encoding.UTF8.GetBytes(responseJson);
                    await ws.SendAsync(responseBytes, WebSocketMessageType.Text, endOfMessage: true, ctx.RequestAborted)
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    var errBytes = Encoding.UTF8.GetBytes(
                        JsonSerializer.Serialize(new { error = ex.Message }));
                    await ws.SendAsync(errBytes, WebSocketMessageType.Text, endOfMessage: true, CancellationToken.None)
                        .ConfigureAwait(false);
                }
            }

            if (ws.State == WebSocketState.Open)
            {
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None)
                    .ConfigureAwait(false);
            }
        }
        finally
        {
            // G-WS-05: always return the rented buffer
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}

internal static partial class WebSocketBehaviorBindingLogs
{
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Unexpected error receiving WebSocket message for behavior '{BehaviorId}', aborting connection.")]
    public static partial void UnexpectedReceiveError(ILogger logger, Exception exception, string behaviorId);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Skipping malformed JSON frame on WebSocket for behavior '{BehaviorId}'.")]
    public static partial void MalformedJsonFrame(ILogger logger, Exception exception, string behaviorId);
}
