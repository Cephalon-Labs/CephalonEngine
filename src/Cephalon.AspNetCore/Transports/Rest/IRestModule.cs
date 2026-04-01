using Cephalon.Abstractions.Modules;
using Microsoft.AspNetCore.Routing;

namespace Cephalon.AspNetCore.Transports.Rest;

/// <summary>
/// Defines REST endpoint contributions made by a Cephalon module on ASP.NET Core.
/// </summary>
public interface IRestModule : IModule
{
    /// <summary>
    /// Maps the module's REST endpoints onto the supplied endpoint route builder.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder that receives the module routes.</param>
    void MapRestEndpoints(IEndpointRouteBuilder endpoints);
}
