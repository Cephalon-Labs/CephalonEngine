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
/// HTTP REST transport binding (transport ID: <c>http.rest</c>).
/// Maps <c>POST /behaviors/{id}</c> (with JSON body) and
/// <c>GET /behaviors/{id}</c> (with query-string parameters parsed as JSON input)
/// to the behavior dispatcher.
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

        app.MapGet(route, async (HttpContext ctx) =>
        {
            var input = (object)ParseQueryAsJsonElement(ctx.Request.Query);
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

    /// <summary>
    /// Converts query-string parameters into a <see cref="JsonElement" /> that the
    /// <see cref="BehaviorExecutionSlot" /> can deserialize into the behavior's typed input.
    /// </summary>
    private static JsonElement ParseQueryAsJsonElement(IQueryCollection query)
    {
        if (query.Count == 0)
        {
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
