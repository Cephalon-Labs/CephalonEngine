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
/// GraphQL HTTP transport binding (transport ID: <c>http.graphql</c>). Accepts canonical routes
/// such as <c>POST /graphql/v1/cart/get</c>. The request body uses a standard GraphQL envelope and
/// the <c>variables</c> object is dispatched as the behavior input.
/// </summary>
public sealed class GraphqlHttpBehaviorBinding : IHttpBehaviorBinding
{
    private readonly BehaviorApiSurfaceRouteResolver routeResolver;

    /// <summary>
    /// Initializes a new <see cref="GraphqlHttpBehaviorBinding" />.
    /// </summary>
    /// <param name="configuration">
    /// Optional configuration used to resolve canonical behavior transport routes.
    /// When omitted, the binding falls back to the default <c>/graphql/v1</c> route policy.
    /// </param>
    public GraphqlHttpBehaviorBinding(IConfiguration? configuration = null)
    {
        routeResolver = new BehaviorApiSurfaceRouteResolver(configuration is null
            ? new ApiRoutesOptions()
            : ApiRoutesOptions.FromConfiguration(configuration));
    }

    /// <inheritdoc />
    public string TransportId => "http.graphql";

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
                // This maps the GraphQL variables to the behavior's typed input model.
                object input = body.TryGetProperty("variables", out var variables)
                        && variables.ValueKind == JsonValueKind.Object
                    ? JsonSerializer.Deserialize<object>(variables.GetRawText())!
                    : JsonSerializer.Deserialize<object>("{}")!;

                var context = DefaultBehaviorContext.From(ctx, descriptor.Id);

                try
                {
                    var result = await dispatcher.DispatchAsync(descriptor.Id, input, context, ctx.RequestAborted)
                        .ConfigureAwait(false);
                    return Results.Json(new { data = result });
                }
                catch (Exception ex)
                {
                    return Results.Json(new
                    {
                        errors = new[] { new { message = ex.Message } }
                    });
                }
            })
            .ApplyCephalonRateLimiting(app.Services, TransportId, descriptor.Id)
            .ExcludeFromDescription();
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// Represents a parsed GraphQL request containing the query string and optional variables.
/// </summary>
/// <param name="Query">The GraphQL query string.</param>
/// <param name="Variables">The optional variables document.</param>
public sealed record GraphqlRequest(string Query, JsonElement Variables);
