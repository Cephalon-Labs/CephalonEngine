using Cephalon.AspNetCore.Hosting;
using Cephalon.Engine.Runtime;
using Microsoft.AspNetCore.Builder;

namespace Cephalon.AspNetCore.Transports.Rest;

internal sealed class RestTransportRouteMapper : ITransportRouteMapper
{
    public string TransportId => "rest-api";

    public void MapRoutes(WebApplication app, IRuntime runtime)
    {
        var apiGroup = app.MapGroup("/api");
        foreach (var module in runtime.Modules.OfType<IRestModule>())
        {
            module.MapRestEndpoints(apiGroup);
        }
    }
}
