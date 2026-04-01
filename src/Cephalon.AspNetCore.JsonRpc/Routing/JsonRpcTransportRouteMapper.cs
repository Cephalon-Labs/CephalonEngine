using Cephalon.AspNetCore.Hosting;
using Cephalon.AspNetCore.JsonRpc.Modules;
using Cephalon.Engine.Runtime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Cephalon.AspNetCore.JsonRpc.Routing;

internal sealed class JsonRpcTransportRouteMapper : ITransportRouteMapper
{
    public string TransportId => "json-rpc";

    public void MapRoutes(WebApplication app, IRuntime runtime)
    {
        var rpcGroup = app.MapGroup("/rpc");
        rpcGroup.ExcludeFromDescription();
        foreach (var module in runtime.Modules.OfType<IJsonRpcModule>())
        {
            module.MapJsonRpcEndpoints(rpcGroup);
        }
    }
}
