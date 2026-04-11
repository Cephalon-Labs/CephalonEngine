using Cephalon.AspNetCore.GraphQL.Modules;
using Cephalon.AspNetCore.Hosting;
using Cephalon.Engine.Runtime;
using HotChocolate.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;

namespace Cephalon.AspNetCore.GraphQL.Routing;

internal sealed class GraphQLTransportRouteMapper : ITransportRouteMapper
{
    private readonly ApiRoutesOptions options;

    public GraphQLTransportRouteMapper(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        options = ApiRoutesOptions.FromConfiguration(configuration);
    }

    public string TransportId => "graphql";

    public void MapRoutes(WebApplication app, IRuntime runtime)
    {
        var graphQlModules = runtime.Modules.OfType<IGraphQLModule>().ToArray();
        if (graphQlModules.Length == 0)
        {
            throw new InvalidOperationException(
                "Transport 'graphql' was selected, but no Cephalon modules implemented IGraphQLModule.");
        }

        app.UseWebSockets();

        MapGraphQlHttpEndpoint(
            app,
            options.GraphQLPrefix,
            TransportId,
            "Cephalon GraphQL");
        MapGraphQlSchemaEndpoint(
            app,
            ResolveSchemaRoute(options.GraphQLPrefix),
            TransportId,
            "Cephalon GraphQL Schema");

        if (!string.Equals(options.GraphQLSsePrefix, options.GraphQLPrefix, StringComparison.OrdinalIgnoreCase))
        {
            MapGraphQlHttpEndpoint(
                app,
                options.GraphQLSsePrefix,
                "graphql-sse",
                "Cephalon GraphQL SSE");
        }

        MapGraphQlWebSocketEndpoint(
            app,
            options.GraphQLWsPrefix,
            "graphql-ws",
            "Cephalon GraphQL WebSocket");
    }

    private static void MapGraphQlHttpEndpoint(
        IEndpointRouteBuilder endpoints,
        string routePattern,
        string transportId,
        string displayName)
    {
        endpoints.MapGraphQLHttp(routePattern)
            .WithDisplayName(displayName)
            .ApplyCephalonRateLimiting(endpoints.ServiceProvider, transportId);
    }

    private static void MapGraphQlWebSocketEndpoint(
        IEndpointRouteBuilder endpoints,
        string routePattern,
        string transportId,
        string displayName)
    {
        endpoints.MapGraphQLWebSocket(routePattern)
            .WithDisplayName(displayName)
            .ApplyCephalonRateLimiting(endpoints.ServiceProvider, transportId);
    }

    private static void MapGraphQlSchemaEndpoint(
        IEndpointRouteBuilder endpoints,
        string routePattern,
        string transportId,
        string displayName)
    {
        endpoints.MapGraphQLSchema(routePattern)
            .WithDisplayName(displayName)
            .ApplyCephalonRateLimiting(endpoints.ServiceProvider, transportId);
    }

    private static string ResolveSchemaRoute(string routePattern)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(routePattern);

        return $"{routePattern.TrimEnd('/')}/schema";
    }
}
