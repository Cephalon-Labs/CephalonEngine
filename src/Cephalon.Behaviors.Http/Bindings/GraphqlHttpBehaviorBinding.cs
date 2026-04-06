using System.Text.Json;
using Cephalon.Abstractions.Behaviors;
using Cephalon.Behaviors.Http.Abstractions;
using Cephalon.Behaviors.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Cephalon.Behaviors.Http.Bindings;

/// <summary>
/// GraphQL HTTP transport binding (transport ID: <c>http.graphql</c>).
/// Accepts <c>POST /behaviors/{id}/graphql</c> with a
/// <c>{"query":"...","variables":{...}}</c> payload and returns
/// <c>{"data":{...}}</c> or <c>{"errors":[...]}</c>.
/// </summary>
public sealed class GraphqlHttpBehaviorBinding : IHttpBehaviorBinding
{
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

        var route = $"/behaviors/{descriptor.Id}/graphql";

        app.MapPost(route, async (HttpContext ctx, [FromBody] JsonElement body) =>
        {
            string? query = null;
            JsonElement variables = default;

            if (body.TryGetProperty("query", out var q)) query = q.GetString();
            if (body.TryGetProperty("variables", out var v)) variables = v;

            var input = new GraphqlRequest(query ?? string.Empty, variables);
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
        });

        return Task.CompletedTask;
    }
}

/// <summary>
/// Represents a parsed GraphQL request containing the query string and optional variables.
/// </summary>
/// <param name="Query">The GraphQL query string.</param>
/// <param name="Variables">The optional variables document.</param>
public sealed record GraphqlRequest(string Query, JsonElement Variables);
