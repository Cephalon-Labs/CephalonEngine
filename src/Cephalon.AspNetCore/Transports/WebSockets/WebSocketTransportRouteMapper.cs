using Cephalon.AspNetCore.Hosting;
using Cephalon.Engine.Runtime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Cephalon.AspNetCore.Transports.WebSockets;

internal sealed class WebSocketTransportRouteMapper : ITransportRouteMapper
{
    private readonly ApiRoutesOptions options;

    public WebSocketTransportRouteMapper(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        options = ApiRoutesOptions.FromConfiguration(configuration);
    }

    public string TransportId => "websocket";

    public void MapRoutes(WebApplication app, IRuntime runtime)
    {
        app.UseWebSockets();

        var webSocketGroup = app.MapGroup(options.WsPrefix)
            .ApplyCephalonRateLimiting(app.Services, TransportId);
        webSocketGroup.ExcludeFromDescription();
        foreach (var module in runtime.Modules.OfType<IWebSocketModule>())
        {
            module.MapWebSocketEndpoints(webSocketGroup);
        }
    }
}
