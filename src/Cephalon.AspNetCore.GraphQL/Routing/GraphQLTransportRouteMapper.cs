using Cephalon.AspNetCore.GraphQL.Modules;
using Cephalon.AspNetCore.Hosting;
using Cephalon.Engine.Runtime;
using HotChocolate.AspNetCore;
using Microsoft.AspNetCore.Builder;
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
        app.MapGraphQL(options.GraphQLPrefix)
            .WithDisplayName("Cephalon GraphQL");
    }
}
