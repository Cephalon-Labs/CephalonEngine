using Cephalon.AspNetCore.GraphQL.Modules;
using Cephalon.AspNetCore.Hosting;
using Cephalon.Engine.Runtime;
using HotChocolate.AspNetCore;
using Microsoft.AspNetCore.Builder;

namespace Cephalon.AspNetCore.GraphQL.Routing;

internal sealed class GraphQLTransportRouteMapper : ITransportRouteMapper
{
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
        app.MapGraphQL("/graphql")
            .WithDisplayName("Cephalon GraphQL");
    }
}
