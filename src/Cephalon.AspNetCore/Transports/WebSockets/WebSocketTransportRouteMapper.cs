using Cephalon.AspNetCore.Hosting;
using Cephalon.Engine.Runtime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Cephalon.AspNetCore.Transports.WebSockets;

internal sealed class WebSocketTransportRouteMapper : ITransportRouteMapper
{
    public string TransportId => "websocket";

    public void MapRoutes(WebApplication app, IRuntime runtime)
    {
        app.UseWebSockets();

        var webSocketGroup = app.MapGroup("/ws");
        webSocketGroup.ExcludeFromDescription();
        foreach (var module in runtime.Modules.OfType<IWebSocketModule>())
        {
            module.MapWebSocketEndpoints(webSocketGroup);
        }
    }
}
