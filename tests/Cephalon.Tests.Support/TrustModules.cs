using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Modules;
using Microsoft.AspNetCore.Builder;
using Cephalon.AspNetCore.Modules;
using Cephalon.AspNetCore.Transports.Rest;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Cephalon.Tests.Support;

internal sealed class RestrictedCapabilityModule : ModuleBase, IEndpointModule
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "restricted",
        displayName: "Restricted",
        description: "Test module that gates an endpoint with capability policy.",
        tags: ["security", "rest"]);

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
        capabilities.Add(new Capability(
            key: "restricted.secret",
            displayName: "Restricted secret",
            description: "Secret capability protected by trust policy."));
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/restricted");
        group.MapGet("/secret", () => TypedResults.Ok(new { Message = "classified" }))
            .RequireCapability("restricted.secret");
    }
}
