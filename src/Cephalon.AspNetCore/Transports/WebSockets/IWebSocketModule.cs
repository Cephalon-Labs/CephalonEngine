using Cephalon.Abstractions.Modules;
using Microsoft.AspNetCore.Routing;

namespace Cephalon.AspNetCore.Transports.WebSockets;

/// <summary>
/// Defines WebSocket endpoint contributions made by a Cephalon module on ASP.NET Core.
/// </summary>
public interface IWebSocketModule : IModule
{
    /// <summary>
    /// Maps the module's WebSocket endpoints onto the supplied endpoint route builder.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder that receives the module routes.</param>
    void MapWebSocketEndpoints(IEndpointRouteBuilder endpoints);
}
