using System.Text;
using System.Text.Json;
using Cephalon.Abstractions.Behaviors;
using Cephalon.Behaviors.Http.Abstractions;
using Cephalon.Behaviors.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Cephalon.Behaviors.Http.Bindings;

/// <summary>
/// GraphQL over Server-Sent Events (SSE) transport binding (transport ID: <c>http.graphql-sse</c>).
/// Accepts <c>POST /behaviors/{id}/graphql/sse</c> with a standard GraphQL body.
/// The <c>variables</c> object is used as the behavior input, then the result is
/// streamed as SSE events before sending a <c>complete</c> event.
/// </summary>
public sealed class GraphqlSseBehaviorBinding : IHttpBehaviorBinding
{
    /// <inheritdoc />
    public string TransportId => "http.graphql-sse";

    /// <inheritdoc />
    public Task MapAsync(
        WebApplication app,
        BehaviorTopologyDescriptor descriptor,
        BehaviorDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(dispatcher);

        var route = $"/behaviors/{descriptor.Id}/graphql/sse";

        app.MapPost(route, async (HttpContext ctx, [FromBody] JsonElement body) =>
        {
            // Extract the variables object as the behavior input.
            object input = body.TryGetProperty("variables", out var variables)
                    && variables.ValueKind == JsonValueKind.Object
                ? JsonSerializer.Deserialize<object>(variables.GetRawText())!
                : JsonSerializer.Deserialize<object>("{}")!;

            var context = DefaultBehaviorContext.From(ctx, descriptor.Id);

            ctx.Response.ContentType = "text/event-stream";
            ctx.Response.Headers.CacheControl = "no-cache";
            ctx.Response.Headers.Connection = "keep-alive";
            ctx.Response.Headers["X-Accel-Buffering"] = "no"; // nginx compat

            try
            {
                var result = await dispatcher.DispatchAsync(descriptor.Id, input, context, ctx.RequestAborted)
                    .ConfigureAwait(false);

                var json = JsonSerializer.Serialize(new { data = result });
                var nextEvent = $"event: next\ndata: {json}\n\n";
                await ctx.Response.WriteAsync(nextEvent, Encoding.UTF8, ctx.RequestAborted).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var errorJson = JsonSerializer.Serialize(new
                {
                    errors = new[] { new { message = ex.Message } }
                });
                var errorEvent = $"event: next\ndata: {errorJson}\n\n";
                await ctx.Response.WriteAsync(errorEvent, Encoding.UTF8, ctx.RequestAborted).ConfigureAwait(false);
            }
            finally
            {
                await ctx.Response.WriteAsync("event: complete\n\n", Encoding.UTF8, CancellationToken.None)
                    .ConfigureAwait(false);
                await ctx.Response.Body.FlushAsync(CancellationToken.None).ConfigureAwait(false);
            }
        });

        return Task.CompletedTask;
    }
}
