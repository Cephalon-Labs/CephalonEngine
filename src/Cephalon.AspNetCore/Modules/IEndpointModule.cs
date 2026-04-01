using Cephalon.Abstractions.Modules;
using Cephalon.AspNetCore.Transports.Rest;
using Microsoft.AspNetCore.Routing;

namespace Cephalon.AspNetCore.Modules;

/// <summary>
/// Legacy REST module contract kept for compatibility with earlier Cephalon hosts.
/// </summary>
/// <remarks>
/// New module code should generally implement <see cref="IRestModule" /> directly.
/// </remarks>
public interface IEndpointModule : IRestModule
{
    /// <summary>
    /// Maps the module's REST endpoints onto the supplied endpoint route builder.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder that receives the module routes.</param>
    void MapEndpoints(IEndpointRouteBuilder endpoints);

    void IRestModule.MapRestEndpoints(IEndpointRouteBuilder endpoints)
    {
        MapEndpoints(endpoints);
    }
}
