using Cephalon.Abstractions.Modules;
using Microsoft.AspNetCore.Routing;

namespace Cephalon.AspNetCore.JsonRpc.Modules;

/// <summary>
/// Defines JSON-RPC endpoint contributions made by a Cephalon module on ASP.NET Core.
/// </summary>
public interface IJsonRpcModule : IModule
{
    /// <summary>
    /// Maps the module's JSON-RPC endpoints onto the supplied endpoint route builder.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder that receives the module routes.</param>
    void MapJsonRpcEndpoints(IEndpointRouteBuilder endpoints);
}
