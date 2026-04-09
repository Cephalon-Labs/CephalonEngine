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
/// HTTP REST transport binding (transport ID: <c>http.rest</c>).
/// Maps canonical behavior routes such as <c>POST /api/v1/cart/get</c> and
/// <c>GET /api/v1/cart/get</c> as the default per-behavior REST surface.
/// </summary>
/// <remarks>
/// Canonical routes are derived from the shared <see cref="BehaviorApiSurfaceDescriptor" /> plus
/// the configured REST prefix (canonically <c>ApiRoutes:Prefixes:Rest</c>) together with the
/// resolved default behavior document name. <see cref="ApiRoutesOptions.RestPrefix" /> controls the
/// configurable root prefix. Query-string parameters are parsed as JSON input for GET requests.
/// </remarks>
public sealed class RestHttpBehaviorBinding : IHttpBehaviorBinding
{
    private readonly BehaviorApiSurfaceRouteResolver routeResolver;

    /// <summary>
    /// Initializes a new <see cref="RestHttpBehaviorBinding" />.
    /// </summary>
    /// <param name="configuration">
    /// Optional configuration used to resolve canonical behavior transport routes.
    /// When omitted, the binding falls back to the default <c>/api/v1</c> route policy.
    /// </param>
    public RestHttpBehaviorBinding(IConfiguration? configuration = null)
    {
        routeResolver = new BehaviorApiSurfaceRouteResolver(configuration is null
            ? new ApiRoutesOptions()
            : ApiRoutesOptions.FromConfiguration(configuration));
    }

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

        var contract = BehaviorRestTransportContract.FromDescriptor(descriptor);

        foreach (var route in routeResolver.ResolveRoutes(TransportId, descriptor))
        {
            if (contract.HasExplicitHttpMethod)
            {
                MapExplicitContractEndpoint(app, route, descriptor, dispatcher, contract);
                continue;
            }

            app.MapPost(route, async (HttpContext ctx, [FromBody] JsonElement body) =>
            {
                return await DispatchBodyAsync(ctx, descriptor, dispatcher, body).ConfigureAwait(false);
            }).ExcludeFromDescription();

            app.MapGet(route, async (HttpContext ctx) =>
            {
                var input = (object)ParseQueryAsJsonElement(ctx.Request.Query);
                return await DispatchAsync(ctx, descriptor, dispatcher, input).ConfigureAwait(false);
            }).ExcludeFromDescription();
        }

        return Task.CompletedTask;
    }

    private static void MapExplicitContractEndpoint(
        WebApplication app,
        string route,
        BehaviorTopologyDescriptor descriptor,
        BehaviorDispatcher dispatcher,
        BehaviorRestTransportContract contract)
    {
        var acceptsBody = AcceptsBody(contract.HttpMethod!);
        app.MapMethods(route, [contract.HttpMethod!], async (HttpContext ctx) =>
        {
            object input;
            if (acceptsBody)
            {
                var body = await ComposeBehaviorBodyAsync(ctx, contract).ConfigureAwait(false);
                input = JsonSerializer.Deserialize<object>(body)!;
            }
            else
            {
                input = await ComposeBehaviorRequestAsync(ctx, contract).ConfigureAwait(false);
            }

            return await DispatchAsync(ctx, descriptor, dispatcher, input).ConfigureAwait(false);
        }).ExcludeFromDescription();
    }

    private static async Task<IResult> DispatchBodyAsync(
        HttpContext ctx,
        BehaviorTopologyDescriptor descriptor,
        BehaviorDispatcher dispatcher,
        JsonElement body)
    {
        var input = JsonSerializer.Deserialize<object>(body)!;
        return await DispatchAsync(ctx, descriptor, dispatcher, input).ConfigureAwait(false);
    }

    private static async Task<IResult> DispatchAsync(
        HttpContext ctx,
        BehaviorTopologyDescriptor descriptor,
        BehaviorDispatcher dispatcher,
        object input)
    {
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
    }

    private static async Task<object> ComposeBehaviorRequestAsync(
        HttpContext context,
        BehaviorRestTransportContract contract)
    {
        var payload = await BehaviorRequestJsonComposer
            .ComposeAsync<object>(context, acceptsBody: false, contract)
            .ConfigureAwait(false);
        return payload;
    }

    private static async Task<JsonElement> ComposeBehaviorBodyAsync(
        HttpContext context,
        BehaviorRestTransportContract contract)
    {
        return await BehaviorRequestJsonComposer
            .ComposeAsync<object>(context, acceptsBody: true, contract)
            .ConfigureAwait(false);
    }

    private static bool AcceptsBody(string httpMethod)
    {
        return !string.Equals(httpMethod, "GET", StringComparison.OrdinalIgnoreCase) &&
               !string.Equals(httpMethod, "DELETE", StringComparison.OrdinalIgnoreCase);
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
