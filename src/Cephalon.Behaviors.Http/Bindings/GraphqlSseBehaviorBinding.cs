using System.Text;
using System.Text.Json;
using Cephalon.Abstractions.Behaviors;
using Cephalon.AspNetCore.Hosting;
using Cephalon.Behaviors.Http.Abstractions;
using Cephalon.Behaviors.Http.Hosting;
using Cephalon.Behaviors.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Cephalon.Behaviors.Http.Bindings;

/// <summary>
/// GraphQL over Server-Sent Events (SSE) transport binding (transport ID: <c>http.graphql-sse</c>).
/// Accepts canonical routes such as <c>POST /graphql-sse/v1/cart/get</c>.
/// The <c>variables</c> object is used as the behavior input, then the result is streamed as SSE
/// events before sending a <c>complete</c> event.
/// </summary>
public sealed class GraphqlSseBehaviorBinding : IHttpBehaviorBinding
{
    private readonly BehaviorApiSurfaceRouteResolver routeResolver;

    /// <summary>
    /// Initializes a new <see cref="GraphqlSseBehaviorBinding" />.
    /// </summary>
    /// <param name="configuration">
    /// Optional configuration used to resolve canonical behavior transport routes.
    /// When omitted, the binding falls back to the default <c>/graphql-sse/v1</c> route policy.
    /// </param>
    public GraphqlSseBehaviorBinding(IConfiguration? configuration = null)
    {
        routeResolver = new BehaviorApiSurfaceRouteResolver(configuration is null
            ? new ApiRoutesOptions()
            : ApiRoutesOptions.FromConfiguration(configuration));
    }

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

        foreach (var route in routeResolver.ResolveRoutes(TransportId, descriptor))
        {
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
            }).ExcludeFromDescription();
        }

        return Task.CompletedTask;
    }
}
