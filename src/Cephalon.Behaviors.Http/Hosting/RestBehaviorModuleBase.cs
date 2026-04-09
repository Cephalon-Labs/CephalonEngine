using Cephalon.AspNetCore.Transports.Rest;
using Cephalon.Behaviors.Modules;
using Microsoft.AspNetCore.Routing;

namespace Cephalon.Behaviors.Http.Hosting;

/// <summary>
/// Base class for modules that own behaviors and expose a public REST surface for some of them.
/// </summary>
/// <remarks>
/// REST mapping remains in the ASP.NET Core adapter layer, while behavior ownership stays
/// host-agnostic through <see cref="BehaviorModuleBase" />.
/// </remarks>
public abstract class RestBehaviorModuleBase : BehaviorModuleBase, IRestModule
{
    /// <summary>
    /// Maps the module-owned REST endpoints onto the supplied route builder.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder that receives the module routes.</param>
    public abstract void MapEndpoints(IEndpointRouteBuilder endpoints);

    /// <inheritdoc />
    void IRestModule.MapRestEndpoints(IEndpointRouteBuilder endpoints)
    {
        MapEndpoints(endpoints);
    }
}
