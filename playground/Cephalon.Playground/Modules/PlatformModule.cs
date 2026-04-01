using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Modules;
using Cephalon.AspNetCore.Modules;
using Cephalon.Playground.Services;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Playground.Modules;

public sealed class PlatformModule : ModuleBase, IEndpointModule
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "platform",
        displayName: "Platform",
        description: "Foundation runtime services for time and operational posture.",
        tags: ["foundation", "runtime"],
        version: "1.0.0",
        metadata: new Dictionary<string, string>
        {
            ["layer"] = "foundation",
            ["surface"] = "runtime"
        });

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IEngineClock, SystemEngineClock>();
    }

    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
        capabilities.Add(new Capability(
            key: "platform.clock",
            displayName: "Platform Clock",
            description: "Provides consistent time access for engine modules.",
            metadata: new Dictionary<string, string>
            {
                ["category"] = "foundation",
                ["surface"] = "service"
            }));
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/platform");
        group.MapGet("/time", (IEngineClock clock) =>
            TypedResults.Ok(new PlatformTimeEnvelope(clock.GetUtcNow(), "utc")));
    }
}

/// <summary>
/// Platform time payload exposed by the REST transport.
/// </summary>
public sealed record PlatformTimeEnvelope(DateTimeOffset UtcNow, string Kind);
