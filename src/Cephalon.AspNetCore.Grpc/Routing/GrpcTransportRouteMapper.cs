using Cephalon.AspNetCore.Grpc.Modules;
using Cephalon.AspNetCore.Hosting;
using Cephalon.Engine.Runtime;
using Microsoft.AspNetCore.Builder;

namespace Cephalon.AspNetCore.Grpc.Routing;

internal sealed class GrpcTransportRouteMapper : ITransportRouteMapper
{
    public string TransportId => "grpc";

    public void MapRoutes(WebApplication app, IRuntime runtime)
    {
        foreach (var module in runtime.Modules.OfType<IGrpcModule>())
        {
            module.MapGrpcEndpoints(app);
        }
    }
}
