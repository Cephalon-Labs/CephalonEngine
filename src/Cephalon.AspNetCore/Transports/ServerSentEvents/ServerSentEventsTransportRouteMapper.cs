using Cephalon.AspNetCore.Hosting;
using Cephalon.AspNetCore.Transports.Streaming;
using Cephalon.Engine.Runtime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Cephalon.AspNetCore.Transports.ServerSentEvents;

internal sealed class ServerSentEventsTransportRouteMapper : ITransportRouteMapper
{
    private readonly ApiRoutesOptions options;

    public ServerSentEventsTransportRouteMapper(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        options = ApiRoutesOptions.FromConfiguration(configuration);
    }

    public string TransportId => "server-sent-events";

    public void MapRoutes(WebApplication app, IRuntime runtime)
    {
        var eventsGroup = app.MapGroup(options.SsePrefix)
            .ApplyCephalonRateLimiting(app.Services, TransportId)
            .ApplyCephalonDirectStreamingModuleResilience(
                app.Services,
                TransportId,
                DirectStreamingModuleTransportKind.ServerSentEvents);
        eventsGroup.ExcludeFromDescription();
        foreach (var module in runtime.Modules.OfType<IServerSentEventsModule>())
        {
            module.MapServerSentEvents(eventsGroup);
        }
    }
}
