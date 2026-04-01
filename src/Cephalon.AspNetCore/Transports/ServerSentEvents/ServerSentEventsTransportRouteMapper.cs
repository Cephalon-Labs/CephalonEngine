using Cephalon.AspNetCore.Hosting;
using Cephalon.Engine.Runtime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Cephalon.AspNetCore.Transports.ServerSentEvents;

internal sealed class ServerSentEventsTransportRouteMapper : ITransportRouteMapper
{
    public string TransportId => "server-sent-events";

    public void MapRoutes(WebApplication app, IRuntime runtime)
    {
        var eventsGroup = app.MapGroup("/events");
        eventsGroup.ExcludeFromDescription();
        foreach (var module in runtime.Modules.OfType<IServerSentEventsModule>())
        {
            module.MapServerSentEvents(eventsGroup);
        }
    }
}
