using Cephalon.Abstractions.Modules;
using Microsoft.AspNetCore.Routing;

namespace Cephalon.AspNetCore.Transports.ServerSentEvents;

/// <summary>
/// Defines Server-Sent Events endpoint contributions made by a Cephalon module on ASP.NET Core.
/// </summary>
public interface IServerSentEventsModule : IModule
{
    /// <summary>
    /// Maps the module's Server-Sent Events endpoints onto the supplied endpoint route builder.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder that receives the module routes.</param>
    void MapServerSentEvents(IEndpointRouteBuilder endpoints);
}
