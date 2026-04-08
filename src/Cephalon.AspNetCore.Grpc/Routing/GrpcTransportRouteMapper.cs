using Cephalon.AspNetCore.Grpc.Modules;
using Cephalon.AspNetCore.Hosting;
using Cephalon.Engine.Runtime;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace Cephalon.AspNetCore.Grpc.Routing;

internal sealed class GrpcTransportRouteMapper : ITransportRouteMapper
{
    private readonly ApiRoutesOptions options;

    public GrpcTransportRouteMapper(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        options = ApiRoutesOptions.FromConfiguration(configuration);
    }

    public string TransportId => "grpc";

    public void MapRoutes(WebApplication app, IRuntime runtime)
    {
        var grpcGroup = app.MapGroup(options.GrpcPrefix);
        foreach (var module in runtime.Modules.OfType<IGrpcModule>())
        {
            module.MapGrpcEndpoints(grpcGroup);
        }
    }
}
