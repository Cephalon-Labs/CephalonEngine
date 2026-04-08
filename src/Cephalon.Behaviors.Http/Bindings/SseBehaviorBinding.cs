using System.Text;
using System.Text.Json;
using Cephalon.Abstractions.Behaviors;
using Cephalon.AspNetCore.Hosting;
using Cephalon.Behaviors.Http.Abstractions;
using Cephalon.Behaviors.Http.Hosting;
using Cephalon.Behaviors.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Cephalon.Behaviors.Http.Bindings;

/// <summary>
/// Server-Sent Events transport binding (transport ID: <c>http.sse</c>).
/// Opens a long-lived SSE stream at canonical routes such as
/// <c>GET /sse/v1/cart/get</c>, while optionally keeping the legacy
/// <c>/behaviors/{id}/events</c> alias enabled for compatibility.
/// Query-string parameters are parsed into a JSON object and deserialized as the
/// behavior's typed input. The behavior is dispatched immediately; its return value
/// is streamed as a <c>data: {json}\n\n</c> event. The connection stays alive with
/// periodic heartbeat comments until the client disconnects.
/// </summary>
/// <remarks>
/// Canonical routes are derived from the shared <see cref="BehaviorApiSurfaceDescriptor" /> plus
/// the configured SSE prefix (canonically <c>ApiRoutes:Prefixes:Sse</c>) and the resolved default
/// behavior document name. GraphQL-over-SSE participates in the same shared API-surface model
/// through its own dedicated prefix rather than reusing the generic SSE endpoint.
/// </remarks>
public sealed class SseBehaviorBinding : IHttpBehaviorBinding
{
    private readonly BehaviorApiSurfaceRouteResolver routeResolver;

    /// <summary>
    /// Initializes a new <see cref="SseBehaviorBinding" />.
    /// </summary>
    /// <param name="configuration">
    /// Optional configuration used to resolve canonical behavior transport routes.
    /// When omitted, the binding falls back to the default <c>/sse/v1</c> route policy.
    /// </param>
    public SseBehaviorBinding(IConfiguration? configuration = null)
    {
        routeResolver = new BehaviorApiSurfaceRouteResolver(configuration is null
            ? new ApiRoutesOptions()
            : ApiRoutesOptions.FromConfiguration(configuration));
    }

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

        foreach (var route in routeResolver.ResolveRoutes(TransportId, descriptor))
        {
            app.MapGet(route, async (HttpContext ctx) =>
            {
                // G-SSE-01/02/03: required SSE headers
                ctx.Response.ContentType = "text/event-stream";
                ctx.Response.Headers.CacheControl = "no-cache";
                ctx.Response.Headers.Connection = "keep-alive";
                ctx.Response.Headers["X-Accel-Buffering"] = "no"; // nginx compat

                var input = ParseQueryAsJsonElement(ctx.Request.Query);
                var context = DefaultBehaviorContext.From(ctx, descriptor.Id);

                try
                {
                    var result = await dispatcher.DispatchAsync(descriptor.Id, input, context, ctx.RequestAborted)
                        .ConfigureAwait(false);

                    if (result is not null)
                    {
                        // G-SSE-06: double newline per SSE spec
                        var json = JsonSerializer.Serialize(result);
                        var sseEvent = $"event: result\ndata: {json}\n\n";
                        await ctx.Response.WriteAsync(sseEvent, Encoding.UTF8, ctx.RequestAborted)
                            .ConfigureAwait(false);
                        // G-SSE-04: flush after every event
                        await ctx.Response.Body.FlushAsync(ctx.RequestAborted).ConfigureAwait(false);
                    }

                    // G-SSE-07: keep connection alive with heartbeat comments
                    // until client disconnects, allowing EventSource to stay open
                    while (!ctx.RequestAborted.IsCancellationRequested)
                    {
                        await Task.Delay(15_000, ctx.RequestAborted).ConfigureAwait(false);
                        await ctx.Response.WriteAsync(": heartbeat\n\n", Encoding.UTF8, ctx.RequestAborted)
                            .ConfigureAwait(false);
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
                    var errorEvent = $"event: error\ndata: {errorJson}\n\n";
                    await ctx.Response.WriteAsync(errorEvent, Encoding.UTF8, CancellationToken.None)
                        .ConfigureAwait(false);
                    await ctx.Response.Body.FlushAsync(CancellationToken.None).ConfigureAwait(false);
                }
            });
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Converts query-string parameters into a <see cref="JsonElement" /> object that the
    /// <see cref="BehaviorExecutionSlot" /> can deserialize into the behavior's typed input.
    /// </summary>
    private static JsonElement ParseQueryAsJsonElement(IQueryCollection query)
    {
        if (query.Count == 0)
        {
            // Return an empty JSON object — behaviors with no required input will accept this.
            return JsonSerializer.Deserialize<JsonElement>("{}");
        }

        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        writer.WriteStartObject();

        foreach (var pair in query)
        {
            var values = pair.Value;
            if (values.Count == 1)
            {
                writer.WritePropertyName(pair.Key);
                WriteJsonValue(writer, values[0]!);
            }
            else if (values.Count > 1)
            {
                writer.WriteStartArray(pair.Key);
                foreach (var v in values)
                {
                    WriteJsonValue(writer, v!);
                }
                writer.WriteEndArray();
            }
        }

        writer.WriteEndObject();
        writer.Flush();

        return JsonSerializer.Deserialize<JsonElement>(stream.ToArray());
    }

    /// <summary>
    /// Writes a single query-string value, coercing booleans and numbers where possible.
    /// </summary>
    private static void WriteJsonValue(Utf8JsonWriter writer, string value)
    {
        if (bool.TryParse(value, out var boolVal))
        {
            writer.WriteBooleanValue(boolVal);
        }
        else if (long.TryParse(value, out var longVal))
        {
            writer.WriteNumberValue(longVal);
        }
        else if (double.TryParse(value, out var doubleVal) &&
                 !double.IsNaN(doubleVal) && !double.IsInfinity(doubleVal))
        {
            writer.WriteNumberValue(doubleVal);
        }
        else
        {
            writer.WriteStringValue(value);
        }
    }
}
