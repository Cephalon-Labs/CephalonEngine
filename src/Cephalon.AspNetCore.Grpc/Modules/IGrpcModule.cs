using Cephalon.Abstractions.Modules;
using Microsoft.AspNetCore.Routing;

namespace Cephalon.AspNetCore.Grpc.Modules;

/// <summary>
/// Defines gRPC endpoint contributions made by a Cephalon module on ASP.NET Core.
/// </summary>
public interface IGrpcModule : IModule
{
    /// <summary>
    /// Maps the module's gRPC endpoints onto the supplied endpoint route builder.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder that receives the module routes.</param>
    void MapGrpcEndpoints(IEndpointRouteBuilder endpoints);
}
