using System.Text.Json;
using Cephalon.Abstractions.Behaviors;
using Cephalon.Behaviors.Http.Abstractions;
using Cephalon.Behaviors.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Cephalon.Behaviors.Http.Bindings;

/// <summary>
/// HTTP REST transport binding (transport ID: <c>http.rest</c>).
/// Maps <c>POST /behaviors/{id}</c> (with JSON body) and
/// <c>GET /behaviors/{id}</c> (with optional query string) to the behavior dispatcher.
/// </summary>
public sealed class RestHttpBehaviorBinding : IHttpBehaviorBinding
{
    /// <inheritdoc />
    public string TransportId => "http.rest";

    /// <inheritdoc />
    public Task MapAsync(
        WebApplication app,
        BehaviorTopologyDescriptor descriptor,
        BehaviorDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(dispatcher);

        var route = $"/behaviors/{descriptor.Id}";

        app.MapPost(route, async (HttpContext ctx, [FromBody] JsonElement body) =>
        {
            var input = JsonSerializer.Deserialize<object>(body)!;
            var context = DefaultBehaviorContext.From(ctx, descriptor.Id);
            try
            {
                var result = await dispatcher.DispatchAsync(descriptor.Id, input, context, ctx.RequestAborted)
                    .ConfigureAwait(false);
                return result is null ? Results.NoContent() : Results.Json(result, contentType: "application/json");
            }
            catch (BehaviorNotFoundException)
            {
                return Results.NotFound();
            }
        });

        app.MapGet(route, async (HttpContext ctx, [FromQuery] string? q) =>
        {
            var input = (object?)q ?? new object();
            var context = DefaultBehaviorContext.From(ctx, descriptor.Id);
            try
            {
                var result = await dispatcher.DispatchAsync(descriptor.Id, input, context, ctx.RequestAborted)
                    .ConfigureAwait(false);
                return result is null ? Results.NoContent() : Results.Json(result, contentType: "application/json");
            }
            catch (BehaviorNotFoundException)
            {
                return Results.NotFound();
            }
        });

        return Task.CompletedTask;
    }
}
