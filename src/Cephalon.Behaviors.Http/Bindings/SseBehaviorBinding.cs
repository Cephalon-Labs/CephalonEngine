using System.Text;
using System.Text.Json;
using Cephalon.Abstractions.Behaviors;
using Cephalon.Behaviors.Http.Abstractions;
using Cephalon.Behaviors.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Cephalon.Behaviors.Http.Bindings;

/// <summary>
/// Server-Sent Events transport binding (transport ID: <c>http.sse</c>).
/// Opens a long-lived SSE stream at <c>GET /behaviors/{id}/events</c>.
/// The behavior is dispatched immediately; its return value is streamed as
/// a single <c>data: {json}\n\n</c> event, then the connection is closed.
/// </summary>
public sealed class SseBehaviorBinding : IHttpBehaviorBinding
{
    /// <inheritdoc />
    public string TransportId => "http.sse";

    /// <inheritdoc />
    public Task MapAsync(
        WebApplication app,
        BehaviorTopologyDescriptor descriptor,
        BehaviorDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(dispatcher);

        var route = $"/behaviors/{descriptor.Id}/events";

        app.MapGet(route, async (HttpContext ctx) =>
        {
            // G-SSE-01/02/03: required SSE headers
            ctx.Response.ContentType = "text/event-stream";
            ctx.Response.Headers.CacheControl = "no-cache";
            ctx.Response.Headers.Connection = "keep-alive";
            ctx.Response.Headers["X-Accel-Buffering"] = "no"; // nginx compat

            var input = new object();
            var context = DefaultBehaviorContext.From(ctx, descriptor.Id);

            try
            {
                var result = await dispatcher.DispatchAsync(descriptor.Id, input, context, ctx.RequestAborted)
                    .ConfigureAwait(false);

                if (result is not null)
                {
                    // G-SSE-06: double newline per SSE spec
                    var json = JsonSerializer.Serialize(result);
                    var sseEvent = $"data: {json}\n\n";
                    await ctx.Response.WriteAsync(sseEvent, Encoding.UTF8, ctx.RequestAborted)
                        .ConfigureAwait(false);
                    // G-SSE-04: flush after every event
                    await ctx.Response.Body.FlushAsync(ctx.RequestAborted).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // G-SSE-05: client disconnected — exit gracefully, no error event
            }
            catch (Exception ex)
            {
                var errorJson = JsonSerializer.Serialize(new { error = ex.Message });
                var errorEvent = $"data: {errorJson}\n\n";
                await ctx.Response.WriteAsync(errorEvent, Encoding.UTF8, CancellationToken.None)
                    .ConfigureAwait(false);
                await ctx.Response.Body.FlushAsync(CancellationToken.None).ConfigureAwait(false);
            }
        });

        return Task.CompletedTask;
    }
}
