using Cephalon.Abstractions.Modules;
using Cephalon.AspNetCore.Transports.Rest;
using Microsoft.AspNetCore.Routing;

namespace Cephalon.Behaviors.Http.Hosting;

internal static class RestBehaviorProjectionMaterializer
{
    internal static void MapModule(
        IEndpointRouteBuilder endpoints,
        IModule module,
        RestBehaviorModuleProjection projection)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(module);
        ArgumentNullException.ThrowIfNull(projection);

        foreach (var groupProjection in projection.Groups)
        {
            MapGroup(endpoints, module, groupProjection);
        }
    }

    internal static void MapGroup(
        IEndpointRouteBuilder endpoints,
        IModule module,
        RestBehaviorRouteGroupProjection projection)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(module);
        ArgumentNullException.ThrowIfNull(projection);

        var group = endpoints.MapBehaviorRestGroup(module, projection.Prefix);
        group.UseRuntimeSourceKind(RestEndpointRuntimeMetadata.ModuleDslSourceKind);
        group.UseRuntimeAuthoringStyle(RestEndpointRuntimeMetadata.BehaviorModuleDslAuthoringStyle);
        if (!string.IsNullOrWhiteSpace(projection.TagName))
        {
            group.WithTagName(projection.TagName);
        }

        if (projection.HasExplicitTagDescription)
        {
            group.WithTagDescription(projection.TagDescription);
        }

        if (projection.ApiVersionMajor.HasValue)
        {
            group.ApiVersion(projection.ApiVersionMajor.Value);
        }

        foreach (var convention in projection.GroupConventions)
        {
            convention(group.Routes);
        }

        foreach (var endpointProjection in projection.Endpoints)
        {
            endpointProjection.Apply(group);
        }
    }
}
