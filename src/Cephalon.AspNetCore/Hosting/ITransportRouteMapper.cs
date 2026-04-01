using Cephalon.Engine.Runtime;
using Microsoft.AspNetCore.Builder;

namespace Cephalon.AspNetCore.Hosting;

/// <summary>
/// Maps the routes associated with one selected transport onto an ASP.NET Core host.
/// </summary>
public interface ITransportRouteMapper
{
    /// <summary>
    /// Gets the transport identifier that this mapper handles.
    /// </summary>
    string TransportId { get; }

    /// <summary>
    /// Maps the transport's routes onto the supplied application.
    /// </summary>
    /// <param name="app">The ASP.NET Core application to extend.</param>
    /// <param name="runtime">The runtime whose manifest and services back the mapped routes.</param>
    void MapRoutes(WebApplication app, IRuntime runtime);
}
