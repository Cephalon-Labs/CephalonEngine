using Cephalon.AspNetCore.Hosting;
using Cephalon.AspNetCore.JsonRpc.Hosting;
using Cephalon.AspNetCore.JsonRpc.Modules;
using Cephalon.Engine.Runtime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Cephalon.AspNetCore.JsonRpc.Routing;

internal sealed class JsonRpcTransportRouteMapper : ITransportRouteMapper
{
    private readonly ApiRoutesOptions options;

    public JsonRpcTransportRouteMapper(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        options = ApiRoutesOptions.FromConfiguration(configuration);
    }

    public string TransportId => "json-rpc";

    public void MapRoutes(WebApplication app, IRuntime runtime)
    {
        var rpcGroup = app.MapGroup(options.JsonRpcPrefix)
            .ApplyCephalonRateLimiting(app.Services, TransportId);
        rpcGroup.AddEndpointFilter<JsonRpcDirectModuleResilienceFilter>();
        rpcGroup.ExcludeFromDescription();
        foreach (var module in runtime.Modules.OfType<IJsonRpcModule>())
        {
            module.MapJsonRpcEndpoints(rpcGroup);
        }
    }
}
