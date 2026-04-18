using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Modules;
using Cephalon.Behaviors.Http.Abstractions;
using Cephalon.Behaviors.Http.Hosting;
using CephalonTemplateApp.Contracts;
using CephalonTemplateApp.Modules.Customers.Features.Welcome.Application;
using Microsoft.Extensions.DependencyInjection;

namespace CephalonTemplateApp.Modules.Customers.Features.Welcome.Api;

public sealed class CustomersModule : RestBehaviorModuleBase
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "customers",
        displayName: "Customers",
        description: "Customer-facing service module with explicit contracts.",
        tags: ["generated", "microservice"],
        version: "1.0.0");

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<WelcomeApplicationService>();
    }

    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
        capabilities.Add(new Capability(
            key: "customers.welcome",
            displayName: "Customer welcome",
            description: "Exposes a service-boundary welcome contract."));
    }

    public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
    {
        behaviors.Group("/customers")
            .WithTagName("Customers API")
            .MapProfile<GetWelcomeBehavior>();
    }
}

[AppBehavior("customers.welcome.get")]
[BehaviorAllowedPatterns("direct")]
[BehaviorRestProfile(BehaviorRestMethod.Get, "/welcome/{name?}", ApiVersionMajor = 1)]
internal sealed class GetWelcomeBehavior : IAppBehavior<GetWelcomeInput, Result<WelcomeContract>>
{
    private readonly WelcomeApplicationService service;

    public GetWelcomeBehavior(WelcomeApplicationService service)
    {
        this.service = service;
    }

    public Task<Result<WelcomeContract>> HandleAsync(
        GetWelcomeInput input,
        IBehaviorContext context,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Ok(
            service.Build(input.Name, input.Tenant),
            message: "Customer welcome resolved."));
    }

    public static void ConfigureTopology(IBehaviorTopologyBuilder builder)
    {
        builder.AsDirect();
    }
}

internal sealed record GetWelcomeInput(string? Name = null, string? Tenant = null);
